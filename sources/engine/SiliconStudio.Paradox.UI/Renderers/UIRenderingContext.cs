﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.UI.Renderers
{
    /// <summary>
    /// The UI drawing context.
    /// It provides information about how to render <see cref="UIElement"/>s for drawing.
    /// </summary>
    public class UIRenderingContext
    {
        /// <summary>
        /// The current time.
        /// </summary>
        public GameTime Time { get; internal set; }

        /// <summary>
        /// The final render target to draw to.
        /// </summary>
        public Texture RenderTarget { get; set; }

        /// <summary>
        /// The final depth stencil buffer to draw to.
        /// </summary>
        public Texture DepthStencilBuffer { get; set; }

        /// <summary>
        /// The current reference value for the stencil test.
        /// </summary>
        public int StencilTestReferenceValue { get; set; }

        /// <summary>
        /// The value of the depth bias to use for draw call.
        /// </summary>
        public int DepthBias { get; set; }

        /// <summary>
        /// Gets or sets the value indicating if UI text should be snapped.
        /// </summary>
        public bool ShouldSnapText { get; set; }

        /// <summary>
        /// Gets the  virtual resolution of the UI.
        /// </summary>
        public Vector3 Resolution;

        /// <summary>
        /// Gets the view matrix of the UI.
        /// </summary>
        public Matrix ViewMatrix;

        /// <summary>
        /// Gets the projection matrix of the UI.
        /// </summary>
        public Matrix ProjectionMatrix;

        /// <summary>
        /// Gets the view projection matrix of the UI.
        /// </summary>
        public Matrix ViewProjectionMatrix;
    }
}