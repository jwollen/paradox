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
using System.Reflection;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Paradox.Games.Time;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization.Assets;

namespace SiliconStudio.Paradox.Games
{
    /// <summary>
    /// The game.
    /// </summary>
    public abstract class GameBase : ComponentBase, IGame
    {
        #region Fields

        private readonly Dictionary<object, ProfilingKey> updateProfilers = new Dictionary<object, ProfilingKey>();
        private readonly Dictionary<object, ProfilingKey> drawProfilers = new Dictionary<object, ProfilingKey>();
        private readonly GameTime updateTime;
        private readonly GameTime drawTime;
        private readonly TimerTick playTimer;
        private readonly TimerTick updateTimer;
        private readonly int[] lastUpdateCount;
        private readonly float updateCountAverageSlowLimit;
        private readonly GamePlatform gamePlatform;
        private ProfilingState profilingDraw;
        private TimeSpan singleFrameUpdateTime;
        private IGraphicsDeviceService graphicsDeviceService;
        protected IGraphicsDeviceManager graphicsDeviceManager;
        private ResumeManager resumeManager;
        private bool isEndRunRequired;
        private bool isExiting;
        private bool suppressDraw;

        private TimeSpan totalUpdateTime;
        private TimeSpan totalDrawTime;
        private readonly TimeSpan maximumElapsedTime;
        private TimeSpan accumulatedElapsedGameTime;
        private TimeSpan lastFrameElapsedGameTime;
        private int nextLastUpdateCountIndex;
        private bool drawRunningSlowly;
        private bool forceElapsedTimeToZero;

        private readonly TimerTick timer;

        protected readonly ILogger Log;

        private bool isMouseVisible;

        internal bool SlowDownDrawCalls;

        #endregion

        #region Constructors and Destructors

        /// <summary>
        /// Initializes a new instance of the <see cref="GameBase" /> class.
        /// </summary>
        protected GameBase()
        {
            // Internals
            Log = GlobalLogger.GetLogger(GetType().GetTypeInfo().Name);
            updateTime = new GameTime();
            drawTime = new GameTime();
            playTimer = new TimerTick();
            updateTimer = new TimerTick();
            totalUpdateTime = new TimeSpan();
            timer = new TimerTick();
            IsFixedTimeStep = false;
            maximumElapsedTime = TimeSpan.FromMilliseconds(500.0);
            TargetElapsedTime = TimeSpan.FromTicks(10000000 / 60); // target elapsed time is by default 60Hz
            lastUpdateCount = new int[4];
            nextLastUpdateCountIndex = 0;

            // Calculate the updateCountAverageSlowLimit (assuming moving average is >=3 )
            // Example for a moving average of 4:
            // updateCountAverageSlowLimit = (2 * 2 + (4 - 2)) / 4 = 1.5f
            const int BadUpdateCountTime = 2; // number of bad frame (a bad frame is a frame that has at least 2 updates)
            var maxLastCount = 2 * Math.Min(BadUpdateCountTime, lastUpdateCount.Length);
            updateCountAverageSlowLimit = (float)(maxLastCount + (lastUpdateCount.Length - maxLastCount)) / lastUpdateCount.Length;

            // Externals
            Services = new ServiceRegistry();

            // Asset manager
            Asset = new AssetManager(Services);

            LaunchParameters = new LaunchParameters();
            GameSystems = new GameSystemCollection();

            // Create Platform
            gamePlatform = GamePlatform.Create(this);
            gamePlatform.Activated += gamePlatform_Activated;
            gamePlatform.Deactivated += gamePlatform_Deactivated;
            gamePlatform.Exiting += gamePlatform_Exiting;
            gamePlatform.WindowCreated += GamePlatformOnWindowCreated;

            // Setup registry
            Services.AddService(typeof(IGame), this);
            Services.AddService(typeof(IGamePlatform), gamePlatform);

            IsActive = true;
        }

        #endregion

        #region Public Events

        /// <summary>
        /// Occurs when [activated].
        /// </summary>
        public event EventHandler<EventArgs> Activated;

