﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.Rendering.Images
{
    /// <summary>
    /// Applies some light-streaks effect to an image.
    /// This takes in input a bright-pass buffer, calculates the light-streaks and blends them 
    /// additively to the specified output.
    /// </summary>
    [DataContract("LightStreak")]
    public class LightStreak : ImageEffect
    {
        private GaussianBlur blur;
        private ColorCombiner combiner;
        private ImageEffectShader lightStreakEffect;

        private const int STREAK_MAX_COUNT = 8;

        private int tapsPerIteration;
        private int streakCount;
        private bool isAnamorphic;

        private Vector2[] tapOffsetsWeights;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightStreak"/> class.
        /// </summary>
        public LightStreak()
        {
            TapsPerIteration = 4;
            StreakCount = 4;
            Attenuation = 0.7f;
            Amount = 0.25f;
            IterationCount = 5;
            Phase = 30f;
            ColorAberrationStrength = 0.2f;
            ColorAberrationCoefficients = new Vector3(1.2f, 1.8f, 2.8f);
            IsAnamorphic = false;
        }

        protected override void InitializeCore()
        {
            base.InitializeCore();

            combiner = ToLoadAndUnload(new ColorCombiner());
            blur = ToLoadAndUnload(new GaussianBlur());
            lightStreakEffect = ToLoadAndUnload(new ImageEffectShader("LightStreakEffect"));
        }

        /// <summary>
        /// Amount of light streak (intensity).
        /// </summary>
        [DataMember(10)]
        [DefaultValue(0.25f)]
        public float Amount { get; set; }

        /// <summary>
        /// Number of light streaks.
        /// </summary>
        [DataMember(20)]
        [DefaultValue(4)]
        [DataMemberRange(1, STREAK_MAX_COUNT, 1, 1, 0)]
        public int StreakCount
        {
            get
            {
                return streakCount;
            }

            set
            {
                if (value <= 0) value = 0;
                if (value > STREAK_MAX_COUNT) value = STREAK_MAX_COUNT;
                streakCount = value;
            }
        }

        /// <summary>
        /// Number of stretching iterations to apply. 
        /// </summary>
        /// <remarks>
        /// Each iteration rises the length of the light streak to the next power of <see cref="TapsPerIteration"/>.
        /// </remarks>
        [DataMemberIgnore]
        public int IterationCount { get; set; }

        /// <summary>
        /// Number of texture taps for each iteration of light streak extension.
        /// </summary>
        [DataMemberIgnore]
        public int TapsPerIteration
        {
            get
            {
                return tapsPerIteration;
            }

            private set
            {
                tapsPerIteration = value;
                tapOffsetsWeights = new Vector2[tapsPerIteration];
            }
        }

        /// <summary>
        /// How fast the attenuation is along a streak. (Affects the streak length.)
        /// </summary>
        [DataMember(30)]
        [DefaultValue(0.95f)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f, 2)]
        public float Attenuation { get; set; }

        /// <summary>
        /// Phase angle for the streaks, in degrees.
        /// </summary>
        [DataMember(40)]
        [DefaultValue(30f)]
        [DataMemberRange(0.0, 180.0, 1.0, 10.0, 1)]
        public float Phase { get; set; }

        /// <summary>
        /// RGB coefficients to apply for color aberration along a streak.
        /// </summary>
        [DataMemberIgnore]
        public Vector3 ColorAberrationCoefficients { get; set; }

        /// <summary>
        /// Strength of the color aberration.
        /// </summary>
        [DataMember(50)]
        [DefaultValue(0.2f)]
        [DataMemberRange(0.0, 1.0, 0.01, 0.1, 2)]
        public float ColorAberrationStrength { get; set; }

        /// <summary>
        /// Applies an anamorphic effect to the streak.
        /// </summary>
        [DataMember(60)]
        [DefaultValue(false)]
        public bool IsAnamorphic
        {
            get
            {
                return isAnamorphic;
            }

            set
            {
                isAnamorphic = value;
                if (!isAnamorphic)
                {
                    AnamorphicOffsetsWeights = new Vector3[]
                        {
                            new Vector3(0f, 0f, 1f)
                        };
                }
                else
                {
                    AnamorphicOffsetsWeights = new Vector3[]
                        {
                            new Vector3(0f,  4f, 0.05f),
                            new Vector3(0f,  0f, 1f),
                            new Vector3(0f, -4f, 0.05f)
                        };
                }
            }
        }

        /// <summary>
        /// For each light streak, you can define some sub-light-streaks drawn at a certain 
        /// offset of the original streak, with a certain weight.
        /// </summary>
        [DataMemberIgnore]
        public Vector3[] AnamorphicOffsetsWeights { get; set; }

        protected override void DrawCore(RenderContext contextParameters)
        {
            var input = GetInput(0);
            var output = GetOutput(0) ?? input;

            if (input == null || StreakCount == 0 || IterationCount == 0)
            {
                return;
            }

            // Downscale to 1 / 4
            var halfSize = input.Size.Down2();
            var halfSizeRenderTarget = NewScopedRenderTarget2D(halfSize.Width, halfSize.Height, input.Format);
            Scaler.SetInput(input);
            Scaler.SetOutput(halfSizeRenderTarget);
            Scaler.Draw(contextParameters, "Downsize to 0.5");

            var fourthSize = halfSize.Down2();
            var fourthSizeRenderTarget = NewScopedRenderTarget2D(fourthSize.Width, fourthSize.Height, input.Format);
            Scaler.SetInput(halfSizeRenderTarget);
            Scaler.SetOutput(fourthSizeRenderTarget);
            Scaler.Draw(contextParameters, "Downsize to 0.25");

            var originalDownsize = fourthSizeRenderTarget;

            // Put all the streaks in an accumulation buffer
            var accumulationBuffer = NewScopedRenderTarget2D(fourthSizeRenderTarget.Description);
            
            // 2 scratch textures to ping-pong between
            var scratchTextureA = NewScopedRenderTarget2D(fourthSizeRenderTarget.Description);
            var scratchTextureB = NewScopedRenderTarget2D(fourthSizeRenderTarget.Description);
            var writeToScratchA = true;

            Vector2 direction;
            Texture currentInput = null, currentOutput = null;

            Vector3 colorAberration;
            colorAberration.X = (float)MathUtil.Lerp(1.0, ColorAberrationCoefficients.X, ColorAberrationStrength);
            colorAberration.Y = (float)MathUtil.Lerp(1.0, ColorAberrationCoefficients.Y, ColorAberrationStrength);
            colorAberration.Z = (float)MathUtil.Lerp(1.0, ColorAberrationCoefficients.Z, ColorAberrationStrength);

            lightStreakEffect.Parameters.Set(LightStreakShaderKeys.ColorAberrationCoefficients, colorAberration);

            for (int streak = 0; streak < StreakCount; streak++)
            {
                // Treats one streak

                // Direction vector
                float angle = MathUtil.DegreesToRadians(Phase) + streak * MathUtil.TwoPi / StreakCount;
                direction.X = (float)Math.Cos(angle);
                direction.Y = (float)Math.Sin(angle);

                // Extends the length recursively
                for (int level = 0; level < IterationCount; level++)
                {
                    // Calculates weights and attenuation factors for all the taps
                    float totalWeight = 0;
                    float passLength = (float) Math.Pow(TapsPerIteration, level);
                    for (int i = 0; i < TapsPerIteration; i++)
                    {
                        tapOffsetsWeights[i].X = i * passLength;
                        tapOffsetsWeights[i].Y = (float) Math.Pow(MathUtil.Lerp(0.7f, 1.0f, Attenuation), i * passLength);
                        totalWeight += tapOffsetsWeights[i].Y;
                    }
                    // Normalizes the weights
                    for (int i = 0; i < TapsPerIteration; i++)
                    {
                        tapOffsetsWeights[i].Y /= totalWeight;
                    }

                    currentInput = writeToScratchA ? scratchTextureB : scratchTextureA;
                    if (level == 0) currentInput = originalDownsize;
                    currentOutput = writeToScratchA ? scratchTextureA : scratchTextureB;

                    lightStreakEffect.Parameters.Set(LightStreakKeys.Count, TapsPerIteration);
                    lightStreakEffect.Parameters.Set(LightStreakKeys.AnamorphicCount, AnamorphicOffsetsWeights.Length);
                    lightStreakEffect.Parameters.Set(LightStreakShaderKeys.TapOffsetsWeights, tapOffsetsWeights);
                    lightStreakEffect.Parameters.Set(LightStreakShaderKeys.AnamorphicOffsetsWeight, AnamorphicOffsetsWeights);
                    lightStreakEffect.Parameters.Set(LightStreakShaderKeys.Direction, direction);
                    lightStreakEffect.SetInput(0, currentInput);
                    lightStreakEffect.SetOutput(currentOutput);
                    lightStreakEffect.Draw(contextParameters, "Light streak {0} iteration {0}", streak, level);

                    writeToScratchA = !writeToScratchA;
                }

                // Writes this streak to the accumulation buffer
                if (streak > 0) GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Additive);

                combiner.SetInput(0, currentOutput);
                combiner.Factors[0] = (1f / StreakCount) * 0.2f * Amount;
                combiner.SetOutput(accumulationBuffer);
                combiner.Draw(contextParameters);
                GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
            }
            
            // All the light streaks have been drawn to the accumulation buffer.
            // Upscales and blurs the accumulation buffer.
            var accumulationUpscaled = NewScopedRenderTarget2D(halfSizeRenderTarget.Description);
            Scaler.SetInput(accumulationBuffer);
            Scaler.SetOutput(accumulationUpscaled);
            Scaler.Draw(contextParameters);

            blur.Radius = 3;
            blur.SetInput(accumulationUpscaled);
            blur.SetOutput(accumulationUpscaled);
            blur.Draw(contextParameters);

            // Adds the result to the original color buffer.
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Additive);
            Scaler.SetInput(accumulationUpscaled);
            Scaler.SetOutput(output);
            Scaler.Draw(contextParameters);
            GraphicsDevice.SetBlendState(GraphicsDevice.BlendStates.Default);
        }
    }
}