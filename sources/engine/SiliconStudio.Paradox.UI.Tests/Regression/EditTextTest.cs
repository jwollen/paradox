﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;
using SiliconStudio.Paradox.UI.Controls;
using SiliconStudio.Paradox.UI.Events;
using SiliconStudio.Paradox.UI.Panels;

namespace SiliconStudio.Paradox.UI.Tests.Regression
{
    /// <summary>
    /// Class for rendering tests on the <see cref="ImageElement"/> 
    /// </summary>
    public class EditTextTest : UnitTestGameBase
    {
        private EditText edit1;
        private EditText edit2;
        private EditText edit3;
        private EditText edit4;

        public EditTextTest()
        {
            CurrentVersion = 4;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var middleOfScreen = new Vector3(UI.VirtualResolution.X, UI.VirtualResolution.Y, 0) / 2;

            edit1 = new EditText(Services)
            {
                Name = "TestEdit1",
                Font = Asset.Load<SpriteFont>("MSMincho10"),
                MinimumWidth = 100,
                Text = "Sample Text1",
                MaxLength = 35,
                TextSize = 20,
                SynchronousCharacterGeneration = true
            };
            edit1.DependencyProperties.Set(Canvas.PinOriginPropertyKey, 0.5f * Vector3.One);
            edit1.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(middleOfScreen.X, 100, 0));
            edit1.TextChanged += Edit1OnTextChanged;

            edit2 = new EditText(Services)
            {
                Name = "TestEdit2",
                Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"),
                MinimumWidth = 100,
                Text = "Sample2 Text2",
                MaxLength = 10,
                CharacterFilterPredicate = IsLetter,
                SynchronousCharacterGeneration = true
            };
            edit2.DependencyProperties.Set(Canvas.PinOriginPropertyKey, 0.5f * Vector3.One);
            edit2.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(middleOfScreen.X, 200, 0));
            edit2.TextChanged += Edit2OnTextChanged;

