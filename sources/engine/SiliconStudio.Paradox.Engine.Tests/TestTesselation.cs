﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Rendering.Lights;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Rendering.ProceduralModels;
using SiliconStudio.Paradox.Rendering.Tessellation;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Input;

namespace SiliconStudio.Paradox.Engine.Tests
{
    [ReferenceToEffects]
    public class TestTesselation : EngineTestBase
    {
        private List<Entity> entities = new List<Entity>();
        private List<Material> materials = new List<Material>();

        private Entity currentEntity;
        private Material currentMaterial;

        private int currentModelIndex;

        private TestCamera camera;

        private int currentMaterialIndex;

        private bool isWireframe;

        private RasterizerState wireframeState;

        private SpriteBatch spriteBatch;

        private SpriteFont font;

        private bool debug;

        public TestTesselation() : this(false)
        {
        }

        public TestTesselation(bool isDebug)
            : base("ParadoxEffectBase")
        {
            CurrentVersion = 1;
            debug = isDebug;
            GraphicsDeviceManager.DeviceCreationFlags = DeviceCreationFlags.Debug;
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            spriteBatch = new SpriteBatch(GraphicsDevice);
            font = Asset.Load<SpriteFont>("Font");

            wireframeState = RasterizerState.New(GraphicsDevice, new RasterizerStateDescription(CullMode.Back) { FillMode = FillMode.Wireframe });

            materials.Add(Asset.Load<Material>("NoTessellation"));
            materials.Add(Asset.Load<Material>("FlatTessellation"));
            materials.Add(Asset.Load<Material>("PNTessellation"));
            materials.Add(Asset.Load<Material>("PNTessellationAE"));
            materials.Add(Asset.Load<Material>("FlatTessellationDispl"));
            materials.Add(Asset.Load<Material>("FlatTessellationDisplAE"));
            materials.Add(Asset.Load<Material>("PNTessellationDisplAE"));

            var cube = new Entity("Cube") { new ModelComponent(new ProceduralModelDescriptor(new CubeProceduralModel { Size = new Vector3(80), MaterialInstance = { Material = materials[0] } }).GenerateModel(Services)) };
            var sphere = new Entity("Sphere") { new ModelComponent(new ProceduralModelDescriptor(new SphereProceduralModel { Diameter = 100, Tessellation = 5, MaterialInstance = { Material = materials[0] }} ).GenerateModel(Services)) };
            
            var megalodon = Asset.Load<Entity>("megalodon Entity"); 
            megalodon.Transform.Position= new Vector3(0, -30f, -10f);

            var knight = Asset.Load<Entity>("AnimatedModel");
            knight.Transform.RotationEulerXYZ = new Vector3(-MathUtil.Pi / 2, MathUtil.Pi / 4, 0);
            knight.Transform.Position = new Vector3(0, -50f, 20f);
            knight.Transform.Scale= new Vector3(0.6f);

            entities.Add(sphere);
            entities.Add(cube);
            entities.Add(megalodon);
            entities.Add(knight);

            camera = new TestCamera();
            CameraComponent = camera.Camera;
            Script.Add(camera);

            LightingKeys.EnableFixedAmbientLight(GraphicsDevice.Parameters, true);
            GraphicsDevice.Parameters.Set(EnvironmentLightKeys.GetParameterKey(LightSimpleAmbientKeys.AmbientLight, 0), (Color3)Color.White);

            ChangeModel(0);
            SetWireframe(true);

            camera.Position = new Vector3(25, 45, 80);
            camera.SetTarget(currentEntity, true);
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).Draw(() => ChangeModel(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).Draw(() => ChangeModel(-1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
            FrameGameSystem.Draw(() => ChangeMaterial(1)).TakeScreenshot();
        }

        protected override void PostCameraRendererDraw(RenderContext context, RenderFrame frame)
        {
            if (!debug)
                return;

            spriteBatch.Begin();
            spriteBatch.DrawString(font, "Desired triangle size: {0}".ToFormat(currentMaterial.Parameters.Get(TessellationKeys.DesiredTriangleSize)), new Vector2(0), Color.Black);
            spriteBatch.DrawString(font, "FPS: {0}".ToFormat(DrawTime.FramePerSecond), new Vector2(0, 20), Color.Black);
            spriteBatch.End();
        }

        protected override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (Input.IsKeyPressed(Keys.Up))
                ChangeModel(1);

            if (Input.IsKeyPressed(Keys.Down))
                ChangeModel(-1);

            if (Input.IsKeyPressed(Keys.Left))
                ChangeMaterial(-1);

            if (Input.IsKeyPressed(Keys.Right))
                ChangeMaterial(1);

            if (Input.IsKeyDown(Keys.NumPad1))
                ChangeDesiredTriangleSize(-0.2f);

            if (Input.IsKeyDown(Keys.NumPad2))
                ChangeDesiredTriangleSize(0.2f);

            if (Input.IsKeyPressed(Keys.Space))
                SetWireframe(!isWireframe);
        }

        private void SetWireframe(bool wireframeActivated)
        {
            isWireframe = wireframeActivated;

            if (currentMaterial != null)
                currentMaterial.Parameters.Set(Effect.RasterizerStateKey, isWireframe ? wireframeState : GraphicsDevice.RasterizerStates.CullBack);
        }

        private void ChangeDesiredTriangleSize(float f)
        {
            if(currentMaterial == null)
                return;

            var oldValue = currentMaterial.Parameters.Get(TessellationKeys.DesiredTriangleSize);
            currentMaterial.Parameters.Set(TessellationKeys.DesiredTriangleSize, oldValue + f);
        }

        private void ChangeModel(int offset)
        {
            if (currentEntity != null)
            {
                Scene.RemoveChild(currentEntity);
                currentEntity = null;
            }

            currentModelIndex = (currentModelIndex + offset + entities.Count) % entities.Count;
            currentEntity = entities[currentModelIndex];

            Scene.AddChild(currentEntity);

            ChangeMaterial(0);
        }

        private void ChangeMaterial(int i)
        {
            currentMaterialIndex = ((currentMaterialIndex + i + materials.Count) % materials.Count);
            currentMaterial = materials[currentMaterialIndex];

            if (currentEntity != null)
            {
                var modelComponent = currentEntity.Get<ModelComponent>();
                modelComponent.Materials.Clear();

                if (modelComponent.Model != null)
                {
                    // ensure the same number of materials than original model.
                    for (int j = 0; j < modelComponent.Model.Materials.Count; j++)
                        modelComponent.Materials.Add(currentMaterial);
                }
            }

            SetWireframe(isWireframe);
        }

        [Test]
        public void RunTestGame()
        {
            RunGameTest(new TestTesselation());
        }

        static public void Main()
        {
            using (var game = new TestTesselation(true))
            {
                game.Run();
            }
        }
    }
}