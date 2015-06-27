﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Rendering.Images
{
    /// <summary>
    /// A bright pass filter.
    /// </summary>
    [DataContract("BrightFilter")]
    public class BrightFilter : ImageEffect
    {
        // TODO: Add Brightpass filters based on average luminance and key value, taking into account the tonemap
        private readonly ImageEffectShader brightPassFilter;

        /// <summary>
        /// Initializes a new instance of the <see cref="BrightFilter"/> class.
        /// </summary>
        public BrightFilter()
            : this("BrightFilterShader")
        {
            Threshold = 1.0f;
            Color = new Color3(1.0f);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BrightFilter"/> class.
        /// </summary>
        /// <param name="brightPassShaderName">Name of the bright pass shader.</param>
        public BrightFilter(string brightPassShaderName) : base(brightPassShaderName)
        {
            if (brightPassShaderName == null) throw new ArgumentNullException("brightPassShaderName");
            brightPassFilter = new ImageEffectShader(brightPassShaderName);
        }

        /// <summary>
        /// Gets or sets the threshold relative to the <see cref="WhitePoint"/>.
        /// </summary>
        /// <value>The threshold.</value>
        [DataMember(10)]
        [DefaultValue(1.0f)]
        public float Threshold { get; set; }

        /// <summary>
        /// Modulate the bloom by a certain color.
        /// </summary>
        /// <value>The color.</value>
        /// <userdoc>Modulates the bloom/lightstreak by a color</userdoc>
        [DataMember(20)]
        public Color3 Color { get; set; }

        protected override void InitializeCore()
        {
            base.InitializeCore();
            ToLoadAndUnload(brightPassFilter);
        }

        protected override void SetDefaultParameters()
        {
            Color = new Color3(1.0f);

            base.SetDefaultParameters();
        }

        protected override void DrawCore(RenderContext context)
        {
            var input = GetInput(0);
            var output = GetOutput(0);
            if (input == null || output == null)
            {
                return;
            }
        
            brightPassFilter.Parameters.Set(BrightFilterShaderKeys.BrightPassThreshold, Threshold);
            brightPassFilter.Parameters.Set(BrightFilterShaderKeys.ColorModulator, Color);
            
            brightPassFilter.SetInput(input);
            brightPassFilter.SetOutput(output);
            brightPassFilter.Draw(context);
        }
    }
}