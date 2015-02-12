﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
//
// Copyright (c) 2010-2012 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using System;
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics.Internals;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// Base class to batch a group of draw calls into one.
    /// </summary>
    /// <typeparam name="TDrawInfo">A structure containing all the required information to draw one element of the batch.</typeparam>
    public abstract class BatchBase<TDrawInfo> : ComponentBase  where TDrawInfo : struct
    {
        /// <summary>
        /// The structure containing all the information required to batch one element.
        /// </summary>
        protected struct ElementInfo
        {
            /// <summary>
            /// The number of vertex needed to draw the element.
            /// </summary>
            public int VertexCount;

            /// <summary>
            /// The number of indices needed to draw the element.
            /// </summary>
            public int IndexCount;

            /// <summary>
            /// The depth of the element. Used to sort the elements.
            /// </summary>
            public float Depth;

            /// <summary>
            /// The user draw information.
            /// </summary>
            public TDrawInfo DrawInfo;

            public ElementInfo(int vertexCount, int indexCount, ref TDrawInfo drawInfo, float depth = 0)
            {
                VertexCount = vertexCount;
                IndexCount = indexCount;
                DrawInfo = drawInfo;
                Depth = depth;
            }
        }

        // TODO: dispose vertex array when Effect is disposed
        protected readonly DeviceResourceContext ResourceContext;

        protected GraphicsDevice GraphicsDevice;
        protected BlendState BlendState;
        protected RasterizerState RasterizerState;
        protected SamplerState SamplerState;
        protected DepthStencilState DepthStencilState;
        protected int StencilReferenceValue;
        protected SpriteSortMode SortMode;
        private EffectParameterResourceBinding? texture0Updater;
        private EffectParameterResourceBinding? texture1Updater;

        private int[] sortIndices;
        private ElementInfo[] sortedDraws;
        private ElementInfo[] drawsQueue;
        private int drawsQueueCount;
        private DrawTextures[] drawTextures;

        private readonly int vertexStructSize;
        private readonly int indexStructSize;
        private readonly VertexDeclaration vertexDeclaration;

        private readonly ParameterCollection parameters;

        /// <summary>
        /// Boolean indicating if we are between a call of Begin and End.
        /// </summary>
        private bool isBeginCalled;

        /// <summary>
        /// The effect used for the current Begin/End session.
        /// </summary>
        protected Effect Effect { get; private set; }
        protected readonly Effect DefaultEffect;

        protected TextureIdComparer TextureComparer { get; set; }
        protected QueueComparer<ElementInfo> BackToFrontComparer { get; set; }
        protected QueueComparer<ElementInfo> FrontToBackComparer { get; set; }

        internal const float DepthBiasShiftOneUnit = 0.0001f;

        protected BatchBase(GraphicsDevice device, Shaders.EffectBytecode defaultEffectByteCode, ResourceBufferInfo resourceBufferInfo, VertexDeclaration vertexDeclaration, int indexSize = sizeof(short))
        {
            if (resourceBufferInfo == null) throw new ArgumentNullException("resourceBufferInfo");
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");

            GraphicsDevice = device;
            DefaultEffect = new Effect(device, defaultEffectByteCode) { Name = "BatchDefaultEffect"};

            drawsQueue = new ElementInfo[resourceBufferInfo.BatchCapacity];
            drawTextures = new DrawTextures[resourceBufferInfo.BatchCapacity];

            TextureComparer = new TextureIdComparer();
            BackToFrontComparer = new SpriteBackToFrontComparer();
            FrontToBackComparer = new SpriteFrontToBackComparer();

            // set the vertex layout and size
            indexStructSize = indexSize;
            this.vertexDeclaration = vertexDeclaration;
            vertexStructSize = vertexDeclaration.CalculateSize();

            parameters = new ParameterCollection();
            
            // Creates the vertex buffer (shared by within a device context).
            ResourceContext = GraphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerContext, resourceBufferInfo.ResourceKey, () => new DeviceResourceContext(GraphicsDevice, DefaultEffect, vertexDeclaration, resourceBufferInfo, indexStructSize));
        }

        /// <summary>
        /// Gets the parameters applied on the SpriteBatch effect.
        /// </summary>
        /// <value>The parameters.</value>
        public ParameterCollection Parameters
        {
            get
            {
                return parameters;
            }
        }

        /// <summary>
        /// Begins a sprite batch rendering using the specified sorting mode and blend state, sampler, depth stencil, rasterizer state objects and a custom effect. 
        /// Passing null for any of the state objects selects the default default state objects (BlendState.AlphaBlend, depthStencilState.None, RasterizerState.CullCounterClockwise, SamplerState.LinearClamp). 
        /// Passing a null effect selects the default effect shader. 
        /// </summary>
        /// <param name="effect">The effect to use for this begin/end draw session (default effect if null)</param>
        /// <param name="sessionSortMode">Sprite drawing order used for the Begin/End session.</param>
        /// <param name="sessionBlendState">Blending state used for the Begin/End session</param>
        /// <param name="sessionSamplerState">Texture sampling used for the Begin/End session</param>
        /// <param name="sessionDepthStencilState">Depth and stencil state used for the Begin/End session</param>
        /// <param name="sessionRasterizerState">Rasterization state used for the Begin/End session</param>
        /// <param name="stencilValue">The value of the stencil buffer to take as reference for the Begin/End session</param>
        protected void Begin(Effect effect, SpriteSortMode sessionSortMode, BlendState sessionBlendState, SamplerState sessionSamplerState, DepthStencilState sessionDepthStencilState, RasterizerState sessionRasterizerState, int stencilValue)
        {
            CheckEndHasBeenCalled("begin");

            SortMode = sessionSortMode;
            BlendState = sessionBlendState;
            SamplerState = sessionSamplerState;
            DepthStencilState = sessionDepthStencilState;
            RasterizerState = sessionRasterizerState;
            StencilReferenceValue = stencilValue;

            Effect = effect ?? DefaultEffect;

            texture0Updater = null;
            texture1Updater = null;
            if (Effect.HasParameter(TexturingKeys.Texture0))
                texture0Updater = Effect.GetParameterFastUpdater(TexturingKeys.Texture0);
            if (Effect.HasParameter(TexturingKeys.Texture1))
                texture1Updater = Effect.GetParameterFastUpdater(TexturingKeys.Texture1);

            // Immediate mode, then prepare for rendering here instead of End()
            if (sessionSortMode == SpriteSortMode.Immediate)
            {
                if (ResourceContext.IsInImmediateMode)
                {
                    throw new InvalidOperationException("Only one SpriteBatch at a time can use SpriteSortMode.Immediate");
                }

                PrepareForRendering();

                ResourceContext.IsInImmediateMode = true;
            }

            // Sets to true isBeginCalled
            isBeginCalled = true;
        }

        protected virtual void PrepareForRendering()
        {
            // Use LinearClamp for sampler state
            var localSamplerState = SamplerState ?? GraphicsDevice.SamplerStates.LinearClamp;

            // Sets the sampler state of the effect
            Parameters.Set(TexturingKeys.Sampler, localSamplerState);
            Effect.Apply(Parameters);

            // Setup states (Blend, DepthStencil, Rasterizer)
            GraphicsDevice.SetBlendState(BlendState ?? GraphicsDevice.BlendStates.AlphaBlend);
            GraphicsDevice.SetDepthStencilState(DepthStencilState ?? GraphicsDevice.DepthStencilStates.Default, StencilReferenceValue);
            GraphicsDevice.SetRasterizerState(RasterizerState ?? GraphicsDevice.RasterizerStates.CullBack);

            // Set VertexInputLayout
            GraphicsDevice.SetVertexArrayObject(ResourceContext.VertexArrayObject);

            // If this is a deferred D3D context, reset position so the first Map call will use D3D11_MAP_WRITE_DISCARD.
            if (GraphicsDevice.IsDeferred)
            {
                ResourceContext.VertexBufferPosition = 0;
            }
        }

        protected void CheckBeginHasBeenCalled(string functionName)
        {
            if (!isBeginCalled)
            {
                throw new InvalidOperationException("Begin must be called before " + functionName);
            }
        }

        protected void CheckEndHasBeenCalled(string functionName)
        {
            if (isBeginCalled)
            {
                throw new InvalidOperationException("End must be called before " + functionName);
            }
        }

        /// <summary>
        /// Flushes the sprite batch and restores the device state to how it was before Begin was called. 
        /// </summary>
        public void End()
        {
            CheckBeginHasBeenCalled("End");

            if (SortMode == SpriteSortMode.Immediate)
            {
                ResourceContext.IsInImmediateMode = false;
            }
            else if (drawsQueueCount > 0)
            {
                // Draw the queued sprites now.
                if (ResourceContext.IsInImmediateMode)
                {
                    throw new InvalidOperationException("Cannot end one SpriteBatch while another is using SpriteSortMode.Immediate");
                }

                // If not immediate, then setup and render all sprites
                PrepareForRendering();
                FlushBatch();
            }

            // We are with begin pair
            isBeginCalled = false;
        }
        
        private void SortSprites()
        {
            IComparer<int> comparer;

            switch (SortMode)
            {
                case SpriteSortMode.Texture:
                    TextureComparer.SpriteTextures = drawTextures;
                    comparer = TextureComparer;
                    break;

                case SpriteSortMode.BackToFront:
                    BackToFrontComparer.ImageInfos = drawsQueue;
                    comparer = BackToFrontComparer;
                    break;

                case SpriteSortMode.FrontToBack:
                    FrontToBackComparer.ImageInfos = drawsQueue;
                    comparer = FrontToBackComparer;
                    break;
                default:
                    throw new NotSupportedException();
            }

            if ((sortIndices == null) || (sortIndices.Length < drawsQueueCount))
            {
                sortIndices = new int[drawsQueueCount];
                sortedDraws = new ElementInfo[drawsQueueCount];
            }

            // Reset all indices to the original order
            for (int i = 0; i < drawsQueueCount; i++)
            {
                sortIndices[i] = i;
            }

            Array.Sort(sortIndices, 0, drawsQueueCount, comparer);
        }

        private void FlushBatch()
        {
            ElementInfo[] spriteQueueForBatch;

            // If Deferred, then sprites are displayed in the same order they arrived
            if (SortMode == SpriteSortMode.Deferred)
            {
                spriteQueueForBatch = drawsQueue;
            }
            else
            {
                // Else Sort all sprites according to their sprite order mode.
                SortSprites();
                spriteQueueForBatch = sortedDraws;
            }

            // Iterate on all sprites and group batch per texture.
            int offset = 0;
            var previousTexture = new DrawTextures();
            for (int i = 0; i < drawsQueueCount; i++)
            {
                DrawTextures texture;

                if (SortMode == SpriteSortMode.Deferred)
                {
                    texture = drawTextures[i];
                }
                else
                {
                    // Copy ordered sprites to the queue to batch
                    int index = sortIndices[i];
                    spriteQueueForBatch[i] = drawsQueue[index];

                    // Get the texture indirectly
                    texture = drawTextures[index];
                }

                if (DrawTextures.NotEqual(ref texture, ref previousTexture))
                {
                    if (i > offset)
                    {
                        DrawBatchPerTexture(previousTexture.Texture0, previousTexture.Texture1, spriteQueueForBatch, offset, i - offset);
                    }

                    offset = i;
                    previousTexture = texture;
                }
            }

            // Draw the last batch
            DrawBatchPerTexture(previousTexture.Texture0, previousTexture.Texture1, spriteQueueForBatch, offset, drawsQueueCount - offset);

            // Reset the queue.
            Array.Clear(drawTextures, 0, drawsQueueCount);
            drawsQueueCount = 0;

            // When sorting is disabled, we persist mSortedSprites data from one batch to the next, to avoid
            // unnecessary work in GrowSortedSprites. But we never reuse these when sorting, because re-sorting
            // previously sorted items gives unstable ordering if some sprites have identical sort keys.
            if (SortMode != SpriteSortMode.Deferred)
            {
                Array.Clear(sortedDraws, 0, sortedDraws.Length);
            }
        }

        private void DrawBatchPerTexture(Texture texture, Texture texture1, ElementInfo[] sprites, int offset, int count)
        {
            // Sets the texture for this sprite effect.
            // Use an optimized version in order to avoid to reapply the sprite effect here just to change texture
            // We are calling directly the PixelShaderStage. We assume that the texture is on slot 0 as it is
            // setup in the original BasicEffect.fx shader.
            if (texture0Updater.HasValue)
                texture0Updater.Value.ApplyParameter(GraphicsDevice, texture);
            if (texture1Updater.HasValue)
                texture1Updater.Value.ApplyParameter(GraphicsDevice, texture1);

            // Draw the batch of sprites
            DrawBatchPerTextureAndPass(sprites, offset, count);
        }

        private void DrawBatchPerTextureAndPass(ElementInfo[] sprites, int offset, int count)
        {
            while (count > 0)
            {
                // How many index/vertex do we want to draw?
                var indexCount = 0;
                var vertexCount = 0;
                var batchSize = 0;

                while (batchSize < count)
                {
                    var spriteIndex = offset + batchSize;

                    // How many sprites does the D3D vertex buffer have room for?
                    var remainingVertexSpace = ResourceContext.VertexCount - ResourceContext.VertexBufferPosition - vertexCount;
                    var remainingIndexSpace = ResourceContext.IndexCount - ResourceContext.IndexBufferPosition - indexCount;

                    // if there is not enough place let for either the indices or vertices of the current element..., 
                    if (sprites[spriteIndex].IndexCount > remainingIndexSpace || sprites[spriteIndex].VertexCount > remainingVertexSpace)
                    {
                        // if we haven't started the current batch yet, we restart at the beginning of the buffers.
                        if (batchSize == 0)
                        {
                            ResourceContext.VertexBufferPosition = 0;
                            ResourceContext.IndexBufferPosition = 0;
                            continue;
                        }

                        // else we perform the draw call and  batch remaining elements in next draw call.
                        break;
                    }

                    ++batchSize;
                    vertexCount += sprites[spriteIndex].VertexCount;
                    indexCount += sprites[spriteIndex].IndexCount;
                }

                // Sets the data directly to the buffer in memory
                var offsetVertexInBytes = ResourceContext.VertexBufferPosition * vertexStructSize;
                var offsetIndexInBytes = ResourceContext.IndexBufferPosition * indexStructSize;
                
                var noOverwriteVertex = ResourceContext.VertexBufferPosition == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;
                var noOverwriteIndex = ResourceContext.IndexBufferPosition == 0 ? MapMode.WriteDiscard : MapMode.WriteNoOverwrite;

                // ------------------------------------------------------------------------------------------------------------
                // CAUTION: Performance problem under x64 resolved by this special codepath:
                // For some unknown reasons, It seems that writing directly to the pointer returned by the MapSubresource is 
                // extremely inefficient using x64 but using a temporary buffer and performing a mempcy to the locked region
                // seems to be running at the same speed than x86
                // ------------------------------------------------------------------------------------------------------------
                // TODO Check again why we need this code
                //if (IntPtr.Size == 8)
                //{
                //    if (x64TempBuffer == null)
                //    {
                //        x64TempBuffer = ToDispose(new DataBuffer(Utilities.SizeOf<VertexPositionColorTexture>() * MaxBatchSize * VerticesPerSprite));
                //    }

                //    // Perform the update of all vertices on a temporary buffer
                //    var texturePtr = (VertexPositionColorTexture*)x64TempBuffer.DataPointer;
                //    for (int i = 0; i < batchSize; i++)
                //    {
                //        UpdateBufferValuesFromElementInfo(ref sprites[offset + i], ref texturePtr, deltaX, deltaY);
                //    }

                //    // Then copy this buffer in one shot
                //    resourceContext.VertexBuffer.SetData(GraphicsDevice, new DataPointer(x64TempBuffer.DataPointer, batchSize * VerticesPerSprite * Utilities.SizeOf<VertexPositionColorTexture>()), offsetInBytes, noOverwrite);
                //}
                //else
                {
                    var mappedIndices = new MappedResource();
                    var mappedVertices = GraphicsDevice.MapSubresource(ResourceContext.VertexBuffer, 0, noOverwriteVertex, false, offsetVertexInBytes, vertexCount * vertexStructSize);
                    if (ResourceContext.IsIndexBufferDynamic)
                        mappedIndices = GraphicsDevice.MapSubresource(ResourceContext.IndexBuffer, 0, noOverwriteIndex, false, offsetIndexInBytes, indexCount * indexStructSize);

                    var vertexPointer = mappedVertices.DataBox.DataPointer;
                    var indexPointer = mappedIndices.DataBox.DataPointer;

                    for (var i = 0; i < batchSize; i++)
                    {
                        var spriteIndex = offset + i;

                        UpdateBufferValuesFromElementInfo(ref sprites[spriteIndex], vertexPointer, indexPointer, ResourceContext.VertexBufferPosition);
                        
                        ResourceContext.VertexBufferPosition += sprites[spriteIndex].VertexCount;
                        vertexPointer += vertexStructSize * sprites[spriteIndex].VertexCount;
                        indexPointer += indexStructSize * sprites[spriteIndex].IndexCount;
                    }

                    GraphicsDevice.UnmapSubresource(mappedVertices);
                    if (ResourceContext.IsIndexBufferDynamic)
                        GraphicsDevice.UnmapSubresource(mappedIndices);
                }

                // Draw from the specified index
                GraphicsDevice.DrawIndexed(PrimitiveType.TriangleList, indexCount, ResourceContext.IndexBufferPosition);

                // Update position, offset and remaining count
                ResourceContext.IndexBufferPosition += indexCount;
                offset += batchSize;
                count -= batchSize;
            }
        }

        protected void Draw(Texture texture, Texture texture1, ref ElementInfo elementInfo)
        {
            // Make sure that Begin was called
            CheckBeginHasBeenCalled("draw");

            // Resize the buffer of SpriteInfo
            if (drawsQueueCount >= drawsQueue.Length)
            {
                Array.Resize(ref drawsQueue, drawsQueue.Length * 2);
            }

            // set the info required to draw the image
            drawsQueue[drawsQueueCount] = elementInfo;

            // If we are in immediate mode, render the sprite directly
            if (SortMode == SpriteSortMode.Immediate)
            {
                DrawBatchPerTexture(texture, texture1, drawsQueue, 0, 1);
            }
            else
            {
                if (drawTextures.Length < drawsQueue.Length)
                {
                    Array.Resize(ref drawTextures, drawsQueue.Length);
                }
                drawTextures[drawsQueueCount].Texture0 = texture;
                drawTextures[drawsQueueCount].Texture1 = texture1;
                drawsQueueCount++;
            }
        }

        /// <summary>
        /// Update the mapped vertex and index buffer values using the provided element info.
        /// </summary>
        /// <param name="elementInfo">The structure containing the information about the element to draw.</param>
        /// <param name="vertexPointer">The pointer to the vertex array buffer to update.</param>
        /// <param name="indexPointer">The pointer to the index array buffer to update. This value is null if the index buffer used is static.</param>
        /// <param name="vexterStartOffset">The offset in the vertex buffer where the vertex of the element starts</param>
        protected abstract void UpdateBufferValuesFromElementInfo(ref ElementInfo elementInfo, IntPtr vertexPointer, IntPtr indexPointer, int vexterStartOffset);
        
        #region Nested types

        protected struct DrawTextures
        {
            public Texture Texture0;
            public Texture Texture1;
            
            public static bool NotEqual(ref DrawTextures left, ref DrawTextures right)
            {
                return left.Texture0 != right.Texture0 || left.Texture1 != right.Texture1;
            }
        }

        /// <summary>
        /// A class containing information on how to build the batch vertex and index buffer.
        /// </summary>
        protected class ResourceBufferInfo
        {
            /// <summary>
            /// The key used to identify the GPU resource.
            /// </summary>
            public readonly string ResourceKey;

            /// <summary>
            /// The initial number of draw calls that can be batched at one time.
            /// </summary>
            /// <remarks>Data structure will adjust their size when needed if capacity is not sufficient</remarks>
            public int BatchCapacity { get; set; }

            /// <summary>
            /// Gets the number indices of the vertex buffer.
            /// </summary>
            public int VertexCount { get; protected set; }

            /// <summary>
            /// Gets the number indices of the index buffer.
            /// </summary>
            public int IndexCount { get; private set; }

            /// <summary>
            /// Gets or sets the static indices to use for the index buffer.
            /// </summary>
            public short[] StaticIndices;
            
            /// <summary>
            /// Gets the value indicating whether the index buffer is static or dynamic.
            /// </summary>
            public bool IsIndexBufferDynamic { get { return StaticIndices == null; } }

            /// <summary>
            /// Create the buffer resource information for a batch having both a dynamic index buffer and vertex buffer.
            /// </summary>
            /// <param name="resourceKey">The name of key to use to identify the resource</param>
            /// <param name="indexCount">The number of indices contained by the index buffer</param>
            /// <param name="vertexCount">The number of vertices contained by the vertex buffer</param>
            public static ResourceBufferInfo CreateDynamicIndexBufferInfo(string resourceKey, int indexCount, int vertexCount)
            {
                return new ResourceBufferInfo(resourceKey, null, indexCount, vertexCount);
            }

            /// <summary>
            /// Create the buffer resource information for a batch having a dynamic vertex buffer but a static index buffer.
            /// </summary>
            /// <param name="resourceKey">The name of key to use to identify the resource</param>
            /// <param name="staticIndices">The value of the indices to upload into the index buffer.</param>
            /// <param name="vertexCount">The number of vertices contained by the vertex buffer</param>
            public static ResourceBufferInfo CreateStaticIndexBufferInfo(string resourceKey, short[] staticIndices, int vertexCount)
            {
                return new ResourceBufferInfo(resourceKey, staticIndices, 0, vertexCount);
            }

            protected ResourceBufferInfo(string resourceKey, short[] staticIndices, int indexCount, int vertexCount)
            {
                if (staticIndices != null)
                    indexCount = staticIndices.Length;

                BatchCapacity = 64;
                ResourceKey = resourceKey;
                StaticIndices = staticIndices;
                IndexCount = indexCount;
                VertexCount = vertexCount;
            }
        }
        /// <summary>
        /// A class containing the information required to build a vertex and index buffer for simple quad based batching.
        /// </summary>
        /// <remarks>
        /// The index buffer is used in static mode and contains six indices for each quad. The vertex buffer contains 4 vertices for each quad.
        /// Rectangle is composed of two triangles as follow: 
        ///  v0 - - - v1
        ///  |  \      |
        ///  |    \ t1 |
        ///  | t2   \  |
        ///  v3 - - - v2
        /// </remarks>
        protected class StaticQuadBufferInfo: ResourceBufferInfo
        {
            public const int IndicesByElement = 6;

            public const int VertexByElement = 4;

            private StaticQuadBufferInfo(string resourceKey, short[] staticIndices, int vertexCount)
                : base(resourceKey, staticIndices, 0, vertexCount)
            {
            }

            public static StaticQuadBufferInfo CreateQuadBufferInfo(string resourceKey, int maxQuadNumber, int batchCapacity = 64)
            {
                var indices = new short[maxQuadNumber * IndicesByElement];
                var k = 0;
                for (var i = 0; i < indices.Length; k += VertexByElement)
                {
                    indices[i++] = (short)(k + 0);
                    indices[i++] = (short)(k + 1);
                    indices[i++] = (short)(k + 2);
                    indices[i++] = (short)(k + 0);
                    indices[i++] = (short)(k + 2);
                    indices[i++] = (short)(k + 3);
                }

                return new StaticQuadBufferInfo(resourceKey, indices, VertexByElement * maxQuadNumber) { BatchCapacity = batchCapacity };
            }
        }

        protected class TextureIdComparer : IComparer<int>
        {
            public DrawTextures[] SpriteTextures;

            public int Compare(int left, int right)
            {
                return SpriteTextures[left].Texture0.Id.CompareTo(SpriteTextures[right].Texture0.Id);
            }
        }

        private class SpriteBackToFrontComparer : QueueComparer<ElementInfo>
        {
            public override int Compare(int left, int right)
            {
                return ImageInfos[left].Depth.CompareTo(ImageInfos[right].Depth);
            }
        }

        private class SpriteFrontToBackComparer : QueueComparer<ElementInfo>
        {
            public override int Compare(int left, int right)
            {
                return ImageInfos[right].Depth.CompareTo(ImageInfos[left].Depth);
            }
        }
        
        protected abstract class QueueComparer<TInfo> : IComparer<int>
        {
            public TInfo[] ImageInfos;

            public abstract int Compare(int x, int y);
        }
        
        /// <summary>
        /// Use a ResourceContext per GraphicsDevice (DeviceContext)
        /// </summary>
        protected class DeviceResourceContext : ComponentBase
        {
            /// <summary>
            /// Gets the number of vertices.
            /// </summary>
            public readonly int VertexCount;

            /// <summary>
            /// The vertex buffer of the batch.
            /// </summary>
            public readonly Buffer VertexBuffer;

            /// <summary>
            /// Gets the number of indices.
            /// </summary>
            public readonly int IndexCount;

            /// <summary>
            /// The index buffer of the batch.
            /// </summary>
            public readonly Buffer IndexBuffer;

            /// <summary>
            /// Gets a boolean indicating if the index buffer is dynamic.
            /// </summary>
            public readonly bool IsIndexBufferDynamic;

            /// <summary>
            /// The VertexArrayObject of the batch.
            /// </summary>
            public readonly VertexArrayObject VertexArrayObject;

            /// <summary>
            /// The current position in vertex into the vertex array buffer.
            /// </summary>
            public int VertexBufferPosition;

            /// <summary>
            /// The current position in index into the index array buffer.
            /// </summary>
            public int IndexBufferPosition;

            /// <summary>
            /// Indicate if the batch system is drawing in immediate mode for this buffer.
            /// </summary>
            public bool IsInImmediateMode;

            public DeviceResourceContext(GraphicsDevice device, Effect effect, VertexDeclaration declaration, ResourceBufferInfo resourceBufferInfo, int indexStructSize)
            {
                var vertexSize = declaration.CalculateSize();

                VertexCount = resourceBufferInfo.VertexCount;
                IndexCount = resourceBufferInfo.IndexCount;
                IsIndexBufferDynamic = resourceBufferInfo.IsIndexBufferDynamic;

                VertexBuffer = Buffer.Vertex.New(device, VertexCount * vertexSize, GraphicsResourceUsage.Dynamic).DisposeBy(this);

                if (IsIndexBufferDynamic)
                {
                    IndexBuffer = Buffer.Index.New(device, IndexCount * indexStructSize, GraphicsResourceUsage.Dynamic).DisposeBy(this);
                }
                else
                {
                    IndexBuffer = Buffer.Index.New(device, resourceBufferInfo.StaticIndices).DisposeBy(this);
                    IndexBuffer.Reload = graphicsResource => ((Buffer)graphicsResource).Recreate(resourceBufferInfo.StaticIndices);
                }

                var indexBufferBinding = new IndexBufferBinding(IndexBuffer, indexStructSize == sizeof(int), IndexBuffer.Description.SizeInBytes / indexStructSize);
                var vertexBufferBinding = new VertexBufferBinding(VertexBuffer, declaration, VertexCount, vertexSize);

                // Creates a VAO
                VertexArrayObject = VertexArrayObject.New(device, effect.InputSignature, indexBufferBinding, vertexBufferBinding).DisposeBy(this);
            }
        }

        #endregion
    }
}
