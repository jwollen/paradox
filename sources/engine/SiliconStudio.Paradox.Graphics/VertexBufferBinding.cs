﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Serializers;

namespace SiliconStudio.Paradox.Graphics
{    
    /// <summary>
    /// Binding structure that specifies a vertex buffer and other per-vertex parameters (such as offset and instancing) for a graphics device.
    /// </summary>
    [DataSerializer(typeof(VertexBufferBinding.Serializer))]
    public struct VertexBufferBinding : IEquatable<VertexBufferBinding>
    {
        private readonly int hashCode;

        /// <summary>
        /// Creates an instance of this object.
        /// </summary>
        /// <param name="vertexBuffer">The vertex buffer</param>
        /// <param name="vertexDeclaration">The vertex declaration.</param>
        /// <param name="vertexCount">The vertex count.</param>
        /// <param name="vertexStride">The vertex stride.</param>
        /// <param name="vertexOffset">Offset (in Vertex ElementCount) from the beginning of the buffer to the first vertex to use.</param>
        public VertexBufferBinding(Buffer vertexBuffer, VertexDeclaration vertexDeclaration, int vertexCount, int vertexStride = 0, int vertexOffset = 0) : this()
        {
            if (vertexBuffer == null) throw new ArgumentNullException("vertexBuffer");
            if (vertexDeclaration == null) throw new ArgumentNullException("vertexDeclaration");

            Buffer = vertexBuffer;
            Stride = vertexStride != 0 ? vertexStride : vertexDeclaration.VertexStride;
            Offset = vertexOffset;
            Count = vertexCount;
            Declaration = vertexDeclaration;

            unchecked
            {
                hashCode = Buffer.GetHashCode();
                hashCode = (hashCode*397) ^ Offset;
                hashCode = (hashCode*397) ^ Stride;
                hashCode = (hashCode*397) ^ Count;
                hashCode = (hashCode*397) ^ Declaration.GetHashCode();
            }
        }

        /// <summary>
        /// Gets a vertex buffer.
        /// </summary>
        public Buffer Buffer { get; private set; }

        /// <summary>
        /// Gets the offset (vertex index) between the beginning of the buffer and the vertex data to use.
        /// </summary>
        public int Offset { get; private set; }

        /// <summary>
        /// Gets the vertex stride.
        /// </summary>
        public int Stride { get; private set; }

        /// <summary>
        /// Gets the number of vertex.
        /// </summary>
        /// <value>The count.</value>
        public int Count { get; private set; }

        /// <summary>
        /// Gets the layout of the vertex buffer.
        /// </summary>
        /// <value>The declaration.</value>
        public VertexDeclaration Declaration { get; private set; }

        public bool Equals(VertexBufferBinding other)
        {
            return Buffer.Equals(other.Buffer) && Offset == other.Offset && Stride == other.Stride && Count == other.Count && Declaration.Equals(other.Declaration);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexBufferBinding && Equals((VertexBufferBinding)obj);
        }

        public override int GetHashCode()
        {
            return hashCode;
        }

        internal class Serializer : DataSerializer<VertexBufferBinding>
        {
            public override void Serialize(ref VertexBufferBinding vertexBufferBinding, ArchiveMode mode, SerializationStream stream)
            {
                if (mode == ArchiveMode.Deserialize)
                {
                    var buffer = stream.Read<Buffer>();
                    var declaration = stream.Read<VertexDeclaration>();
                    var count = stream.ReadInt32();
                    var stride = stream.ReadInt32();
                    var offset = stream.ReadInt32();

                    vertexBufferBinding = new VertexBufferBinding(buffer, declaration, count, stride, offset);
                }
                else
                {
                    stream.Write(vertexBufferBinding.Buffer);
                    stream.Write(vertexBufferBinding.Declaration);
                    stream.Write(vertexBufferBinding.Count);
                    stream.Write(vertexBufferBinding.Stride);
                    stream.Write(vertexBufferBinding.Offset);
                }
            }
        }
    }
}
