﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Runtime.InteropServices;

using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// Describes a custom vertex format structure that contains position as a Vector2. 
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public struct VertexPosition2 : IEquatable<VertexPosition2>
    {
        /// <summary>
        /// Initializes a new <see cref="VertexPositionTexture"/> instance.
        /// </summary>
        /// <param name="position">The position of this vertex.</param>
        public VertexPosition2(Vector2 position)
            : this()
        {
            Position = position;
        }

        /// <summary>
        /// XY position.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        /// Defines structure byte size.
        /// </summary>
        public static readonly int Size = 8;

        /// <summary>
        /// The vertex layout of this struct.
        /// </summary>
        public static readonly VertexDeclaration Layout = new VertexDeclaration(VertexElement.Position<Vector2>());

        public bool Equals(VertexPosition2 other)
        {
            return Position.Equals(other.Position);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is VertexPosition2 && Equals((VertexPosition2)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return Position.GetHashCode();
            }
        }

        public static bool operator ==(VertexPosition2 left, VertexPosition2 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(VertexPosition2 left, VertexPosition2 right)
        {
            return !left.Equals(right);
        }

        public override string ToString()
        {
            return string.Format("Position: {0}", Position);
        }
    }
}
