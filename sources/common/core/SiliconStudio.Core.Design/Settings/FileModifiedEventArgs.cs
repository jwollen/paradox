﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Core.Settings
{
    /// <summary>
    /// Arguments of the <see cref="SettingsProfile.FileModified"/> event.
    /// </summary>
    public class FileModifiedEventArgs : EventArgs
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="FileModifiedEventArgs"/>
        /// </summary>
        /// <param name="profile">The profile corresponding to the file that has been modified.</param>
        public FileModifiedEventArgs(SettingsProfile profile)
        {
            Profile = profile;
        }

        /// <summary>
        /// Gets the profile corresponding to the file that has been modified..
        /// </summary>
        public SettingsProfile Profile { get; private set; }

        /// <summary>
        /// Gets or sets whether the modified file should be reloaded. False by default.
        /// </summary>
        public bool ReloadFile { get; set; }
    }
}