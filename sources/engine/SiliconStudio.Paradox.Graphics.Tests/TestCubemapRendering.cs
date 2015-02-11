﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Effects.Cubemap;
using SiliconStudio.Paradox.Effects.Processors;
using SiliconStudio.Paradox.Effects.Renderers;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.EntityModel;
using SiliconStudio.Paradox.Extensions;
using SiliconStudio.Paradox.Input;

namespace SiliconStudio.Paradox.Graphics.Tests
{
    public class TestCubemapRendering : TestGameBase
    {
        private Entity mainCamera;
        private Vector3 cameraInitPos = new Vector3(10, 0, 0);
        private Vector3 cameraUp = Vector3.UnitY;
        private Entity[] primitiveEntities;
        private Vector3[] rotationAxis;

        public TestCubemapRendering()
        {
            // cannot render cubemap in level below 10.1
            GraphicsDeviceManager.PreferredGraphicsProfile = new[] { GraphicsProfile.Level_11_0 };
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            CreatePipeline(true);

            IsMouseVisible = true;

            //Input.ActivatedGestures.Add(new GestureConfigDrag());

            // Creates all primitives
            //                                     type           pos     axis
            var primitives = new List<Tuple<GeometricPrimitive, Vector3, Vector3>>
                             {
                                 Tuple.Create(GeometricPrimitive.Cube.New(GraphicsDevice), 2 * Vector3.UnitX, new Vector3(1,1,1)),
                                 Tuple.Create(GeometricPrimitive.Teapot.New(GraphicsDevice), -2 * Vector3.UnitX, new Vector3(-1,1,1)),
                                 Tuple.Create(GeometricPrimitive.GeoSphere.New(GraphicsDevice), 2 * Vector3.UnitY, new Vector3(1,0,1)),
                                 Tuple.Create(GeometricPrimitive.Cylinder.New(GraphicsDevice), -2 * Vector3.UnitY, new Vector3(-1,-1,1)),
                                 Tuple.Create(GeometricPrimitive.Torus.New(GraphicsDevice), 2 * Vector3.UnitZ, new Vector3(1,-1,1)),
                                 Tuple.Create(GeometricPrimitive.Sphere.New(GraphicsDevice), -2 * Vector3.UnitZ, new Vector3(0,1,1)),
                             };

            primitiveEntities = new Entity[primitives.Count];
            rotationAxis = new Vector3[primitives.Count];
            var material = Asset.Load<Material>("BasicMaterial");
            for (var i =0; i < primitives.Count; ++i)
            {
                var mesh = new Mesh()
                {
                    Draw = primitives[i].Item1.ToMeshDraw(),
                    Material = material
                };
                mesh.Parameters.Set(RenderingParameters.RenderLayer, RenderLayers.RenderLayer1);

                var entity = new Entity()
                {
                    new ModelComponent()
                    {
                        Model = new Model() { mesh }
                    },
                    new TransformationComponent() { Translation = primitives[i].Item2 }
                };
                Entities.Add(entity);
                primitiveEntities[i] = entity;
                rotationAxis[i] = primitives[i].Item3;
            }

            var reflectivePrimitive = GeometricPrimitive.Sphere.New(GraphicsDevice);
            var reflectiveMesh = new Mesh()
            {
                Draw = reflectivePrimitive.ToMeshDraw(),
            };
            reflectiveMesh.Parameters.Set(RenderingParameters.RenderLayer, RenderLayers.RenderLayer2);

            var reflectEntity = new Entity()
            {
                new ModelComponent()
                {
                    Model = new Model() { reflectiveMesh }
                },
                new TransformationComponent(),
                new CubemapSourceComponent() { IsDynamic = true, Enabled = true, Size = 128 }
            };
            Entities.Add(reflectEntity);
            reflectEntity.Get<ModelComponent>().Parameters.Set(TexturingKeys.TextureCube0, reflectEntity.Get<CubemapSourceComponent>().Texture);

            var mainCameraTargetEntity = new Entity(Vector3.Zero);
            Entities.Add(mainCameraTargetEntity);
            mainCamera = new Entity()
            {
                new CameraComponent
                {
                    AspectRatio = 8/4.8f,
                    FarPlane = 1000,
                    NearPlane = 1,
                    VerticalFieldOfView = 0.6f,
                    Target = mainCameraTargetEntity,
                    TargetUp = cameraUp,
                },
                new TransformationComponent
                {
                    Translation = cameraInitPos
                }
            };
            Entities.Add(mainCamera);

            RenderSystem.Pipeline.SetCamera(mainCamera.Get<CameraComponent>());

            // Add a custom script
            Script.Add(GameScript1);
        }

        private void CreatePipeline(bool renderInOnePass)
        {
            // Processor
            Entities.Processors.Add(new CubemapSourceProcessor(GraphicsDevice));

            // Rendering pipeline
            var cubeMapPipeline = new RenderPipeline("CubeMap");
            cubeMapPipeline.Renderers.Add(new ModelRenderer(Services, renderInOnePass ? "CubemapGeomEffect" : "CubemapEffect").AddLayerFilter(RenderLayers.RenderLayer1));
            RenderSystem.Pipeline.Renderers.Add(new CubemapRenderer(Services, cubeMapPipeline, renderInOnePass));
            RenderSystem.Pipeline.Renderers.Add(new CameraSetter(Services));
            RenderSystem.Pipeline.Renderers.Add(new RenderTargetSetter(Services) { ClearColor = Color.CornflowerBlue });
            RenderSystem.Pipeline.Renderers.Add(new ModelRenderer(Services, "CubemapEffect"));
        }

        private async Task GameScript1()
        {
            var dragValue = Vector2.Zero;
            var rotationFactor = 0.125f;
            var rotationUpFactor = 0.1f;
            var rotate = true;
            while (IsRunning)
            {
                // Wait next rendering frame
                await Script.NextFrame();

                if (Input.IsKeyPressed(Keys.Space))
                    rotate = !rotate;

                if (rotate)
                {
                    var rotationPrim = (float) (2*Math.PI*UpdateTime.Total.TotalMilliseconds/15000);
                    for (var i = 0; i < primitiveEntities.Length; ++i)
                    {
                        primitiveEntities[i].Transformation.Rotation = Quaternion.RotationAxis(rotationAxis[i], rotationPrim);
                    }
                }

                // rotate camera
                dragValue = 0.95f * dragValue;
                if (Input.PointerEvents.Count > 0)
                {
                    dragValue = Input.PointerEvents.Aggregate(Vector2.Zero, (t, x) => x.DeltaPosition + t);
                }
                rotationFactor -= dragValue.X;
                rotationUpFactor += dragValue.Y;
                if (rotationUpFactor > 0.45f)
                    rotationUpFactor = 0.45f;
                else if (rotationUpFactor < -0.45f)
                    rotationUpFactor = -0.45f;
                mainCamera.Transformation.Translation = Vector3.Transform(cameraInitPos, Quaternion.RotationZ((float)(Math.PI * rotationUpFactor)) * Quaternion.RotationY((float)(2 * Math.PI * rotationFactor)));
            }
        }

        public static void Main()
        {
            using (var game = new TestCubemapRendering())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Test]
        public void RunCubemapRendering()
        {
            RunGameTest(new TestCubemapRendering());
        }
    }
}