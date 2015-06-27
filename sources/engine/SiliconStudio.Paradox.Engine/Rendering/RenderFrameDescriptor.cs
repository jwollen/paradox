// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.ComponentModel;

using SiliconStudio.Core;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Paradox.Rendering
{
    /// <summary>
    /// A descriptor used to create a <see cref="RenderFrame"/>.
    /// </summary>
    [DataContract("RenderFrameDescriptor")]
    public struct RenderFrameDescriptor : IEquatable<RenderFrameDescriptor>
    {
        public RenderFrameDescriptor(int width, int height, RenderFrameFormat format, RenderFrameDepthFormat depthFormat)
            : this()
        {
            Mode = RenderFrameSizeMode.Fixed;
            Width = width;
            Height = height;
            Format = format;
            DepthFormat = depthFormat;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderFrameDescriptor"/> class.
        /// </summary>
        public static RenderFrameDescriptor Default()
        {
            return new RenderFrameDescriptor()
            {
                Mode = RenderFrameSizeMode.Relative,
                Width = 100,
                Height = 100,
                Format = RenderFrameFormat.LDR,
                DepthFormat = RenderFrameDepthFormat.Shared,
            };
        }

        /// <summary>
        /// Gets or sets the mode.
        /// </summary>
        /// <value>The mode.</value>
        [DataMember(10)]
        [DefaultValue(RenderFrameSizeMode.Relative)]
        public RenderFrameSizeMode Mode { get; set; }

        /// <summary>
        /// Gets or sets the width, in pixels when <see cref="Mode"/> is <see cref="RenderFrameSizeMode.Fixed"/> 
        /// or in percentage when <see cref="RenderFrameSizeMode.Relative"/>
        /// </summary>
        /// <value>The width.</value>
        [DataMember(20)]
        [DefaultValue(100)]
        public int Width { get; set; }

        /// <summary>
        /// Gets or sets the height, in pixels when <see cref="Mode"/> is <see cref="RenderFrameSizeMode.Fixed"/> 
        /// or in percentage when <see cref="RenderFrameSizeMode.Relative"/>
        /// </summary>
        /// <value>The height.</value>
        [DataMember(30)]
        [DefaultValue(100)]
        public int Height { get; set; }

        /// <summary>
        /// Gets or sets the pixel format of this render frame.
        /// </summary>
        /// <value>The format.</value>
        [DataMember(40)]
        [DefaultValue(RenderFrameFormat.LDR)]
        public RenderFrameFormat Format { get; set; }

        /// <summary>
        /// Gets or sets the depth format.
        /// </summary>
        /// <value>The depth format.</value>
        [DataMember(50)]
        [DefaultValue(RenderFrameDepthFormat.Shared)]
        public RenderFrameDepthFormat DepthFormat { get; set; }

        public bool Equals(RenderFrameDescriptor other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Mode == other.Mode && Width == other.Width && Height == other.Height && Format == other.Format && DepthFormat == other.DepthFormat;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            return obj is RenderFrameDescriptor && Equals((RenderFrameDescriptor)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = (int)Mode;
                hashCode = (hashCode * 397) ^ Width;
                hashCode = (hashCode * 397) ^ Height;
                hashCode = (hashCode * 397) ^ (int)Format;
                hashCode = (hashCode * 397) ^ (int)DepthFormat;
                return hashCode;
            }
        }

        public static bool operator ==(RenderFrameDescriptor left, RenderFrameDescriptor right)
        {
            return Equals(left, right);
        }

        public static bool operator !=(RenderFrameDescriptor left, RenderFrameDescriptor right)
        {
            return !Equals(left, right);
        }

        public RenderFrameDescriptor Clone()
        {
            return (RenderFrameDescriptor)MemberwiseClone();
        }

        private class Factory : IObjectFactory
        {
            public object New(Type type)
            {
                return Default();
            }
        }
    }
}