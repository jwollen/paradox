﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;

using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Paradox.Shaders.Parser.Mixins
{
    /// <summary>
    /// Class ShaderSourceManager
    /// </summary>
    public class ShaderSourceManager
    {
        private readonly object locker = new object();
        private readonly Dictionary<string, ShaderSourceWithHash> loadedShaderSources = new Dictionary<string, ShaderSourceWithHash>();
        private readonly Dictionary<string, string> classNameToPath = new Dictionary<string, string>();

        /// <summary>
        /// The file provider used to load shader sources.
        /// </summary>
        private readonly IVirtualFileProvider fileProvider;

        private const string DefaultEffectFileExtension = ".pdxsl";

        /// <summary>
        /// Gets the directory list.
        /// </summary>
        /// <value>The directory list.</value>
        public List<string> LookupDirectoryList { get; private set; }

        /// <summary>
        /// Gets or sets the URL mapping to file path.
        /// </summary>
        /// <value>The URL automatic file path.</value>
        public Dictionary<string, string> UrlToFilePath { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether [use file system]. (Currently used only by tests, made static)
        /// </summary>
        /// <value><c>true</c> if [use file system]; otherwise, <c>false</c>.</value>
        public bool UseFileSystem { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderSourceManager" /> class.
        /// </summary>
        /// <param name="fileProvider">The file provider to use for loading shader sources.</param>
        public ShaderSourceManager(IVirtualFileProvider fileProvider)
        {
            this.fileProvider = fileProvider;
            LookupDirectoryList = new List<string>();
            UrlToFilePath = new Dictionary<string, string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderSourceManager"/> class.
        /// </summary>
        public ShaderSourceManager()
            : this(AssetManager.FileProvider)
        {
        }

        /// <summary>
        /// Adds the shader source registered manually.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="sourceCode">The source code.</param>
        /// <param name="sourcePath">The source path.</param>
        public void AddShaderSource(string type, string sourceCode, string sourcePath)
        {
            lock (locker)
            {
                var shaderSource = new ShaderSourceWithHash() { Source = sourceCode, Path = sourcePath };
                shaderSource.Hash = ObjectId.FromBytes(Encoding.UTF8.GetBytes(shaderSource.Source));
                loadedShaderSources[type] = shaderSource;
                classNameToPath[type] = sourcePath;
            }
        }


        /// <summary>
        /// Deletes the shader cache for the specified shaders.
        /// </summary>
        /// <param name="modifiedShaders">The modified shaders.</param>
        public void DeleteObsoleteCache(HashSet<string> modifiedShaders)
        {
            lock (locker)
            {
                foreach (var shaderName in modifiedShaders)
                {
                    loadedShaderSources.Remove(shaderName);
                    classNameToPath.Remove(shaderName);
                }
            }
        }

        public ObjectId GetShaderSourceHash(string type)
        {
            return LoadShaderSource(type).Hash;
        }

        public static ShaderSourceWithHash CreateShaderSourceWithHash(string type, string source)
        {
            return new ShaderSourceWithHash()
            {
                Path = type,
                Source = source,
                Hash = CalculateHashFromSource(source)
            };
        }

        /// <summary>
        /// Loads the shader source with the specified type name.
        /// </summary>
        /// <param name="type">The typeName.</param>
        /// <returns>ShaderSourceWithHash.</returns>
        /// <exception cref="System.IO.FileNotFoundException">If the file was not found</exception>
        public ShaderSourceWithHash LoadShaderSource(string type)
        {
            lock (locker)
            {
                // Load file
                ShaderSourceWithHash shaderSource;
                if (!loadedShaderSources.TryGetValue(type, out shaderSource))
                {
                    var sourceUrl = FindFilePath(type);
                    if (sourceUrl != null)
                    {
                        shaderSource = new ShaderSourceWithHash();
                        if (!UrlToFilePath.TryGetValue(sourceUrl, out shaderSource.Path))
                        {
                            shaderSource.Path = sourceUrl;
                        }

                        // On Windows, Always try to load first from the original URL in order to get the latest version
                        if (Platform.IsWindowsDesktop)
                        {
                            // TODO: the "/path" is hardcoded, used in ImportStreamCommand and EffectSystem. Find a place to share this correctly.
                            var pathUrl = sourceUrl + "/path";
                            if (FileExists(pathUrl))
                            {
                                using (var fileStream = OpenStream(pathUrl))
                                {
                                    string shaderSourcePath;
                                    using (var sr = new StreamReader(fileStream, Encoding.UTF8))
                                        shaderSourcePath = sr.ReadToEnd();

                                    if (File.Exists(shaderSourcePath))
                                    {
                                        // Replace path with a local path
                                        shaderSource.Path = Path.Combine(Environment.CurrentDirectory, shaderSourcePath);

                                        // Optimization: It currently reads the source file twice
                                        shaderSource.Hash = ObjectId.FromBytes(File.ReadAllBytes(shaderSourcePath));
                                        using (var sourceStream = File.Open(shaderSourcePath, FileMode.Open, FileAccess.Read, FileShare.Read))
                                        {
                                            using (var sr = new StreamReader(sourceStream))
                                                shaderSource.Source = sr.ReadToEnd();
                                        }
                                    }
                                }
                            }
                        }

                        if (shaderSource.Source == null)
                        {
                            using (var sourceStream = OpenStream(sourceUrl))
                            {
                                var databaseStream = sourceStream as IDatabaseStream;
                                var fileStream = sourceStream as FileStream;
                                if (databaseStream != null || fileStream != null)
                                {
                                    using (var sr = new StreamReader(sourceStream))
                                        shaderSource.Source = sr.ReadToEnd();

                                    if (databaseStream != null)
                                        shaderSource.Hash = databaseStream.ObjectId;
                                    else
                                        shaderSource.Hash = ObjectId.FromBytes(File.ReadAllBytes(sourceUrl));
                                }
                                else
                                {
                                    throw new Exception(string.Format("Unsupported Stream type to load shader [{0}.pdxsl]", type));
                                }
                            }
                        }

                        loadedShaderSources[type] = shaderSource;
                    }
                    else
                    {
                        throw new FileNotFoundException(string.Format("Unable to find shader [{0}]", type), string.Format("{0}.pdxsl", type));
                    }
                }
                return shaderSource;
            }
        }

        private static ObjectId CalculateHashFromSource(string source)
        {
            return ObjectId.FromBytes(Encoding.UTF8.GetBytes(source));
        }

        /// <summary>
        /// Determines whether a class with the specified type name exists.
        /// </summary>
        /// <param name="typeName">The typeName.</param>
        /// <returns><c>true</c> if a class with the specified type name exists; otherwise, <c>false</c>.</returns>
        public bool IsClassExists(string typeName)
        {
            return FindFilePath(typeName) != null;
        }
        
        public string FindFilePath(string type)
        {
            lock (locker)
            {
                if (LookupDirectoryList == null)
                    return null;

                string path = null;
                if (classNameToPath.TryGetValue(type, out path))
                    return path;

                foreach (var directory in LookupDirectoryList)
                {
                    var fileName = Path.ChangeExtension(type, DefaultEffectFileExtension);
                    var testPath = Path.Combine(directory, fileName).Replace('\\', '/'); // use / for directory separation to allow to work with both Storage and FileSystem.
                    if (FileExists(testPath))
                    {
                        path = testPath;
                        break;
                    }
                }

                if (path != null)
                {
                    classNameToPath.Add(type, path);
                }

                return path;
            }
        }

        private bool FileExists(string path)
        {
            if (UseFileSystem)
            {
                var fileInfo = new FileInfo(path);
                if (fileInfo.Exists)
                {
                    var shaderName = Path.GetFileNameWithoutExtension(path);
                    var realPath = GetWindowsPhysicalPath(path);
                    if (!string.IsNullOrWhiteSpace(realPath))
                    {
                        var shaderNameOnDisk = Path.GetFileNameWithoutExtension(realPath);
                        return string.CompareOrdinal(shaderName, shaderNameOnDisk) == 0;
                    }
                }
            }
            else
            {
                return fileProvider.FileExists(path);
            }
            return false;
        }

        private Stream OpenStream(string path)
        {
            return UseFileSystem ? File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read) : fileProvider.OpenStream(path, VirtualFileMode.Open, VirtualFileAccess.Read, VirtualFileShare.Read);
        }

        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        static extern uint GetLongPathName(string shortPath, StringBuilder sb, int buffer);

        [DllImport("kernel32.dll")]
        static extern uint GetShortPathName(string longpath, StringBuilder sb, int buffer);

        private static string GetWindowsPhysicalPath(string path)
        {
            var builder = new StringBuilder(255);

            // names with long extension can cause the short name to be actually larger than
            // the long name.
            GetShortPathName(path, builder, builder.Capacity);

            path = builder.ToString();

            uint result = GetLongPathName(path, builder, builder.Capacity);

            if (result > 0 && result < builder.Capacity)
            {
                //Success retrieved long file name
                builder[0] = char.ToLower(builder[0]);
                return builder.ToString(0, (int)result);
            }

            if (result > 0)
            {
                //Need more capacity in the buffer
                //specified in the result variable
                builder = new StringBuilder((int)result);
                result = GetLongPathName(path, builder, builder.Capacity);
                builder[0] = char.ToLower(builder[0]);
                return builder.ToString(0, (int)result);
            }

            return null;
        }

        public struct ShaderSourceWithHash
        {
            public string Path;
            public string Source;
            public ObjectId Hash;
        }
    }
}