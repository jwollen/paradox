﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.UI.Controls;

namespace SiliconStudio.Paradox.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="TextBlock"/> 
    /// </summary>
    public class TextBlockTest : UnitTestGameBase
    {
        private TextBlock textBlock;

        public TextBlockTest()
        {
            CurrentVersion = 4;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            UIComponent.VirtualResolution = new Vector3(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, 500);

            textBlock = new TextBlock
            {
                TextColor = Color.Black,
                Font = Asset.Load<SpriteFont>("MSMincho10"),
                Text = @"Text Block test
にほんご ニホンゴ 人
Several line of texts with different width.
Next is empty.

This is the last line.",
                SynchronousCharacterGeneration = true,
                BackgroundColor = Color.LightSkyBlue
            };

            UIComponent.RootElement = textBlock;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.Down))
                UIComponent.VirtualResolution = 3 * UIComponent.VirtualResolution / 4;
            if (Input.IsKeyPressed(Keys.Up))
                UIComponent.VirtualResolution = 4 * UIComponent.VirtualResolution / 3;
            if (Input.IsKeyPressed(Keys.R))
                UIComponent.VirtualResolution = new Vector3(GraphicsDevice.BackBuffer.Width, GraphicsDevice.BackBuffer.Height, 500);

            if (Input.IsKeyPressed(Keys.Left))
                textBlock.TextSize = 3 * textBlock.TextSize / 4;
            if (Input.IsKeyPressed(Keys.Right))
                textBlock.TextSize = 4 * textBlock.TextSize / 3;
            if (Input.IsKeyPressed(Keys.Delete))
                textBlock.TextSize = textBlock.Font.Size;

            if (Input.IsKeyReleased(Keys.NumPad1))
                textBlock.VerticalAlignment = VerticalAlignment.Top;
            if (Input.IsKeyReleased(Keys.NumPad2))
                textBlock.VerticalAlignment = VerticalAlignment.Center;
            if (Input.IsKeyReleased(Keys.NumPad3))
                textBlock.VerticalAlignment = VerticalAlignment.Bottom;

            if (Input.IsKeyReleased(Keys.NumPad4))
                textBlock.HorizontalAlignment = HorizontalAlignment.Left;
            if (Input.IsKeyReleased(Keys.NumPad5))
                textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            if (Input.IsKeyReleased(Keys.NumPad6))
                textBlock.HorizontalAlignment = HorizontalAlignment.Right;

            if (Input.IsKeyReleased(Keys.NumPad7))
                textBlock.TextAlignment = TextAlignment.Left;
            if (Input.IsKeyReleased(Keys.NumPad8))
                textBlock.TextAlignment = TextAlignment.Center;
            if (Input.IsKeyReleased(Keys.NumPad9))
                textBlock.TextAlignment = TextAlignment.Right;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(Draw0).TakeScreenshot();
            FrameGameSystem.Draw(Draw1).TakeScreenshot();
            FrameGameSystem.Draw(Draw2).TakeScreenshot();
            FrameGameSystem.Draw(Draw3).TakeScreenshot();
            FrameGameSystem.Draw(Draw4).TakeScreenshot();
            FrameGameSystem.Draw(Draw5).TakeScreenshot();
            FrameGameSystem.Draw(Draw6).TakeScreenshot();
            FrameGameSystem.Draw(Draw7).TakeScreenshot();
            FrameGameSystem.Draw(Draw8).TakeScreenshot();
            FrameGameSystem.Draw(Draw9).TakeScreenshot();
            FrameGameSystem.Draw(Draw10).TakeScreenshot();
            FrameGameSystem.Draw(Draw11).TakeScreenshot();
            FrameGameSystem.Draw(Draw12).TakeScreenshot();
            FrameGameSystem.Draw(Draw13).TakeScreenshot();
            FrameGameSystem.Draw(Draw14).TakeScreenshot();
        }

        public void Draw0()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        public void Draw1()
        {
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        public void Draw2()
        {
            textBlock.TextAlignment = TextAlignment.Right;
            textBlock.VerticalAlignment = VerticalAlignment.Stretch;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        public void Draw3()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Top;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        public void Draw4()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Bottom;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        public void Draw5()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Stretch;
        }
        public void Draw6()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Left;
        }
        public void Draw7()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Right;
        }
        public void Draw8()
        {
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        public void Draw9()
        {
            textBlock.TextAlignment = TextAlignment.Center;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        public void Draw10()
        {
            textBlock.TextAlignment = TextAlignment.Right;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        public void Draw11()
        {
            textBlock.TextSize = textBlock.Font.Size * 2;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        public void Draw12()
        {
            textBlock.TextSize = textBlock.Font.Size / 2;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
        }
        public void Draw13()
        {
            textBlock.TextSize = textBlock.Font.Size;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            UIComponent.VirtualResolution = new Vector3(GraphicsDevice.BackBuffer.Width / 2, GraphicsDevice.BackBuffer.Height / 2, 500);
        }
        public void Draw14()
        {
            textBlock.TextSize = textBlock.Font.Size;
            textBlock.TextAlignment = TextAlignment.Left;
            textBlock.VerticalAlignment = VerticalAlignment.Center;
            textBlock.HorizontalAlignment = HorizontalAlignment.Center;
            UIComponent.VirtualResolution = new Vector3(GraphicsDevice.BackBuffer.Width * 2, GraphicsDevice.BackBuffer.Height * 2, 500);
        }

        [Test]
        public void RunTextBlockTest()
        {
            RunGameTest(new TextBlockTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new TextBlockTest())
                game.Run();
        }
    }
}