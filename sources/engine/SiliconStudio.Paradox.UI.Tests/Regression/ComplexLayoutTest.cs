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
    /// Class for rendering tests on the <see cref="ImageElement"/> 
    /// </summary>
    public class ComplexLayoutTest : UnitTestGameBase
    {
        private StackPanel stackPanel;

        private ToggleButton toggle;

        private ScrollViewer scrollViewer;

        private ScrollingText scrollingText;

        public bool IsUpdateAutomatic;

        public ComplexLayoutTest()
        {
            CurrentVersion = 7;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var resolution = (Vector3)UIComponent.VirtualResolution;

            var canvas = new Canvas();
            var imgElt = new ImageElement { Name = "UV image", Source = new UIImage(Asset.Load<Texture>("uv")), Width = resolution.X / 5, Height = resolution.Y / 5, StretchType = StretchType.Fill };
            imgElt.DependencyProperties.Set(Canvas.PinOriginPropertyKey, 0.5f * Vector3.One);
            imgElt.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(resolution.X / 10, resolution.Y / 10, 0));
            imgElt.DependencyProperties.Set(Panel.ZIndexPropertyKey, -1);

            stackPanel = new StackPanel { Orientation = Orientation.Vertical };

            scrollViewer = new ScrollViewer { ScrollMode = ScrollingMode.Vertical };
            scrollViewer.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(resolution.X / 4, resolution.Y / 10, 0));
            scrollViewer.Content = stackPanel;

            var button1 = new Button { Margin = Thickness.UniformRectangle(5), Padding = Thickness.UniformRectangle(5), LocalMatrix = Matrix.Scaling(2, 2, 2) };
            var textOnly = new TextBlock { Text = "Text only button", Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"), TextColor = new Color(1f, 0, 0, 0.5f) };
            button1.Content = textOnly;

            var button2 = new Button { Name = "Button2", Margin = Thickness.UniformRectangle(5), Padding = Thickness.UniformRectangle(5) };
            var imageContent = new ImageElement { Name = "Image Button2", Source = new UIImage(Asset.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch, MaximumHeight = 50 };
            button2.Content = imageContent;

            var button3 = new Button { Margin = Thickness.UniformRectangle(5), Padding = Thickness.UniformRectangle(5) };
            var stackContent = new StackPanel { Orientation = Orientation.Horizontal };
            var stackImage = new ImageElement { Name = "Image stack panel", Source = new UIImage(Asset.Load<Texture>("uv")), MaximumHeight = 50 };
            var stackText = new TextBlock { Text = "button text", Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"), Margin = Thickness.UniformRectangle(5) };
            stackContent.Children.Add(stackImage);
            stackContent.Children.Add(stackText);
            button3.Content = stackContent;

            var button4 = new Button { Margin = Thickness.UniformRectangle(5), HorizontalAlignment = HorizontalAlignment.Right, Padding = Thickness.UniformRectangle(5) };
            var imageContent2 = new ImageElement { Name = "button 4 uv image", Source = new UIImage(Asset.Load<Texture>("uv")), StretchType = StretchType.FillOnStretch, MaximumHeight = 40, Opacity = 0.5f };
            button4.Content = imageContent2;

            var button5 = new Button { Margin = Thickness.UniformRectangle(5), HorizontalAlignment = HorizontalAlignment.Left, Padding = Thickness.UniformRectangle(5) };
            var textOnly2 = new TextBlock { Text = "Left aligned", Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15") };
            button5.Content = textOnly2;

            var button6 = new ImageButton
            {
                Height = 50,
                Margin = Thickness.UniformRectangle(5),
                HorizontalAlignment = HorizontalAlignment.Center,
                PressedImage = new UIImage(Asset.Load<Texture>("ImageButtonPressed")),
                NotPressedImage = new UIImage(Asset.Load<Texture>("ImageButtonNotPressed")),
            };

            toggle = new ToggleButton
            {
                Content = new TextBlock { Text = "Toggle button test", Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15") },
                IsThreeState = true
            };

            scrollingText = new ScrollingText { Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"), Text = "<<<--- Scrolling text in a button ", IsEnabled = IsUpdateAutomatic };
            var button7 = new Button { Margin = Thickness.UniformRectangle(5), Content = scrollingText };

            var uniformGrid = new UniformGrid { Rows = 2, Columns = 2 };
            var gridText = new TextBlock { Text = "Uniform grid", Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"), TextAlignment = TextAlignment.Center};
            gridText.DependencyProperties.Set(GridBase.ColumnSpanPropertyKey, 2);
            var buttonLeft = new Button { Content = new TextBlock { Text = "unif-grid left", Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"), TextAlignment = TextAlignment.Center } };
            buttonLeft.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            var buttonRight = new Button { Content = new TextBlock { Text = "unif-grid right", Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"), TextAlignment = TextAlignment.Center } };
            buttonRight.DependencyProperties.Set(GridBase.RowPropertyKey, 1);
            buttonRight.DependencyProperties.Set(GridBase.ColumnPropertyKey, 1);
            uniformGrid.Children.Add(gridText);
            uniformGrid.Children.Add(buttonLeft);
            uniformGrid.Children.Add(buttonRight);

            stackPanel.Children.Add(button1);
            stackPanel.Children.Add(button2);
            stackPanel.Children.Add(button3);
            stackPanel.Children.Add(button4);
            stackPanel.Children.Add(button5);
            stackPanel.Children.Add(button6);
            stackPanel.Children.Add(toggle);
            stackPanel.Children.Add(button7);
            stackPanel.Children.Add(uniformGrid);

            canvas.Children.Add(imgElt);
            canvas.Children.Add(scrollViewer);

            UIComponent.RootElement = canvas;
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.B))
                scrollViewer.ClipToBounds = !scrollViewer.ClipToBounds;

            if (Input.IsKeyDown(Keys.Down))
                stackPanel.Opacity *= 0.95f;
            if (Input.IsKeyDown(Keys.Up))
                stackPanel.Opacity /= 0.95f;

            if (Input.IsKeyPressed(Keys.H))
                toggle.Visibility = Visibility.Hidden;
            if (Input.IsKeyPressed(Keys.V))
                toggle.Visibility = Visibility.Visible;
            if (Input.IsKeyPressed(Keys.C))
                toggle.Visibility = Visibility.Collapsed;

            if (Input.IsKeyPressed(Keys.E))
                scrollViewer.IsEnabled = !scrollViewer.IsEnabled;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(DrawTest0).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest1).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest3).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest4).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest5).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest6).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest7).TakeScreenshot();
        }

        public void DrawTest0()
        {
            scrollViewer.ClipToBounds = false;
        }

        public void DrawTest1()
        {
            scrollViewer.ClipToBounds = true;
        }

        public void DrawTest2()
        {
            stackPanel.Opacity = 0.5f;
        }

        public void DrawTest3()
        {
            stackPanel.Opacity = 0f;
        }

        public void DrawTest4()
        {
            stackPanel.Opacity = 1f;
            // set the scrolling text to a fixed position
            scrollingText.IsEnabled = true;
            ((IUIElementUpdate)scrollingText).Update(new GameTime(new TimeSpan(), new TimeSpan(0, 0, 0, 10)));
            scrollingText.IsEnabled = false;
            scrollViewer.ScrollTo(new Vector3(50, 100, 0));
        }

        public void DrawTest5()
        {
            toggle.Visibility = Visibility.Hidden;
        }

        public void DrawTest6()
        {
            toggle.Visibility = Visibility.Collapsed;
        }

        public void DrawTest7()
        {
            toggle.Visibility = Visibility.Visible;
        }

        [Test]
        public void RunComplexLayoutTest()
        {
            RunGameTest(new ComplexLayoutTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new ComplexLayoutTest())
            {
                game.IsUpdateAutomatic = true;
                game.Run();
            }
        }
    }
}