﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Threading.Tasks;
using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Input;

namespace SiliconStudio.Paradox.Graphics.Tests
{

    /// <summary>
    /// Test a sprite font imported from an external ttf file (not system font).
    /// </summary>
    [TestFixture]
    public class TestExternSpriteFont : TestGameBase
    {
        private SpriteBatch spriteBatch;
        private SpriteFont testFont;
        private Texture colorTexture;

        public TestExternSpriteFont()
        {
            CurrentVersion = 2;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawText).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            testFont = Asset.Load<SpriteFont>("StaticFonts/ExternFont");

            // Instantiate a SpriteBatch
            spriteBatch = new SpriteBatch(GraphicsDevice);
            colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });

            spriteBatch = new SpriteBatch(GraphicsDevice);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if(!ScreenShotAutomationEnabled)
                DrawText();
        }

        private void DrawText()
        {
            GraphicsDevice.Clear(GraphicsDevice.BackBuffer, Color.Black);
            GraphicsDevice.Clear(GraphicsDevice.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);

            // Render the text
            spriteBatch.Begin();

            const string text = "This is a font created from an external font file.";
            var dim = testFont.MeasureString(text);

            const int x = 20;
            const int y = 20;
            spriteBatch.Draw(colorTexture, new Rectangle(x, y, (int)dim.X, (int)dim.Y), Color.Green);
            spriteBatch.DrawString(testFont, text, new Vector2(x, y), Color.White);
            spriteBatch.DrawString(testFont, text, new Vector2(x, y + dim.Y + 20), Color.Red);

            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.S))
                SaveTexture(GraphicsDevice.BackBuffer, "sprite-font-extern-test.png");
        }

        public static void Main()
        {
            using (var game = new TestExternSpriteFont())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Test]
        public void RunExternSpriteFont()
        {
            RunGameTest(new TestExternSpriteFont());
        }
    }
}