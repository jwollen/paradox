﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.UI.Controls;
using SiliconStudio.Paradox.UI.Panels;

namespace SiliconStudio.Paradox.UI.Tests.Regression
{
    public class StackPanelTest : UnitTestGameBase
    {
        private StackPanel stackPanel1;
        private StackPanel stackPanel2;
        private StackPanel stackPanel3;
        private StackPanel stackPanel4;
        private StackPanel currentStackPanel;

        private ScrollViewer scrollViewer;

        public StackPanelTest()
        {
            CurrentVersion = 4;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.DrawOrder = -1;

            //FrameGameSystem.Update(() => SetCurrentContent(stackPanel1));
            //TakeSequenceOfScreenShots();

            //FrameGameSystem.Update(() => SetCurrentContent(stackPanel2));
            //TakeSequenceOfScreenShots();

            FrameGameSystem.Update(() => SetCurrentContent(stackPanel3));
            TakeSequenceOfScreenShots();
            
            //FrameGameSystem.Update(() => SetCurrentContent(stackPanel4));
            //FrameGameSystem.TakeScreenshot();
        }

        private void TakeSequenceOfScreenShots()
        {
            FrameGameSystem.TakeScreenshot();

            TakeSubSequenceOfScreenShots(false);
            TakeSubSequenceOfScreenShots(true);

            FrameGameSystem.Update(ScrollToFixedElementMiddle).TakeScreenshot();
            
            FrameGameSystem.Update(() => currentStackPanel.ItemVirtualizationEnabled = !currentStackPanel.ItemVirtualizationEnabled).TakeScreenshot();

            TakeSubSequenceOfScreenShots(false);
            TakeSubSequenceOfScreenShots(true);
        }

        private void TakeSubSequenceOfScreenShots(bool invalidateStackPanel)
        {
            // test scrolling functions with no arrangement invalidation
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(ScrollToFixedElementMiddle).TakeScreenshot();

            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToNextLine()).TakeScreenshot();
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToPreviousLine()).TakeScreenshot();

            FrameGameSystem.Update(ScrollToFixedElementMiddle);
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToPreviousLine()).TakeScreenshot();
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToNextLine()).TakeScreenshot();

            FrameGameSystem.Update(ScrollToFixedElementMiddle);
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToNextPage()).TakeScreenshot();
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToPreviousPage()).TakeScreenshot();

            FrameGameSystem.Update(ScrollToFixedElementMiddle);
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToPreviousPage()).TakeScreenshot();
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToNextPage()).TakeScreenshot();

            FrameGameSystem.Update(ScrollToFixedElementMiddle);
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToBeginning()).TakeScreenshot();
            if (invalidateStackPanel) FrameGameSystem.Update(InvalidateStackPanel);
            FrameGameSystem.Update(() => currentStackPanel.ScrollToEnd()).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var random = new Random(0);

            var uiImages = Asset.Load<UIImageGroup>("UIImages");
            var img1 = new ImageElement { Source = new UIImage(Asset.Load<Texture>("uv")) };
            var img2 = new ImageElement { Source = uiImages["GameScreenLeft"] };
            var img3 = new ImageElement { Source = uiImages["GameScreenRight"] };

            stackPanel1 = new StackPanel { Orientation = Orientation.Vertical, ItemVirtualizationEnabled = true };
            stackPanel1.Children.Add(img1);
            stackPanel1.Children.Add(img2);
            stackPanel1.Children.Add(img3);

            stackPanel2 = new StackPanel { Orientation = Orientation.Vertical, ItemVirtualizationEnabled = true };
            for (var i = 0; i < 1000; i++)
                stackPanel2.Children.Add(new Button { Name = "" + i, Height = 75, Content = new TextBlock { Text = "button number " + i, Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15") } });

            stackPanel3 = new StackPanel { Orientation = Orientation.Vertical, ItemVirtualizationEnabled = true, VerticalAlignment = VerticalAlignment.Center };
            for (var i = 0; i < 103; i++)
                stackPanel3.Children.Add(new Button { Name = "" + i, Height = 50 + 500 * random.NextFloat(), Content = new TextBlock { Text = "random button number " + i, Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15") } });

            stackPanel4 = new StackPanel { Orientation = Orientation.Vertical, ItemVirtualizationEnabled = true };
            for (var i = 0; i < 5; i++)
                stackPanel4.Children.Add(new Button { Name = "" + i, Height = i * 30, Content = new TextBlock { Text = "random button number " + i, Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15") } });

            currentStackPanel = stackPanel1;

            scrollViewer = new ScrollViewer { Name = "sv", Content = currentStackPanel, ScrollMode = ScrollingMode.Vertical };

            UI.RootElement = scrollViewer;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyReleased(Keys.D1))
                SetCurrentContent(stackPanel1);
            if (Input.IsKeyReleased(Keys.D2))
                SetCurrentContent(stackPanel2);
            if (Input.IsKeyReleased(Keys.D3))
                SetCurrentContent(stackPanel3);
            if (Input.IsKeyReleased(Keys.D4))
                SetCurrentContent(stackPanel4);

            if (Input.IsKeyReleased(Keys.V))
                currentStackPanel.ItemVirtualizationEnabled = !currentStackPanel.ItemVirtualizationEnabled;

            if (Input.IsKeyReleased(Keys.S))
                scrollViewer.SnapToAnchors = !scrollViewer.SnapToAnchors;

            if(Input.IsKeyReleased(Keys.PageDown))
                currentStackPanel.ScrollToNextPage(Orientation.Vertical);
            if(Input.IsKeyReleased(Keys.PageUp))
                currentStackPanel.ScrollToPreviousPage(Orientation.Vertical);
            if(Input.IsKeyReleased(Keys.Down))
                currentStackPanel.ScrollToNextLine(Orientation.Vertical);
            if(Input.IsKeyReleased(Keys.Up))
                currentStackPanel.ScrollToPreviousLine(Orientation.Vertical);
            if(Input.IsKeyReleased(Keys.End))
                currentStackPanel.ScrollToEnd(Orientation.Vertical);
            if(Input.IsKeyReleased(Keys.Home))
                currentStackPanel.ScrollToBeginning(Orientation.Vertical);
        }

        private void SetCurrentContent(StackPanel newContent)
        {
            currentStackPanel = newContent;
            scrollViewer.Content = currentStackPanel;
        }

        private void ScrollToFixedElementMiddle()
        {
            currentStackPanel.ScrolllToElement(1.3f);
        }

        private void InvalidateStackPanel()
        {
            currentStackPanel.InvalidateArrange();
        }

        public void Draw0()
        {
            // check that scrolling works before any layouting
            scrollViewer.ScrollToEnd(Orientation.Vertical);
            scrollViewer.ScrollToEnd(Orientation.Horizontal);
        }


        [Test]
        public void RunStackPanelTest()
        {
            RunGameTest(new StackPanelTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new StackPanelTest())
                game.Run();
        } 
    }
}