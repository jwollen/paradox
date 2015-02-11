﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
namespace SiliconStudio.Paradox.Graphics
{
    public partial struct TextureDescription
    {
        /// <summary>
        /// Creates a new 1D <see cref="TextureDescription" /> with a single mipmap.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of 1D <see cref="TextureDescription" /> class.</returns>
        public static TextureDescription New1D(int width, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New1D(width, false, format, textureFlags, arraySize, usage);
        }

        /// <summary>
        /// Creates a new 1D <see cref="TextureDescription" />.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="mipCount">Number of mipmaps, set to true to have all mipmaps, set to an int &gt;=1 for a particular mipmap count.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="arraySize">Size of the texture 2D array, default to 1.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of 1D <see cref="TextureDescription" /> class.</returns>
        public static TextureDescription New1D(int width, MipMapCount mipCount, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, int arraySize = 1, GraphicsResourceUsage usage = GraphicsResourceUsage.Default)
        {
            return New1D(width, format, textureFlags, mipCount, arraySize, usage);
        }

        /// <summary>
        /// Creates a new 1D <see cref="TextureDescription" /> with a single level of mipmap.
        /// </summary>
        /// <param name="width">The width.</param>
        /// <param name="format">Describes the format to use.</param>
        /// <param name="textureFlags">true if the texture needs to support unordered read write.</param>
        /// <param name="usage">The usage.</param>
        /// <returns>A new instance of 1D <see cref="TextureDescription" /> class.</returns>
        /// <remarks>The first dimension of mipMapTextures describes the number of array (Texture1D Array), second dimension is the mipmap, the third is the texture data for a particular mipmap.</remarks>
        public static TextureDescription New1D(int width, PixelFormat format, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            return New1D(width, format, textureFlags, 1, 1, usage);
        }

        private static TextureDescription New1D(int width, PixelFormat format, TextureFlags flags, int mipCount, int arraySize, GraphicsResourceUsage usage)
        {
            usage = (flags & TextureFlags.UnorderedAccess) != 0 ? GraphicsResourceUsage.Default : usage;
            var desc = new TextureDescription()
            {
                Dimension = TextureDimension.Texture1D,
                Width = width,
                Height = 1,
                Depth = 1,
                ArraySize = arraySize,
                Flags = flags,
                Format = format,
                MipLevels = Texture.CalculateMipMapCount(mipCount, width),
                Usage = Texture.GetUsageWithFlags(usage, flags),
            };
            return desc;
        }
    }
}