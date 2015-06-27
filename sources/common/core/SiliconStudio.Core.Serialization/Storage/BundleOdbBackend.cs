﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.LZ4;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.Serialization.Serializers;

namespace SiliconStudio.Core.Storage
{
    /// <summary>
    /// Object Database Backend (ODB) implementation that bundles multiple chunks into a .bundle files, optionally compressed with LZ4.
    /// </summary>
    [DataSerializerGlobal(null, typeof(List<string>))]
    [DataSerializerGlobal(null, typeof(List<KeyValuePair<ObjectId, BundleOdbBackend.ObjectInfo>>))]
    [DataSerializerGlobal(null, typeof(List<KeyValuePair<string, ObjectId>>))]
    public class BundleOdbBackend : IOdbBackend
    {
        /// <summary>
        /// The bundle file extension.
        /// </summary>
        public const string BundleExtension = ".bundle";

        /// <summary>
        /// The default directory where bundle are stored.
        /// </summary>
        private readonly string vfsBundleDirectory;

        private readonly Dictionary<ObjectId, ObjectLocation> objects = new Dictionary<ObjectId, ObjectLocation>();

        // Stream pool to avoid reopening same file multiple time
        private readonly Dictionary<string, Stream> bundleStreams = new Dictionary<string, Stream>();

        // Bundle name => Bundle VFS URL
        private readonly Dictionary<string, string> resolvedBundles = new Dictionary<string, string>();

        private readonly List<LoadedBundle> loadedBundles = new List<LoadedBundle>(); 

        private readonly ObjectDatabaseAssetIndexMap assetIndexMap = new ObjectDatabaseAssetIndexMap();

        public delegate Task<string> BundleResolveDelegate(string bundleName);

        /// <summary>
        /// Bundle resolve event asynchronous handler.
        /// </summary>
        public BundleResolveDelegate BundleResolve { get; set; }

        /// <inheritdoc/>
        public IAssetIndexMap AssetIndexMap
        {
            get { return assetIndexMap; }
        }

        public string BundleDirectory { get { return vfsBundleDirectory; } }

        public BundleOdbBackend(string vfsRootUrl)
        {
            vfsBundleDirectory = vfsRootUrl + "/bundles/";

            if (!VirtualFileSystem.DirectoryExists(vfsBundleDirectory))
                VirtualFileSystem.CreateDirectory(vfsBundleDirectory);

            BundleResolve += DefaultBundleResolve;
        }

        public Dictionary<ObjectId, ObjectInfo> GetObjectInfos()
        {
            lock (objects)
            {
                return objects.ToDictionary(pair => pair.Key, value => value.Value.Info);
            }
        }

        private Task<string> DefaultBundleResolve(string bundleName)
        {
            // Try to find [bundleName].bundle
            var bundleFile = VirtualFileSystem.Combine(vfsBundleDirectory, bundleName + BundleExtension);
            if (VirtualFileSystem.FileExists(bundleFile))
                return Task.FromResult(bundleFile);

            return Task.FromResult<string>(null);
        }

        private async Task<string> ResolveBundle(string bundleName, bool throwExceptionIfNotFound)
        {
            string bundleUrl;

            lock (resolvedBundles)
            {
                if (resolvedBundles.TryGetValue(bundleName, out bundleUrl))
                {
                    if (bundleUrl == null)
                        throw new InvalidOperationException(string.Format("Bundle {0} is being loaded twice (either cyclic dependency or concurrency issue)", bundleName));
                    return bundleUrl;
                }

                // Store null until resolved (to detect cyclic dependencies)
                resolvedBundles[bundleName] = null;
            }

            if (BundleResolve != null)
            {
                // Iterate over each handler and find the first one that returns non-null result
                foreach (BundleResolveDelegate bundleResolvedHandler in BundleResolve.GetInvocationList())
                {
                    // Use handler to resolve package
                    bundleUrl = await bundleResolvedHandler(bundleName);
                    if (bundleUrl != null)
                        break;
                }
            }

            // Check if it has been properly resolved
            if (bundleUrl == null)
            {
                // Remove from resolved bundles
                lock (resolvedBundles)
                {
                    resolvedBundles.Remove(bundleName);
                }

                if (!throwExceptionIfNotFound)
                    return null;

                throw new FileNotFoundException(string.Format("Bundle {0} could not be resolved", bundleName));
            }

            // Register resolved package
            lock (resolvedBundles)
            {
                resolvedBundles[bundleName] = bundleUrl;
            }

            return bundleUrl;
        }

