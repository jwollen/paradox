﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_PLATFORM_IOS
using System;
using System.Diagnostics;
using System.Drawing;
using OpenTK.Platform.iPhoneOS;
using MonoTouch.OpenGLES;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Graphics.OpenGL;
using Rectangle = SiliconStudio.Core.Mathematics.Rectangle;

namespace SiliconStudio.Paradox.Games
{
    /// <summary>
    /// An abstract window.
    /// </summary>
    internal class GameWindowiOS : GameWindow
    {
        private bool hasBeenInitialized;
        private iPhoneOSGameView gameForm;
        private WindowHandle nativeWindow;

        public override WindowHandle NativeWindow
        {
            get
            {
                return nativeWindow;
            }
        }

        public override void BeginScreenDeviceChange(bool willBeFullScreen)
        {

        }

        public override void EndScreenDeviceChange(int clientWidth, int clientHeight)
        {

        }

        protected internal override void SetSupportedOrientations(DisplayOrientation orientations)
        {
            // Desktop doesn't have orientation (unless on Windows 8?)
        }

        internal override bool CanHandle(GameContext gameContext)
        {
            return gameContext.ContextType == AppContextType.iOS;
        }

        internal override void Initialize(GameContext gameContext)
        {
            GameContext = gameContext;

            gameForm = gameContext.GameView;
            nativeWindow = new WindowHandle(AppContextType.iOS, gameForm);

            gameForm.Load += gameForm_Load;
            gameForm.Unload += gameForm_Unload;
            gameForm.RenderFrame += gameForm_RenderFrame;
            
            // get the OpenGL ES version
            var contextAvailable = false;
            foreach (var version in OpenGLUtils.GetGLVersions(gameContext.RequestedGraphicsProfile))
            {
                var contextRenderingApi = MajorVersionTOEAGLRenderingAPI(version);
                EAGLContext contextTest = null;
                try
                {
                    contextTest = new EAGLContext(contextRenderingApi);

                    // delete extra context
                    if (contextTest != null)
                        contextTest.Dispose();

                    gameForm.ContextRenderingApi = contextRenderingApi;
                    contextAvailable = true;
                }
                catch (Exception)
                {
                    // TODO: log message
                }
            }

            if (!contextAvailable)
                throw new Exception("Graphics context could not be created.");

            gameForm.LayerColorFormat = MonoTouch.OpenGLES.EAGLColorFormat.RGBA8;
            //gameForm.LayerRetainsBacking = false;

            // Setup the initial size of the window
            var width = gameContext.RequestedWidth;
            if (width == 0)
            {
                width = (int)(gameForm.Size.Width * gameForm.ContentScaleFactor);
            }

            var height = gameContext.RequestedHeight;
            if (height == 0)
            {
                height = (int)(gameForm.Size.Height * gameForm.ContentScaleFactor);
            }

            gameForm.Size = new Size(width, height);

            //gameForm.Resize += OnClientSizeChanged;
        }

        void gameForm_Load(object sender, EventArgs e)
        {
            hasBeenInitialized = false;
        }

        void gameForm_Unload(object sender, EventArgs e)
        {
            if (hasBeenInitialized)
            {
                OnPause();
                hasBeenInitialized = false;
            }
        }
        
        void gameForm_RenderFrame(object sender, OpenTK.FrameEventArgs e)
        {
            if (InitCallback != null)
            {
                InitCallback();
                InitCallback = null;
            }

            RunCallback();

            if (!hasBeenInitialized)
            {
                OnResume();
                hasBeenInitialized = true;
            }
        }

        internal override void Run()
        {
            Debug.Assert(InitCallback != null);
            Debug.Assert(RunCallback != null);

            if (gameForm.GraphicsContext != null)
            {
                throw new NotImplementedException("Only supports not yet initialized iPhoneOSGameView.");
            }

            var view = gameForm as IAnimatedGameView;
            if (view != null)
            {
                view.StartAnimating();
            }
            else
            {
                gameForm.Run();
            }
        }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="GameWindow" /> is visible.
        /// </summary>
        /// <value><c>true</c> if visible; otherwise, <c>false</c>.</value>
        public override bool Visible
        {
            get
            {
                return gameForm.Visible;
            }
            set
            {
                gameForm.Visible = value;
            }
        }

        protected override void SetTitle(string title)
        {
            gameForm.Title = title;
        }

        internal override void Resize(int width, int height)
        {
            gameForm.Size = new Size(width, height);
        }

        public override bool IsBorderLess
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public override bool AllowUserResizing
        {
            get
            {
                return true;
            }
            set
            {
            }
        }

        public override Rectangle ClientBounds
        {
            get
            {
                return new Rectangle(0, 0, (int)(gameForm.Size.Width * gameForm.ContentScaleFactor), (int)(gameForm.Size.Height * gameForm.ContentScaleFactor));
            }
        }

        public override DisplayOrientation CurrentOrientation
        {
            get
            {
                return DisplayOrientation.Default;
            }
        }

        public override bool IsMinimized
        {
            get
            {
                return gameForm.WindowState == OpenTK.WindowState.Minimized;
            }
        }

        public override bool IsMouseVisible
        {
            get { return false; }
            set {}
        }

        protected override void Destroy()
        {
            if (gameForm != null)
            {
                GraphicsDevice.UnbindGraphicsContext(gameForm.GraphicsContext);

                var view = gameForm as IAnimatedGameView;
                if (view != null)
                {
                    view.StopAnimating();
                    gameForm.Close();
                }
                else
                {
                    gameForm.Close();
                    gameForm.Dispose();
                }

                gameForm = null;
            }

            base.Destroy();
        }

        private static EAGLRenderingAPI MajorVersionTOEAGLRenderingAPI(int major)
        {
            if (major >= 3)
                return EAGLRenderingAPI.OpenGLES3;
            else
                return EAGLRenderingAPI.OpenGLES2;
        }
    }
}
#endif