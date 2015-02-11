﻿using System;
using System.Collections.Generic;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Effects.Data;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders;

using Buffer = SiliconStudio.Paradox.Graphics.Buffer;

namespace SiliconStudio.Paradox.Effects
{
    /// <summary>
    /// TODO: Update summary.
    /// </summary>
    public class PostEffectSeparateShaderPlugin : ShaderPlugin<RenderPassPlugin>
    {
        /// <param name="effectMesh"></param>
        /// <inheritdoc/>
        /// <inheritdoc/>
        public override void SetupShaders(EffectMesh effectMesh)
        {
            // Create shader from base shader
            var postEffect = new ShaderClassSource("PostEffectBase");
            DefaultShaderPass.Shader.Mixins.Add(postEffect);
        }

        /// <param name="effectMesh"></param>
        /// <inheritdoc/>
        public override void SetupResources(EffectMesh effectMesh)
        {
            // PrepareMesh event so that this quad is used for each EffectMesh
            Effect.PrepareMesh += SetupMeshResources;
        }

        void SetupMeshResources(EffectOld effect, EffectMesh effectMesh)
        {
            // Generates a quad for post effect rendering (should be utility function)
            var vertices = new[]
            {
                -1.0f,  1.0f, 
                 1.0f,  1.0f,
                -1.0f, -1.0f, 
                 1.0f, -1.0f,
            };

            // Use the quad for this effectMesh
            effectMesh.MeshData.Draw = new MeshDraw
                {
                    DrawCount = 4,
                    PrimitiveType = PrimitiveType.TriangleStrip,
                    VertexBuffers = new[]
                            {
                                new VertexBufferBinding(Buffer.Vertex.New(GraphicsDevice, vertices), new VertexDeclaration(VertexElement.Position<Vector2>()), 4)
                            }
                };
        
            // TODO: unbind render targets
            var previousRender = effectMesh.Render;
            effectMesh.Render += (threadContext) =>
                {
                    // Setup render target
                    var renderTarget = effectMesh.Parameters.Get(RenderTargetKeys.RenderTarget);
                    var desc = renderTarget.Description;
                    threadContext.GraphicsDevice.SetViewport(new Viewport(0, 0, desc.Width, desc.Height));
                    threadContext.GraphicsDevice.SetRenderTarget(renderTarget);

                    // Draw
                    previousRender.Invoke(threadContext);

                    // Unbind RenderTargets
                    threadContext.GraphicsDevice.UnsetRenderTargets();
                };
        }
    }
}