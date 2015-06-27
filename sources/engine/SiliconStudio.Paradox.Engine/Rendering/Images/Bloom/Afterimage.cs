﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.Rendering.Images
{
    /// <summary>
    /// Afterimage simulates the persistence of the bright areas on the retina. 
    /// </summary>
    [DataContract("Afterimage")]
    public class Afterimage : ImageEffect
    {

        private readonly ImageEffectShader bloomAfterimageShader;
        private readonly ImageEffectShader bloomAfterimageCombineShader;

        private Texture persistenceTexture;

        /// <summary>
        /// Initializes a new instance of the <see cref="Afterimage"/> class.
        /// </summary>
        public Afterimage()
        {
            bloomAfterimageShader = new ImageEffectShader("BloomAfterimageShader");
            bloomAfterimageCombineShader = new ImageEffectShader("BloomAfterimageCombineShader");
            FadeOutSpeed = 0.9f;
            Sensitivity = 0.1f;
        }

        /// <summary>
        /// How fast the persistent image fades out. 
        /// </summary>
        [DataMember(10)]
        [DefaultValue(0.9f)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f)]
        public float FadeOutSpeed { get; set; }

        /// <summary>
        /// How sensitive we are to the bright light.
        /// </summary>
        [DataMember(20)]
        [DefaultValue(0.1f)]
        public float Sensitivity { get; set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            ToLoadAndUnload(bloomAfterimageShader);
            ToLoadAndUnload(bloomAfterimageCombineShader);
        }

        protected override void Destroy()
        {
            if (persistenceTexture != null) Context.Allocator.ReleaseReference(persistenceTexture);
            base.Destroy();
        }

        protected override void DrawCore(RenderContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0);

            if (FadeOutSpeed == 0f)
            {
                // Nothing to do
                if (input != output)
                {
                    GraphicsDevice.Copy(input, output);
                }
                return;
            }

            if (input == output)
            {
                var newInput = NewScopedRenderTarget2D(input.Description);
                GraphicsDevice.Copy(input, newInput);
                input = newInput;
            }

            // Check we have a render target to hold the persistence over a few frames
            if (persistenceTexture == null || persistenceTexture.Description != output.Description)
            {
                // We need to re-allocate the texture
                if (persistenceTexture != null)
                {
                    Context.Allocator.ReleaseReference(persistenceTexture);
                }

                persistenceTexture = Context.Allocator.GetTemporaryTexture2D(output.Description);
                // Initializes to black
                GraphicsDevice.Clear(persistenceTexture, Color.Black);
            }

            var accumulationPersistence = NewScopedRenderTarget2D(persistenceTexture.Description);

            // For persistence, we combine the current brightness with the one of the previous frames.
            bloomAfterimageShader.Parameters.Set(BloomAfterimageShaderKeys.FadeOutSpeed, FadeOutSpeed);
            bloomAfterimageShader.Parameters.Set(BloomAfterimageShaderKeys.Sensitivity, Sensitivity / 100f);
            bloomAfterimageShader.SetInput(0, input);
            bloomAfterimageShader.SetInput(1, persistenceTexture);
            bloomAfterimageShader.SetOutput(accumulationPersistence);
            bloomAfterimageShader.Draw("Afterimage persistence accumulation");

            // Keep the final brightness buffer for the following frames
            GraphicsDevice.Copy(accumulationPersistence, persistenceTexture);

            // Merge persistence and current bloom into the final result
            bloomAfterimageCombineShader.SetInput(0, input);
            bloomAfterimageCombineShader.SetInput(1, persistenceTexture);
            bloomAfterimageCombineShader.SetOutput(output);
            bloomAfterimageCombineShader.Draw("Afterimage persistence combine");
        }
    }
}