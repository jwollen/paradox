﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Linq;
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
    /// Regression tests for <see cref="Border"/>
    /// </summary>
    public class BorderTest : UnitTestGameBase
    {
        private Border border;

        public BorderTest()
        {
            CurrentVersion = 5;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            border = new Border { Width = 200, Height = 150, Content = new Button { NotPressedImage = new UIImage(Asset.Load<Texture>("uv")), DepthAlignment = DepthAlignment.Back}};
            border.SetCanvasPinOrigin(new Vector3(0.5f));
            
            border.BackgroundColor = Color.Red;

            ResetBorderElement();

            UIComponent.RootElement = new Canvas { Children = { border } };
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            
            const float DepthIncrement = 10f;
            const float RotationIncrement = 0.1f;

            var localMatrix = border.LocalMatrix;

            if (Input.IsKeyPressed(Keys.Up))
                localMatrix.M43 -= DepthIncrement;
            if (Input.IsKeyPressed(Keys.Down))
                localMatrix.M43 += DepthIncrement;
            if (Input.IsKeyPressed(Keys.NumPad4))
                localMatrix = localMatrix * Matrix.RotationY(-RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad6))
                localMatrix = localMatrix * Matrix.RotationY(+RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad2))
                localMatrix = localMatrix * Matrix.RotationX(+RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad8))
                localMatrix = localMatrix * Matrix.RotationX(-RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad1))
                localMatrix = localMatrix * Matrix.RotationZ(-RotationIncrement);
            if (Input.IsKeyPressed(Keys.NumPad9))
                localMatrix = localMatrix * Matrix.RotationZ(+RotationIncrement);

            if (Input.IsKeyPressed(Keys.L))
                border.BorderThickness += new Thickness(1, 0, 0, 0, 0, 0);
            if (Input.IsKeyPressed(Keys.R))
                border.BorderThickness += new Thickness(0, 0, 0, 1, 0, 0);
            if (Input.IsKeyPressed(Keys.T))
                border.BorderThickness += new Thickness(0, 1, 0, 0, 0, 0);
            if (Input.IsKeyPressed(Keys.B))
                border.BorderThickness += new Thickness(0, 0, 0, 0, 1, 0);
            if (Input.IsKeyPressed(Keys.F))
                border.BorderThickness += new Thickness(0, 0, 0, 0, 0, 1);
            if (Input.IsKeyPressed(Keys.S))
                border.BorderThickness += new Thickness(0, 0, 1, 0, 0, 0);

            if (Input.KeyEvents.Any())
                border.LocalMatrix = localMatrix;

            if (Input.IsKeyPressed(Keys.D1))
                ResetBorderElement();
            if (Input.IsKeyPressed(Keys.D2))
                TurnBorderElement();
            if (Input.IsKeyPressed(Keys.D3))
                FlattenBorderElement();
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(TurnBorderElement).TakeScreenshot();
            FrameGameSystem.Draw(FlattenBorderElement).TakeScreenshot();
        }

        private void ResetBorderElement()
        {
            border.Depth = 100;
            border.LocalMatrix = Matrix.Identity;
            border.BorderThickness = new Thickness(3, 5, 1, 4, 6, 2);
            border.SetCanvasRelativePosition(new Vector3(0.5f));
        }

        private void FlattenBorderElement()
        {
            border.LocalMatrix = Matrix.Identity;
            border.SetCanvasRelativePosition(new Vector3(0.5f, 0.5f, 0f));
            border.Depth = 0;

            var borderSize = border.BorderThickness;
            borderSize.Front = 0;
            borderSize.Back = 0;
            border.BorderThickness = borderSize;
        }

        private void TurnBorderElement()
        {
            border.LocalMatrix = Matrix.RotationYawPitchRoll(-0.2f, -0.3f, 0.4f);
        }

        [Test]
        public void RunBorderTest()
        {
            RunGameTest(new BorderTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new BorderTest())
                game.Run();
        }
    }
}