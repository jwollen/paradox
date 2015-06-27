// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    /// <summary>
    /// Number of cascades used for a shadow map.
    /// </summary>
    [DataContract("LightShadowMapCascadeCount")]
    public enum LightShadowMapCascadeCount
    {
        /// <summary>
        /// A shadow map with one cascade.
        /// </summary>
        [Display("One Cascade")]
        OneCascade = 1,

        /// <summary>
        /// A shadow map with two cascades.
        /// </summary>
        [Display("Two Cascades")]
        TwoCascades = 2,

        /// <summary>
        /// A shadow map with four cascades.
        /// </summary>
        [Display("Four Cascades")]
        FourCascades = 4
    }
}