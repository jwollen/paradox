// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.ComponentModel;

using SiliconStudio.Core;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Rendering.Materials;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering
{
    /// <summary>
    /// A forward rendering mode.
    /// </summary>
    [DataContract("CameraRendererModeForward")]
    [Display("Forward")]
    public sealed class CameraRendererModeForward : CameraRendererMode
    {
        private const string ForwardEffect = "ParadoxForwardShadingEffect";

        /// <summary>
        /// Initializes a new instance of the <see cref="CameraRendererModeForward"/> class.
        /// </summary>
        public CameraRendererModeForward()
        {
            ModelEffect = ForwardEffect;
        }

        /// <inheritdoc/>
        [DefaultValue(ForwardEffect)]
        public override string ModelEffect { get; set; }

        /// <summary>
        /// Gets or sets the material filter used to render this scene camera.
        /// </summary>
        /// <value>The material filter.</value>
        [DataMemberIgnore]
        public ShaderSource MaterialFilter { get; set; }

        protected override void DrawCore(RenderContext context)
        {
            // TODO: Find a better extensibility point for PixelStageSurfaceFilter
            var currentFilter = context.Parameters.Get(MaterialKeys.PixelStageSurfaceFilter);
            if (!ReferenceEquals(currentFilter, MaterialFilter))
            {
                context.Parameters.Set(MaterialKeys.PixelStageSurfaceFilter, MaterialFilter);
            }

            base.DrawCore(context);
        }
    }
}