﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using NUnit.Framework;

using System.Threading.Tasks;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;

namespace SiliconStudio.Paradox.Graphics.Tests
{
    public class TestSpriteFontAlignment : TestGameBase
    {
        private SpriteFont arial;

        private SpriteBatch spriteBatch;
        private Texture colorTexture;

        private const string AssetPrefix = "StaticFonts/";

        private const string Text1 = @"This is a sample text.
It covers several lines
Short ones,
Medium ones,
And very long long ones.";

        private const string Text2 = @"
One blank line above


Two blank lines above
One blank line below
";

        public TestSpriteFontAlignment()
        {
            CurrentVersion = 1;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawText).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            arial = Asset.Load<SpriteFont>(AssetPrefix + "Arial13");

            colorTexture = Texture.New2D(GraphicsDevice, 1, 1, PixelFormat.R8G8B8A8_UNorm, new[] { Color.White });

            // Instantiate a SpriteBatch
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

            var dim1 = arial.MeasureString(Text1);
            var dim2 = arial.MeasureString(Text2);

            var x = 20;
            var y = 10;
            var title = "Arial Left aligned";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim1.X, (int)dim1.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text1, new Vector2(x, y + 20), Color.Black);

            x = 270;
            title = "Arial center aligned";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim1.X, (int)dim1.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text1, new Vector2(x, y + 20), Color.Black, TextAlignment.Center);

            x = 520;
            title = "Arial right aligned";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim1.X, (int)dim1.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text1, new Vector2(x, y + 20), Color.Black, TextAlignment.Right);

            x = 20;
            y = 250;
            title = "Test on blank lines";
            spriteBatch.DrawString(arial, title, new Vector2(x, y), Color.Red);
            spriteBatch.Draw(colorTexture, new Rectangle(x, y + 20, (int)dim2.X, (int)dim2.Y), Color.LightGreen);
            spriteBatch.DrawString(arial, Text2, new Vector2(x, y + 20), Color.Black, TextAlignment.Center);

            spriteBatch.End();
        }

        public static void Main()
        {
            using (var game = new TestSpriteFontAlignment())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Test]
        public void RunTestSpriteFontAlignment()
        {
            RunGameTest(new TestSpriteFontAlignment());
        }
    }
}