        /// <summary>
        /// Loads the specified bundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle.</param>
        /// <param name="objectDatabaseAssetIndexMap">The object database asset index map, where newly loaded assets will be merged (ignored if null).</param>
        /// <returns></returns>
        public async Task LoadBundle(string bundleName, ObjectDatabaseAssetIndexMap objectDatabaseAssetIndexMap)
        {
            if (bundleName == null) throw new ArgumentNullException("bundleName");

            // Check loaded bundles
            lock (loadedBundles)
            {
                foreach (var currentBundle in loadedBundles)
                {
                    if (currentBundle.BundleName == bundleName)
                    {
                        currentBundle.ReferenceCount++;
                        return;
                    }
                }
            }

            // Resolve package
            var vfsUrl = await ResolveBundle(bundleName, true);

            await LoadBundleFromUrl(bundleName, objectDatabaseAssetIndexMap, vfsUrl);
        }

        public async Task LoadBundleFromUrl(string bundleName, ObjectDatabaseAssetIndexMap objectDatabaseAssetIndexMap, string bundleUrl, bool ignoreDependencies = false)
        {
            BundleDescription bundle;

            using (var packStream = VirtualFileSystem.OpenStream(bundleUrl, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                bundle = ReadBundleDescription(packStream);
            }

            // Read and resolve dependencies
            if (!ignoreDependencies)
            {
                foreach (var dependency in bundle.Dependencies)
                {
                    await LoadBundle(dependency, objectDatabaseAssetIndexMap);
                }
            }

            lock (loadedBundles)
            {
                LoadedBundle loadedBundle = null;

                foreach (var currentBundle in loadedBundles)
                {
                    if (currentBundle.BundleName == bundleName)
                    {
                        loadedBundle = currentBundle;
                        break;
                    }
                }

                if (loadedBundle == null)
                {
                    loadedBundle = new LoadedBundle
                    {
                        BundleName = bundleName,
                        BundleUrl = bundleUrl,
                        Description = bundle,
                        ReferenceCount = 1
                    };

                    loadedBundles.Add(loadedBundle);
                }
                else
                {
                    loadedBundle.ReferenceCount++;
                }
            }

            // Read objects
            lock (objects)
            {
                foreach (var objectEntry in bundle.Objects)
                {
                    objects[objectEntry.Key] = new ObjectLocation { Info = objectEntry.Value, BundleUrl = bundleUrl };
                }
            }

            // Merge with local (asset bundles) index map
            assetIndexMap.Merge(bundle.Assets);

            // Merge with global object database map
            objectDatabaseAssetIndexMap.Merge(bundle.Assets);
        }

        /// <summary>
        /// Unload the specified bundle.
        /// </summary>
        /// <param name="bundleName">Name of the bundle.</param>
        /// <param name="objectDatabaseAssetIndexMap">The object database asset index map, where newly loaded assets will be merged (ignored if null).</param>
        /// <returns></returns>
        public void UnloadBundle(string bundleName, ObjectDatabaseAssetIndexMap objectDatabaseAssetIndexMap)
        {
            lock (loadedBundles)
            lock (objects)
            {
                // Unload package
                UnloadBundleRecursive(bundleName, objectDatabaseAssetIndexMap);

                // Remerge previously loaded packages
                foreach (var otherLoadedBundle in loadedBundles)
                {
                    var bundle = otherLoadedBundle.Description;

                    // Read objects
                    foreach (var objectEntry in bundle.Objects)
                    {
                        objects[objectEntry.Key] = new ObjectLocation { Info = objectEntry.Value, BundleUrl = otherLoadedBundle.BundleUrl };
                    }

                    assetIndexMap.Merge(bundle.Assets);
                    objectDatabaseAssetIndexMap.Merge(bundle.Assets);
                }
            }
        }

        private void UnloadBundleRecursive(string bundleName, ObjectDatabaseAssetIndexMap objectDatabaseAssetIndexMap)
        {
            if (bundleName == null) throw new ArgumentNullException("bundleName");

            lock (loadedBundles)
            {
                int loadedBundleIndex = -1;

                for (int index = 0; index < loadedBundles.Count; index++)
                {
                    var currentBundle = loadedBundles[index];
                    if (currentBundle.BundleName == bundleName)
                    {
                        loadedBundleIndex = index;
                        break;
                    }
                }

                if (loadedBundleIndex == -1)
                    throw new InvalidOperationException("Bundle has not been loaded.");

                var loadedBundle = loadedBundles[loadedBundleIndex];
                var bundle = loadedBundle.Description;
                if (--loadedBundle.ReferenceCount == 0)
                {
                    // Remove and dispose stream from pool
                    lock (bundleStreams)
                    {
                        Stream stream;
                        if (bundleStreams.TryGetValue(loadedBundle.BundleUrl, out stream))
                        {
                            bundleStreams.Remove(loadedBundle.BundleUrl);
                            stream.Dispose();
                        }
                    }

                    // Actually unload bundle
                    loadedBundles.RemoveAt(loadedBundleIndex);

                    // Unload objects from index map (if possible, replace with objects of other bundles
                    var removedObjects = new HashSet<ObjectId>();
                    foreach (var objectEntry in bundle.Objects)
                    {
                        objects.Remove(objectEntry.Key);
                        removedObjects.Add(objectEntry.Key);
                    }

                    // Unmerge with local (asset bundles) index map
                    assetIndexMap.Unmerge(bundle.Assets);

                    // Unmerge with global object database map
                    objectDatabaseAssetIndexMap.Unmerge(bundle.Assets);

                    // Remove dependencies too
                    foreach (var dependency in bundle.Dependencies)
                    {
                        UnloadBundleRecursive(dependency, objectDatabaseAssetIndexMap);
                    }
                }
            }
        }

        /// <summary>
        /// Reads the bundle description.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <returns></returns>
        /// <exception cref="System.InvalidOperationException">
        /// Invalid bundle header
        /// or
        /// Bundle has not been properly written
        /// </exception>
        public static BundleDescription ReadBundleDescription(Stream stream)
        {
            var binaryReader = new BinarySerializationReader(stream);

            // Read header
            var header = binaryReader.Read<Header>();

            var result = new BundleDescription();
            result.Header = header;

            // Check magic header
            if (header.MagicHeader != Header.MagicHeaderValid)
            {
                throw new InvalidOperationException("Invalid bundle header");
            }

            // Ensure size has properly been set
            if (header.Size != stream.Length)
            {
                throw new InvalidOperationException("Bundle has not been properly written");
            }

            // Read dependencies
            List<string> dependencies = result.Dependencies;
            binaryReader.Serialize(ref dependencies, ArchiveMode.Deserialize);
                
            // Read objects
            List<KeyValuePair<ObjectId, ObjectInfo>> objects = result.Objects;
            binaryReader.Serialize(ref objects, ArchiveMode.Deserialize);

            // Read assets
            List<KeyValuePair<string, ObjectId>> assets = result.Assets;
            binaryReader.Serialize(ref assets, ArchiveMode.Deserialize);

            return result;
        }

        public static void CreateBundle(string vfsUrl, IOdbBackend backend, ObjectId[] objectIds, ISet<ObjectId> disableCompressionIds, Dictionary<string, ObjectId> indexMap, IList<string> dependencies)
        {
            if (objectIds.Length == 0)
                throw new InvalidOperationException("Nothing to pack.");

            // Early exit if package didn't change (header-check only)
            if (VirtualFileSystem.FileExists(vfsUrl))
            {
                try
                {
                    using (var packStream = VirtualFileSystem.OpenStream(vfsUrl, VirtualFileMode.Open, VirtualFileAccess.Read))
                    {
                        var bundle = ReadBundleDescription(packStream);

                        // If package didn't change since last time, early exit!
                        if (ArrayExtensions.ArraysEqual(bundle.Dependencies, dependencies)
                            && ArrayExtensions.ArraysEqual(bundle.Assets.OrderBy(x => x.Key).ToList(), indexMap.OrderBy(x => x.Key).ToList())
                            && ArrayExtensions.ArraysEqual(bundle.Objects.Select(x => x.Key).OrderBy(x => x).ToList(), objectIds.OrderBy(x => x).ToList()))
                        {
                            return;
                        }
                    }
                }
                catch (Exception)
                {
                    // Could not read previous bundle (format changed?)
                    // Let's just mute this error as new bundle will overwrite it anyway
                }
            }

            using (var packStream = VirtualFileSystem.OpenStream(vfsUrl, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                var header = new Header();
                header.MagicHeader = Header.MagicHeaderValid;

                var binaryWriter = new BinarySerializationWriter(packStream);
                binaryWriter.Write(header);

                // Write dependencies
                binaryWriter.Write(dependencies.ToList());

                // Save location of object ids
                var objectIdPosition = packStream.Position;

                // Write empty object ids (reserve space, will be rewritten later)
                var objects = new List<KeyValuePair<ObjectId, ObjectInfo>>();
                for (int i = 0; i < objectIds.Length; ++i)
                {
                    objects.Add(new KeyValuePair<ObjectId, ObjectInfo>(objectIds[i], new ObjectInfo()));
                }

                binaryWriter.Write(objects);
                objects.Clear();

                // Write index
                binaryWriter.Write(indexMap.ToList());

                for (int i = 0; i < objectIds.Length; ++i)
                {
                    using (var objectStream = backend.OpenStream(objectIds[i]))
                    {
                        // Prepare object info
                        var objectInfo = new ObjectInfo { StartOffset = packStream.Position, SizeNotCompressed = objectStream.Length };

                        // re-order the file content so that it is not necessary to seek while reading the input stream (header/object/refs -> header/refs/object)
                        var inputStream = objectStream;
                        var originalStreamLength = objectStream.Length;
                        var streamReader = new BinarySerializationReader(inputStream);
                        var chunkHeader = ChunkHeader.Read(streamReader);
                        if (chunkHeader != null)
                        {
                            // create the reordered stream
                            var reorderedStream = new MemoryStream((int)originalStreamLength);

                            // copy the header
                            var streamWriter = new BinarySerializationWriter(reorderedStream);
                            chunkHeader.Write(streamWriter);

                            // copy the references
                            var newOffsetReferences = reorderedStream.Position;
                            inputStream.Position = chunkHeader.OffsetToReferences;
                            inputStream.CopyTo(reorderedStream);

                            // copy the object
                            var newOffsetObject = reorderedStream.Position;
                            inputStream.Position = chunkHeader.OffsetToObject;
                            inputStream.CopyTo(reorderedStream, chunkHeader.OffsetToReferences - chunkHeader.OffsetToObject);

                            // rewrite the chunk header with correct offsets
                            chunkHeader.OffsetToObject = (int)newOffsetObject;
                            chunkHeader.OffsetToReferences = (int)newOffsetReferences;
                            reorderedStream.Position = 0;
                            chunkHeader.Write(streamWriter);

                            // change the input stream to use reordered stream
                            inputStream = reorderedStream;
                            inputStream.Position = 0;
                        }
 
                        // compress the stream
                        if (!disableCompressionIds.Contains(objectIds[i]))
                        {
                            objectInfo.IsCompressed = true;

                            var lz4OutputStream = new LZ4Stream(packStream, CompressionMode.Compress);
                            inputStream.CopyTo(lz4OutputStream);
                            lz4OutputStream.Flush();
                        }
                        else // copy the stream "as is"
                        {
                            // Write stream
                            inputStream.CopyTo(packStream);
                        }

                        // release the reordered created stream
                        if (chunkHeader != null)
                            inputStream.Dispose();

                        // Add updated object info
                        objectInfo.EndOffset = packStream.Position;
                        objects.Add(new KeyValuePair<ObjectId, ObjectInfo>(objectIds[i], objectInfo));
                    }
                }

                // Rewrite header
                header.Size = packStream.Length;
                packStream.Position = 0;
                binaryWriter.Write(header);

                // Rewrite object locations
                packStream.Position = objectIdPosition;
                binaryWriter.Write(objects);
            }
        }

        public Stream OpenStream(ObjectId objectId, VirtualFileMode mode = VirtualFileMode.Open, VirtualFileAccess access = VirtualFileAccess.Read, VirtualFileShare share = VirtualFileShare.Read)
        {
            ObjectLocation objectLocation;
            lock (objects)
            {
                if (!objects.TryGetValue(objectId, out objectLocation))
                    throw new FileNotFoundException();
            }

            Stream stream;

            // Try to reuse same streams
            lock (bundleStreams)
            {
                // Available stream?
                if (bundleStreams.TryGetValue(objectLocation.BundleUrl, out stream))
                {
                    // Remove from available streams
                    bundleStreams.Remove(objectLocation.BundleUrl);
                }
                else
                {
                    stream = VirtualFileSystem.OpenStream(objectLocation.BundleUrl, VirtualFileMode.Open, VirtualFileAccess.Read);
                }
            }

            if (objectLocation.Info.IsCompressed)
            {
                stream.Position = objectLocation.Info.StartOffset;
                return new PackageFileStreamLZ4(this, objectLocation.BundleUrl, stream, CompressionMode.Decompress, objectLocation.Info.SizeNotCompressed, objectLocation.Info.EndOffset - objectLocation.Info.StartOffset);
            }

            return new PackageFileStream(this, objectLocation.BundleUrl, stream, objectLocation.Info.StartOffset, objectLocation.Info.EndOffset, false);
        }

        public int GetSize(ObjectId objectId)
        {
            lock (objects)
            {
                var objectInfo = objects[objectId].Info;
                return (int)(objectInfo.EndOffset - objectInfo.StartOffset);
            }
        }

        public ObjectId Write(ObjectId objectId, Stream dataStream, int length, bool forceWrite)
        {
            throw new NotSupportedException();
        }

        public OdbStreamWriter CreateStream()
        {
            throw new NotSupportedException();
        }

        public bool Exists(ObjectId objectId)
        {
            lock (objects)
            {
                return objects.ContainsKey(objectId);
            }
        }

        public IEnumerable<ObjectId> EnumerateObjects()
        {
            lock (objects)
            {
                return objects.Select(x => x.Key).ToList();
            }
        }

        public void Delete(ObjectId objectId)
        {
            throw new NotSupportedException();
        }

        public string GetFilePath(ObjectId objectId)
        {
            throw new NotSupportedException();
        }

        private struct ObjectLocation
        {
            public ObjectInfo Info;
            public string BundleUrl;
        }

        private class LoadedBundle
        {
            public string BundleName;
            public string BundleUrl;
            public int ReferenceCount;
            public BundleDescription Description;
        }

        internal void ReleasePackageStream(string packageLocation, Stream stream)
        {
            lock (bundleStreams)
            {
                if (!bundleStreams.ContainsKey(packageLocation))
                {
                    bundleStreams.Add(packageLocation, stream);
                }
                else
                {
                    stream.Dispose();
                }
            }
        }

        [DataContract]
        [DataSerializer(typeof(Serializer))]
        public struct ObjectInfo
        {
            public long StartOffset;
            public long EndOffset;
            public long SizeNotCompressed;
            public bool IsCompressed;

            internal class Serializer : DataSerializer<ObjectInfo>
            {
                public override void Serialize(ref ObjectInfo obj, ArchiveMode mode, SerializationStream stream)
                {
                    stream.Serialize(ref obj.StartOffset);
                    stream.Serialize(ref obj.EndOffset);
                    stream.Serialize(ref obj.SizeNotCompressed);
                    stream.Serialize(ref obj.IsCompressed);
                }
            }
        }

        [DataContract]
        [DataSerializer(typeof(Header.Serializer))]
        public struct Header
        {
            public const uint MagicHeaderValid = 0x42584450; // "PDXB"

            public uint MagicHeader;
            public long Size;
            public uint Crc; // currently unused

            internal class Serializer : DataSerializer<Header>
            {
                public override void Serialize(ref Header obj, ArchiveMode mode, SerializationStream stream)
                {
                    stream.Serialize(ref obj.MagicHeader);
                    stream.Serialize(ref obj.Size);
                    stream.Serialize(ref obj.Crc);
                }
            }
        }
        public class PackageFileStreamLZ4 : LZ4Stream
        {
            private readonly BundleOdbBackend bundleOdbBackend;
            private readonly string packageLocation;
            private readonly Stream innerStream;

            public PackageFileStreamLZ4(BundleOdbBackend bundleOdbBackend, string packageLocation, Stream innerStream, CompressionMode compressionMode, long uncompressedStreamSize, long compressedSize)
                : base(innerStream, compressionMode, uncompressedSize: uncompressedStreamSize, compressedSize: compressedSize, disposeInnerStream: false)
            {
                this.bundleOdbBackend = bundleOdbBackend;
                this.packageLocation = packageLocation;
                this.innerStream = innerStream;
            }

            protected override void Dispose(bool disposing)
            {
                bundleOdbBackend.ReleasePackageStream(packageLocation, innerStream);

                base.Dispose(disposing);
            }
        }

        public class PackageFileStream : VirtualFileStream
        {
            private readonly BundleOdbBackend bundleOdbBackend;
            private readonly string packageLocation;

            public PackageFileStream(BundleOdbBackend bundleOdbBackend, string packageLocation, Stream internalStream, long startPosition = 0, long endPosition = -1, bool disposeInternalStream = true, bool seekToBeginning = true)
                : base(internalStream, startPosition, endPosition, disposeInternalStream, seekToBeginning)
            {
                this.bundleOdbBackend = bundleOdbBackend;
                this.packageLocation = packageLocation;
            }

            protected override void Dispose(bool disposing)
            {
                bundleOdbBackend.ReleasePackageStream(packageLocation, virtualFileStream ?? InternalStream);

                // If there was a VirtualFileStream, we don't want it to be released as it has been pushed back in the stream pool
                virtualFileStream = null;

                base.Dispose(disposing);
            }
        }

        public void DeleteBundles(Func<string, bool> bundleFileDeletePredicate)
        {
            var bundleFiles = VirtualFileSystem.ListFiles(vfsBundleDirectory, "*.bundle", VirtualSearchOption.TopDirectoryOnly).Result;

            // Obsolete: Android used to have .bundle.mp3 to avoid compression. Still here so that they get deleted on next build.
            // This can be removed later.
            bundleFiles = bundleFiles.Union(VirtualFileSystem.ListFiles(vfsBundleDirectory, "*.mp3", VirtualSearchOption.TopDirectoryOnly).Result).ToArray();

            foreach (var bundleFile in bundleFiles)
            {
                var bundleRealFile = VirtualFileSystem.GetAbsolutePath(bundleFile);

                // Remove ".mp3" (Android only)
                if (bundleRealFile.EndsWith(".mp3", StringComparison.CurrentCultureIgnoreCase))
                    bundleRealFile = bundleRealFile.Substring(0, bundleRealFile.Length - 4);

                if (bundleFileDeletePredicate(bundleRealFile))
                {
                    NativeFile.FileDelete(bundleRealFile);
                }
            }
        }
    }
}