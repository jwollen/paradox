﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Threading.Tasks;
using NUnit.Framework;

using SiliconStudio.Core.IO;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Games;

namespace SiliconStudio.Paradox.Graphics.Tests
{
    [TestFixture]
    public class TestImageLoad : TestGameBase
    {
        private SpriteBatch spriteBatch;
        private Texture jpg;
        private Texture png;

        public TestImageLoad()
        {
            CurrentVersion = 2;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawImages).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            spriteBatch = new SpriteBatch(GraphicsDevice);

            using (var pngStream = AssetManager.FileProvider.OpenStream("PngImage.png", VirtualFileMode.Open, VirtualFileAccess.Read))
            using (var pngImage = Image.Load(pngStream))
                png = Texture.New(GraphicsDevice, pngImage);

            using (var jpgStream = AssetManager.FileProvider.OpenStream("JpegImage.jpg", VirtualFileMode.Open, VirtualFileAccess.Read))
            using (var jpgImage = Image.Load(jpgStream))
                jpg = Texture.New(GraphicsDevice, jpgImage);
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if(!ScreenShotAutomationEnabled)
                DrawImages();
        }

        private void DrawImages()
        {
            GraphicsDevice.Clear(GraphicsDevice.BackBuffer, Color.AntiqueWhite);
            GraphicsDevice.Clear(GraphicsDevice.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer);
            GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);
            spriteBatch.Begin();

            var screenSize = new Vector2(GraphicsDevice.BackBuffer.ViewWidth, GraphicsDevice.BackBuffer.ViewHeight);

            spriteBatch.Draw(jpg, new Rectangle(0, 0, (int)screenSize.X, (int)(screenSize.Y / 2)), Color.White);
            spriteBatch.Draw(png, new Rectangle(0, (int)(screenSize.Y / 2), (int)screenSize.X, (int)(screenSize.Y / 2)), Color.White);

            spriteBatch.End();
        }

        public static void Main()
        {
            using (var game = new TestImageLoad())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Test]
        public void RunImageLoad()
        {
            RunGameTest(new TestImageLoad());
        }
    }
}
