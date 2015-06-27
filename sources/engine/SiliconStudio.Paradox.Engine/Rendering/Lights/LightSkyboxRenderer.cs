﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Rendering.Skyboxes;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Rendering.Shadows;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    /// <summary>
    /// Light renderer for <see cref="LightSkybox"/>.
    /// </summary>
    public class LightSkyboxRenderer : LightGroupRendererBase
    {
        public LightSkyboxRenderer()
        {
            IsEnvironmentLight = true;
            LightMaxCount = 4;
        }
       
        public override LightShaderGroup CreateLightShaderGroup(string compositionName, int lightMaxCount, ILightShadowMapShaderGroupData shadowGroup)
        {
            var mixin = new ShaderMixinGeneratorSource("LightSkyboxEffect");
            return new LightSkyBoxShaderGroup(mixin, compositionName);
        }

        private class LightSkyBoxShaderGroup : LightShaderGroupAndDataPool<LightSkyBoxShaderGroupData>
        {
            internal readonly ParameterKey<float> IntensityKey;
            internal readonly ParameterKey<Matrix> SkyMatrixKey;
            internal readonly ParameterKey<ShaderSource> LightDiffuseColorKey;
            internal readonly ParameterKey<ShaderSource> LightSpecularColorKey;
            internal readonly ParameterKey<Color3[]> SphericalColorsKey;
            internal readonly ParameterKey<Texture> SpecularCubeMapkey;
            internal readonly ParameterKey<float> SpecularMipCountKey;

            public LightSkyBoxShaderGroup(ShaderSource mixin, string compositionName)
                : base(mixin, compositionName, null)
            {
                IntensityKey = LightSkyboxShaderKeys.Intensity.ComposeWith(compositionName);
                SkyMatrixKey = LightSkyboxShaderKeys.SkyMatrix.ComposeWith(compositionName);
                LightDiffuseColorKey = LightSkyboxShaderKeys.LightDiffuseColor.ComposeWith(compositionName);
                LightSpecularColorKey = LightSkyboxShaderKeys.LightSpecularColor.ComposeWith(compositionName);

                SphericalColorsKey = SphericalHarmonicsEnvironmentColorKeys.SphericalColors.ComposeWith("lightDiffuseColor." + compositionName);
                SpecularCubeMapkey = RoughnessCubeMapEnvironmentColorKeys.CubeMap.ComposeWith("lightSpecularColor." + compositionName);
                SpecularMipCountKey = RoughnessCubeMapEnvironmentColorKeys.MipCount.ComposeWith("lightSpecularColor." + compositionName);
            }

            protected override LightSkyBoxShaderGroupData CreateData()
            {
                return new LightSkyBoxShaderGroupData(this);
            }
        }

        private class LightSkyBoxShaderGroupData : LightShaderGroupData
        {
            private readonly ParameterKey<float> intensityKey;

            private readonly ParameterKey<Matrix> skyMatrixKey;

            private readonly ParameterKey<ShaderSource> lightDiffuseColorKey;

            private readonly ParameterKey<ShaderSource> lightSpecularColorKey;

            private float intensity;

            private Matrix rotationMatrix;

            private ShaderSource lightDiffuseColorShader;

            private ShaderSource lightSpecularColorShader;

            private Color3[] sphericalColors;

            private readonly ParameterKey<Color3[]> sphericalColorsKey;

            private readonly ParameterKey<Texture> specularCubeMapkey;

            private readonly ParameterKey<float> specularMipCountKey;

            private static readonly ShaderClassSource EmptyComputeEnvironmentColorSource = new ShaderClassSource("IComputeEnvironmentColor");

            private Skybox previousSkybox;

            private Texture specularCubemap;

            private int specularCubemapLevels;

            public LightSkyBoxShaderGroupData(LightSkyBoxShaderGroup group) : base(null)
            {
                intensityKey = group.IntensityKey;
                skyMatrixKey = group.SkyMatrixKey;
                lightDiffuseColorKey = group.LightDiffuseColorKey;
                lightSpecularColorKey = group.LightSpecularColorKey;

                sphericalColorsKey = group.SphericalColorsKey;
                specularCubeMapkey = group.SpecularCubeMapkey;
                specularMipCountKey = group.SpecularMipCountKey;
            }

            protected override void AddLightInternal(LightComponent light)
            {
                // TODO: If there is a performance penalty for accessing the SkyboxComponent, this could be prepared by the LightProcessor
                var lightSkybox = ((LightSkybox)light.Type);
                var skyboxComponent = lightSkybox.SkyboxComponent;
                var skybox = skyboxComponent.Skybox;

                intensity = light.Intensity;
                if (skyboxComponent.Enabled)
                {
                    intensity *= skyboxComponent.Intensity;
                }

                rotationMatrix = lightSkybox.SkyMatrix;

                var diffuseParameters = skybox.DiffuseLightingParameters;
                var specularParameters = skybox.SpecularLightingParameters;

                specularCubemap = specularParameters.Get(SkyboxKeys.CubeMap);
                if (specularCubemap != null)
                {
                    specularCubemapLevels = specularCubemap.MipLevels;
                }
                sphericalColors = diffuseParameters.Get(SphericalHarmonicsEnvironmentColorKeys.SphericalColors);
                lightDiffuseColorShader = diffuseParameters.Get(SkyboxKeys.Shader) ?? EmptyComputeEnvironmentColorSource;
                lightSpecularColorShader = specularParameters.Get(SkyboxKeys.Shader) ?? EmptyComputeEnvironmentColorSource;

                previousSkybox = skybox;
            }

            protected override void ApplyParametersInternal(ParameterCollection parameters)
            {
                // global parameters
                parameters.Set(intensityKey, intensity);
                parameters.Set(skyMatrixKey, rotationMatrix);

                if (!ReferenceEquals(parameters.Get(lightDiffuseColorKey), lightDiffuseColorShader))
                {
                    parameters.Set(lightDiffuseColorKey, lightDiffuseColorShader);
                }
                if (!ReferenceEquals(parameters.Get(lightSpecularColorKey), lightSpecularColorShader))
                {
                    parameters.Set(lightSpecularColorKey, lightSpecularColorShader);
                }

                parameters.Set(sphericalColorsKey, sphericalColors);
                parameters.Set(specularCubeMapkey, specularCubemap);
                parameters.Set(specularMipCountKey, specularCubemapLevels);
            }
        }
    }
}