        /// <summary>
        /// Occurs when [deactivated].
        /// </summary>
        public event EventHandler<EventArgs> Deactivated;

        /// <summary>
        /// Occurs when [exiting].
        /// </summary>
        public event EventHandler<EventArgs> Exiting;

        /// <summary>
        /// Occurs when [window created].
        /// </summary>
        public event EventHandler<EventArgs> WindowCreated;

        public event EventHandler<GameUnhandledExceptionEventArgs> UnhandledException;

        #endregion

        #region Public Properties

        /// <summary>
        /// Gets the current update time from the start of the game.
        /// </summary>
        /// <value>The current update time.</value>
        public GameTime UpdateTime
        {
            get
            {
                return updateTime;
            }
        }

        /// <summary>
        /// Gets the current draw time from the start of the game.
        /// </summary>
        /// <value>The current update time.</value>
        public GameTime DrawTime
        {
            get
            {
                return drawTime;
            }
        }

        /// <summary>
        /// Gets the draw interpolation factor, which is (<see cref="UpdateTime"/> - <see cref="DrawTime"/>) / <see cref="TargetElapsedTime"/>.
        /// If <see cref="IsFixedTimeStep"/> is false, it will be 0 as <see cref="UpdateTime"/> and <see cref="DrawTime"/> will be equal.
        /// </summary>
        /// <value>
        /// The draw interpolation factor.
        /// </value>
        public float DrawInterpolationFactor { get; private set; }

        /// <summary>
        /// Gets the play time, can be changed to match to the time of the current rendering scene.
        /// </summary>
        /// <value>The play time.</value>
        public TimerTick PlayTime
        {
            get
            {
                return playTimer;
            }
        }

        /// <summary>
        /// Gets or sets the <see cref="AssetManager"/>.
        /// </summary>
        /// <value>The content manager.</value>
        public AssetManager Asset { get; private set; }

        /// <summary>
        /// Gets the game components registered by this game.
        /// </summary>
        /// <value>The game components.</value>
        public GameSystemCollection GameSystems { get; private set; }

        /// <summary>
        /// Gets the game context.
        /// </summary>
        /// <value>The game context.</value>
        public GameContext Context { get; private set; }

        /// <summary>
        /// Gets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice
        {
            get
            {
                if (graphicsDeviceService == null)
                {
                    throw new InvalidOperationException("GraphicsDeviceService is not yet initialized");
                }

                return graphicsDeviceService.GraphicsDevice;
            }
        }

        /// <summary>
        /// Gets or sets the inactive sleep time.
        /// </summary>
        /// <value>The inactive sleep time.</value>
        public TimeSpan InactiveSleepTime { get; set; }

