﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using SiliconStudio.Core;
using SiliconStudio.Core.ReferenceCounting;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// This class is a frontend to <see cref="SwapChain" /> and <see cref="SwapChain1" />.
    /// </summary>
    /// <remarks>
    /// In order to create a new <see cref="GraphicsPresenter"/>, a <see cref="GraphicsDevice"/> should have been initialized first.
    /// </remarks>
    public abstract class GraphicsPresenter : ComponentBase
    {
        private Texture depthStencilBuffer;

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsPresenter" /> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="presentationParameters"> </param>
        protected GraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
        {
            GraphicsDevice = device.RootDevice;
            Description = presentationParameters.Clone();

            DefaultViewport = new Viewport(0, 0, Description.BackBufferWidth, Description.BackBufferHeight);

            // Creates a default DepthStencilBuffer.
            CreateDepthStencilBuffer();
        }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Gets the description of this presenter.
        /// </summary>
        public PresentationParameters Description { get; private set; }

        /// <summary>
        /// Default viewport that covers the whole presenter surface.
        /// </summary>
        public Viewport DefaultViewport { get; protected set; }

        /// <summary>
        /// Gets the default back buffer for this presenter.
        /// </summary>
        public abstract Texture BackBuffer { get; }

        /// <summary>
        /// Gets the default depth stencil buffer for this presenter.
        /// </summary>
        public Texture DepthStencilBuffer
        {
            get
            {
                return depthStencilBuffer;
            }

            protected set
            {
                depthStencilBuffer = value;
            }
        }

        /// <summary>
        /// Gets the underlying native presenter (can be a <see cref="SharpDX.DXGI.SwapChain"/> or <see cref="SharpDX.DXGI.SwapChain1"/> or null, depending on the platform).
        /// </summary>
        /// <value>The native presenter.</value>
        public abstract object NativePresenter { get; }

        /// <summary>
        /// Gets or sets fullscreen mode for this presenter.
        /// </summary>
        /// <value><c>true</c> if this instance is full screen; otherwise, <c>false</c>.</value>
        /// <remarks>This method is only valid on Windows Desktop and has no effect on Windows Metro.</remarks>
        public abstract bool IsFullScreen { get; set; }

        /// <summary>
        /// Gets or sets the <see cref="PresentInterval"/>. Default is to wait for one vertical blanking.
        /// </summary>
        /// <value>The present interval.</value>
        public PresentInterval PresentInterval
        {
            get { return Description.PresentationInterval; }
            set { Description.PresentationInterval = value; }
        }

        /// <summary>
        /// Presents the Backbuffer to the screen.
        /// </summary>
        public abstract void Present();

        /// <summary>
        /// Resizes the current presenter, by resizing the back buffer and the depth stencil buffer.
        /// </summary>
        /// <param name="width"></param>
        /// <param name="height"></param>
        public void Resize(int width, int height, PixelFormat format)
        {
            Description.BackBufferWidth = width;
            Description.BackBufferHeight = height;
            Description.BackBufferFormat = format;

            DefaultViewport = new Viewport(0, 0, Description.BackBufferWidth, Description.BackBufferHeight);

            ResizeBackBuffer(width, height, format);
            ResizeDepthStencilBuffer(width, height, format);
        }

        protected abstract void ResizeBackBuffer(int width, int height, PixelFormat format);

        protected abstract void ResizeDepthStencilBuffer(int width, int height, PixelFormat format);

        protected void ReleaseCurrentDepthStencilBuffer()
        {
            if (DepthStencilBuffer != null)
            {
                depthStencilBuffer.RemoveKeepAliveBy(this);
            }
        }

        /// <summary>
        /// Called when [destroyed].
        /// </summary>
        public virtual void OnDestroyed()
        {
        }

        /// <summary>
        /// Called when [recreated].
        /// </summary>
        public virtual void OnRecreated()
        {
        }

        /// <summary>
        /// Creates the depth stencil buffer.
        /// </summary>
        protected virtual void CreateDepthStencilBuffer()
        {
            // If no depth stencil buffer, just return
            if (Description.DepthStencilFormat == PixelFormat.None)
                return;

            // Creates the depth stencil buffer.
            var flags = TextureFlags.DepthStencil;
            if (GraphicsDevice.Features.Profile >= GraphicsProfile.Level_10_0)
            {
                flags |= TextureFlags.ShaderResource;
            }

            var depthTexture = Texture.New2D(GraphicsDevice, Description.BackBufferWidth, Description.BackBufferHeight, Description.DepthStencilFormat, flags);
            DepthStencilBuffer = depthTexture.KeepAliveBy(this);
        }
    }
}