            edit3 = new EditText(Services)
            {
                Name = "TestEdit3",
                Font = Asset.Load<SpriteFont>("MSMincho10"),
                MinimumWidth = 100,
                Text = "secret",
                MaxLength = 15,
                TextSize = 24,
                InputType = EditText.InputTypeFlags.Password,
                SynchronousCharacterGeneration = true
            };
            edit3.DependencyProperties.Set(Canvas.PinOriginPropertyKey, 0.5f * Vector3.One);
            edit3.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(middleOfScreen.X, 300, 0));

            edit4 = new EditText(Services)
            {
                Name = "TestEdit4",
                Font = Asset.Load<SpriteFont>("MicrosoftSansSerif15"),
                MinimumWidth = 200,
                Text = "aligned text",
                TextSize = 24,
                SynchronousCharacterGeneration = true
            };
            edit4.DependencyProperties.Set(Canvas.PinOriginPropertyKey, 0.5f * Vector3.One);
            edit4.DependencyProperties.Set(Canvas.AbsolutePositionPropertyKey, new Vector3(middleOfScreen.X, 400, 0));

            var canvas = new Canvas();
            canvas.Children.Add(edit1);
            canvas.Children.Add(edit2);
            canvas.Children.Add(edit3);
            canvas.Children.Add(edit4);

            UI.RootElement = canvas;
        }

        private bool IsLetter(char c)
        {
            return char.IsLetter(c);
        }

        private void Edit1OnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            Logger.Info("The text of the edit1 box changed: text={0}", edit1.Text);
        }
        private void Edit2OnTextChanged(object sender, RoutedEventArgs routedEventArgs)
        {
            Logger.Info("The text of the edit2 box changed: text={0}", edit2.Text);
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.F1))
                edit1.Text = "Sample Text";
            if (Input.IsKeyPressed(Keys.F2))
                edit1.IsReadOnly = !edit1.IsReadOnly;

            if (Input.IsKeyPressed(Keys.NumPad4))
                edit4.TextAlignment = TextAlignment.Left;
            if (Input.IsKeyPressed(Keys.NumPad5))
                edit4.TextAlignment = TextAlignment.Center;
            if (Input.IsKeyPressed(Keys.NumPad6))
                edit4.TextAlignment = TextAlignment.Right;
            
            if (Input.PointerEvents.Count > 0)
            {
                foreach (var pointerEvent in Input.PointerEvents)
                {
                    if (pointerEvent.State == PointerState.Up && pointerEvent.Position.X < 0.1)
                        edit3.InputType = edit3.InputType ^ EditText.InputTypeFlags.Password;
                }
            }
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();
            FrameGameSystem.DrawOrder = -1;
            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(DrawTest1).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest2Bis).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest3).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest4).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest5).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest6).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest7).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest8).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest9).TakeScreenshot();
            FrameGameSystem.Draw(DrawTest10).TakeScreenshot();
            if (Platform.IsWindowsDesktop)
            {
                FrameGameSystem.Draw(SelectionTest1);
                FrameGameSystem.Draw(SelectionTest2);
                FrameGameSystem.Draw(SelectionTest3);
                FrameGameSystem.Draw(SelectionTest4);
                FrameGameSystem.Draw(SelectionTest5);
                FrameGameSystem.Draw(SelectionTest6);
                FrameGameSystem.Draw(SelectionTest7);
                FrameGameSystem.Draw(SelectionTest8);
                FrameGameSystem.Draw(SelectionTest9);
                FrameGameSystem.Draw(SelectionGraphicTest1).TakeScreenshot();
                FrameGameSystem.Draw(SelectionGraphicTest2).TakeScreenshot();
                FrameGameSystem.Draw(SelectionGraphicTest3).TakeScreenshot();
            }
        }

        public void DrawTest1()
        {
            edit1.IsSelectionActive = true;
        }

        public void DrawTest2()
        {
            edit1.IsSelectionActive = true;
            edit1.Select(1, 5);
        }

        public void DrawTest2Bis()
        {
            edit1.IsSelectionActive = true;
            edit1.Select(1, 5, true);
        }

        public void DrawTest3()
        {
            edit1.IsSelectionActive = true;
            edit1.Select(1, 5);
            edit1.SelectedText = "-Text inserted in the midle-";
        }

        public void DrawTest4()
        {
            edit1.IsSelectionActive = true;
            edit1.IsEnabled = false;
        }

        public void DrawTest5()
        {
            edit2.IsSelectionActive = true;
            edit2.SelectionStart = 0;
        }

        public void DrawTest6()
        {
            edit2.IsSelectionActive = true;
            edit2.Clear();
        }

        public void DrawTest7()
        {
            edit2.IsSelectionActive = true;
            edit2.AppendText("Too long Text for the edit");
        }

        public void DrawTest8()
        {
            edit2.IsSelectionActive = true;
            edit1.IsSelectionActive = true;
        }

        public void DrawTest9()
        {
            edit4.TextAlignment = TextAlignment.Center;
            edit4.IsSelectionActive = true;
        }

        public void DrawTest10()
        {
            edit4.ResetCaretBlinking(); // ensure that caret is visible if time since last frame is more than caret flickering duration.
            edit4.TextAlignment = TextAlignment.Right;
        }

        public void SelectionTest1()
        {
            edit4.TextAlignment = TextAlignment.Center;
            edit4.IsSelectionActive = false;

            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Down, new Vector2(0.49625f, 0.8f)));
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Up, new Vector2(0.49625f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(5, edit4.SelectionStart);
            Assert.AreEqual(0, edit4.SelectionLength);
            Assert.AreEqual(5, edit4.CaretPosition);
        }

        public void SelectionTest2()
        {
            edit4.TextAlignment = TextAlignment.Center;
            edit4.IsSelectionActive = false;

            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Down, new Vector2(0.5f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(6, edit4.SelectionStart);
            Assert.AreEqual(0, edit4.SelectionLength);
            Assert.AreEqual(6, edit4.CaretPosition);
        }

        public void SelectionTest3()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.525f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(6, edit4.SelectionStart);
            Assert.AreEqual(3, edit4.SelectionLength);
            Assert.AreEqual(9, edit4.CaretPosition);
        }

        public void SelectionTest4()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.57f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(6, edit4.SelectionStart);
            Assert.AreEqual(6, edit4.SelectionLength);
            Assert.AreEqual(12, edit4.CaretPosition);
        }

        public void SelectionTest5()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.55f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(6, edit4.SelectionStart);
            Assert.AreEqual(5, edit4.SelectionLength);
            Assert.AreEqual(11, edit4.CaretPosition);
        }

        public void SelectionTest6()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.49f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(5, edit4.SelectionStart);
            Assert.AreEqual(1, edit4.SelectionLength);
            Assert.AreEqual(5, edit4.CaretPosition);
        }

        public void SelectionTest7()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.42f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(0, edit4.SelectionStart);
            Assert.AreEqual(6, edit4.SelectionLength);
            Assert.AreEqual(0, edit4.CaretPosition);
        }

        public void SelectionTest8()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.47f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(3, edit4.SelectionStart);
            Assert.AreEqual(3, edit4.SelectionLength);
            Assert.AreEqual(3, edit4.CaretPosition);
        }

        public void SelectionTest9()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.50f, 0.8f)));
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Up, new Vector2(0.50f, 0.8f)));

            UI.Update(new GameTime());

            Assert.AreEqual(6, edit4.SelectionStart);
            Assert.AreEqual(0, edit4.SelectionLength);
            Assert.AreEqual(6, edit4.CaretPosition);
        }

        public void SelectionGraphicTest1()
        {
            edit4.TextAlignment = TextAlignment.Center;
            edit4.IsSelectionActive = false;

            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Down, new Vector2(0.5f, 0.8f)));

            UI.Update(new GameTime());
        }

        public void SelectionGraphicTest2()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.57f, 0.8f)));

            UI.Update(new GameTime());
        }

        public void SelectionGraphicTest3()
        {
            Input.PointerEvents.Clear();
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Move, new Vector2(0.42f, 0.8f)));
            Input.PointerEvents.Add(CreatePointerEvent(PointerState.Up, new Vector2(0.42f, 0.8f)));

            UI.Update(new GameTime());
        }

        [Test]
        public void RunEditTextTest()
        {
            RunGameTest(new EditTextTest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new EditTextTest())
                game.Run();
        }
    }
}