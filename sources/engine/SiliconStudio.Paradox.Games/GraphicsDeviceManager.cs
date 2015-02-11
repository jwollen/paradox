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
using System.Collections.Generic;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Games
{
    /// <summary>
    /// Manages the <see cref="GraphicsDevice"/> lifecycle.
    /// </summary>
    public class GraphicsDeviceManager : ComponentBase, IGraphicsDeviceManager, IGraphicsDeviceService
    {
        #region Fields

        /// <summary>
        /// Default width for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferWidth = 1280;

        /// <summary>
        /// Default height for the back buffer.
        /// </summary>
        public static readonly int DefaultBackBufferHeight = 720;

        private GameBase game;

        private bool deviceSettingsChanged;

        private bool isFullScreen;

        private bool preferMultiSampling;

        private PixelFormat preferredBackBufferFormat;

        private int preferredBackBufferHeight;

        private int preferredBackBufferWidth;

        private Rational preferredRefreshRate;

        private PixelFormat preferredDepthStencilFormat;

        private DisplayOrientation supportedOrientations;

        private bool synchronizeWithVerticalRetrace;

        private int preferredFullScreenOutputIndex;

        private bool isChangingDevice;

        private int resizedBackBufferWidth;

        private int resizedBackBufferHeight;

        private bool isBackBufferToResize = false;

        private DisplayOrientation currentWindowOrientation;

        private bool beginDrawOk;

        private IGraphicsDeviceFactory graphicsDeviceFactory;

        private bool isReallyFullScreen;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsDeviceManager" /> class.
        /// </summary>
        /// <param name="game">The game.</param>
        /// <exception cref="System.ArgumentNullException">The game instance cannot be null.</exception>
        internal GraphicsDeviceManager(GameBase game)
        {
            this.game = game;
            if (this.game == null)
            {
                throw new ArgumentNullException("game");
            }

            // Defines all default values
            SynchronizeWithVerticalRetrace = true;
            PreferredBackBufferFormat = PixelFormat.R8G8B8A8_UNorm;;
            PreferredDepthStencilFormat = PixelFormat.D24_UNorm_S8_UInt;
            preferredBackBufferWidth = DefaultBackBufferWidth;
            preferredBackBufferHeight = DefaultBackBufferHeight;
            preferredRefreshRate = new Rational(60, 1);
            PreferMultiSampling = false;
            PreferredGraphicsProfile = new[]
                {
#if SILICONSTUDIO_PLATFORM_WINDOWS_PHONE
                    GraphicsProfile.Level_9_3, 
#else
                    GraphicsProfile.Level_11_1, 
                    GraphicsProfile.Level_11_0, 
                    GraphicsProfile.Level_10_1, 
                    GraphicsProfile.Level_10_0, 
                    GraphicsProfile.Level_9_3, 
                    GraphicsProfile.Level_9_2, 
                    GraphicsProfile.Level_9_1, 
#endif
                };

            // Register the services to the registry
            game.Services.AddService(typeof(IGraphicsDeviceManager), this);
            game.Services.AddService(typeof(IGraphicsDeviceService), this);

            graphicsDeviceFactory = (IGraphicsDeviceFactory)game.Services.GetService(typeof(IGraphicsDeviceFactory));
            if (graphicsDeviceFactory == null)
            {
                throw new InvalidOperationException("IGraphicsDeviceFactory is not registered as a service");
            }

            game.WindowCreated += GameOnWindowCreated;
        }

        private void GameOnWindowCreated(object sender, EventArgs eventArgs)
        {
            game.Window.ClientSizeChanged += Window_ClientSizeChanged;
            game.Window.OrientationChanged += Window_OrientationChanged;
        }

        #endregion

        #region Public Events

        public event EventHandler<EventArgs> DeviceCreated;

        public event EventHandler<EventArgs> DeviceDisposing;

        public event EventHandler<EventArgs> DeviceReset;

        public event EventHandler<EventArgs> DeviceResetting;

        public event EventHandler<PreparingDeviceSettingsEventArgs> PreparingDeviceSettings;

        #endregion

        #region Public Properties

        public GraphicsDevice GraphicsDevice { get; internal set; }

        /// <summary>
        /// Gets or sets the list of graphics profile to select from the best feature to the lower feature. See remarks.
        /// </summary>
        /// <value>The graphics profile.</value>
        /// <remarks>
        /// By default, the PreferredGraphicsProfile is set to { <see cref="GraphicsProfile.Level_11_1"/>, 
        /// <see cref="GraphicsProfile.Level_11_0"/>,
        /// <see cref="GraphicsProfile.Level_10_1"/>,
        /// <see cref="GraphicsProfile.Level_10_0"/>,
        /// <see cref="GraphicsProfile.Level_9_3"/>,
        /// <see cref="GraphicsProfile.Level_9_2"/>,
        /// <see cref="GraphicsProfile.Level_9_1"/>}
        /// </remarks>
        public GraphicsProfile[] PreferredGraphicsProfile { get; set; }

        /// <summary>
        /// Gets or sets the shader graphics profile that will be used to compile shaders. See remarks.
        /// </summary>
        /// <value>The shader graphics profile.</value>
        /// <remarks>If this property is not set, the profile used to compile the shader will be taken from the <see cref="GraphicsDevice"/> 
        /// based on the list provided by <see cref="PreferredGraphicsProfile"/></remarks>
        public GraphicsProfile? ShaderProfile { get; set; }

        /// <summary>
        /// Gets or sets the device creation flags that will be used to create the <see cref="GraphicsDevice"/>
        /// </summary>
        /// <value>The device creation flags.</value>
        public DeviceCreationFlags DeviceCreationFlags { get; set; }

        /// <summary>
        /// Sets the preferred graphics profile.
        /// </summary>
        /// <param name="levels">The levels.</param>
        /// <seealso cref="PreferredGraphicsProfile"/>
        public void SetPreferredGraphicsProfile(params GraphicsProfile[] levels)
        {
            PreferredGraphicsProfile = levels;
        }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is full screen.
        /// </summary>
        /// <value><c>true</c> if this instance is full screen; otherwise, <c>false</c>.</value>
        public bool IsFullScreen
        {
            get
            {
                return isFullScreen;
            }

            set
            {
                if (isFullScreen != value)
                {
                    isFullScreen = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [prefer multi sampling].
        /// </summary>
        /// <value><c>true</c> if [prefer multi sampling]; otherwise, <c>false</c>.</value>
        public bool PreferMultiSampling
        {
            get
            {
                return preferMultiSampling;
            }

            set
            {
                if (preferMultiSampling != value)
                {
                    preferMultiSampling = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred back buffer format.
        /// </summary>
        /// <value>The preferred back buffer format.</value>
        public PixelFormat PreferredBackBufferFormat
        {
            get
            {
                return preferredBackBufferFormat;
            }

            set
            {
                if (preferredBackBufferFormat != value)
                {
                    preferredBackBufferFormat = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the height of the preferred back buffer.
        /// </summary>
        /// <value>The height of the preferred back buffer.</value>
        public int PreferredBackBufferHeight
        {
            get
            {
                return preferredBackBufferHeight;
            }

            set
            {
                if (preferredBackBufferHeight != value)
                {
                    preferredBackBufferHeight = value;
                    isBackBufferToResize = false;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the width of the preferred back buffer.
        /// </summary>
        /// <value>The width of the preferred back buffer.</value>
        public int PreferredBackBufferWidth
        {
            get
            {
                return preferredBackBufferWidth;
            }

            set
            {
                if (preferredBackBufferWidth != value)
                {
                    preferredBackBufferWidth = value;
                    isBackBufferToResize = false;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred depth stencil format.
        /// </summary>
        /// <value>The preferred depth stencil format.</value>
        public PixelFormat PreferredDepthStencilFormat
        {
            get
            {
                return preferredDepthStencilFormat;
            }

            set
            {
                if (preferredDepthStencilFormat != value)
                {
                    preferredDepthStencilFormat = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the preferred refresh rate.
        /// </summary>
        /// <value>The preferred refresh rate.</value>
        public Rational PreferredRefreshRate
        {
            get
            {
                return preferredRefreshRate;
            }

            set
            {
                if (preferredRefreshRate != value)
                {
                    preferredRefreshRate = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// The output (monitor) index to use when switching to fullscreen mode. Doesn't have any effect when windowed mode is used.
        /// </summary>
        public int PreferredFullScreenOutputIndex
        {
            get
            {
                return preferredFullScreenOutputIndex;
            }

            set
            {
                if (preferredFullScreenOutputIndex != value)
                {
                    preferredFullScreenOutputIndex = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets the supported orientations.
        /// </summary>
        /// <value>The supported orientations.</value>
        public DisplayOrientation SupportedOrientations
        {
            get
            {
                return supportedOrientations;
            }

            set
            {
                if (supportedOrientations != value)
                {
                    supportedOrientations = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether [synchronize with vertical retrace].
        /// </summary>
        /// <value><c>true</c> if [synchronize with vertical retrace]; otherwise, <c>false</c>.</value>
        public bool SynchronizeWithVerticalRetrace
        {
            get
            {
                return synchronizeWithVerticalRetrace;
            }
            set
            {
                if (synchronizeWithVerticalRetrace != value)
                {
                    synchronizeWithVerticalRetrace = value;
                    deviceSettingsChanged = true;
                }
            }
        }

        #endregion

        #region Public Methods and Operators

        /// <summary>
        /// Applies the changes from this instance and change or create the <see cref="GraphicsDevice"/> according to the new values.
        /// </summary>
        public void ApplyChanges()
        {
            if (GraphicsDevice == null || deviceSettingsChanged)
            {
                ChangeOrCreateDevice(false);
            }
        }

        bool IGraphicsDeviceManager.BeginDraw()
        {
            if (GraphicsDevice == null)
            {
                return false;
            }

            beginDrawOk = false;

            if (!CheckDeviceState())
                return false;

            GraphicsDevice.Begin();

            // Before drawing, we should clear the state to make sure that there is no unstable graphics device states (On some WP8 devices for example)
            // An application should not rely on previous state (last frame...etc.) after BeginDraw.
            GraphicsDevice.ClearState();

            // By default, we setup the render target to the back buffer, and the viewport as well.
            if (GraphicsDevice.BackBuffer != null)
            {
                GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);
            }

            beginDrawOk = true;
            return beginDrawOk;
        }

        private bool CheckDeviceState()
        {
            switch (GraphicsDevice.GraphicsDeviceStatus)
            {
                case GraphicsDeviceStatus.Removed:
                    Utilities.Sleep(TimeSpan.FromMilliseconds(20));
                    return false;
                case GraphicsDeviceStatus.Reset:
                    Utilities.Sleep(TimeSpan.FromMilliseconds(20));
                    try
                    {
                        ChangeOrCreateDevice(true);
                    }
                    catch (Exception)
                    {
                        return false;
                    }
                    break;
            }

            return true;
        }

        void IGraphicsDeviceManager.CreateDevice()
        {
            // Force the creation of the device
            ChangeOrCreateDevice(true);
        }

        void IGraphicsDeviceManager.EndDraw(bool present)
        {
            if (beginDrawOk && GraphicsDevice != null)
            {
                if (present && GraphicsDevice.Presenter != null)
                {
                    try
                    {
                        GraphicsDevice.Presenter.Present();
                    }
                    catch (GraphicsException ex)
                    {
                        // If this is not a DeviceRemoved or DeviceReset, than throw an exception
                        if (ex.Status != GraphicsDeviceStatus.Removed && ex.Status != GraphicsDeviceStatus.Reset)
                        {
                            throw;
                        }
                    }
                    finally
                    {
                        beginDrawOk = false;
                        GraphicsDevice.End();
                    }
                }
            }
        }

        #endregion

        protected static DisplayOrientation SelectOrientation(DisplayOrientation orientation, int width, int height, bool allowLandscapeLeftAndRight)
        {
            if (orientation != DisplayOrientation.Default)
            {
                return orientation;
            }

            if (width <= height)
            {
                return DisplayOrientation.Portrait;
            }

            if (allowLandscapeLeftAndRight)
            {
                return DisplayOrientation.LandscapeRight | DisplayOrientation.LandscapeLeft;
            }

            return DisplayOrientation.LandscapeRight;
        }

        protected override void Destroy()
        {
            if (game != null)
            {
                if (game.Services.GetService(typeof(IGraphicsDeviceService)) == this)
                {
                    game.Services.RemoveService(typeof(IGraphicsDeviceService));
                }

                game.Window.ClientSizeChanged -= Window_ClientSizeChanged;
                game.Window.OrientationChanged -= Window_OrientationChanged;
            }

            if (GraphicsDevice != null)
            {
                if (GraphicsDevice.Presenter != null)
                {
                    // Make sure that the Presenter is reverted to window before shuting down
                    // otherwise the Direct3D11.Device will generate an exception on Dispose()
                    GraphicsDevice.Presenter.IsFullScreen = false;
                    GraphicsDevice.Presenter.Dispose();
                    GraphicsDevice.Presenter = null;
                }

                GraphicsDevice.Dispose();
                GraphicsDevice = null;
            }

            base.Destroy();
        }

        /// <summary>
        /// Determines whether this instance is compatible with the the specified new <see cref="GraphicsDeviceInformation"/>.
        /// </summary>
        /// <param name="newDeviceInfo">The new device info.</param>
        /// <returns><c>true</c> if this instance this instance is compatible with the the specified new <see cref="GraphicsDeviceInformation"/>; otherwise, <c>false</c>.</returns>
        protected virtual bool CanResetDevice(GraphicsDeviceInformation newDeviceInfo)
        {
            // By default, a reset is compatible when we stay under the same graphics profile.
            return GraphicsDevice.Features.Profile == newDeviceInfo.GraphicsProfile;
        }

        /// <summary>
        /// Finds the best device that is compatible with the preferences defined in this instance.
        /// </summary>
        /// <param name="anySuitableDevice">if set to <c>true</c> a device can be selected from any existing adapters, otherwise, it will select only from default adapter.</param>
        /// <returns>The graphics device information.</returns>
        protected virtual GraphicsDeviceInformation FindBestDevice(bool anySuitableDevice)
        {
            // Setup preferred parameters before passing them to the factory
            var preferredParameters = new GameGraphicsParameters
                {
                    PreferredBackBufferWidth = PreferredBackBufferWidth,
                    PreferredBackBufferHeight = PreferredBackBufferHeight,
                    PreferredBackBufferFormat = PreferredBackBufferFormat,
                    PreferredDepthStencilFormat = PreferredDepthStencilFormat,
                    PreferredRefreshRate =  PreferredRefreshRate,
                    PreferredFullScreenOutputIndex = PreferredFullScreenOutputIndex,
                    IsFullScreen = IsFullScreen,
                    PreferMultiSampling = PreferMultiSampling,
                    SynchronizeWithVerticalRetrace = SynchronizeWithVerticalRetrace,
                    PreferredGraphicsProfile = (GraphicsProfile[])PreferredGraphicsProfile.Clone(),
                };

            // Setup resized value if there is a resize pending
            if (!IsFullScreen && isBackBufferToResize)
            {
                preferredParameters.PreferredBackBufferWidth = resizedBackBufferWidth;
                preferredParameters.PreferredBackBufferHeight = resizedBackBufferHeight;
            }

            var devices = graphicsDeviceFactory.FindBestDevices(preferredParameters);
            if (devices.Count == 0)
            {
                throw new InvalidOperationException("No screen modes found");
            }

            RankDevices(devices);

            if (devices.Count == 0)
            {
                throw new InvalidOperationException("No screen modes found after ranking");
            }
            return devices[0];
        }

        /// <summary>
        /// Ranks a list of <see cref="GraphicsDeviceInformation"/> before creating a new device.
        /// </summary>
        /// <param name="foundDevices">The list of devices that can be reorder.</param>
        protected virtual void RankDevices(List<GraphicsDeviceInformation> foundDevices)
        {
            // Don't sort if there is a single device (mostly for XAML/WP8)
            if (foundDevices.Count == 1)
            {
                return;
            }

            foundDevices.Sort(
                (left, right) =>
                    {
                        var leftParams = left.PresentationParameters;
                        var rightParams = right.PresentationParameters;

                        var leftAdapter = left.Adapter;
                        var rightAdapter = right.Adapter;

                        // Sort by GraphicsProfile
                        if (left.GraphicsProfile != right.GraphicsProfile)
                        {
                            return left.GraphicsProfile <= right.GraphicsProfile ? 1 : -1;
                        }

                        // Sort by FullScreen mode
                        if (leftParams.IsFullScreen != rightParams.IsFullScreen)
                        {
                            return IsFullScreen != leftParams.IsFullScreen ? 1 : -1;
                        }

                        // Sort by BackBufferFormat
                        int leftFormat = CalculateRankForFormat(leftParams.BackBufferFormat);
                        int rightFormat = CalculateRankForFormat(rightParams.BackBufferFormat);
                        if (leftFormat != rightFormat)
                        {
                            return leftFormat >= rightFormat ? 1 : -1;
                        }

                        // Sort by MultiSampleCount
                        if (leftParams.MultiSampleCount != rightParams.MultiSampleCount)
                        {
                            return leftParams.MultiSampleCount <= rightParams.MultiSampleCount ? 1 : -1;
                        }

                        // Sort by AspectRatio
                        var targetAspectRatio = (PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0) ? (float)DefaultBackBufferWidth / DefaultBackBufferHeight : (float)PreferredBackBufferWidth / PreferredBackBufferHeight;
                        var leftDiffRatio = Math.Abs(((float)leftParams.BackBufferWidth / leftParams.BackBufferHeight) - targetAspectRatio);
                        var rightDiffRatio = Math.Abs(((float)rightParams.BackBufferWidth / rightParams.BackBufferHeight) - targetAspectRatio);
                        if (Math.Abs(leftDiffRatio - rightDiffRatio) > 0.2f)
                        {
                            return leftDiffRatio >= rightDiffRatio ? 1 : -1;
                        }

                        // Sort by PixelCount
                        int leftPixelCount;
                        int rightPixelCount;
                        if (IsFullScreen)
                        {
                            if ( ((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0)) &&
                                PreferredFullScreenOutputIndex < leftAdapter.Outputs.Length && 
                                PreferredFullScreenOutputIndex < rightAdapter.Outputs.Length)
                            {
                                // assume we got here only adapters that have the needed number of outputs:
                                var leftOutput = leftAdapter.Outputs[PreferredFullScreenOutputIndex];
                                var rightOutput = rightAdapter.Outputs[PreferredFullScreenOutputIndex];

                                leftPixelCount = leftOutput.CurrentDisplayMode.Width * leftOutput.CurrentDisplayMode.Height;
                                rightPixelCount = rightOutput.CurrentDisplayMode.Width * rightOutput.CurrentDisplayMode.Height;
                            }
                            else
                            {
                                leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                            }
                        }
                        else if ((PreferredBackBufferWidth == 0) || (PreferredBackBufferHeight == 0))
                        {
                            leftPixelCount = rightPixelCount = DefaultBackBufferWidth * DefaultBackBufferHeight;
                        }
                        else
                        {
                            leftPixelCount = rightPixelCount = PreferredBackBufferWidth * PreferredBackBufferHeight;
                        }

                        int leftDeltaPixelCount = Math.Abs((leftParams.BackBufferWidth * leftParams.BackBufferHeight) - leftPixelCount);
                        int rightDeltaPixelCount = Math.Abs((rightParams.BackBufferWidth * rightParams.BackBufferHeight) - rightPixelCount);
                        if (leftDeltaPixelCount != rightDeltaPixelCount)
                        {
                            return leftDeltaPixelCount >= rightDeltaPixelCount ? 1 : -1;
                        }

                        // Sort by default Adapter, default adapter first
                        if (left.Adapter != right.Adapter)
                        {
                            if (left.Adapter.IsDefaultAdapter)
                            {
                                return -1;
                            }

                            if (right.Adapter.IsDefaultAdapter)
                            {
                                return 1;
                            }
                        }

                        return 0;
                    });
        }

        private int CalculateRankForFormat(PixelFormat format)
        {
            if (format == PreferredBackBufferFormat)
            {
                return 0;
            }

            if (CalculateFormatSize(format) == CalculateFormatSize(PreferredBackBufferFormat))
            {
                return 1;
            }

            return int.MaxValue;
        }
        
        private int CalculateFormatSize(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R8G8B8A8_UNorm:
                case PixelFormat.R8G8B8A8_UNorm_SRgb:
                case PixelFormat.B8G8R8A8_UNorm:
                case PixelFormat.B8G8R8A8_UNorm_SRgb:
                case PixelFormat.R10G10B10A2_UNorm:
                    return 32;

                case PixelFormat.B5G6R5_UNorm:
                case PixelFormat.B5G5R5A1_UNorm:
                    return 16;
            }

            return 0;
        }

        protected virtual void OnDeviceCreated(object sender, EventArgs args)
        {
            var handler = DeviceCreated;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        protected virtual void OnDeviceDisposing(object sender, EventArgs args)
        {
            var handler = DeviceDisposing;
            if (handler != null)
            {
                handler(sender, args);
            }
        }
        
        protected virtual void OnDeviceReset(object sender, EventArgs args)
        {
            var handler = DeviceReset;
            if (handler != null)
            {
                handler(sender, args);
            }
        }
        
        protected virtual void OnDeviceResetting(object sender, EventArgs args)
        {
            var handler = DeviceResetting;
            if (handler != null)
            {
                handler(sender, args);
            }
        }
        
        protected virtual void OnPreparingDeviceSettings(object sender, PreparingDeviceSettingsEventArgs args)
        {
            var handler = PreparingDeviceSettings;
            if (handler != null)
            {
                handler(sender, args);
            }
        }

        private void Window_ClientSizeChanged(object sender, EventArgs e)
        {
            if (!isChangingDevice && ((game.Window.ClientBounds.Height != 0) || (game.Window.ClientBounds.Width != 0)))
            {
                resizedBackBufferWidth = game.Window.ClientBounds.Width;
                resizedBackBufferHeight = game.Window.ClientBounds.Height;
                isBackBufferToResize = true;
                ChangeOrCreateDevice(false);
            }
        }

        void Window_OrientationChanged(object sender, EventArgs e)
        {
            if ((!isChangingDevice && ((game.Window.ClientBounds.Height != 0) || (game.Window.ClientBounds.Width != 0))) && (game.Window.CurrentOrientation != currentWindowOrientation))
            {
                ChangeOrCreateDevice(false);
            }
        }


        private void CreateDevice(GraphicsDeviceInformation newInfo)
        {
            newInfo.PresentationParameters.IsFullScreen = isFullScreen;
            newInfo.PresentationParameters.PresentationInterval = SynchronizeWithVerticalRetrace ? PresentInterval.One : PresentInterval.Immediate;
            newInfo.DeviceCreationFlags = DeviceCreationFlags;

            OnPreparingDeviceSettings(this, new PreparingDeviceSettingsEventArgs(newInfo));

            // this.ValidateGraphicsDeviceInformation(newInfo);

            bool deviceRecreate = GraphicsDevice != null;

            // Notify device is resetting (usually this should result in graphics resources being destroyed)
            if (deviceRecreate)
                OnDeviceResetting(this, EventArgs.Empty);

            // Create (or recreate) the graphics device
            GraphicsDevice = graphicsDeviceFactory.ChangeOrCreateDevice(GraphicsDevice, newInfo);

            // Notify device is reset (usually this should result in graphics resources being recreated/reloaded)
            if (deviceRecreate)
                OnDeviceReset(this, EventArgs.Empty);

            GraphicsDevice.ShaderProfile = ShaderProfile;

            // TODO HANDLE Device Resetting/Reset/Lost
            //GraphicsDevice.DeviceResetting += GraphicsDevice_DeviceResetting;
            //GraphicsDevice.DeviceReset += GraphicsDevice_DeviceReset;
            //GraphicsDevice.DeviceLost += GraphicsDevice_DeviceLost;
            if (!deviceRecreate)
                GraphicsDevice.Disposing += GraphicsDevice_Disposing;

            OnDeviceCreated(this, EventArgs.Empty);
        }

        void GraphicsDevice_DeviceResetting(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        void GraphicsDevice_DeviceReset(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        void GraphicsDevice_DeviceLost(object sender, EventArgs e)
        {
            // TODO what to do?
        }

        void GraphicsDevice_Disposing(object sender, EventArgs e)
        {
            OnDeviceDisposing(sender, e);
        }

        private void ChangeOrCreateDevice(bool forceCreate)
        {
            using (var profile = Profiler.Begin(GraphicsDeviceManagerProfilingKeys.CreateDevice))
            {
                isChangingDevice = true;
                int width = game.Window.ClientBounds.Width;
                int height = game.Window.ClientBounds.Height;

                bool isBeginScreenDeviceChange = false;
                try
                {
                    // Notifies the game window for the new orientation
                    game.Window.SetSupportedOrientations(SelectOrientation(supportedOrientations, PreferredBackBufferWidth, PreferredBackBufferHeight, true));

                    var graphicsDeviceInformation = FindBestDevice(forceCreate);
                    game.Window.BeginScreenDeviceChange(graphicsDeviceInformation.PresentationParameters.IsFullScreen);
                    isBeginScreenDeviceChange = true;
                    bool needToCreateNewDevice = true;

                    // If we are not forced to create a new device and this is already an existing GraphicsDevice
                    // try to reset and resize it.
                    if (!forceCreate && GraphicsDevice != null)
                    {
                        OnPreparingDeviceSettings(this, new PreparingDeviceSettingsEventArgs(graphicsDeviceInformation));
                        if (CanResetDevice(graphicsDeviceInformation))
                        {
                            try
                            {
                                var newWidth = graphicsDeviceInformation.PresentationParameters.BackBufferWidth;
                                var newHeight = graphicsDeviceInformation.PresentationParameters.BackBufferHeight;
                                var newFormat = graphicsDeviceInformation.PresentationParameters.BackBufferFormat;
                                var newOutputIndex = graphicsDeviceInformation.PresentationParameters.PreferredFullScreenOutputIndex;

                                GraphicsDevice.Presenter.Description.PreferredFullScreenOutputIndex = newOutputIndex;
                                GraphicsDevice.Presenter.Description.RefreshRate = graphicsDeviceInformation.PresentationParameters.RefreshRate;
                                GraphicsDevice.Presenter.Resize(newWidth, newHeight, newFormat);

                                // Change full screen if needed
                                GraphicsDevice.Presenter.IsFullScreen = graphicsDeviceInformation.PresentationParameters.IsFullScreen;

                                needToCreateNewDevice = false;
                            }
                            catch
                            {
                            }
                        }
                    }

                    // If we still need to create a device, then we need to create it
                    if (needToCreateNewDevice)
                    {
                        CreateDevice(graphicsDeviceInformation);
                    }

                    var presentationParameters = GraphicsDevice.Presenter.Description;
                    isReallyFullScreen = presentationParameters.IsFullScreen;
                    if (presentationParameters.BackBufferWidth != 0)
                    {
                        width = presentationParameters.BackBufferWidth;
                    }

                    if (presentationParameters.BackBufferHeight != 0)
                    {
                        height = presentationParameters.BackBufferHeight;
                    }

                    deviceSettingsChanged = false;
                }
                finally
                {
                    if (isBeginScreenDeviceChange)
                    {
                        game.Window.EndScreenDeviceChange(width, height);
                    }

                    currentWindowOrientation = game.Window.CurrentOrientation;
                    isChangingDevice = false;
                }
            }
        }
    }
}