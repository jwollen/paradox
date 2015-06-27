﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.UI.Controls;

namespace SiliconStudio.Paradox.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="ModalElement"/>.
    /// </summary>
    internal class DefaultModalElementRenderer : ElementRenderer
    {
        private Matrix identity = Matrix.Identity;

        private readonly DepthStencilState noStencilNoDepth;

        public DefaultModalElementRenderer(IServiceRegistry services)
            : base(services)
        {
            noStencilNoDepth = DepthStencilState.New(GraphicsDevice, new DepthStencilStateDescription(false, false));
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            var modalElement = (ModalElement)element;

            // end the current UI image batching so that the overlay is written over it with correct transparency
            Batch.End();

            var uiResolution = new Vector3(context.Resolution.X, context.Resolution.Y, 0);
            Batch.Begin(ref context.ViewProjectionMatrix, GraphicsDevice.BlendStates.AlphaBlend, noStencilNoDepth, 0);
            Batch.DrawRectangle(ref identity, ref uiResolution, ref modalElement.OverlayColorInternal, context.DepthBias);
            Batch.End(); // ensure that overlay is written before possible next transparent element.

            // restart the image batch session
            Batch.Begin(ref context.ViewProjectionMatrix, GraphicsDevice.BlendStates.AlphaBlend, KeepStencilValueState, context.StencilTestReferenceValue);

            context.DepthBias += 1;

            base.RenderColor(element, context);
        }

        protected override void Destroy()
        {
            base.Destroy();
            
            noStencilNoDepth.Dispose();
        }
    }
}