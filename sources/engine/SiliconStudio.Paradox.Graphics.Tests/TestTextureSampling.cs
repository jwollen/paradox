﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Threading.Tasks;
using NUnit.Framework;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics.Internals;

namespace SiliconStudio.Paradox.Graphics.Tests
{
    [TestFixture]
    public class TestTextureSampling : TestGameBase
    {
        struct Vertex
        {
            public Vector3 Position;
            public Vector2 TexCoords;
        };

        private struct DrawOptions
        {
            public Matrix Transform;

            public SamplerState Sampler;
        };

        private Effect simpleEffect;

        private VertexArrayObject vao;

        private DrawOptions[] myDraws;

        private EffectParameterCollectionGroup parameterCollectionGroup;

        private ParameterCollection parameterCollection;

        public TestTextureSampling()
        {
            CurrentVersion = 1;
        }

        protected override void RegisterTests()
        {
            base.RegisterTests();

            FrameGameSystem.Draw(DrawTextureSampling).TakeScreenshot();
        }

        protected override async Task LoadContent()
        {
            await base.LoadContent();

            var vertices = new Vertex[4];
            vertices[0] = new Vertex { Position = new Vector3(-1, -1, 0.5f), TexCoords = new Vector2(0, 0) };
            vertices[1] = new Vertex { Position = new Vector3(-1, 1, 0.5f), TexCoords = new Vector2(3, 0) };
            vertices[2] = new Vertex { Position = new Vector3(1, 1, 0.5f), TexCoords = new Vector2(3, 3) };
            vertices[3] = new Vertex { Position = new Vector3(1, -1, 0.5f), TexCoords = new Vector2(0, 3) };

            var indices = new short[] { 0, 1, 2, 0, 2, 3 };

            var vertexBuffer = Buffer.Vertex.New(GraphicsDevice, vertices, GraphicsResourceUsage.Default);
            var indexBuffer = Buffer.Index.New(GraphicsDevice, indices, GraphicsResourceUsage.Default);
            var meshDraw = new MeshDraw
            {
                DrawCount = 4,
                PrimitiveType = PrimitiveType.TriangleList,
                VertexBuffers = new[]
                {
                    new VertexBufferBinding(vertexBuffer,
                        new VertexDeclaration(VertexElement.Position<Vector3>(),
                            VertexElement.TextureCoordinate<Vector2>()),
                        4)
                },
                IndexBuffer = new IndexBufferBinding(indexBuffer, false, indices.Length),
            };

            var mesh = new Mesh
            {
                Draw = meshDraw,
            };

            simpleEffect = new Effect(GraphicsDevice, SpriteEffect.Bytecode);
            parameterCollection = new ParameterCollection();
            parameterCollectionGroup = new EffectParameterCollectionGroup(GraphicsDevice, simpleEffect, new[] { parameterCollection });
            parameterCollection.Set(TexturingKeys.Texture0, UVTexture);

            vao = VertexArrayObject.New(GraphicsDevice, mesh.Draw.IndexBuffer, mesh.Draw.VertexBuffers);

            myDraws = new DrawOptions[3];
            myDraws[0] = new DrawOptions { Sampler = GraphicsDevice.SamplerStates.LinearClamp, Transform = Matrix.Multiply(Matrix.Scaling(0.4f), Matrix.Translation(-0.5f, 0.5f, 0f)) };
            myDraws[1] = new DrawOptions { Sampler = GraphicsDevice.SamplerStates.LinearWrap, Transform = Matrix.Multiply(Matrix.Scaling(0.4f), Matrix.Translation(0.5f, 0.5f, 0f)) };
            myDraws[2] = new DrawOptions { Sampler = SamplerState.New(GraphicsDevice, new SamplerStateDescription(TextureFilter.Linear, TextureAddressMode.Mirror)), Transform = Matrix.Multiply(Matrix.Scaling(0.4f), Matrix.Translation(0.5f, -0.5f, 0f)) };
            //var borderDescription = new SamplerStateDescription(TextureFilter.Linear, TextureAddressMode.Border) { BorderColor = Color.Purple };
            //var border = SamplerState.New(GraphicsDevice, borderDescription);
            //myDraws[3] = new DrawOptions { Sampler = border, Transform = Matrix.Multiply(Matrix.Scale(0.3f), Matrix.Translation(-0.5f, -0.5f, 0f)) };
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            if(!ScreenShotAutomationEnabled)
                DrawTextureSampling();
        }

        private void DrawTextureSampling()
        {
            // Clears the screen 
            GraphicsDevice.Clear(GraphicsDevice.BackBuffer, Color.LightBlue);
            GraphicsDevice.Clear(GraphicsDevice.DepthStencilBuffer, DepthStencilClearOptions.DepthBuffer | DepthStencilClearOptions.Stencil);
            GraphicsDevice.SetDepthAndRenderTarget(GraphicsDevice.DepthStencilBuffer, GraphicsDevice.BackBuffer);
            GraphicsDevice.SetRasterizerState(GraphicsDevice.RasterizerStates.CullNone);

            GraphicsDevice.SetVertexArrayObject(vao);

            for (var i = 0; i < myDraws.Length; ++i)
            {
                parameterCollection.Set(TexturingKeys.Sampler, myDraws[i].Sampler);
                parameterCollection.Set(SpriteBaseKeys.MatrixTransform, myDraws[i].Transform);
                simpleEffect.Apply(GraphicsDevice, parameterCollectionGroup, true);
                GraphicsDevice.DrawIndexed(PrimitiveType.TriangleList, 6);
            }
            GraphicsDevice.SetVertexArrayObject(null);
        }

        public static void Main()
        {
            using (var game = new TestTextureSampling())
                game.Run();
        }

        /// <summary>
        /// Run the test
        /// </summary>
        [Test]
        public void RunTestTextureSampling()
        {
            RunGameTest(new TestTextureSampling());
        }
    }
}
