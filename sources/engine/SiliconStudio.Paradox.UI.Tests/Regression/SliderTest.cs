// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.UI.Controls;
using SiliconStudio.Paradox.UI.Panels;

namespace SiliconStudio.Paradox.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="Slider"/> 
    /// </summary>
    public class SliderTest : UnitTestGameBase
    {
        private Slider slider;
        private UniformGrid grid;
        private UIImageGroup sliderImages;

        private bool isRotatedImages;

        public SliderTest()
        {
            CurrentVersion = 2;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            sliderImages = Asset.Load<UIImageGroup>("DebugSlider");

            slider = new Slider { TrackStartingOffsets = new Vector2(10, 6), TickOffset = 10 };
            SetSliderImages(isRotatedImages);

            grid = new UniformGrid { Children = { slider } };

            UIComponent.RootElement = grid;
        }

        private void SetSliderImages(bool setRotatedImages)
        {
            var suffix = setRotatedImages ? "Rotated" : "";

            slider.TrackBackgroundImage = sliderImages["Background" + suffix];
            slider.TrackForegroundImage = sliderImages["Foreground" + suffix];
            slider.ThumbImage= sliderImages["Thumb" + suffix];
            slider.MouseOverThumbImage= sliderImages["ThumbOverred" + suffix];
            slider.TickImage = sliderImages["Tick" + suffix];
        }

        private void ResetSliderImages()
        {
            slider.TrackBackgroundImage = null;
            slider.TrackForegroundImage = null;
            slider.ThumbImage = null;
            slider.MouseOverThumbImage = null;
            slider.TickImage = null;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.Draw(DrawTest1).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest3).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest4).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest5).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest6).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest7).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest8).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest9).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest10).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest11).TakeScreenshot();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            const float RotationStep = 0.05f;

            if (Input.IsKeyReleased(Keys.T))
                slider.AreTicksDisplayed = !slider.AreTicksDisplayed;

            if (Input.IsKeyReleased(Keys.S))
                slider.ShouldSnapToTicks = !slider.ShouldSnapToTicks;

            if (Input.IsKeyReleased(Keys.R))
                slider.IsDirectionReversed = !slider.IsDirectionReversed;

            if (Input.IsKeyReleased(Keys.O))
                slider.Orientation = (Orientation)(((int)slider.Orientation + 1) % 3);

            if(Input.IsKeyReleased(Keys.Left))
                slider.Decrease();

            if(Input.IsKeyReleased(Keys.Right))
                slider.Increase();

            if (Input.IsKeyReleased(Keys.N))
                ResetSliderImages();

            if (Input.IsKeyPressed(Keys.V))
                slider.VerticalAlignment = (VerticalAlignment)(((int)slider.VerticalAlignment + 1) % 4);

            if (Input.IsKeyPressed(Keys.H))
                slider.HorizontalAlignment = (HorizontalAlignment)(((int)slider.HorizontalAlignment + 1) % 4);

            if (Input.IsKeyReleased(Keys.NumPad4))
                slider.LocalMatrix *= Matrix.RotationY(RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad6))
                slider.LocalMatrix *= Matrix.RotationY(-RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad2))
                slider.LocalMatrix *= Matrix.RotationX(RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad8))
                slider.LocalMatrix *= Matrix.RotationX(-RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad1))
                slider.LocalMatrix *= Matrix.RotationZ(RotationStep);
            if (Input.IsKeyReleased(Keys.NumPad9))
                slider.LocalMatrix *= Matrix.RotationZ(-RotationStep);
            if (Input.IsKeyReleased(Keys.Delete))
                slider.LocalMatrix *= Matrix.Translation(-10, 0, 0);
            if (Input.IsKeyReleased(Keys.PageDown))
                slider.LocalMatrix *= Matrix.Translation(10, 0, 0);
            if (Input.IsKeyReleased(Keys.Home))
                slider.LocalMatrix *= Matrix.Translation(0, -10, 0);
            if (Input.IsKeyReleased(Keys.End))
                slider.LocalMatrix *= Matrix.Translation(0, 10, 0);

            if (Input.IsKeyReleased(Keys.G))
                ChangeGridColumnRowNumbers();

            if (Input.IsKeyReleased(Keys.I))
            {
                isRotatedImages = !isRotatedImages;
                SetSliderImages(isRotatedImages);
            }
        }

        private void ChangeGridColumnRowNumbers()
        {
            grid.Rows = grid.Rows % 2 + 1;
            grid.Columns = grid.Columns % 2 + 1;
        }

        public void DrawTest1()
        {
            slider.Value = 0.25f;
        }

        public void DrawTest2()
        {
            slider.AreTicksDisplayed = true;
            slider.VerticalAlignment = VerticalAlignment.Stretch;
        }

        public void DrawTest3()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Down, new Vector2(0.75f, 0.5f)));
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Up, new Vector2(0.75f, 0.5f)));
        }

        public void DrawTest4()
        {
            slider.VerticalAlignment = VerticalAlignment.Center;
            slider.IsDirectionReversed = true;
        }

        public void DrawTest5()
        {
            slider.IsDirectionReversed = false;
            slider.ShouldSnapToTicks = true;
        }

        public void DrawTest6()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Down, new Vector2(0.54f, 0.5f)));
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Up, new Vector2(0.54f, 0.5f)));
        }

        public void DrawTest7()
        {
            SetSliderImages(true);
        }

        public void DrawTest8()
        {
            slider.Orientation = Orientation.Vertical;
        }

        public void DrawTest9()
        {
            SetSliderImages(false);
        }

        public void DrawTest10()
        {
            ChangeGridColumnRowNumbers();
        }

        public void DrawTest11()
        {
            slider.LocalMatrix = Matrix.Translation(20, 30, 0) * Matrix.RotationYawPitchRoll(-0.1f, -0.2f, 0.3f);
        }

        [Test]
        public void RunSliderTest()
        {
            RunGameTest(new SliderTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new SliderTest())
                game.Run();
        }
    }
}