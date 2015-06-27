﻿using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SiliconStudio.Paradox.Rendering.Images
{
    [DataContract("Vignetting")]
    public sealed class Vignetting : ColorTransform
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="Vignetting"/> class.
        /// </summary>
        public Vignetting() : this("VignettingShader")
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Vignetting"/> class.
        /// </summary>
        public Vignetting(string effect) : base(effect)
        {
            Amount = 0.8f;
            Radius = 0.7f;
            Color = new Color3(0f);
        }

        /// <summary>
        /// Amount of vignetting (alpha of the halo).
        /// </summary>
        [DataMember(10)]
        [DefaultValue(0.8f)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f)]
        public float Amount { get; set; }

        /// <summary>
        /// Radius from the center, from which vignetting begins. 
        /// </summary>
        [DataMember(20)]
        [DefaultValue(0.7f)]
        [DataMemberRange(0f, 1f, 0.01f, 0.1f)]
        public float Radius { get; set; }

        /// <summary>
        /// Color of the vignetting halo.
        /// </summary>
        [DataMember(30)]
        public Color3 Color { get; set; }

        public override void UpdateParameters(ColorTransformContext context)
        {
            base.UpdateParameters(context);

            Parameters.Set(VignettingShaderKeys.Amount, Amount);
            Parameters.Set(VignettingShaderKeys.RadiusBegin, Radius);
            Parameters.Set(VignettingShaderKeys.Color, Color);
        }

    }
}
