﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Rendering.Images;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Skyboxes
{
    /// <summary>
    /// A renderer for a skybox.
    /// </summary>
    public class SkyboxComponentRenderer : EntityComponentRendererBase
    {
        private ImageEffectShader skyboxEffect;
        private SkyboxProcessor skyboxProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SkyboxComponentRenderer" /> class.
        /// </summary>
        public SkyboxComponentRenderer()
        {
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            skyboxEffect = ToLoadAndUnload(new ImageEffectShader("SkyboxEffect"));
        }

        protected override void PrepareCore(RenderContext context, RenderItemCollection opaqueList, RenderItemCollection transparentList)
        {
            skyboxProcessor = SceneInstance.GetProcessor<SkyboxProcessor>();
            if (skyboxProcessor == null)
            {
                return;
            }

            var skybox = skyboxProcessor.ActiveSkyboxBackground;

            if (skybox != null)
            {
                // Copy camera/pass parameters
                context.Parameters.CopySharedTo(skyboxEffect.Parameters);

                // Show irradiance in the background
                if (skybox.Background == SkyboxBackground.Irradiance)
                {
                    foreach (var parameterKeyValue in skybox.Skybox.DiffuseLightingParameters)
                    {
                        if (parameterKeyValue.Key == SkyboxKeys.Shader)
                        {
                            skyboxEffect.Parameters.Set(SkyboxKeys.Shader, (ShaderSource)parameterKeyValue.Value);
                        }
                        else
                        {
                            skyboxEffect.Parameters.SetObject(parameterKeyValue.Key.ComposeWith("skyboxColor"), parameterKeyValue.Value);
                        }
                    }
                }
                else
                {
                    // TODO: Should we better use composition on "skyboxColor" for parameters?

                    // Copy Skybox parameters
                    if (skybox.Skybox != null)
                    {
                        foreach (var parameterKeyValue in skybox.Skybox.Parameters)
                        {
                            if (parameterKeyValue.Key == SkyboxKeys.Shader)
                            {
                                skyboxEffect.Parameters.Set(SkyboxKeys.Shader, (ShaderSource)parameterKeyValue.Value);
                            }
                            else
                            {
                                skyboxEffect.Parameters.SetObject(parameterKeyValue.Key, parameterKeyValue.Value);
                            }
                        }
                    }
                }

                // Fake as the skybox was in front of all others (as opaque are rendered back to front)
                opaqueList.Add(new RenderItem(this, skybox, 0.0f));
            }
        }

        protected override void DrawCore(RenderContext context, RenderItemCollection renderItems, int fromIndex, int toIndex)
        {
            var viewport = context.GraphicsDevice.Viewport;

            for (int i = fromIndex; i <= toIndex; i++)
            {
                var skybox = (SkyboxComponent)renderItems[i].DrawContext;

                // Setup the intensity
                skyboxEffect.Parameters.Set(SkyboxKeys.Intensity, skybox.Intensity);

                // Setup the rotation
                skyboxEffect.Parameters.Set(SkyboxKeys.SkyMatrix, Matrix.RotationQuaternion(skybox.Entity.Transform.Rotation));

                skyboxEffect.SetOutput(CurrentRenderFrame.RenderTargets);
                skyboxEffect.SetViewport(viewport);
                skyboxEffect.Draw();
            }

            // Make sure to fully restore the current render frame
            CurrentRenderFrame.Activate(context);

            // Restore the viewport: TODO: We should add a method to Push/Pop Target/Depth/Stencil/Viewport on the GraphicsDevice
            context.GraphicsDevice.SetViewport(viewport);
        }

        protected override void Unload()
        {
            skyboxEffect.Dispose();
            base.Unload();
        }
    }
}