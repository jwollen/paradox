﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.UI.Controls;

namespace SiliconStudio.Paradox.UI
{
    /// <summary>
    /// Interface of the UI system.
    /// </summary>
    public class UISystem : GameSystemBase
    {

        internal UIBatch Batch { get; private set; }

        internal DepthStencilState KeepStencilValueState { get; private set; }

        internal DepthStencilState IncreaseStencilValueState { get; private set; }

        internal DepthStencilState DecreaseStencilValueState { get; private set; }

        private InputManagerBase input;

        public UISystem(IServiceRegistry registry)
            : base(registry)
        {
            Services.AddService(typeof(UISystem), this);
        }

        public override void Initialize()
        {
            base.Initialize();

            input = Services.GetServiceAs<InputManager>();

            Enabled = true;
            Visible = false;

            if (Game != null) // thumbnail system has no game
            {
                Game.Activated += OnApplicationResumed;
                Game.Deactivated += OnApplicationPaused;
            }
        }

        protected override void Destroy()
        {
            if (Game != null) // thumbnail system has no game
            {
                Game.Activated -= OnApplicationResumed;
                Game.Deactivated -= OnApplicationPaused;
            }

            // ensure that OnApplicationPaused is called before destruction, when Game.Deactivated event is not triggered.
            OnApplicationPaused(this, EventArgs.Empty);

            base.Destroy();
        }

        protected override void LoadContent()
        {
            base.LoadContent();

            // create effect and geometric primitives
            Batch = new UIBatch(GraphicsDevice);

            // create depth stencil states
            var depthStencilDescription = new DepthStencilStateDescription(true, true)
                {
                    StencilEnable = true,
                    FrontFace = new DepthStencilStencilOpDescription
                    {
                        StencilDepthBufferFail = StencilOperation.Keep,
                        StencilFail = StencilOperation.Keep,
                        StencilPass = StencilOperation.Keep,
                        StencilFunction = CompareFunction.Equal
                    },
                    BackFace = new DepthStencilStencilOpDescription
                    {
                        StencilDepthBufferFail = StencilOperation.Keep,
                        StencilFail = StencilOperation.Keep,
                        StencilPass = StencilOperation.Keep,
                        StencilFunction = CompareFunction.Equal
                    },
                };
            KeepStencilValueState = DepthStencilState.New(GraphicsDevice, depthStencilDescription);

            depthStencilDescription.FrontFace.StencilPass = StencilOperation.Increment;
            depthStencilDescription.BackFace.StencilPass = StencilOperation.Increment;
            IncreaseStencilValueState = DepthStencilState.New(GraphicsDevice, depthStencilDescription);

            depthStencilDescription.FrontFace.StencilPass = StencilOperation.Decrement;
            depthStencilDescription.BackFace.StencilPass = StencilOperation.Decrement;
            DecreaseStencilValueState = DepthStencilState.New(GraphicsDevice, depthStencilDescription);

            // set the default design of the UI elements.
            var designsTexture = TextureExtensions.FromFileData(GraphicsDevice, DefaultDesigns.Designs);
            Button.PressedImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default button pressed design", designsTexture) { Borders = 8 * Vector4.One, Region = new RectangleF(71, 3, 32, 32)});
            Button.NotPressedImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default button not pressed design", designsTexture) { Borders = 8 * Vector4.One, Region = new RectangleF(3, 3, 32, 32) });
            Button.MouseOverImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default button overred design", designsTexture) { Borders = 8 * Vector4.One, Region = new RectangleF(37, 3, 32, 32) });
            EditText.ActiveImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default edit active design", designsTexture) { Borders = 12 * Vector4.One, Region = new RectangleF(105, 3, 32, 32) });
            EditText.InactiveImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default edit inactive design", designsTexture) { Borders = 12 * Vector4.One, Region = new RectangleF(139, 3, 32, 32) });
            EditText.MouseOverImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default edit overred design", designsTexture) { Borders = 12 * Vector4.One, Region = new RectangleF(173, 3, 32, 32) });
            ToggleButton.CheckedImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default toggle button checked design", designsTexture) { Borders = 8 * Vector4.One, Region = new RectangleF(71, 3, 32, 32) });
            ToggleButton.UncheckedImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default toggle button unchecked design", designsTexture) { Borders = 8 * Vector4.One, Region = new RectangleF(3, 3, 32, 32) });
            ToggleButton.IndeterminateImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default toggle button indeterminate design", designsTexture) { Borders = 8 * Vector4.One, Region = new RectangleF(37, 3, 32, 32) });
            Slider.TrackBackgroundImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default slider track background design", designsTexture) { Borders = 14 * Vector4.One, Region = new RectangleF(207, 3, 32, 32) });
            Slider.TrackForegroundImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default slider track foreground design", designsTexture) { Borders = 0 * Vector4.One, Region = new RectangleF(3, 37, 32, 32) });
            Slider.ThumbImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default slider thumb design", designsTexture) { Borders = 4 * Vector4.One, Region = new RectangleF(37, 37, 16, 32) });
            Slider.MouseOverThumbImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default slider thumb overred design", designsTexture) { Borders = 4 * Vector4.One, Region = new RectangleF(71, 37, 16, 32) });
            Slider.TickImagePropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new UIImage("Default slider track foreground design", designsTexture) { Region = new RectangleF(245, 3, 3, 6) });
            Slider.TickOffsetPropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(13f);
            Slider.TrackStartingOffsetsrPropertyKey.DefaultValueMetadata = DefaultValueMetadata.Static(new Vector2(3));
        }

        /// <summary>
        /// The method to call when the application is put on background.
        /// </summary>
        void OnApplicationPaused(object sender, EventArgs e)
        {
            // validate the edit text and close the keyboard, if any edit text is currently active
            var focusedEdit = UIElement.FocusedElement as EditText;
            if (focusedEdit != null)
                focusedEdit.IsSelectionActive = false;
        }

        /// <summary>
        /// The method to call when the application is put on foreground.
        /// </summary>
        void OnApplicationResumed(object sender, EventArgs e)
        {
            // revert the state of the edit text here?
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            UpdateKeyEvents();
        }

        private void UpdateKeyEvents()
        {
            if (input == null)
                return;

            foreach (var keyEvent in input.KeyEvents)
            {
                if (UIElement.FocusedElement == null || !UIElement.FocusedElement.IsHierarchyEnabled) return;
                var key = keyEvent.Key;
                if (keyEvent.Type == KeyEventType.Pressed)
                {
                    UIElement.FocusedElement.RaiseKeyPressedEvent(new KeyEventArgs { Key = key, Input = input });
                }
                else
                {
                    UIElement.FocusedElement.RaiseKeyReleasedEvent(new KeyEventArgs { Key = key, Input = input });
                }
            }

            foreach (var key in input.KeyDown)
            {
                if (UIElement.FocusedElement == null || !UIElement.FocusedElement.IsHierarchyEnabled) return;
                UIElement.FocusedElement.RaiseKeyDownEvent(new KeyEventArgs { Key = key, Input = input });
            }
        }
    }
}
