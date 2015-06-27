﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.IO;

using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Rendering.ComputeEffect.GGXPrefiltering;
using SiliconStudio.Paradox.Rendering.ComputeEffect.LambertianPrefiltering;
using SiliconStudio.Paradox.Rendering.Images;
using SiliconStudio.Paradox.Rendering.Skyboxes;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Graphics.Data;
using SiliconStudio.Paradox.Shaders;
using SiliconStudio.Paradox.Shaders.Compiler;

namespace SiliconStudio.Paradox.Assets.Skyboxes
{
    public class SkyboxGeneratorContext : ShaderGeneratorContextBase, IDisposable
    {
        public SkyboxGeneratorContext()
        {
            Services = new ServiceRegistry();
            Assets = new AssetManager(Services);
            GraphicsDevice = GraphicsDevice.New();
            GraphicsDeviceService = new GraphicsDeviceServiceLocal(Services, GraphicsDevice);
            EffectSystem = new EffectSystem(Services);
            EffectSystem.Initialize();
            ((IContentable)EffectSystem).LoadContent();
            ((EffectCompilerCache)EffectSystem.Compiler).CompileEffectAsynchronously = false;
            DrawEffectContext = RenderContext.GetShared(Services);
        }

        public IServiceRegistry Services { get; private set; }

        public EffectSystem EffectSystem { get; private set; }

        public GraphicsDevice GraphicsDevice { get; private set; }

        public IGraphicsDeviceService GraphicsDeviceService { get; private set; }

        public RenderContext DrawEffectContext { get; private set; }

        public void Dispose()
        {
            EffectSystem.Dispose();
            GraphicsDevice.Dispose();
        }
    }

    public class SkyboxResult : LoggerResult
    {
        public Skybox Skybox { get; set; }
    }

