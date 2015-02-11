﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_PLATFORM_ANDROID
using System;
using System.Diagnostics;
using System.Drawing;
using Android.Content;
using Android.Views.InputMethods;
using SiliconStudio.Core;
using SiliconStudio.Paradox.Games.Android;
using SiliconStudio.Paradox.Graphics;
using Rectangle = SiliconStudio.Core.Mathematics.Rectangle;
using OpenTK.Platform.Android;

namespace SiliconStudio.Paradox.Games
{
    /// <summary>
    /// An abstract window.
    /// </summary>
    internal class GameWindowAndroid : GameWindow
    {
        private AndroidParadoxGameView paradoxGameForm;
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
            return gameContext.ContextType == AppContextType.Android;
        }

        internal override void Initialize(GameContext gameContext)
        {
            GameContext = gameContext;

            paradoxGameForm = (AndroidParadoxGameView)gameContext.Control;
            nativeWindow = new WindowHandle(AppContextType.Android, paradoxGameForm);

            paradoxGameForm.Load += gameForm_Resume;
            paradoxGameForm.OnPause += gameForm_OnPause;
            paradoxGameForm.Unload += gameForm_Unload;
            paradoxGameForm.RenderFrame += gameForm_RenderFrame;

            // Setup the initial size of the window
            var width = gameContext.RequestedWidth;
            if (width == 0)
            {
                width = paradoxGameForm.Width;
            }

            var height = gameContext.RequestedHeight;
            if (height == 0)
            {
                height = paradoxGameForm.Height;
            }

            // Transmit requested back buffer and depth stencil formats to OpenTK
            paradoxGameForm.RequestedBackBufferFormat = gameContext.RequestedBackBufferFormat;
            paradoxGameForm.RequestedDepthStencilFormat = gameContext.RequestedDepthStencilFormat;
            paradoxGameForm.RequestedGraphicsProfile = gameContext.RequestedGraphicsProfile;

            paradoxGameForm.Size = new Size(width, height);

            //paradoxGameForm.Resize += OnClientSizeChanged;
        }

        void gameForm_Resume(object sender, EventArgs e)
        {
            // Call InitCallback only first time
            if (InitCallback != null)
            {
                InitCallback();
                InitCallback = null;
            }
            paradoxGameForm.Run();

            OnResume();
        }

        void gameForm_OnPause(object sender, EventArgs e)
        {
            // Hide android soft keyboard (doesn't work anymore if done during Unload)
            var inputMethodManager = (InputMethodManager)PlatformAndroid.Context.GetSystemService(Context.InputMethodService);
            inputMethodManager.HideSoftInputFromWindow(GameContext.Control.RootView.WindowToken, HideSoftInputFlags.None);
        }

        void gameForm_Unload(object sender, EventArgs e)
        {
            OnPause();
        }
        
        void gameForm_RenderFrame(object sender, OpenTK.FrameEventArgs e)
        {
            RunCallback();
        }

        internal override void Run()
        {
            Debug.Assert(InitCallback != null);
            Debug.Assert(RunCallback != null);

            if (paradoxGameForm.GraphicsContext != null)
            {
                throw new NotImplementedException("Only supports not yet initialized AndroidGameView.");
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
                return paradoxGameForm.Visible;
            }
            set
            {
                paradoxGameForm.Visible = value;
            }
        }

        protected override void SetTitle(string title)
        {
            paradoxGameForm.Title = title;
        }

        internal override void Resize(int width, int height)
        {
            paradoxGameForm.Size = new Size(width, height);
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
                return new Rectangle(0, 0, paradoxGameForm.Size.Width, paradoxGameForm.Size.Height);
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
                return paradoxGameForm.WindowState == OpenTK.WindowState.Minimized;
            }
        }

        public override bool IsMouseVisible
        {
            get { return false; }
            set {}
        }

        protected override void Destroy()
        {
            if (paradoxGameForm != null)
            {
                paradoxGameForm.Load -= gameForm_Resume;
                paradoxGameForm.OnPause -= gameForm_OnPause;
                paradoxGameForm.Unload -= gameForm_Unload;
                paradoxGameForm.RenderFrame -= gameForm_RenderFrame;

                paradoxGameForm.GraphicsContext.MakeCurrent(null);
                paradoxGameForm.GraphicsContext.Dispose();
                ((AndroidWindow)paradoxGameForm.WindowInfo).TerminateDisplay();
                //paradoxGameForm.Close();
                paradoxGameForm.Holder.RemoveCallback(paradoxGameForm);
                paradoxGameForm.Dispose();
                paradoxGameForm = null;
            }

            base.Destroy();
        }
    }
}
#endif