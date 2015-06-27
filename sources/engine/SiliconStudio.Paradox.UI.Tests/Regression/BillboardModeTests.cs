﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

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
    public class BillboardModeTests : UnitTestGameBase
    {
        public BillboardModeTests()
        {
            CurrentVersion = 3;
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var cube = Asset.Load<Entity>("cube");
            cube.Transform.Scale = new Vector3(10000);
            cube.Transform.Position = new Vector3(0, 0, 10);
            Scene.AddChild(cube);

            var imageElement = new ImageElement { Source = new UIImage(Asset.Load<Texture>("uv")) };
            var imageEntity = new Entity { new UIComponent { RootElement = imageElement, IsFullScreen = false, VirtualResolution = new Vector3(150) } };
            imageEntity.Transform.Position = new Vector3(-500, 0, 0);
            Scene.AddChild(imageEntity);

            var imageEntity2 = new Entity { new UIComponent { RootElement = imageElement, IsFullScreen = false, VirtualResolution = new Vector3(200) } };
            imageEntity2.Transform.Position = new Vector3(0, 250, 0);
            Scene.AddChild(imageEntity2);

            var imageEntity3 = new Entity { new UIComponent { RootElement = imageElement, IsFullScreen = false, VirtualResolution = new Vector3(250) } };
            imageEntity3.Transform.Position = new Vector3(0, 0, -500);
            Scene.AddChild(imageEntity3);
            
            // setup the camera
            var camera = new TestCamera { Yaw = MathUtil.Pi/4, Pitch = MathUtil.Pi/4, Position = new Vector3(500, 500, 500), MoveSpeed = 100 };
            camera.SetTarget(cube, true);
            CameraComponent = camera.Camera;
            Script.Add(camera);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.TakeScreenshot();
        }

        [Test]
        public void RunBillboardModeTests()
        {
            RunGameTest(new BillboardModeTests());
        }

        /// <summary>
        /// Launch the Image test.
        /// </summary>
        public static void Main()
        {
            using (var game = new BillboardModeTests())
                game.Run();
        }
    }
}