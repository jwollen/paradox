// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.ComponentModel;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.Rendering
{
    /// <summary>
    /// A <see cref="SceneRendererBase"/> that supports a <see cref="Viewport"/>.
    /// </summary>
    [DataContract]
    public abstract class SceneRendererViewportBase : SceneRendererBase, ISceneRendererViewport
    {
        protected SceneRendererViewportBase()
        {
            Viewport = new RectangleF(0, 0, 100f, 100f);
            IsViewportInPercentage = true;
        }

        /// <summary>
        /// Gets or sets the viewport in percentage or pixel.
        /// </summary>
        /// <value>The viewport in percentage or pixel.</value>
        [DataMember(110)]
        public RectangleF Viewport { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the viewport is in fixed pixels instead of percentage.
        /// </summary>
        /// <value><c>true</c> if the viewport is in pixels instead of percentage; otherwise, <c>false</c>.</value>
        /// <userdoc>When this value is true, the Viewport size is a percentage (0-100) calculated relatively to the size of the Output, else it is a fixed size in pixels.</userdoc>
        [DataMember(120)]
        [DefaultValue(true)]
        [Display("Viewport in %")]
        public bool IsViewportInPercentage { get; set; }

        protected override void ActivateOutputCore(RenderContext context, RenderFrame output, bool disableDepth)
        {
            base.ActivateOutputCore(context, output, disableDepth);

            Viewport viewport;
            var rect = Viewport;
            // Setup the viewport
            if (IsViewportInPercentage)
            {
                var width = output.Width;
                var height = output.Height;
                viewport = new Viewport((int)(rect.X * width / 100.0f), (int)(rect.Y * height / 100.0f), (int)(rect.Width * width / 100.0f), (int)(rect.Height * height / 100.0f));
            }
            else
            {
                viewport = new Viewport((int)rect.X, (int)rect.Y, (int)rect.Width, (int)rect.Height);
            }
            context.GraphicsDevice.SetViewport(viewport);
        }
    }
}