﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
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
    /// Class for rendering tests on the <see cref="ImageElement"/> 
    /// </summary>
    public class ImageRegionTest : UnitTestGameBase
    {
        private StackPanel stackPanel;

        private int currentElement;

        public ImageRegionTest()
        {
            CurrentVersion = 5;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var image1 = new ImageElement
            {
                Source = new UIImage(Asset.Load<Texture>("BorderButtonCentered")) { Region = new Rectangle(256, 128, 512, 256), Borders = new Vector4(0.125f, 0.125f, 0.25f, 0.25f) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image2 = new ImageElement
            {
                Source = new UIImage(Asset.Load<Texture>("uv")) { Region = new Rectangle(0, 0, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image3 = new ImageElement
            {
                Source = new UIImage(Asset.Load<Texture>("uv")) { Region = new Rectangle(512, 0, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image4 = new ImageElement
            {
                Source = new UIImage(Asset.Load<Texture>("uv")) { Region = new Rectangle(0, 512, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            var image5 = new ImageElement
            {
                Source = new UIImage(Asset.Load<Texture>("uv")) { Region = new Rectangle(512, 512, 512, 512) },
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };

            stackPanel = new StackPanel { Orientation = Orientation.Vertical };
            stackPanel.Children.Add(image1);
            stackPanel.Children.Add(image2);
            stackPanel.Children.Add(image3);
            stackPanel.Children.Add(image4);
            stackPanel.Children.Add(image5);

            UIComponent.RootElement = new ScrollViewer { Content = stackPanel };
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
        }

        public void Draw0()
        {
            stackPanel.ScrolllToElement(0);
        }

        public void Draw1()
        {
            stackPanel.ScrolllToElement(1);
        }

        public void Draw2()
        {
            stackPanel.ScrolllToElement(2);
        }

        public void Draw3()
        {
            stackPanel.ScrolllToElement(3);
        }

        public void Draw4()
        {
            stackPanel.ScrolllToElement(4);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.Left))
            {
                currentElement = (stackPanel.Children.Count + currentElement - 1) % stackPanel.Children.Count;
                stackPanel.ScrolllToElement(currentElement);
            }
            if (Input.IsKeyReleased(Keys.Right))
            {
                currentElement = (stackPanel.Children.Count + currentElement + 1) % stackPanel.Children.Count;
                stackPanel.ScrolllToElement(currentElement);
            }
        }

        [Test]
        public void RunImageRegionTest()
        {
            RunGameTest(new ImageRegionTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new ImageRegionTest())
                game.Run();
        }
    }
}