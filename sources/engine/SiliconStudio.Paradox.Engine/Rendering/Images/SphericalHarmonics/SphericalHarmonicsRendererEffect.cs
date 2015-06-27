﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Rendering.Images.SphericalHarmonics
{
    public class SphericalHarmonicsRendererEffect :ImageEffectShader
    {
        /// <summary>
        /// Gets or sets the harmonic order to use during the filtering.
        /// </summary>
        public Core.Mathematics.SphericalHarmonics InputSH { get; set; }

        public SphericalHarmonicsRendererEffect()
        {
            EffectName = "SphericalHarmonicsRendererEffect";
        }

        protected override void UpdateParameters()
        {
            base.UpdateParameters();

            if (InputSH != null)
            {
                Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, InputSH.Order);
                Parameters.Set(SphericalHarmonicsRendererKeys.SHCoefficients, InputSH.Coefficients);
            }
            else
            {
                Parameters.Set(SphericalHarmonicsParameters.HarmonicsOrder, 1);
                Parameters.Set(SphericalHarmonicsRendererKeys.SHCoefficients, new []{ new Color3() });
            }
        }
    }
}