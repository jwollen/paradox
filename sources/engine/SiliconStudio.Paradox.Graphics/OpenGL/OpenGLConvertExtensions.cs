﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGL 
using System;
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
using ES30 = OpenTK.Graphics.ES30;
using PixelFormatGl = OpenTK.Graphics.ES30.PixelFormat;
#if SILICONSTUDIO_PLATFORM_IOS
using ExtTextureFormatBgra8888 = OpenTK.Graphics.ES30.All;
using ImgTextureCompressionPvrtc = OpenTK.Graphics.ES30.All;
using OesPackedDepthStencil = OpenTK.Graphics.ES30.All;
#elif SILICONSTUDIO_PLATFORM_ANDROID
using ExtTextureFormatBgra8888 = OpenTK.Graphics.ES20.ExtTextureFormatBgra8888;
using OesCompressedEtc1Rgb8Texture = OpenTK.Graphics.ES20.OesCompressedEtc1Rgb8Texture;
#endif
#else
using OpenTK.Graphics.OpenGL;
using PixelFormatGl = OpenTK.Graphics.OpenGL.PixelFormat;
using PrimitiveTypeGl = OpenTK.Graphics.OpenGL.PrimitiveType;
#endif

namespace SiliconStudio.Paradox.Graphics
{
    internal static class OpenGLConvertExtensions
    {
        // Define missing constants
        // values taken form https://www.khronos.org/registry/gles/api/GLES3/gl3.h
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
        private const PixelInternalFormat DepthComponent16 = (PixelInternalFormat)0x81A5;
        private const PixelInternalFormat Depth24Stencil8 = (PixelInternalFormat)0x88F0;
        private const PixelInternalFormat DepthComponent32f = (PixelInternalFormat)0x8CAC;
        private const PixelInternalFormat R8 = (PixelInternalFormat)0x8229;
        private const PixelInternalFormat Rgba16f = (PixelInternalFormat)0x881A;
        private const PixelInternalFormat R32ui = (PixelInternalFormat)0x8236;
        private const PixelInternalFormat R32f = (PixelInternalFormat)0x822E;
        private const PixelInternalFormat Rg32f = (PixelInternalFormat)0x8230;
        private const PixelInternalFormat Rgb32f = (PixelInternalFormat)0x8815;
        private const PixelInternalFormat Rgba32f = (PixelInternalFormat)0x8814;
#else
        private const PixelInternalFormat DepthComponent16 = PixelInternalFormat.DepthComponent16;
        private const PixelInternalFormat Depth24Stencil8 = PixelInternalFormat.Depth24Stencil8;
        private const PixelInternalFormat DepthComponent32f = PixelInternalFormat.DepthComponent32f;
        private const PixelInternalFormat R8 = PixelInternalFormat.R8;
        private const PixelInternalFormat Rgba16f = PixelInternalFormat.Rgba16f;
        private const PixelInternalFormat R32ui = PixelInternalFormat.R32ui;
        private const PixelInternalFormat R32f = PixelInternalFormat.R32f;
        private const PixelInternalFormat Rg32f = PixelInternalFormat.Rg32f;
        private const PixelInternalFormat Rgb32f = PixelInternalFormat.Rgb32f;
        private const PixelInternalFormat Rgba32f = PixelInternalFormat.Rgba32f;
#endif

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES && !SILICONSTUDIO_PLATFORM_MONO_MOBILE
        private const TextureWrapMode TextureWrapModeMirroredRepeat = (TextureWrapMode)0x8370;
#else
        private const TextureWrapMode TextureWrapModeMirroredRepeat = TextureWrapMode.MirroredRepeat;
#endif

        public static ErrorCode GetErrorCode()
        {
#if SILICONSTUDIO_PLATFORM_ANDROID || SILICONSTUDIO_PLATFORM_IOS
            return GL.GetErrorCode();
#else
            return GL.GetError();
#endif
        }

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
#if !SILICONSTUDIO_PLATFORM_MONO_MOBILE
        public static ES30.PrimitiveType ToOpenGL(this PrimitiveType primitiveType)
        {
            return primitiveType.ToOpenGLES();
        }
#else
        public static BeginMode ToOpenGL(this PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return BeginMode.Points;
                case PrimitiveType.LineList:
                    return BeginMode.Lines;
                case PrimitiveType.LineStrip:
                    return BeginMode.LineStrip;
                case PrimitiveType.TriangleList:
                    return BeginMode.Triangles;
                case PrimitiveType.TriangleStrip:
                    return BeginMode.TriangleStrip;
                default:
                    throw new NotImplementedException();
            }
        }
#endif
#else
        public static PrimitiveTypeGl ToOpenGL(this PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return PrimitiveTypeGl.Points;
                case PrimitiveType.LineList:
                    return PrimitiveTypeGl.Lines;
                case PrimitiveType.LineStrip:
                    return PrimitiveTypeGl.LineStrip;
                case PrimitiveType.TriangleList:
                    return PrimitiveTypeGl.Triangles;
                case PrimitiveType.TriangleStrip:
                    return PrimitiveTypeGl.TriangleStrip;
                default:
                    throw new NotImplementedException();
            }
        }
