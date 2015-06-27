﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.UI.Controls;

namespace SiliconStudio.Paradox.UI.Tests.Regression
{
    /// <summary>
    /// Test for UI on scene entities
    /// </summary>
    public class InSceneUITest : UnitTestGameBase
    {
        private List<Entity> elements = new List<Entity>();

        public InSceneUITest()
        {
            CurrentVersion = 4;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            // setup the camera
            var camera = new TestCamera { Yaw = 0, Pitch = 0, Position = new Vector3(0, 0, 1000), MoveSpeed = 100 };
            CameraComponent = camera.Camera;
            Script.Add(camera);

            UIRoot.Transform.Rotation = Quaternion.RotationX(MathUtil.Pi / 3f);
            UIComponent.RootElement = new ImageElement { Source = new UIImage(Asset.Load<Texture>("uv"))};
            UIComponent.IsFullScreen = false;
            UIComponent.IsBillboard = false;
            UIComponent.VirtualResolution = new Vector3(200, 200, 100);

            var cube = Asset.Load<Entity>("cube");
            cube.Transform.Scale = new Vector3(10000);
            cube.Transform.Position = new Vector3(0, 0, 10);
            Scene.AddChild(cube);
            
            var font = Asset.Load<SpriteFont>("CourierNew12");
            var textBlockZ0 = new TextBlock { Font = font, TextColor = Color.Black, TextSize = 20, Text = "At depth 0", VerticalAlignment = VerticalAlignment.Center, SynchronousCharacterGeneration = true, BackgroundColor = Color.Red };
            var entity1 = new Entity { new UIComponent { RootElement = textBlockZ0, IsFullScreen = false, IsBillboard = false, VirtualResolution = new Vector3(150) } };
            entity1.Transform.Position = new Vector3(-500, 0, 0);
            Scene.AddChild(entity1);

            var textBlockZ500 = new TextBlock { Font = font, TextColor = Color.Black, TextSize = 20, Text = "At depth 300", VerticalAlignment = VerticalAlignment.Center, SynchronousCharacterGeneration = true, BackgroundColor = Color.Red };
            var entity2 = new Entity { new UIComponent { RootElement = textBlockZ500, IsFullScreen = false, IsBillboard = false, VirtualResolution = new Vector3(150) } };
            entity2.Transform.Position = new Vector3(300, 0, 300);
            Scene.AddChild(entity2);

            var textBlockZM500 = new TextBlock { Font = font, TextColor = Color.Black, TextSize = 20, Text = "At depth -300", VerticalAlignment = VerticalAlignment.Center, SynchronousCharacterGeneration = true, BackgroundColor = Color.Red };
            var entity3 = new Entity { new UIComponent { RootElement = textBlockZM500, IsFullScreen = false, IsBillboard = false, VirtualResolution = new Vector3(150) } };
            entity3.Transform.Position = new Vector3(0, 300, -300);
            Scene.AddChild(entity3);

            elements.Add(entity1);
            elements.Add(entity2);
            elements.Add(entity3);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
            FrameGameSystem.Draw(ToggleSnapping).TakeScreenshot();
        }

        private void ToggleSnapping()
        {
            foreach (var element in elements)
            {
                var comp = element.Get<UIComponent>();
                if (comp != null)
                    comp.SnapText = !comp.SnapText;
            }
        }

        [Test]
        public void RunInSceneUITest()
        {
            RunGameTest(new InSceneUITest());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new InSceneUITest())
                game.Run();
        }
    }
}