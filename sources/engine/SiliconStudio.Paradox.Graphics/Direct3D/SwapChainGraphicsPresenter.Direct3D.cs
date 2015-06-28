// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
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

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D
using System;
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
using System.Windows.Forms;
#endif
using SharpDX;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// Graphics presenter for SwapChain.
    /// </summary>
    public class SwapChainGraphicsPresenter : GraphicsPresenter
    {
        private SwapChain swapChain;

        private Texture backBuffer;

        private int bufferCount;

        public SwapChainGraphicsPresenter(GraphicsDevice device, PresentationParameters presentationParameters)
            : base(device, presentationParameters)
        {
            PresentInterval = presentationParameters.PresentationInterval;

            // Initialize the swap chain
            swapChain = CreateSwapChain();

            backBuffer = new Texture(device).InitializeFrom(swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0));

            // Reload should get backbuffer from swapchain as well
            //backBufferTexture.Reload = graphicsResource => ((Texture)graphicsResource).Recreate(swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture>(0));
        }

        public override Texture BackBuffer
        {
            get
            {
                return backBuffer;
            }
        }

        public override object NativePresenter
        {
            get
            {
                return swapChain;
            }
        }

        public override bool IsFullScreen
        {
            get
            {
#if SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
                return false;
#else
                return swapChain.IsFullScreen;
#endif
            }

            set
            {
#if !SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
                if (swapChain == null)
                    return;

                var outputIndex = Description.PreferredFullScreenOutputIndex;

                // no outputs connected to the current graphics adapter
                var output = GraphicsDevice.Adapter != null && outputIndex < GraphicsDevice.Adapter.Outputs.Length ? GraphicsDevice.Adapter.Outputs[outputIndex] : null;

                Output currentOutput = null;

                try
                {
                    RawBool isCurrentlyFullscreen;
                    swapChain.GetFullscreenState(out isCurrentlyFullscreen, out currentOutput);

                    // check if the current fullscreen monitor is the same as new one
                    if (isCurrentlyFullscreen == value && output != null && currentOutput != null && currentOutput.NativePointer == output.NativeOutput.NativePointer)
                        return;
                }
                finally
                {
                    if (currentOutput != null)
                        currentOutput.Dispose();
                }

                bool switchToFullScreen = value;
                // If going to fullscreen mode: call 1) SwapChain.ResizeTarget 2) SwapChain.IsFullScreen
                var description = new ModeDescription(backBuffer.ViewWidth, backBuffer.ViewHeight, Description.RefreshRate.ToSharpDX(), (SharpDX.DXGI.Format)Description.BackBufferFormat);
                if (switchToFullScreen)
                {
                    // Force render target destruction
                    // TODO: We should track all user created render targets that points to back buffer as well (or deny their creation?)
                    backBuffer.OnDestroyed();

                    OnDestroyed();

                    Description.IsFullScreen = true;

                    OnRecreated();

                    // Recreate render target
                    backBuffer.OnRecreate();
                }
                else
                {
                    Description.IsFullScreen = false;
                    swapChain.IsFullScreen = false;

                    // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                    Resize(backBuffer.ViewWidth, backBuffer.ViewHeight, backBuffer.ViewFormat);
                }

                // If going to window mode: 
                if (!switchToFullScreen)
                {
                    // call 1) SwapChain.IsFullScreen 2) SwapChain.Resize
                    description.RefreshRate = new SharpDX.DXGI.Rational(0, 0);
                    swapChain.ResizeTarget(ref description);
                }
#endif
            }
        }

        public override void Present()
        {
            try
            {
                swapChain.Present((int) PresentInterval, PresentFlags.None);
            }
            catch (SharpDXException sharpDxException)
            {
                throw new GraphicsException("Unexpected error on Present", sharpDxException, GraphicsDevice.GraphicsDeviceStatus);
            }
        }

        protected override void OnNameChanged()
        {
            base.OnNameChanged();
            if (Name != null && GraphicsDevice != null && GraphicsDevice.IsDebugMode && swapChain != null)
            {
                swapChain.DebugName = Name;
            }
        }

        public override void OnDestroyed()
        {
            // Manually update back buffer texture
            backBuffer.OnDestroyed();
            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Destroyed;

            swapChain.Dispose();
            swapChain = null;

            base.OnDestroyed();
        }

        public override void OnRecreated()
        {
            base.OnRecreated();

            // Recreate swap chain
            swapChain = CreateSwapChain();

            // Get newly created native texture
            var backBufferTexture = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0);

            // Put it in our back buffer texture
            // TODO: Update new size
            backBuffer.InitializeFrom(backBufferTexture);
            backBuffer.LifetimeState = GraphicsResourceLifetimeState.Active;
        }

        protected override void ResizeBackBuffer(int width, int height, PixelFormat format)
        {
            // Manually update back buffer texture
            backBuffer.OnDestroyed();

#if SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
            var swapChainPanel = Description.DeviceWindowHandle.NativeHandle as Windows.UI.Xaml.Controls.SwapChainPanel;
            if (swapChainPanel != null)
            {
                var swapChain2 = swapChain.QueryInterface<SwapChain2>();
                if (swapChain2 != null)
                {
                    swapChain2.MatrixTransform = new RawMatrix3x2 { M11 = 1f / swapChainPanel.CompositionScaleX, M22 = 1f / swapChainPanel.CompositionScaleY };
                    swapChain2.Dispose();
                }
            }
#endif

            swapChain.ResizeBuffers(bufferCount, width, height, (SharpDX.DXGI.Format)format, SwapChainFlags.None);

            // Get newly created native texture
            var backBufferTexture = swapChain.GetBackBuffer<SharpDX.Direct3D11.Texture2D>(0);

            // Put it in our back buffer texture
            backBuffer.InitializeFrom(backBufferTexture);
        }

        protected override void ResizeDepthStencilBuffer(int width, int height, PixelFormat format)
        {
            var newTextureDescrition = DepthStencilBuffer.Description;
            newTextureDescrition.Width = width;
            newTextureDescrition.Height = height;

            // Manually update the texture
            DepthStencilBuffer.OnDestroyed();

            // Put it in our back buffer texture
            DepthStencilBuffer.InitializeFrom(newTextureDescrition);
        }


        private SwapChain CreateSwapChain()
        {
            // Check for Window Handle parameter
            if (Description.DeviceWindowHandle == null)
            {
                throw new ArgumentException("DeviceWindowHandle cannot be null");
            }

#if SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
            return CreateSwapChainForWindowsRuntime();
#else
            return CreateSwapChainForDesktop();
#endif
        }

