﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.UI.Controls;
using SiliconStudio.Paradox.UI.Panels;

namespace SiliconStudio.Paradox.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests to test batching ordering for transparency.
    /// </summary>
    public class TransparencyTest : UnitTestGameBase
    {
        private Button element2;

        private Button element1;

        private float zValue;

        public bool IsAutomatic;

        public TransparencyTest()
        {
            CurrentVersion = 5;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var uiImages = Asset.Load<UIImageGroup>("UIImages");
            element1 = new Button { Name = "1", Width = 300, Height = 150 };
            element1.PressedImage = uiImages["Logo"];
            element1.NotPressedImage = uiImages["BorderButton"];
            element1.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(350, 300, 0));
            element1.DependencyProperties.Set(Panel.ZIndexPropertyKey, 1);

            element2 = new Button { Name = "2", Width = 600, Height = 300 };
            element2.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(200, 100, 50));
            element2.DependencyProperties.Set(Panel.ZIndexPropertyKey, 0);
            element2.PressedImage = new UIImage(Asset.Load<Texture>("ImageButtonPressed"));
            element2.NotPressedImage = new UIImage(Asset.Load<Texture>("ImageButtonNotPressed"));

            var canvas = new Canvas();
            canvas.Children.Add(element1);
            canvas.Children.Add(element2);

            UI.RootElement = canvas;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (IsAutomatic)
            {
                zValue = 100 * (1 + (float)Math.Sin(gameTime.Total.TotalSeconds));

                element1.LocalMatrix = Matrix.Translation(0, 0, zValue);
            }
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(Draw0).TakeScreenshot();
            FrameGameSystem.Draw(Draw1).TakeScreenshot();
            FrameGameSystem.Draw(Draw2).TakeScreenshot();
            FrameGameSystem.Draw(Draw3).TakeScreenshot();
        }

        public void Draw0()
        {
            element1.LocalMatrix = Matrix.Translation(0, 0, 0);
        }

        public void Draw1()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Down, new Vector2(0.5f, 0.75f)));
            UI.Update(new GameTime());
        }

        public void Draw2()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Up, new Vector2(0.5f, 0.75f)));
            UI.Update(new GameTime());

            element1.LocalMatrix = Matrix.Translation(0, 0, 100);
        }

        public void Draw3()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Down, new Vector2(0.5f, 0.75f)));
            UI.Update(new GameTime());
        }
        
        [Test]
        public void RunTransparencyUnitTest()
        {
            RunGameTest(new TransparencyTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new TransparencyTest())
            {
                game.IsAutomatic = true;
                game.Run();
            }
        }
    }
}