#endif

        public static BufferAccessMask ToOpenGLMask(this MapMode mapMode)
        {
            switch (mapMode)
            {
                case MapMode.Read:
                    return BufferAccessMask.MapReadBit;
                case MapMode.Write:
                    return BufferAccessMask.MapWriteBit;
                case MapMode.ReadWrite:
                    return BufferAccessMask.MapReadBit | BufferAccessMask.MapWriteBit;
                case MapMode.WriteDiscard:
                    return BufferAccessMask.MapWriteBit | BufferAccessMask.MapInvalidateBufferBit;
                case MapMode.WriteNoOverwrite:
                    return BufferAccessMask.MapWriteBit | BufferAccessMask.MapUnsynchronizedBit;
                default:
                    throw new ArgumentOutOfRangeException("mapMode");
            }
        }

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
        public static ES30.PrimitiveType ToOpenGLES(this PrimitiveType primitiveType)
        {
            switch (primitiveType)
            {
                case PrimitiveType.PointList:
                    return ES30.PrimitiveType.Points;
                case PrimitiveType.LineList:
                    return ES30.PrimitiveType.Lines;
                case PrimitiveType.LineStrip:
                    return ES30.PrimitiveType.LineStrip;
                case PrimitiveType.TriangleList:
                    return ES30.PrimitiveType.Triangles;
                case PrimitiveType.TriangleStrip:
                    return ES30.PrimitiveType.TriangleStrip;
                default:
                    throw new NotImplementedException();
            }
        }
#else
        public static BufferAccess ToOpenGL(this MapMode mapMode)
        {
            switch (mapMode)
            {
                case MapMode.Read:
                    return BufferAccess.ReadOnly;
                case MapMode.Write:
                case MapMode.WriteDiscard:
                case MapMode.WriteNoOverwrite:
                    return BufferAccess.WriteOnly;
                case MapMode.ReadWrite:
                    return BufferAccess.ReadWrite;
                default:
                    throw new ArgumentOutOfRangeException("mapMode");
            }
        }