#if SILICONSTUDIO_PLATFORM_WINDOWS_RUNTIME
        private SwapChain CreateSwapChainForWindowsRuntime()
        {
            bufferCount = 2;
            var description = new SwapChainDescription1
            {
                // Automatic sizing
                Width = Description.BackBufferWidth,
                Height = Description.BackBufferHeight,
                Format = SharpDX.DXGI.Format.B8G8R8A8_UNorm, // TODO: Check if we can use the Description.BackBufferFormat
                Stereo = false,
                SampleDescription = new SharpDX.DXGI.SampleDescription((int)Description.MultiSampleCount, 0),
                Usage = Usage.BackBuffer | Usage.RenderTargetOutput,
                // Use two buffers to enable flip effect.
                BufferCount = bufferCount,
                Scaling = SharpDX.DXGI.Scaling.Stretch,
                SwapEffect = SharpDX.DXGI.SwapEffect.FlipSequential,
            };

            SwapChain swapChain = null;
            switch (Description.DeviceWindowHandle.Context)
            {
                case Games.AppContextType.WindowsRuntime:
                {
                    var nativePanel = ComObject.As<ISwapChainPanelNative>(Description.DeviceWindowHandle.NativeHandle);
                    // Creates the swap chain for XAML composition
                    swapChain = new SwapChain1(GraphicsAdapterFactory.NativeFactory, GraphicsDevice.NativeDevice, ref description);

                    // Associate the SwapChainPanel with the swap chain
                    nativePanel.SwapChain = swapChain;
                    break;
                }
                default:
                    throw new NotSupportedException(string.Format("Window context [{0}] not supported while creating SwapChain", Description.DeviceWindowHandle.Context));
            }

            return swapChain;
        }
#else
        private SwapChain CreateSwapChainForDesktop()
        {
            var control = Description.DeviceWindowHandle.NativeHandle as Control;
            if (control == null)
            {
                throw new NotSupportedException(string.Format("Form of type [{0}] is not supported. Only System.Windows.Control are supported", Description.DeviceWindowHandle != null ? Description.DeviceWindowHandle.GetType().Name : "null"));
            }

            bufferCount = 1;
            var description = new SwapChainDescription
                {
                    ModeDescription = new ModeDescription(Description.BackBufferWidth, Description.BackBufferHeight, Description.RefreshRate.ToSharpDX(), (SharpDX.DXGI.Format)Description.BackBufferFormat), 
                    BufferCount = bufferCount, // TODO: Do we really need this to be configurable by the user?
                    OutputHandle = control.Handle, 
                    SampleDescription = new SampleDescription((int)Description.MultiSampleCount, 0), 
                    SwapEffect = SwapEffect.Discard,
                    Usage = SharpDX.DXGI.Usage.BackBuffer | SharpDX.DXGI.Usage.RenderTargetOutput,
                    IsWindowed = true,
                    Flags = Description.IsFullScreen ? SwapChainFlags.AllowModeSwitch : SwapChainFlags.None, 
                };

            var newSwapChain = new SwapChain(GraphicsAdapterFactory.NativeFactory, GraphicsDevice.NativeDevice, description);
            if (Description.IsFullScreen)
            {
                // Before fullscreen switch
                newSwapChain.ResizeTarget(ref description.ModeDescription);

                // Switch to full screen
                newSwapChain.IsFullScreen = true;

                // This is really important to call ResizeBuffers AFTER switching to IsFullScreen 
                newSwapChain.ResizeBuffers(bufferCount, Description.BackBufferWidth, Description.BackBufferHeight, (SharpDX.DXGI.Format)Description.BackBufferFormat, SwapChainFlags.AllowModeSwitch);
            }

            return newSwapChain;
        }
#endif
    }
}
#endif