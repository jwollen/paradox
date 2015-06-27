﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Rendering.Shadows;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    /// <summary>
    /// Light renderer for <see cref="LightAmbient"/>.
    /// </summary>
    public class LightAmbientRenderer : LightGroupRendererBase
    {
        private readonly ShaderMixinSource mixin;

        public LightAmbientRenderer()
        {
            mixin = new ShaderMixinSource();
            mixin.Mixins.Add(new ShaderClassSource("LightSimpleAmbient"));
            LightMaxCount = 4;
            IsEnvironmentLight = true;
        }

        public override LightShaderGroup CreateLightShaderGroup(string compositionName, int lightMaxCount, ILightShadowMapShaderGroupData shadowGroup)
        {
            return new LightAmbientShaderGroup(mixin, compositionName);
        }

        private class LightAmbientShaderGroup : LightShaderGroupAndDataPool<LightAmbientShaderGroupData>
        {
            internal readonly ParameterKey<Color3> AmbientLightKey;
            public LightAmbientShaderGroup(ShaderMixinSource mixin, string compositionName)
                : base(mixin, compositionName, null)
            {
                AmbientLightKey = LightSimpleAmbientKeys.AmbientLight.ComposeWith(compositionName);
            }

            protected override LightAmbientShaderGroupData CreateData()
            {
                return new LightAmbientShaderGroupData(this);
            }
        }

        private class LightAmbientShaderGroupData : LightShaderGroupData
        {
            private readonly ParameterKey<Color3> ambientLightKey;
            private Color3 color;

            public LightAmbientShaderGroupData(LightAmbientShaderGroup group)
                : base(null)
            {
                ambientLightKey = group.AmbientLightKey;
            }

            protected override void AddLightInternal(LightComponent light)
            {
                color = light.Color;
            }

            protected override void ApplyParametersInternal(ParameterCollection parameters)
            {
                parameters.Set(ambientLightKey, color);
            }
        }
    }
}