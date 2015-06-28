﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.IO;
using System.Reflection;

namespace SiliconStudio.Core
{
    /// <summary>
    /// Folders used for the running platform.
    /// </summary>
    public class PlatformFolders
    {
        // TODO: This class should not try to initialize directories...etc. Try to find another way to do this

        /// <summary>
        /// The system temporary directory.
        /// </summary>
        public static string TemporaryDirectory = GetTemporaryDirectory();

        /// <summary>
        /// The Application temporary directory.
        /// </summary>
        public static string ApplicationTemporaryDirectory = GetApplicationTemporaryDirectory();

        /// <summary>
        /// The application local directory, where user can write local data (included in backup).
        /// </summary>
        public static readonly string ApplicationLocalDirectory = GetApplicationLocalDirectory();

        /// <summary>
        /// The application roaming directory, where user can write roaming data (included in backup).
        /// </summary>
        public static readonly string ApplicationRoamingDirectory = GetApplicationRoamingDirectory();

        /// <summary>
        /// The application cache directory, where user can write data that won't be backup.
        /// </summary>
        public static readonly string ApplicationCacheDirectory = GetApplicationCacheDirectory();

        /// <summary>
        /// The application data directory, where data is deployed.
        /// It could be read-only on some platforms.
        /// </summary>
        public static readonly string ApplicationDataDirectory = GetApplicationDataDirectory();

        /// <summary>
        /// The (optional) application data subdirectory. If not null or empty, /data will be mounted on <see cref="ApplicationDataDirectory"/>/<see cref="ApplicationDataSubDirectory"/>
        /// </summary>
        /// <remarks>This property should not be written after the VirtualFileSystem static initialization. If so, an InvalidOperationExeception will be thrown.</remarks>
        public static string ApplicationDataSubDirectory
        {
            get { return applicationDataSubDirectory; } 
            set
            {
                if (virtualFileSystemInitialized) 
                    throw new InvalidOperationException("ApplicationDataSubDirectory cannot be modified after the VirtualFileSystem has been initialized."); 
                
                applicationDataSubDirectory = value;
            }
        }

        /// <summary>
        /// The application directory, where assemblies are deployed.
        /// It could be read-only on some platforms.
        /// </summary>
        public static readonly string ApplicationBinaryDirectory = GetApplicationBinaryDirectory();

        public static readonly string ApplicationExecutablePath = GetApplicationExecutablePath();

        private static string applicationDataSubDirectory = "";

        private static bool virtualFileSystemInitialized;

        public static bool IsVirtualFileSystemInitialized
        {
            get
            {
                return virtualFileSystemInitialized;
            }
            internal set
            {
                virtualFileSystemInitialized = value;
            }
        }

        private static string GetApplicationLocalDirectory()
        {
#if SILICONSTUDIO_PLATFORM_ANDROID
            var directory = Path.Combine(PlatformAndroid.Context.FilesDir.AbsolutePath, "local");
            Directory.CreateDirectory(directory);
            return directory;
#elif SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
            return Windows.Storage.ApplicationData.Current.LocalFolder.Path;
#elif SILICONSTUDIO_PLATFORM_IOS
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Local");
            Directory.CreateDirectory(directory);
            return directory;
#else
            // TODO: Should we add "local" ?
            var directory = Path.Combine(GetApplicationBinaryDirectory(), "local");
            Directory.CreateDirectory(directory);
            return directory;
#endif
        }

        private static string GetApplicationRoamingDirectory()
        {
#if SILICONSTUDIO_PLATFORM_ANDROID
            var directory = Path.Combine(PlatformAndroid.Context.FilesDir.AbsolutePath, "roaming");
            Directory.CreateDirectory(directory);
            return directory;
#elif SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
            return Windows.Storage.ApplicationData.Current.RoamingFolder.Path;
#elif SILICONSTUDIO_PLATFORM_IOS
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Roaming");
            Directory.CreateDirectory(directory);
            return directory;
#else
            // TODO: Should we add "local" ?
            var directory = Path.Combine(GetApplicationBinaryDirectory(), "roaming");
            Directory.CreateDirectory(directory);
            return directory;
#endif
        }

        private static string GetApplicationCacheDirectory()
        {
#if SILICONSTUDIO_PLATFORM_ANDROID
            var directory = Path.Combine(PlatformAndroid.Context.FilesDir.AbsolutePath, "cache");
            Directory.CreateDirectory(directory);
            return directory;
#elif SILICONSTUDIO_PLATFORM_WINDOWS_STORE
            var directory = Path.Combine(Windows.Storage.ApplicationData.Current.LocalFolder.Path, "cache");
            IO.NativeFile.DirectoryCreate(directory);
            return directory;
#elif SILICONSTUDIO_PLATFORM_WINDOWS_PHONE
            return Windows.Storage.ApplicationData.Current.LocalCacheFolder.Path;
#elif SILICONSTUDIO_PLATFORM_IOS
            var directory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "Library", "Caches");
            Directory.CreateDirectory(directory);
            return directory;
#else
            // TODO: Should we add "local" ?
            var directory = Path.Combine(GetApplicationBinaryDirectory(), "cache");
            Directory.CreateDirectory(directory);
            return directory;
#endif
        }

        private static string GetApplicationExecutablePath()
        {
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP || SILICONSTUDIO_PLATFORM_MONO_MOBILE
            return (Assembly.GetEntryAssembly() ?? Assembly.GetExecutingAssembly()).Location;
#elif SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
            return Path.Combine(Windows.ApplicationModel.Package.Current.InstalledLocation.Path, "ParadoxGame.exe"); // Use generic name workaround
#else
            throw new NotImplementedException();
#endif
        }

        private static string GetTemporaryDirectory()
        {
            return GetApplicationTemporaryDirectory();
        }

        private static string GetApplicationTemporaryDirectory()
        {
#if SILICONSTUDIO_PLATFORM_ANDROID
            return PlatformAndroid.Context.CacheDir.AbsolutePath;
#elif SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
            return Windows.Storage.ApplicationData.Current.TemporaryFolder.Path;
#elif SILICONSTUDIO_PLATFORM_IOS
            return Path.Combine (Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "..", "tmp");
#else
            return Path.GetTempPath();
#endif
        }

        private static string GetApplicationBinaryDirectory()
        {
            var directoryName = GetApplicationExecutablePath();
            var result = String.IsNullOrWhiteSpace(directoryName) ? String.Empty : Path.GetDirectoryName(GetApplicationExecutablePath());
            if (result == String.Empty)
                result = ".";

            return result;
        }

        private static string GetApplicationDataDirectory()
        {
#if SILICONSTUDIO_PLATFORM_ANDROID
            return Android.OS.Environment.ExternalStorageDirectory.AbsolutePath + "/Android/data/" + PlatformAndroid.Context.PackageName + "/data";
#elif SILICONSTUDIO_PLATFORM_IOS
            return Foundation.NSBundle.MainBundle.BundlePath + "/data";
#elif SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
            return Windows.ApplicationModel.Package.Current.InstalledLocation.Path + @"\data";
#else
            return Path.Combine(Directory.GetCurrentDirectory(), "data");
#endif
        }
    }
}