// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

namespace SiliconStudio.Paradox.Rendering.Lights
{
    /// <summary>
    /// Common interface of a shadowmap filter.
    /// </summary>
    public interface ILightShadowMapFilterType
    {
        bool RequiresCustomBuffer();
    }
}