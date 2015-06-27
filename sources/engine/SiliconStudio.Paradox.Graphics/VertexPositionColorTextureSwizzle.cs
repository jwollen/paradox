﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Runtime.InteropServices;

using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position, color, texture and swizzle information. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPositionColorTextureSwizzle : IEquatable<VertexPositionColorTextureSwizzle>, IVertex
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionColorTexture"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        /// <param name="color">The color of this vertex.</param>
        /// <param name="textureCoordinate">UV texture coordinates.</param>
        /// <param name="swizzle">The swizzle mode</param>
        public VertexPositionColorTextureSwizzle(Vector4 position, Color color, Vector2 textureCoordinate, SwizzleMode swizzle)
            : this()
        {
            Position = position;
            Color = color;
            TextureCoordinate = textureCoordinate;
            Swizzle = (int)swizzle;
        }

        /// <summary>
        /// XYZ position.
        /// </summary>
        public Vector4 Position;

        /// <summary>
        /// The vertex color.
        /// </summary>
        public Color Color;

        /// <summary>
        /// UV texture coordinates.
        /// </summary>
        public Vector2 TextureCoordinate;

        /// <summary>
        /// The Swizzle mode
        /// </summary>
        public float Swizzle;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 32;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(
            VertexElement.Position<Vector4>(),
            VertexElement.Color<Color>(),
            VertexElement.TextureCoordinate<Vector2>(),
            new VertexElement("BATCH_SWIZZLE", PixelFormat.R32_Float)
            );


        public bool Equals(VertexPositionColorTextureSwizzle other)
        {
            return Position.Equals(other.Position) && Color.Equals(other.Color) && TextureCoordinate.Equals(other.TextureCoordinate);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPositionColorTextureSwizzle && Equals((VertexPositionColorTextureSwizzle)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Position.GetHashCode();
                hashCode = (hashCode * 397) ^ Color.GetHashCode();
                hashCode = (hashCode * 397) ^ TextureCoordinate.GetHashCode();
                hashCode = (hashCode * 397) ^ Swizzle.GetHashCode();
                return hashCode;
            }
        }

        public static bool operator ==(VertexPositionColorTextureSwizzle left, VertexPositionColorTextureSwizzle right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPositionColorTextureSwizzle left, VertexPositionColorTextureSwizzle right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}, Color: {1}, Texcoord: {2}, Swizzle: {3}", Position, Color, TextureCoordinate, Swizzle);
        }

        public VertexDeclaration GetLayout()
        {
            return Layout;
        }

        public void FlipWinding()
        {
            TextureCoordinate.X = (1.0f - TextureCoordinate.X);
        }
    }
}