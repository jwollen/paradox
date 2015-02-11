﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Assets.Textures
{
    /// <summary>
    /// Represents the different formats of alpha channel possibly desired.
    /// </summary>
    [DataContract]
    public enum AlphaFormat
    {
        /// <summary>
        /// Alpha channel should be ignored.
        /// </summary>
        /// <userdoc>No alpha channel</userdoc>
        None,

        /// <summary>
        /// Alpha channel should be stored as 1-bit mask if possible.
        /// </summary>
        /// <userdoc>Ensures an alpha channel composed of only absolute opaque or absolute transparent values.</userdoc>
        Mask,

        /// <summary>
        /// Alpha channel should be stored with explicit compression. Well suited to sharp alpha transitions between translucent and opaque areas.
        /// </summary>
        /// <userdoc>Ensures an alpha channel well suited for sharp alpha transitions between translucent and opaque areas.</userdoc>
        Explicit,

        /// <summary>
        /// Alpha channel should be stored using interpolation. Well suited for alpha gradient.
        /// </summary>
        /// <userdoc>Ensure an alpha channel well suited for alpha gradient.</userdoc>
        Interpolated,
    }
}