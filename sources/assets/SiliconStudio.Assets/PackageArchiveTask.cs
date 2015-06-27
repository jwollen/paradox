﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using Microsoft.Build.Framework;
using Microsoft.Build.Utilities;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.Assets
{
    public class PackageArchiveTask : Task
    {
        /// <summary>
        /// Gets or sets the file.
        /// </summary>
        /// <value>The file.</value>
        [Required]
        public ITaskItem File { get; set; }

        public string SpecialVersion { get; set; }

        public override bool Execute()
        {

            var result = new LoggerResult();
            var package = Package.Load(result, File.ItemSpec, new PackageLoadParameters()
                {
                    AutoCompileProjects = false,
                    LoadAssemblyReferences = false,
                    AutoLoadTemporaryAssets = false,
                });

            foreach (var message in result.Messages)
            {
                if (message.Type >= LogMessageType.Error)
                {
                    Log.LogError(message.ToString());
                }
                else if (message.Type == LogMessageType.Warning)
                {
                    Log.LogWarning(message.ToString());
                }
                else
                {
                    Log.LogMessage(message.ToString());
                }
            }

            // If we have errors loading the package, exit
            if (result.HasErrors)
            {
                return false;
            }

            // Override version with task SpecialVersion (if specified by user)
            if (!string.IsNullOrEmpty(SpecialVersion))
            {
                package.Meta.Version = new PackageVersion(package.Meta.Version.Version, SpecialVersion);
            }

            Log.LogMessage(MessageImportance.High, "Packaging [{0}] version [{1}]", package.Meta.Name, package.Meta.Version);

            // Build the package
            PackageArchive.Build(package);
            return true;
        }
    }
}