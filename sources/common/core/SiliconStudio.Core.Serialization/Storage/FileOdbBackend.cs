﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization.Assets;

namespace SiliconStudio.Core.Storage
{
    /// <summary>
    /// Object Database Backend (ODB) implementation using <see cref="VirtualFileSystem"/>
    /// </summary>
    public class FileOdbBackend : IOdbBackend
    {
        private static readonly object LockOnMove = new object();
        private const int WriteBufferSize = 1024;
        private bool isReadOnly;

        // Resolve provider once at initialization
        private readonly IVirtualFileProvider virtualFileProvider;
        private readonly string vfsRootUrl;
        private readonly string vfsTempUrl;
        private AssetIndexMap assetIndexMap;

        /// <inheritdoc/>
        public IAssetIndexMap AssetIndexMap
        {
            get { return assetIndexMap; }
        }

        public FileOdbBackend(string vfsRootUrl, bool isReadOnly, string indexName = "index")
        {
            var resolveProviderResult = VirtualFileSystem.ResolveProvider(vfsRootUrl, true);
            virtualFileProvider = resolveProviderResult.Provider;
            this.vfsRootUrl = resolveProviderResult.Path;
            vfsTempUrl = this.vfsRootUrl + "/tmp/";

            // Ensure directories exists
            if (!virtualFileProvider.DirectoryExists(this.vfsRootUrl))
                virtualFileProvider.CreateDirectory(this.vfsRootUrl);

            this.isReadOnly = isReadOnly;

            assetIndexMap = Serialization.Assets.AssetIndexMap.Load(vfsRootUrl + VirtualFileSystem.DirectorySeparatorChar + indexName, isReadOnly);
            if (!isReadOnly && !virtualFileProvider.DirectoryExists(vfsTempUrl))
            {
                try
                {
                    virtualFileProvider.CreateDirectory(vfsTempUrl);
                }
                catch (Exception)
                {
                    this.isReadOnly = true;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return isReadOnly; }
        }

        /// <inheritdoc/>
        public virtual Stream OpenStream(ObjectId objectId, VirtualFileMode mode = VirtualFileMode.Open, VirtualFileAccess access = VirtualFileAccess.Read, VirtualFileShare share = VirtualFileShare.Read)
        {
            var url = BuildUrl(vfsRootUrl, objectId);

            // Try to early exit if file does not exists while opening, so that it doesn't
            // throw a file not found exception for default logic.
            if (!virtualFileProvider.FileExists(url))
            {
                if (mode == VirtualFileMode.Open || mode == VirtualFileMode.Truncate)
                    throw new FileNotFoundException();

                // Otherwise, file creation is allowed, so make sure directory exists
                virtualFileProvider.CreateDirectory(ExtractPath(url));

            }

            return virtualFileProvider.OpenStream(url, mode, access, share);
        }

        /// <inheritdoc/>
        public virtual int GetSize(ObjectId objectId)
        {
            var url = BuildUrl(vfsRootUrl, objectId);
            using (var file = virtualFileProvider.OpenStream(url, VirtualFileMode.Open, VirtualFileAccess.Read))
            {
                return checked((int)file.Length);
            }
        }

        /// <inheritdoc/>
        public virtual bool Exists(ObjectId objectId)
        {
            var url = BuildUrl(vfsRootUrl, objectId);
            return virtualFileProvider.FileExists(url);
        }

        /// <inheritdoc/>
        public virtual ObjectId Write(ObjectId objectId, Stream dataStream, int length, bool forceWrite = false)
        {
            if (objectId == ObjectId.Empty)
            {
                // This should be avoided
                using (var digestStream = new DigestStream(Stream.Null))
                {
                    dataStream.CopyTo(digestStream);
                    objectId = digestStream.CurrentHash;
                }

                dataStream.Seek(0, SeekOrigin.Begin);
            }

            var url = BuildUrl(vfsRootUrl, objectId);

            if (!forceWrite && virtualFileProvider.FileExists(url))
                return objectId;

            virtualFileProvider.CreateDirectory(ExtractPath(url));
            using (var file = virtualFileProvider.OpenStream(url, VirtualFileMode.Create, VirtualFileAccess.Write))
            {
                // TODO: Fast case for NativeStream. However we still need a file implementation of NativeStream.
                var buffer = new byte[WriteBufferSize];
                for (int offset = 0; offset < length; offset += WriteBufferSize)
                {
                    int blockSize = length - offset;
                    if (blockSize > WriteBufferSize)
                        blockSize = WriteBufferSize;

                    dataStream.Read(buffer, 0, blockSize);
                    file.Write(buffer, 0, blockSize);
                }
            }

            return objectId;
        }

        /// <inheritdoc/>
        public OdbStreamWriter CreateStream()
        {
            if (isReadOnly)
                throw new InvalidOperationException("Read-only backend.");

            string tmpFileName = vfsTempUrl + Guid.NewGuid() + ".tmp";
            Stream stream = virtualFileProvider.OpenStream(tmpFileName, VirtualFileMode.Create, VirtualFileAccess.Write);
            return new DigestStream(stream, tmpFileName) { Disposed = x => SaveStream(x) };
        }

        /// <inheritdoc/>
        private ObjectId SaveStream(OdbStreamWriter stream)
        {
            ObjectId objId = stream.CurrentHash;
            string fileUrl = BuildUrl(vfsRootUrl, objId);

            string temporaryFilePath = stream.TemporaryName;

            lock (LockOnMove)
            {
                // File may already exists, in this case we decide to not override it.
                if (!virtualFileProvider.FileExists(fileUrl))
                {
                    try
                    {
                        // Remove the second part of ObjectId to get the path (cf BuildUrl)
                        virtualFileProvider.CreateDirectory(fileUrl.Substring(0, fileUrl.Length - (ObjectId.HashStringLength - 2)));
                        virtualFileProvider.FileMove(temporaryFilePath, BuildUrl(vfsRootUrl, objId));
                    }
                    catch (IOException e)
                    {
                        // Ignore only IOException "The destination file already exists."
                        // because other exceptions that we want to catch might inherit from IOException.
                        // This happens if two FileMove were performed at the same time.
                        if (e.GetType() != typeof(IOException))
                            throw;

                        // But we should still clean our temporary file
                        virtualFileProvider.FileDelete(temporaryFilePath);
                    }
                }
                else
                {
                    // But we should still clean our temporary file
                    virtualFileProvider.FileDelete(temporaryFilePath);
                }
            }

            return objId;
        }

        /// <inheritdoc/>
        public void Delete(ObjectId objectId)
        {
            var url = BuildUrl(vfsRootUrl, objectId);
            virtualFileProvider.FileDelete(url);
        }

        /// <inheritdoc/>
        public IEnumerable<ObjectId> EnumerateObjects()
        {
            foreach (var file in virtualFileProvider.ListFiles(vfsRootUrl, "*", VirtualSearchOption.AllDirectories))
            {
                if (file.Length >= 2 + ObjectId.HashStringLength)
                {
                    if (file[file.Length - ObjectId.HashStringLength - 2] != VirtualFileSystem.DirectorySeparatorChar
                        || file[file.Length - ObjectId.HashStringLength + 1] != VirtualFileSystem.DirectorySeparatorChar)
                        continue;

                    var objectIdString = new char[ObjectId.HashStringLength];
                    var filePosition = file.Length - ObjectId.HashStringLength - 1;
                    for (int i = 0; i < ObjectId.HashStringLength; ++i)
                    {
                        // Skip /
                        if (i == 2)
                            filePosition++;
                        objectIdString[i] = file[filePosition++];
                    }

                    ObjectId objectId;
                    if (ObjectId.TryParse(new string(objectIdString), out objectId))
                        yield return objectId;
                }
            }
        }

        public string GetFilePath(ObjectId objectId)
        {
            return virtualFileProvider.GetAbsolutePath(BuildUrl(vfsRootUrl, objectId));
        }

        private static string ExtractPath(string url)
        {
            return url.Substring(0, url.LastIndexOf('/'));
        }

        public static string BuildUrl(string vfsRootUrl, ObjectId objectId)
        {
            var id = objectId.ToString();
            var result = new StringBuilder(vfsRootUrl.Length + 2 + ObjectId.HashStringLength);
            result.Append(vfsRootUrl);
            result.Append('/');
            result.Append(id[0]);
            result.Append(id[1]);
            result.Append('/');
            result.Append(id, 2, ObjectId.HashStringLength - 2);

            return result.ToString();
        }
    }
}