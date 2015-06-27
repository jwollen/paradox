// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Paradox.Rendering.Lights;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Shadows
{

    public interface ILightShadowRenderer
    {
        /// <summary>
        /// Reset the state of this instance before calling Render method multiple times for different shadow map textures. See remarks.
        /// </summary>
        /// <remarks>
        /// This method allows the implementation to prepare some internal states before being rendered.
        /// </remarks>
        void Reset();
    }

    /// <summary>
    /// Interface to render a shadow map.
    /// </summary>
    public interface ILightShadowMapRenderer : ILightShadowRenderer
    {
        LightShadowType GetShadowType(LightShadowMap lightShadowMap);

        ILightShadowMapShaderGroupData CreateShaderGroupData(string compositionKey, LightShadowType shadowType, int maxLightCount);

        void Render(RenderContext context, ShadowMapRenderer shadowMapRenderer, LightShadowMapTexture lightShadowMap);
    }
}