#endif

        public static TextureWrapMode ToOpenGL(this TextureAddressMode addressMode)
        {
            switch (addressMode)
            {
                case TextureAddressMode.Border:
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                    return TextureWrapMode.ClampToBorder;
#endif
                case TextureAddressMode.Clamp:
                    return TextureWrapMode.ClampToEdge;
                case TextureAddressMode.Mirror:
                    return TextureWrapModeMirroredRepeat;
                case TextureAddressMode.Wrap:
                    return TextureWrapMode.Repeat;
                default:
                    throw new NotImplementedException();
            }
        }

        public static DepthFunction ToOpenGLDepthFunction(this CompareFunction function)
        {
            switch (function)
            {
                case CompareFunction.Always:
                    return DepthFunction.Always;
                case CompareFunction.Equal:
                    return DepthFunction.Equal;
                case CompareFunction.GreaterEqual:
                    return DepthFunction.Gequal;
                case CompareFunction.Greater:
                    return DepthFunction.Greater;
                case CompareFunction.LessEqual:
                    return DepthFunction.Lequal;
                case CompareFunction.Less:
                    return DepthFunction.Less;
                case CompareFunction.Never:
                    return DepthFunction.Never;
                case CompareFunction.NotEqual:
                    return DepthFunction.Notequal;
                default:
                    throw new NotImplementedException();
            }
        }
        
        public static StencilFunction ToOpenGLStencilFunction(this CompareFunction function)
        {
            switch (function)
            {
                case CompareFunction.Always:
                    return StencilFunction.Always;
                case CompareFunction.Equal:
                    return StencilFunction.Equal;
                case CompareFunction.GreaterEqual:
                    return StencilFunction.Gequal;
                case CompareFunction.Greater:
                    return StencilFunction.Greater;
                case CompareFunction.LessEqual:
                    return StencilFunction.Lequal;
                case CompareFunction.Less:
                    return StencilFunction.Less;
                case CompareFunction.Never:
                    return StencilFunction.Never;
                case CompareFunction.NotEqual:
                    return StencilFunction.Notequal;
                default:
                    throw new NotImplementedException();
            }
        }

        public static StencilOp ToOpenGL(this StencilOperation operation)
        {
            switch (operation)
            {
                case StencilOperation.Keep:
                    return StencilOp.Keep;
                case StencilOperation.Zero:
                    return StencilOp.Zero;
                case StencilOperation.Replace:
                    return StencilOp.Replace;
                case StencilOperation.IncrementSaturation:
                    return StencilOp.Incr;
                case StencilOperation.DecrementSaturation:
                    return StencilOp.Decr;
                case StencilOperation.Invert:
                    return StencilOp.Invert;
                case StencilOperation.Increment:
                    return StencilOp.IncrWrap;
                case StencilOperation.Decrement:
                    return StencilOp.DecrWrap;
                default:
                    throw new ArgumentOutOfRangeException("operation");
            }
        }

        public static void ConvertPixelFormat(GraphicsDevice graphicsDevice, PixelFormat inputFormat, out PixelInternalFormat internalFormat, out PixelFormatGl format, out PixelType type, out int pixelSize, out bool compressed)
        {
            compressed = false;

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
            // check formats is the device is initialized with OpenGL ES 2
            if (graphicsDevice.IsOpenGLES2)
            {
                switch (inputFormat)
                {
                    case PixelFormat.R32_UInt:
                    case PixelFormat.R32_Float:
                    case PixelFormat.R32G32_Float:
                    case PixelFormat.R32G32B32_Float:
                    case PixelFormat.R16G16B16A16_Float:
                    case PixelFormat.R32G32B32A32_Float:
                    case PixelFormat.D32_Float:
                        throw new NotSupportedException(String.Format("Texture format {0} not supported", inputFormat));
                    case PixelFormat.D24_UNorm_S8_UInt:
                        if (!(graphicsDevice.HasDepth24 && graphicsDevice.HasPackedDepthStencilExtension))
                            throw new NotSupportedException(String.Format("Texture format {0} not supported", inputFormat));
                        break;
                }
            }
#endif

            switch (inputFormat)
            {
                case PixelFormat.A8_UNorm:
                    internalFormat = PixelInternalFormat.Alpha;
                    format = PixelFormatGl.Alpha;
                    type = PixelType.UnsignedByte;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8_UNorm:
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                    if (graphicsDevice.IsOpenGLES2)
                    {
                        internalFormat = PixelInternalFormat.Luminance;
                        format = PixelFormatGl.Luminance;
                    }
                    else
#endif
                    {
                        internalFormat = R8;
                        format = PixelFormatGl.Red;
                    }
                    type = PixelType.UnsignedByte;
                    pixelSize = 1;
                    break;
                case PixelFormat.R8G8B8A8_UNorm:
                    internalFormat = PixelInternalFormat.Rgba;
                    format = PixelFormatGl.Rgba;
                    type = PixelType.UnsignedByte;
                    pixelSize = 4;
                    break;
                case PixelFormat.B8G8R8A8_UNorm:
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                    if (!graphicsDevice.HasExtTextureFormatBGRA8888)
                        throw new NotSupportedException();

                    // It seems iOS and Android expects different things
#if SILICONSTUDIO_PLATFORM_IOS
                    internalFormat = PixelInternalFormat.Rgba;
#else
                    internalFormat = (PixelInternalFormat)ExtTextureFormatBgra8888.BgraExt;
#endif
                    format = (PixelFormatGl)ExtTextureFormatBgra8888.BgraExt;
#else
                    internalFormat = PixelInternalFormat.Rgba;
                    format = PixelFormatGl.Bgra;
#endif
                    type = PixelType.UnsignedByte;
                    pixelSize = 4;
                    break;
                case PixelFormat.R16G16B16A16_Float:
                    internalFormat = Rgba16f;
                    format = PixelFormatGl.Rgba;
                    type = PixelType.HalfFloat;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32_UInt:
                    internalFormat = R32ui;
                    format = PixelFormatGl.RedInteger;
                    type = PixelType.UnsignedInt;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32_Float:
                    internalFormat = R32f;
                    format = PixelFormatGl.Red;
                    type = PixelType.Float;
                    pixelSize = 4;
                    break;
                case PixelFormat.R32G32_Float:
                    internalFormat = Rg32f;
                    format = PixelFormatGl.Rg;
                    type = PixelType.Float;
                    pixelSize = 8;
                    break;
                case PixelFormat.R32G32B32_Float:
                    internalFormat = Rgb32f;
                    format = PixelFormatGl.Rgb;
                    type = PixelType.Float;
                    pixelSize = 12;
                    break;
                case PixelFormat.R32G32B32A32_Float:
                    internalFormat = Rgba32f;
                    format = PixelFormatGl.Rgba;
                    type = PixelType.Float;
                    pixelSize = 16;
                    break;
                case PixelFormat.D16_UNorm:
                    internalFormat = DepthComponent16;
                    format = PixelFormatGl.DepthComponent;
                    type = PixelType.UnsignedShort;
                    pixelSize = 2;
                    break;
                case PixelFormat.D24_UNorm_S8_UInt:
                    internalFormat = Depth24Stencil8;
                    format = PixelFormatGl.DepthComponent;
                    type = PixelType.UnsignedInt248;
                    pixelSize = 4;
                    break;
                // TODO: Temporary depth format (need to decide relation between RenderTarget1D and Texture)
                case PixelFormat.D32_Float:
                    internalFormat = DepthComponent32f;
                    format = PixelFormatGl.DepthComponent;
                    type = PixelType.Float;
                    pixelSize = 4;
                    break;
#if SILICONSTUDIO_PLATFORM_IOS
                case PixelFormat.PVRTC_4bpp_RGB:
                    internalFormat = (PixelInternalFormat)ImgTextureCompressionPvrtc.CompressedRgbPvrtc4Bppv1Img;
                    format = (PixelFormatGl)ImgTextureCompressionPvrtc.CompressedRgbPvrtc4Bppv1Img;
                    compressed = true;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    break;
                case PixelFormat.PVRTC_2bpp_RGB:
                    internalFormat = (PixelInternalFormat)ImgTextureCompressionPvrtc.CompressedRgbPvrtc2Bppv1Img;
                    format = (PixelFormatGl)ImgTextureCompressionPvrtc.CompressedRgbPvrtc2Bppv1Img;
                    compressed = true;
                    pixelSize = 2;
                    type = PixelType.UnsignedByte;
                    break;
                case PixelFormat.PVRTC_4bpp_RGBA:
                    internalFormat = (PixelInternalFormat)ImgTextureCompressionPvrtc.CompressedRgbaPvrtc4Bppv1Img;
                    format = (PixelFormatGl)ImgTextureCompressionPvrtc.CompressedRgbaPvrtc4Bppv1Img;
                    compressed = true;
                    pixelSize = 4;
                    type = PixelType.UnsignedByte;
                    break;
                case PixelFormat.PVRTC_2bpp_RGBA:
                    internalFormat = (PixelInternalFormat)ImgTextureCompressionPvrtc.CompressedRgbaPvrtc2Bppv1Img;
                    format = (PixelFormatGl)ImgTextureCompressionPvrtc.CompressedRgbaPvrtc2Bppv1Img;
                    compressed = true;
                    pixelSize = 2;
                    type = PixelType.UnsignedByte;
                    break;  
#elif SILICONSTUDIO_PLATFORM_ANDROID || !SILICONSTUDIO_PLATFORM_MONO_MOBILE && SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
// Desktop OpenGLES
                case PixelFormat.ETC1:
                    // TODO: Runtime check for extension?
                    internalFormat = (PixelInternalFormat)OesCompressedEtc1Rgb8Texture.Etc1Rgb8Oes;
                    format = (PixelFormatGl)OesCompressedEtc1Rgb8Texture.Etc1Rgb8Oes;
                    compressed = true;
                    pixelSize = 2;
                    type = PixelType.UnsignedByte;
                    break;
                case PixelFormat.ETC2_RGBA:
                    internalFormat = (PixelInternalFormat)CompressedInternalFormat.CompressedRgba8Etc2Eac;
                    format = (PixelFormatGl)CompressedInternalFormat.CompressedRgba8Etc2Eac;
                    compressed = true;
                    pixelSize = 2;
                    type = PixelType.UnsignedByte;
                    break;     
#endif
                case PixelFormat.None: // TODO: remove this - this is only for buffers used in compute shaders
                    internalFormat = PixelInternalFormat.Rgba;
                    format = PixelFormatGl.Red;
                    type = PixelType.UnsignedByte;
                    pixelSize = 1;
                    break;
                default:
                    throw new InvalidOperationException("Unsupported texture format");
            }
        }
    }
}
 
#endif 