        /// <summary>
        /// Gets a value indicating whether this instance is active.
        /// </summary>
        /// <value><c>true</c> if this instance is active; otherwise, <c>false</c>.</value>
        public bool IsActive { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is fixed time step.
        /// </summary>
        /// <value><c>true</c> if this instance is fixed time step; otherwise, <c>false</c>.</value>
        public bool IsFixedTimeStep { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether draw can happen as fast as possible, even when <see cref="IsFixedTimeStep"/> is set.
        /// </summary>
        /// <value><c>true</c> if this instance allows desychronized drawing; otherwise, <c>false</c>.</value>
        public bool IsDrawDesynchronized { get; set; }

        public bool EarlyExit { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether the mouse should be visible.
        /// </summary>
        /// <value><c>true</c> if the mouse should be visible; otherwise, <c>false</c>.</value>
        public bool IsMouseVisible
        {
            get
            {
                return isMouseVisible;
            }

            set
            {
                isMouseVisible = value;
                if (Window != null)
                {
                    Window.IsMouseVisible = value;
                }
            }
        }

        /// <summary>
        /// Gets the launch parameters.
        /// </summary>
        /// <value>The launch parameters.</value>
        public LaunchParameters LaunchParameters { get; private set; }

        /// <summary>
        /// Gets a value indicating whether is running.
        /// </summary>
        public bool IsRunning { get; private set; }

        /// <summary>
        /// Gets the service container.
        /// </summary>
        /// <value>The service container.</value>
        public ServiceRegistry Services { get; private set; }

        /// <summary>
        /// Gets or sets the target elapsed time.
        /// </summary>
        /// <value>The target elapsed time.</value>
        public TimeSpan TargetElapsedTime { get; set; }

        /// <summary>
        /// Gets the abstract window.
        /// </summary>
        /// <value>The window.</value>
        public GameWindow Window
        {
            get
            {
                if (gamePlatform != null)
                {
                    return gamePlatform.MainWindow;
                }
                return null;
            }
        }

        public GameState State { get; set; }
        
        #endregion

        internal EventHandler<GameUnhandledExceptionEventArgs> UnhandledExceptionInternal
        {
            get { return UnhandledException; }
        }

        #region Public Methods and Operators

        /// <summary>
        /// Exits the game.
        /// </summary>
        public void Exit()
        {
            isExiting = true;
            gamePlatform.Exit();
        }

        /// <summary>
        /// Resets the elapsed time counter.
        /// </summary>
        public void ResetElapsedTime()
        {
            forceElapsedTimeToZero = true;
            drawRunningSlowly = false;
            Array.Clear(lastUpdateCount, 0, lastUpdateCount.Length);
            nextLastUpdateCountIndex = 0;
        }

        internal void InitializeBeforeRun()
        {
            try
            {
                using (var profile = Profiler.Begin(GameProfilingKeys.GameInitialize))
                {
                    // Initialize this instance and all game systems before trying to create the device.
                    Initialize();

                    // Make sure that the device is already created
                    graphicsDeviceManager.CreateDevice();

                    // Gets the graphics device service
                    graphicsDeviceService = Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;
                    if (graphicsDeviceService == null)
                    {
                        throw new InvalidOperationException("No GraphicsDeviceService found");
                    }

                    // Checks the graphics device
                    if (graphicsDeviceService.GraphicsDevice == null)
                    {
                        throw new InvalidOperationException("No GraphicsDevice found");
                    }

                    // Setup the graphics device if it was not already setup.
                    SetupGraphicsDeviceEvents();

                    // Bind Graphics Context enabling initialize to use GL API eg. SetData to texture ...etc
                    BeginDraw();

                    LoadContentInternal();

                    IsRunning = true;

                    BeginRun();

                    timer.Reset();
                    updateTime.Reset(totalUpdateTime);

                    // Run the first time an update
                    updateTimer.Reset();
                    using (Profiler.Begin(GameProfilingKeys.GameUpdate))
                    {
                        Update(updateTime);
                    }
                    updateTimer.Tick();
                    singleFrameUpdateTime += updateTimer.ElapsedTime;

                    // Reset PlayTime
                    playTimer.Reset();

                    // Unbind Graphics Context without presenting
                    EndDraw(false);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                throw;
            }
        }

        /// <summary>
        /// Call this method to initialize the game, begin running the game loop, and start processing events for the game.
        /// </summary>
        /// <param name="gameContext">The window Context for this game.</param>
        /// <exception cref="System.InvalidOperationException">Cannot run this instance while it is already running</exception>
        public void Run(GameContext gameContext = null)
        {
            if (IsRunning)
            {
                throw new InvalidOperationException("Cannot run this instance while it is already running");
            }

            // Gets the graphics device manager
            graphicsDeviceManager = Services.GetService(typeof(IGraphicsDeviceManager)) as IGraphicsDeviceManager;
            if (graphicsDeviceManager == null)
            {
                throw new InvalidOperationException("No GraphicsDeviceManager found");
            }

            // Gets the GameWindow Context
            Context = gameContext ?? new GameContext();

            PrepareRun();

            try
            {
                // TODO temporary workaround as the engine doesn't support yet resize
                var graphicsDeviceManagerImpl = (GraphicsDeviceManager) graphicsDeviceManager;
                Context.RequestedWidth = graphicsDeviceManagerImpl.PreferredBackBufferWidth;
                Context.RequestedHeight = graphicsDeviceManagerImpl.PreferredBackBufferHeight;
                Context.RequestedBackBufferFormat = graphicsDeviceManagerImpl.PreferredBackBufferFormat;
                Context.RequestedDepthStencilFormat = graphicsDeviceManagerImpl.PreferredDepthStencilFormat;
                Context.RequestedGraphicsProfile = graphicsDeviceManagerImpl.PreferredGraphicsProfile;

                gamePlatform.Run(Context);

                if (gamePlatform.IsBlockingRun)
                {
                    // If the previous call was blocking, then we can call Endrun
                    EndRun();
                }
                else
                {
                    // EndRun will be executed on Game.Exit
                    isEndRunRequired = true;
                }
            }
            finally
            {
                if (!isEndRunRequired)
                {
                    IsRunning = false;
                }
            }
        }

        internal protected virtual void PrepareRun()
        {
        }

        /// <summary>
        /// Prevents calls to Draw until the next Update.
        /// </summary>
        public void SuppressDraw()
        {
            suppressDraw = true;
        }

        /// <summary>
        /// Updates the game's clock and calls Update and Draw.
        /// </summary>
        public void Tick()
        {
            try
            {

                // If this instance is existing, then don't make any further update/draw
                if (isExiting)
                {
                    CheckEndRun();
                    return;
                }

                // If this instance is not active, sleep for an inactive sleep time
                if (!IsActive)
                {
                    Utilities.Sleep(InactiveSleepTime);
                    return;
                }

                // Update the timer
                if (updateTime.FrameCount < 2) //-> delay timer reset after first draw to avoid important gap in game time space
                {
                    timer.Reset();
                }
                timer.Tick();

                // Update the playTimer timer
                playTimer.Tick();

                // Measure updateTimer
                updateTimer.Reset();

                var elapsedAdjustedTime = timer.ElapsedTimeWithPause;

                if (forceElapsedTimeToZero)
                {
                    elapsedAdjustedTime = TimeSpan.Zero;
                    forceElapsedTimeToZero = false;
                }

                if (elapsedAdjustedTime > maximumElapsedTime)
                {
                    elapsedAdjustedTime = maximumElapsedTime;
                }

                bool suppressNextDraw = true;
                int updateCount = 1;
                var singleFrameElapsedTime = elapsedAdjustedTime;
                var drawLag = 0L;

                if (IsFixedTimeStep)
                {
                    // If the rounded TargetElapsedTime is equivalent to current ElapsedAdjustedTime
                    // then make ElapsedAdjustedTime = TargetElapsedTime. We take the same internal rules as XNA 
                    if (Math.Abs(elapsedAdjustedTime.Ticks - TargetElapsedTime.Ticks) < (TargetElapsedTime.Ticks >> 6))
                    {
                        elapsedAdjustedTime = TargetElapsedTime;
                    }

                    // Update the accumulated time
                    accumulatedElapsedGameTime += elapsedAdjustedTime;

                    // Calculate the number of update to issue
                    updateCount = (int)(accumulatedElapsedGameTime.Ticks/TargetElapsedTime.Ticks);

                    if (IsDrawDesynchronized)
                    {
                        drawLag = accumulatedElapsedGameTime.Ticks%TargetElapsedTime.Ticks;
                        suppressNextDraw = false;
                    }
                    else if (updateCount == 0)
                    {
                        // If there is no need for update, then exit
                        return;
                    }

                    // Calculate a moving average on updateCount
                    lastUpdateCount[nextLastUpdateCountIndex] = updateCount;
                    float updateCountMean = 0;
                    for (int i = 0; i < lastUpdateCount.Length; i++)
                    {
                        updateCountMean += lastUpdateCount[i];
                    }

                    updateCountMean /= lastUpdateCount.Length;
                    nextLastUpdateCountIndex = (nextLastUpdateCountIndex + 1)%lastUpdateCount.Length;

                    // Test when we are running slowly
                    drawRunningSlowly = updateCountMean > updateCountAverageSlowLimit;

                    // We are going to call Update updateCount times, so we can substract this from accumulated elapsed game time
                    accumulatedElapsedGameTime = new TimeSpan(accumulatedElapsedGameTime.Ticks - (updateCount*TargetElapsedTime.Ticks));
                    singleFrameElapsedTime = TargetElapsedTime;
                }
                else
                {
                    Array.Clear(lastUpdateCount, 0, lastUpdateCount.Length);
                    nextLastUpdateCountIndex = 0;
                    drawRunningSlowly = false;
                }

                bool beginDrawSuccessful = false;
                try
                {
                    beginDrawSuccessful = BeginDraw();

                    // Reset the time of the next frame
                    for (lastFrameElapsedGameTime = TimeSpan.Zero; updateCount > 0 && !isExiting; updateCount--)
                    {
                        updateTime.Update(totalUpdateTime, singleFrameElapsedTime, singleFrameUpdateTime, drawRunningSlowly, true);
                        try
                        {
                            UpdateAndProfile(updateTime);
                            if (EarlyExit)
                                return;

                            // If there is no exception, then we can draw the frame
                            suppressNextDraw &= suppressDraw;
                            suppressDraw = false;
                        }
                        finally
                        {
                            lastFrameElapsedGameTime += singleFrameElapsedTime;
                            totalUpdateTime += singleFrameElapsedTime;
                        }
                    }

                    // End measuring update time
                    updateTimer.Tick();
                    singleFrameUpdateTime += updateTimer.ElapsedTime;

                    // Update game time just before calling draw
                    //updateTime.Update(totalUpdateTime, singleFrameElapsedTime, singleFrameUpdateTime, drawRunningSlowly, true);

                    if (!suppressNextDraw)
                    {
                        totalDrawTime = TimeSpan.FromTicks(totalUpdateTime.Ticks + drawLag);
                        DrawInterpolationFactor = drawLag/(float)TargetElapsedTime.Ticks;
                        DrawFrame();
                    }

                    singleFrameUpdateTime = TimeSpan.Zero;
                }
                finally
                {
                    if (beginDrawSuccessful)
                    {
                        using (Profiler.Begin(GameProfilingKeys.GameEndDraw))
                        {
                            EndDraw(true);
                        }
                    }

                    CheckEndRun();
                }
            }
            catch (Exception ex)
            {
                Log.Error("Unexpected exception", ex);
                throw;
            }
        }

        private void CheckEndRun()
        {
            if (isExiting && IsRunning && isEndRunRequired)
            {
                EndRun();
                IsRunning = false;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Starts the drawing of a frame. This method is followed by calls to Draw and EndDraw.
        /// </summary>
        /// <returns><c>true</c> to continue drawing, false to not call <see cref="Draw"/> and <see cref="EndDraw"/></returns>
        protected virtual bool BeginDraw()
        {
            if ((graphicsDeviceManager != null) && !graphicsDeviceManager.BeginDraw())
            {
                return false;
            }

            return true;
        }

        /// <summary>
        /// Called after all components are initialized but before the first update in the game loop.
        /// </summary>
        protected virtual void BeginRun()
        {
        }

        protected override void Destroy()
        {
            lock (this)
            {
                if (Window != null && Window.IsActivated) // force the window to be in an correct state during destroy (Deactivated events are sometimes dropped on windows)
                    Window.OnPause();

                var array = new IGameSystemBase[GameSystems.Count];
                GameSystems.CopyTo(array, 0);
                for (int i = 0; i < array.Length; i++)
                {
                    var disposable = array[i] as IDisposable;
                    if (disposable != null)
                    {
                        disposable.Dispose();
                    }
                }

                var disposableGraphicsManager = graphicsDeviceManager as IDisposable;
                if (disposableGraphicsManager != null)
                {
                    disposableGraphicsManager.Dispose();
                }

                DisposeGraphicsDeviceEvents();

                if (gamePlatform != null)
                {
                    gamePlatform.Release();
                }
            }

            base.Destroy();
        }

        /// <summary>
        /// Reference page contains code sample.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Draw.
        /// </param>
        protected virtual void Draw(GameTime gameTime)
        {
            GameSystems.Draw(gameTime);

            // Make sure that the render target is set back to the back buffer
            // From a user perspective this is better. From an internal point of view,
            // this code is already present in GraphicsDeviceManager.BeginDraw()
            // but due to the fact that a GameSystem can modify the state of GraphicsDevice
            // we need to restore the default render targets
            // TODO: Check how we can handle this more cleanly
            if (GraphicsDevice != null && GraphicsDevice.BackBuffer != null)
            {
                GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);
            }
        }

        /// <summary>Ends the drawing of a frame. This method is preceeded by calls to Draw and BeginDraw.</summary>
        protected virtual void EndDraw(bool present)
        {
            if (graphicsDeviceManager != null)
            {
                graphicsDeviceManager.EndDraw(present);
            }
        }

        /// <summary>Called after the game loop has stopped running before exiting.</summary>
        protected virtual void EndRun()
        {
        }

        /// <summary>Called after the Game and GraphicsDevice are created, but before LoadContent.  Reference page contains code sample.</summary>
        protected virtual void Initialize()
        {
            GameSystems.Initialize();
        }

        internal virtual void LoadContentInternal()
        {
            GameSystems.LoadContent();
        }

        internal bool IsExiting()
        {
            return isExiting;
        }

        /// <summary>
        /// Raises the Activated event. Override this method to add code to handle when the game gains focus.
        /// </summary>
        /// <param name="sender">The Game.</param>
        /// <param name="args">Arguments for the Activated event.</param>
        protected virtual void OnActivated(object sender, EventArgs args)
        {
            var handler = Activated;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Raises the Deactivated event. Override this method to add code to handle when the game loses focus.
        /// </summary>
        /// <param name="sender">The Game.</param>
        /// <param name="args">Arguments for the Deactivated event.</param>
        protected virtual void OnDeactivated(object sender, EventArgs args)
        {
            var handler = Deactivated;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        /// <summary>
        /// Raises an Exiting event. Override this method to add code to handle when the game is exiting.
        /// </summary>
        /// <param name="sender">The Game.</param>
        /// <param name="args">Arguments for the Exiting event.</param>
        protected virtual void OnExiting(object sender, EventArgs args)
        {
            var handler = Exiting;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        protected virtual void OnWindowCreated()
        {
            EventHandler<EventArgs> handler = WindowCreated;
            if (handler != null)
            {
                handler(this, EventArgs.Empty);
            }
        }

        private void GamePlatformOnWindowCreated(object sender, EventArgs eventArgs)
        {
            IsMouseVisible = true;
            OnWindowCreated();
        }


        /// <summary>
        /// This is used to display an error message if there is no suitable graphics device or sound card.
        /// </summary>
        /// <param name="exception">The exception to display.</param>
        /// <returns>The <see cref="bool" />.</returns>
        protected virtual bool ShowMissingRequirementMessage(Exception exception)
        {
            return true;
        }

        /// <summary>
        /// Called when graphics resources need to be unloaded. Override this method to unload any game-specific graphics resources.
        /// </summary>
        protected virtual void UnloadContent()
        {
            GameSystems.UnloadContent();
        }

        /// <summary>
        /// Reference page contains links to related conceptual articles.
        /// </summary>
        /// <param name="gameTime">
        /// Time passed since the last call to Update.
        /// </param>
        protected virtual void Update(GameTime gameTime)
        {
            GameSystems.Update(gameTime);
        }

        private void UpdateAndProfile(GameTime gameTime)
        {
            updateTimer.Reset();
            using (Profiler.Begin(GameProfilingKeys.GameUpdate))
            {
                Update(gameTime);
            }
            updateTimer.Tick();
            singleFrameUpdateTime += updateTimer.ElapsedTime;
        }

        private void gamePlatform_Activated(object sender, EventArgs e)
        {
            if (!IsActive)
            {
                IsActive = true;
                OnActivated(this, EventArgs.Empty);
            }
        }

        private void gamePlatform_Deactivated(object sender, EventArgs e)
        {
            if (IsActive)
            {
                IsActive = false;
                OnDeactivated(this, EventArgs.Empty);
            }
        }

        private void gamePlatform_Exiting(object sender, EventArgs e)
        {
            OnExiting(this, EventArgs.Empty);
        }

        private void DrawFrame()
        {
            if (SlowDownDrawCalls && (UpdateTime.FrameCount & 1) == 1) // skip the draw call about one frame over two.
                return;

            try
            {
                // Initialized
                if (!profilingDraw.IsInitialized)
                {
                    profilingDraw = Profiler.Begin(GameProfilingKeys.GameDrawFPS);
                }

                // Update profiling data
                profilingDraw.CheckIfEnabled();

                if (!isExiting && GameSystems.IsFirstUpdateDone && !Window.IsMinimized)
                {
                    drawTime.Update(totalDrawTime, lastFrameElapsedGameTime, singleFrameUpdateTime, drawRunningSlowly, true);

                    if (drawTime.FramePerSecondUpdated)
                    {
                        // TODO: store some GameTime information as attributes in the Profiling using  profilingDraw.SetAttribute(..)
                        profilingDraw.Mark("Frame = {0}, Update = {1:0.000}ms, Draw = {2:0.000}ms, FPS = {3:0.00}", drawTime.FrameCount, updateTime.TimePerFrame.TotalMilliseconds, drawTime.TimePerFrame.TotalMilliseconds, drawTime.FramePerSecond);
                    }

                    using (Profiler.Begin(GameProfilingKeys.GameDraw))
                    {
                        Draw(drawTime);
                    }
                }
            }
            finally
            {
                lastFrameElapsedGameTime = TimeSpan.Zero;
            }
        }

        private void SetupGraphicsDeviceEvents()
        {
            // Find the IGraphicsDeviceSerive.
            graphicsDeviceService = Services.GetService(typeof(IGraphicsDeviceService)) as IGraphicsDeviceService;

            // If there is no graphics device service, don't go further as the whole Game would not work
            if (graphicsDeviceService == null)
            {
                throw new InvalidOperationException("Unable to create a IGraphicsDeviceService");
            }

            if (graphicsDeviceService.GraphicsDevice == null)
            {
                throw new InvalidOperationException("Unable to find a GraphicsDevice instance");
            }

            resumeManager = new ResumeManager(Services);

            graphicsDeviceService.DeviceCreated += graphicsDeviceService_DeviceCreated;
            graphicsDeviceService.DeviceResetting += graphicsDeviceService_DeviceResetting;
            graphicsDeviceService.DeviceReset += graphicsDeviceService_DeviceReset;
            graphicsDeviceService.DeviceDisposing += graphicsDeviceService_DeviceDisposing;
        }

        private void DisposeGraphicsDeviceEvents()
        {
            if (graphicsDeviceService != null)
            {
                graphicsDeviceService.DeviceCreated -= graphicsDeviceService_DeviceCreated;
                graphicsDeviceService.DeviceResetting -= graphicsDeviceService_DeviceResetting;
                graphicsDeviceService.DeviceReset -= graphicsDeviceService_DeviceReset;
                graphicsDeviceService.DeviceDisposing -= graphicsDeviceService_DeviceDisposing;
            }
        }

        private void graphicsDeviceService_DeviceCreated(object sender, EventArgs e)
        {
            if (GameSystems.State != GameSystemState.ContentLoaded)
            {
                LoadContentInternal();
            }
        }

        private void graphicsDeviceService_DeviceDisposing(object sender, EventArgs e)
        {
            // TODO: Unload all assets
            //Asset.UnloadAll();

            if (GameSystems.State == GameSystemState.ContentLoaded)
            {
                UnloadContent();
            }
        }

        private void graphicsDeviceService_DeviceReset(object sender, EventArgs e)
        {
            // TODO: ResumeManager?
            //throw new NotImplementedException();
            resumeManager.OnReload();
            resumeManager.OnRecreate();
        }

        private void graphicsDeviceService_DeviceResetting(object sender, EventArgs e)
        {
            // TODO: ResumeManager?
            //throw new NotImplementedException();
            resumeManager.OnDestroyed();
        }

        #endregion
    }
}