    public class SkyboxGenerator
    {
        public static SkyboxResult Compile(SkyboxAsset asset, SkyboxGeneratorContext context)
        {
            if (asset == null) throw new ArgumentNullException("asset");
            if (context == null) throw new ArgumentNullException("context");
            var result = new SkyboxResult { Skybox = new Skybox() };

            var parameters = context.Parameters;
            var skybox = result.Skybox;
            skybox.Parameters = parameters;

            if (asset.Model != null)
            {
                var cubemap = ((SkyboxCubeMapModel)asset.Model).CubeMap;
                if (cubemap == null)
                {
                    return result;
                }

                // load the skybox texture from the asset.
                var reference = AttachedReferenceManager.GetAttachedReference(cubemap);
                var skyboxTexture = context.Assets.Load<Texture>(reference.Url);
                if (skyboxTexture.Dimension != TextureDimension.TextureCube)
                {
                    result.Error("SkyboxGenerator: The texture used as skybox should be a Cubemap.");
                    return result;
                }

                var shaderSource = asset.Model.Generate(context);
                parameters.Set(SkyboxKeys.Shader, shaderSource);

                // -------------------------------------------------------------------
                // Calculate Diffuse prefiltering
                // -------------------------------------------------------------------
                var lamberFiltering = new LambertianPrefilteringSH(context.DrawEffectContext);


                lamberFiltering.HarmonicOrder = (int)asset.DiffuseSHOrder;
                lamberFiltering.RadianceMap = skyboxTexture;
                lamberFiltering.Draw();

                var coefficients = lamberFiltering.PrefilteredLambertianSH.Coefficients;

                // TODO: MOVE THE COEFFICIENTS TO THE SphericalHarmonics type in Core.Mathematics
                var PI4 = 4 * Math.PI;
                var PI16 = 16 * Math.PI;
                var PI64 = 64 * Math.PI;
                var SQRT_PI = 1.77245385090551602729;

                var bases = new float[coefficients.Length];
                bases[0] = (float)(1.0 / (2.0 * SQRT_PI));

                bases[1] = (float)(-Math.Sqrt(3.0 / PI4));
                bases[2] = (float)(Math.Sqrt(3.0 / PI4));
                bases[3] = (float)(-Math.Sqrt(3.0 / PI4));

                bases[4] = (float)(Math.Sqrt(15.0 / PI4));
                bases[5] = (float)(-Math.Sqrt(15.0 / PI4));
                bases[6] = (float)(Math.Sqrt(5.0 / PI16));
                bases[7] = (float)(-Math.Sqrt(15.0 / PI4));
                bases[8] = (float)(Math.Sqrt(15.0 / PI16));

                if (asset.DiffuseSHOrder == SkyboxPreFilteringDiffuseOrder.Order5)
                {
                    bases[9] = -(float)Math.Sqrt(7 / PI64);
                    bases[10] = (float)Math.Sqrt(105 / PI4);
                    bases[11] = -(float)Math.Sqrt(21 / PI16);
                    bases[12] = (float)Math.Sqrt(7 / PI16);
                    bases[13] = -(float)Math.Sqrt(42 / PI64);
                    bases[14] = (float)Math.Sqrt(105 / PI16);
                    bases[15] = -(float)Math.Sqrt(70 / PI64);

                    bases[16] = 3 * (float)Math.Sqrt(35 / PI16);
                    bases[17] = -3 * (float)Math.Sqrt(70 / PI64);
                    bases[18] = 3 * (float)Math.Sqrt(5 / PI16);
                    bases[19] = -3 * (float)Math.Sqrt(10 / PI64);
                    bases[20] = (float)(1.0 / (16.0 * SQRT_PI));
                    bases[21] = -3 * (float)Math.Sqrt(10 / PI64);
                    bases[22] = 3 * (float)Math.Sqrt(5 / PI64);
                    bases[23] = -3 * (float)Math.Sqrt(70 / PI64);
                    bases[24] = 3 * (float)Math.Sqrt(35 / (4 * PI64));
                }

                for (int i = 0; i < coefficients.Length; i++)
                {
                    coefficients[i] = coefficients[i] * bases[i];
                }

                skybox.DiffuseLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("SphericalHarmonicsEnvironmentColor", lamberFiltering.HarmonicOrder));
                skybox.DiffuseLightingParameters.Set(SphericalHarmonicsEnvironmentColorKeys.SphericalColors, coefficients);

                // -------------------------------------------------------------------
                // Calculate Specular prefiltering
                // -------------------------------------------------------------------
                var specularRadiancePrefilterGGX = new RadiancePrefilteringGGX(context.DrawEffectContext);

                var textureSize = asset.SpecularCubeMapSize <= 0 ? 64 : asset.SpecularCubeMapSize;
                textureSize = (int)Math.Pow(2, Math.Round(Math.Log(textureSize, 2)));
                if (textureSize < 64) textureSize = 64;

                // TODO: Add support for HDR 32bits 
                var filteringTextureFormat = skyboxTexture.Format.IsHDR() ? skyboxTexture.Format : PixelFormat.R8G8B8A8_UNorm;

                //var outputTexture = Texture.New2D(graphicsDevice, 256, 256, skyboxTexture.Format, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess, 6);
                using (var outputTexture = Texture.New2D(context.GraphicsDevice, textureSize, textureSize, true, filteringTextureFormat, TextureFlags.ShaderResource | TextureFlags.UnorderedAccess, 6))
                {
                    specularRadiancePrefilterGGX.RadianceMap = skyboxTexture;
                    specularRadiancePrefilterGGX.PrefilteredRadiance = outputTexture;
                    specularRadiancePrefilterGGX.Draw();

                    var cubeTexture = Texture.NewCube(context.GraphicsDevice, textureSize, true, skyboxTexture.Format);
                    context.GraphicsDevice.Copy(outputTexture, cubeTexture);

                    cubeTexture.SetSerializationData(cubeTexture.GetDataAsImage());

                    skybox.SpecularLightingParameters.Set(SkyboxKeys.Shader, new ShaderClassSource("RoughnessCubeMapEnvironmentColor"));
                    skybox.SpecularLightingParameters.Set(SkyboxKeys.CubeMap, cubeTexture);
                }

                // TODO: cubeTexture is not deallocated
            }

            return result;
        }
    }
}