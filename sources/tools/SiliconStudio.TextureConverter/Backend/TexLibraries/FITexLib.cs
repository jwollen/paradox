﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.TextureConverter.Requests;
using FreeImageAPI;
using FreeImageAPI.Plugins;

namespace SiliconStudio.TextureConverter.TexLibraries
{
    /// <summary>
    /// Class containing the needed native Data used by FreeImage
    /// </summary>
    internal class FreeImageTextureLibraryData : ITextureLibraryData
    {
        /// <summary>
        /// Array of <see cref="FIBITMAP" />, each one being a sub image of the texture (a mipmap in a specific array member)
        /// </summary>
        public FIBITMAP[] Bitmaps { get; set; }

        /// <summary>
        /// Pointer to the beginning of the texture data (used for allocation/deallocation)
        /// </summary>
        public IntPtr Data { get; set; }
    }

    /// <summary>
    /// Peforms requests from <see cref="TextureTool" /> using FreeImage library.
    /// </summary>
    internal class FITexLib : ITexLibrary
    {
        private static Logger Log = GlobalLogger.GetLogger("FITexLib");

        /// <summary>
        /// Initializes a new instance of the <see cref="FITexLib"/> class.
        /// </summary>
        public FITexLib() {}

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources. Nothing in this case.
        /// </summary>
        public void Dispose() {}

        public void Dispose(TexImage image)
        {
            FreeImageTextureLibraryData libraryData = (FreeImageTextureLibraryData) image.LibraryData[this];
            if (libraryData.Data != IntPtr.Zero) Marshal.FreeHGlobal(libraryData.Data);
        }

        public void StartLibrary(TexImage image)
        {
            if(image.Format.IsCompressed())
            {
                Log.Error("FreeImage can't process compressed texture.");
                throw new TextureToolsException("FreeImage can't process compressed texture.");
            }

            var libraryData = new FreeImageTextureLibraryData();
            image.LibraryData[this] = libraryData;

            libraryData.Bitmaps = new FIBITMAP[image.SubImageArray.Length];

            FREE_IMAGE_TYPE type;
            uint bpp, redMask, greenMask, blueMask;
            if (!FreeImage.GetFormatParameters(image.Format, out type, out bpp, out redMask, out greenMask, out blueMask))
            {
                throw new ArgumentException("The pixel format '{0}' is not supported by FreeImage".ToFormat(image.Format));
            }
            for (int i = 0; i < image.SubImageArray.Length; ++i)
            {
                var data = image.SubImageArray[i].Data;
                var width = image.SubImageArray[i].Width;
                var heigth = image.SubImageArray[i].Height;
                var pitch = image.SubImageArray[i].RowPitch;
                libraryData.Bitmaps[i] = FreeImage.ConvertFromRawBits(data, type, width, heigth, pitch, bpp, redMask, greenMask, blueMask, false);
            }

            if (image.DisposingLibrary != null) image.DisposingLibrary.Dispose(image);

            image.DisposingLibrary = this;

            libraryData.Data = IntPtr.Zero;
        }

        public void EndLibrary(TexImage image)
        {
            if (!image.LibraryData.ContainsKey(this)) return;
            FreeImageTextureLibraryData libraryData = (FreeImageTextureLibraryData)image.LibraryData[this];

            IntPtr buffer = Marshal.AllocHGlobal(image.DataSize);
            int offset = 0;
            int size, rowPitch, slicePitch;

            image.SubImageArray = new TexImage.SubImage[libraryData.Bitmaps.Length];

            try
            {
                for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
                {
                    image.SubImageArray[i].Width = (int)FreeImage.GetWidth(libraryData.Bitmaps[i]);
                    image.SubImageArray[i].Height = (int)FreeImage.GetHeight(libraryData.Bitmaps[i]);

                    Tools.ComputePitch(image.Format, image.SubImageArray[i].Width, image.SubImageArray[i].Height, out rowPitch, out slicePitch);
                    size = slicePitch;

                    image.SubImageArray[i].Data = new IntPtr(buffer.ToInt64() + offset);
                    image.SubImageArray[i].DataSize = size;
                    image.SubImageArray[i].RowPitch = rowPitch;
                    image.SubImageArray[i].SlicePitch = slicePitch;

                    Utilities.CopyMemory(image.SubImageArray[i].Data, FreeImage.GetBits(libraryData.Bitmaps[i]), size);
                    offset += size;
                }
            }
            catch (AccessViolationException e)
            {
                Log.Error("Failed to convert FreeImage native data to TexImage texture. ", e);
                throw new TextureToolsException("Failed to convert FreeImage native data to TexImage texture. ", e);
            }

            image.Data = image.SubImageArray[0].Data;
            libraryData.Data = image.Data;

            // Freeing native Data, the texture data has been copied.
            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                FreeImage.Unload(libraryData.Bitmaps[i]);
            }

            libraryData.Bitmaps = null;
            image.DisposingLibrary = this;
        }

        public bool CanHandleRequest(TexImage image, IRequest request)
        {
            switch (request.Type)
            {
                case RequestType.Loading:
                    {
                        LoadingRequest loader = (LoadingRequest)request;
                        FREE_IMAGE_FORMAT format = FreeImage.GetFIFFromFilename(loader.FilePath);
                        return format != FREE_IMAGE_FORMAT.FIF_UNKNOWN && format != FREE_IMAGE_FORMAT.FIF_DDS; // FreeImage can load DDS texture, but can't handle their mipmaps..
                    }
                case RequestType.Export:
                    {
                        ExportRequest export = (ExportRequest)request;
                        FREE_IMAGE_FORMAT format = FreeImage.GetFIFFromFilename(export.FilePath);
                        return format != FREE_IMAGE_FORMAT.FIF_UNKNOWN && format != FREE_IMAGE_FORMAT.FIF_DDS;
                    }
                case RequestType.Rescaling:
                    RescalingRequest rescale = (RescalingRequest)request;
                    return rescale.Filter != Filter.Rescaling.Nearest;

                case RequestType.SwitchingChannels:
                case RequestType.GammaCorrection:
                case RequestType.Flipping:
                case RequestType.FlippingSub:
                case RequestType.Swapping:
                    return true;
                default:
                    return false;
            }
        }

        public void Execute(TexImage image, IRequest request)
        {
            FreeImageTextureLibraryData libraryData = image.LibraryData.ContainsKey(this) ? (FreeImageTextureLibraryData)image.LibraryData[this] : null;

            switch (request.Type)
            {
                case RequestType.Loading:
                    Load(image, libraryData, (LoadingRequest)request);
                    break;

                case RequestType.Rescaling:
                    Rescale(image, libraryData, (RescalingRequest)request);
                    break;

                case RequestType.SwitchingChannels:
                    SwitchChannels(image, libraryData, (SwitchingBRChannelsRequest)request);
                    break;

                case RequestType.Flipping:
                    Flip(image, libraryData, (FlippingRequest)request);
                    break;

                case RequestType.FlippingSub:
                    FlipSub(image, libraryData, (FlippingSubRequest)request);
                    break;

                case RequestType.Swapping:
                    Swap(image, libraryData, (SwappingRequest)request);
                    break;

                case RequestType.Export:
                    Export(image, libraryData, (ExportRequest)request);
                    break;

                case RequestType.GammaCorrection:
                    CorrectGamma(image, libraryData, (GammaCorrectionRequest)request);
                    break;

                default:
                    Log.Error("FITexLib (FreeImage) can't handle this request: " + request.Type);
                    throw new TextureToolsException("FITexLib (FreeImage) can't handle this request: " + request.Type);
            }
        }


        /// <summary>
        /// Loads the specified image.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="loader">The loader.</param>
        /// <exception cref="TextureToolsException">If loading failed : mostly not supported format or path error (FileNotFound).</exception>
        private void Load(TexImage image, FreeImageTextureLibraryData libraryData, LoadingRequest loader)
        {
            Log.Info("Loading " + loader.FilePath + " ...");

            FIBITMAP temp;
            try
            {
                temp = FreeImage.LoadEx(loader.FilePath);
                FreeImage.FlipVertical(temp);

                if (temp.IsNull)
                    throw new Exception("FreeImage's image data is null");
            } 
            catch (Exception e)
            {
                Log.Error("Loading file " + loader.FilePath + " failed: " + e.Message);
                throw new TextureToolsException("Loading file " + loader.FilePath + " failed: " + e.Message);
            }

            // Converting the image into BGRA_8888 format
            libraryData = new FreeImageTextureLibraryData { Bitmaps = new [] { FreeImage.ConvertTo32Bits(temp) } };
            image.LibraryData[this] = libraryData;

            FreeImage.Unload(temp);

            image.Data = FreeImage.GetBits(libraryData.Bitmaps[0]);
            image.Width = (int)FreeImage.GetWidth(libraryData.Bitmaps[0]);
            image.Height = (int)FreeImage.GetHeight(libraryData.Bitmaps[0]);
            image.Depth = 1;
            image.Dimension = image.Height == 1 ? TexImage.TextureDimension.Texture1D : TexImage.TextureDimension.Texture2D;
            image.Format = loader.LoadAsSRgb? PixelFormat.B8G8R8A8_UNorm_SRgb : PixelFormat.B8G8R8A8_UNorm;
            
            int rowPitch, slicePitch;
            Tools.ComputePitch(image.Format, image.Width, image.Height, out rowPitch, out slicePitch);
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;
            
            //Only one image in the SubImageArray, FreeImage is only used to load images, not textures.
            image.SubImageArray[0].Data = image.Data;
            image.SubImageArray[0].DataSize = image.DataSize;
            image.SubImageArray[0].Width = image.Width;
            image.SubImageArray[0].Height = image.Height;
            image.SubImageArray[0].RowPitch = rowPitch;
            image.SubImageArray[0].SlicePitch = slicePitch;
            image.DataSize = (int) (FreeImage.GetDIBSize(libraryData.Bitmaps[0]) - GetHeaderSize()); // header size of a bitmap is included in their size calculus
            libraryData.Data = IntPtr.Zero;
            image.DisposingLibrary = this;
        }

        /// <summary>
        /// Gets the size of the header of a Bitmap.
        /// </summary>
        /// <returns></returns>
        private unsafe uint GetHeaderSize()
        {
            return (uint)sizeof(BITMAPINFOHEADER);
        }


        /// <summary>
        /// Rescales the specified image.
        /// </summary>
        /// <remarks>
        /// The MipmapCount will be reset to 1 after this operation
        /// </remarks>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="rescale">The rescale.</param>
        private void Rescale(TexImage image, FreeImageTextureLibraryData libraryData, RescalingRequest rescale)
        {
            int width = rescale.ComputeWidth(image);
            int height = rescale.ComputeHeight(image);

            Log.Info("Rescaling image to " + width + "x" + height + " with " + rescale.Filter + " ...");

            FIBITMAP[] newTab;

            if (image.Dimension == TexImage.TextureDimension.Texture3D) // in case of 3D Texture, we must rescale each slice of the top mipmap level
            {
                newTab = new FIBITMAP[image.ArraySize * image.FaceCount * image.Depth];
                int curDepth;

                int nbSubImageWithMipMapPerArrayMemeber = 0; // calculating the number of sub images we have to jump to reach the next top level mipmap of the next array member
                curDepth = image.Depth;
                for (int i = 0; i < image.MipmapCount; ++i)
                {
                    nbSubImageWithMipMapPerArrayMemeber += curDepth;
                    curDepth = curDepth > 1 ? curDepth >>= 1 : curDepth;
                }

                int ct = 0;
                for (int j = 0; j < image.ArraySize; ++j)
                {
                    for (int i = 0; i < image.Depth; ++i)
                    {
                        newTab[ct] = FreeImage.Rescale(libraryData.Bitmaps[i + j * nbSubImageWithMipMapPerArrayMemeber], width, height, (FREE_IMAGE_FILTER)rescale.Filter);
                        ++ct;
                    }
                }
            }
            else
            {
                newTab = new FIBITMAP[image.ArraySize];
                int ct = 0;
                for (int i = 0; i < libraryData.Bitmaps.Length; i += image.MipmapCount)
                {
                    newTab[ct] = FreeImage.Rescale(libraryData.Bitmaps[i], width, height, (FREE_IMAGE_FILTER)rescale.Filter);
                    ++ct;
                }
            }

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                FreeImage.Unload(libraryData.Bitmaps[i]);
            }

            libraryData.Bitmaps = newTab;
            image.Data = FreeImage.GetBits(newTab[0]);

            // Updating image data
            image.Rescale(width, height);

            int rowPitch, slicePitch;
            Tools.ComputePitch(image.Format, width, height, out rowPitch, out slicePitch);
            
            image.RowPitch = rowPitch;
            image.SlicePitch = slicePitch;
            image.MipmapCount = 1;
            image.DataSize = image.SlicePitch * image.ArraySize * image.FaceCount * image.Depth;
        }


        /// <summary>
        /// Switches the channels R and B.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="switchC">The switch request</param>
        /// <remarks>
        /// Some libraries can't handle BGRA order so we need to change it to RGBA.
        /// </remarks>
        private void SwitchChannels(TexImage image, FreeImageTextureLibraryData libraryData, SwitchingBRChannelsRequest switchC)
        {

            Log.Info("Switching channels R and G ...");

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                FIBITMAP blueChannel = FreeImage.GetChannel(libraryData.Bitmaps[i], FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
                FIBITMAP redChannel = FreeImage.GetChannel(libraryData.Bitmaps[i], FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
                FreeImage.SetChannel(libraryData.Bitmaps[i], redChannel, FREE_IMAGE_COLOR_CHANNEL.FICC_BLUE);
                FreeImage.SetChannel(libraryData.Bitmaps[i], blueChannel, FREE_IMAGE_COLOR_CHANNEL.FICC_RED);
                FreeImage.Unload(blueChannel);
                FreeImage.Unload(redChannel);
            }

            if (image.Format.IsInBGRAOrder())
                image.Format = PixelFormat.R8G8B8A8_UNorm;
            else
                image.Format = PixelFormat.B8G8R8A8_UNorm;
        }

        public bool SupportBGRAOrder()
        {
            return true;
        }


        /// <summary>
        /// Flips the specified image horizontally or vertically.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="flip">The flip request.</param>
        private void Flip(TexImage image, FreeImageTextureLibraryData libraryData, FlippingRequest flip)
        {
            Log.Info("Flipping image : " + flip.Flip + " ...");

            for (int i = 0; i < libraryData.Bitmaps.Length; ++i)
            {
                switch (flip.Flip)
                {
                    case Orientation.Vertical:
                        FreeImage.FlipVertical(libraryData.Bitmaps[i]);
                        break;

                    case Orientation.Horizontal:
                        FreeImage.FlipHorizontal(libraryData.Bitmaps[i]);
                        break;
                }
            }

            image.Flip(flip.Flip);
        }


        /// <summary>
        /// Flips the specified sub-image horizontally or vertically.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="flipSub">The flip request.</param>
        private void FlipSub(TexImage image, FreeImageTextureLibraryData libraryData, FlippingSubRequest flipSub)
        {
            Log.Info("Flipping image : sub-image " + flipSub.SubImageIndex + " " + flipSub.Flip + " ...");

            if (flipSub.SubImageIndex >= 0 && flipSub.SubImageIndex < libraryData.Bitmaps.Length)
            {
                switch (flipSub.Flip)
                {
                    case Orientation.Vertical:
                        FreeImage.FlipVertical(libraryData.Bitmaps[flipSub.SubImageIndex]);
                        break;

                    case Orientation.Horizontal:
                        FreeImage.FlipHorizontal(libraryData.Bitmaps[flipSub.SubImageIndex]);
                        break;
                }
            }
            else
            {
                Log.Warning("Cannot flip the sub-image " + flipSub.SubImageIndex + " because there is only " + libraryData.Bitmaps.Length + " sub-images.");
            }

            // TODO: texture atlas update?
        }


        /// <summary>
        /// Swaps two sub-images.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="swap">The swap request.</param>
        private void Swap(TexImage image, FreeImageTextureLibraryData libraryData, SwappingRequest swap)
        {
            Log.Info("Swapping image : sub-image " + swap.FirstSubImageIndex + " and " + swap.SecondSubImageIndex + " ...");

            if (swap.FirstSubImageIndex >= 0 && swap.FirstSubImageIndex < libraryData.Bitmaps.Length
                && swap.SecondSubImageIndex >= 0 && swap.SecondSubImageIndex < libraryData.Bitmaps.Length)
            {
                // copy first image
                var firstImage = FreeImage.Copy(libraryData.Bitmaps[swap.FirstSubImageIndex], 0, 0, image.Width, image.Height);
                FreeImage.Paste(libraryData.Bitmaps[swap.FirstSubImageIndex], libraryData.Bitmaps[swap.SecondSubImageIndex], 0, 0, 256);
                FreeImage.Paste(libraryData.Bitmaps[swap.SecondSubImageIndex], firstImage, 0, 0, 256);

                // TODO: free firstImage?
            }
            else
            {
                Log.Warning("Cannot swap the sub-images " + swap.FirstSubImageIndex + " and " + swap.SecondSubImageIndex + " because there is only " + libraryData.Bitmaps.Length + " sub-images.");
            }

            // TODO: texture atlas update?
        }


        /// <summary>
        /// Exports the specified image to the requested file name.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        /// <exception cref="TexLibraryException">
        /// Export failure.
        /// </exception>
        /// <remarks>
        /// In case of mipmapping or array texture, may images will be output.
        /// </remarks>
        private void Export(TexImage image, FreeImageTextureLibraryData libraryData, ExportRequest request)
        {
            String directory = Path.GetDirectoryName(request.FilePath);
            String fileName = Path.GetFileNameWithoutExtension(request.FilePath);
            String extension = Path.GetExtension(request.FilePath);
            String finalName;

            if (image.Dimension == TexImage.TextureDimension.Texture3D)
            {
                Log.Error("Not implemented.");
                throw new TextureToolsException("Not implemented.");
            }

            if(!image.Format.IsInBGRAOrder())
            {
                SwitchChannels(image, libraryData, new SwitchingBRChannelsRequest());
            }

            if (image.SubImageArray.Length > 1 && request.MinimumMipMapSize < FreeImage.GetWidth(libraryData.Bitmaps[0]) && request.MinimumMipMapSize < FreeImage.GetHeight(libraryData.Bitmaps[0]))
            {
                int imageCount = 0;
                for (int i = 0; i < image.ArraySize; ++i)
                {
                    for (int j = 0; j < image.MipmapCount; ++j)
                    {
                        if (FreeImage.GetWidth(libraryData.Bitmaps[imageCount]) < request.MinimumMipMapSize || FreeImage.GetHeight(libraryData.Bitmaps[imageCount]) < request.MinimumMipMapSize)
                            break;

                        finalName = directory + "/" + fileName + "-ind_" + i + "-mip_" + j + extension;
                        FreeImage.FlipVertical(libraryData.Bitmaps[imageCount]);
                        if (!FreeImage.SaveEx(libraryData.Bitmaps[imageCount], finalName))
                        {
                            Log.Error("Export failure.");
                            throw new TextureToolsException("Export failure.");
                        }
                        FreeImage.FlipVertical(libraryData.Bitmaps[imageCount]);
                        Log.Info("Exporting image to " + finalName + " ...");
                        ++imageCount;
                    }
                }
            }
            else
            {
                FreeImage.FlipVertical(libraryData.Bitmaps[0]);
                if (!FreeImage.SaveEx(libraryData.Bitmaps[0], request.FilePath))
                {
                    Log.Error("Export failure.");
                    throw new TextureToolsException("Export failure.");
                }
                FreeImage.FlipVertical(libraryData.Bitmaps[0]);
                Log.Info("Exporting image to " + request.FilePath + " ...");
            }

            image.Save(request.FilePath);
        }


        /// <summary>
        /// Corrects the gamma.
        /// </summary>
        /// <param name="image">The image.</param>
        /// <param name="libraryData">The library data.</param>
        /// <param name="request">The request.</param>
        public void CorrectGamma(TexImage image, FreeImageTextureLibraryData libraryData, GammaCorrectionRequest request)
        {
            Log.Info("Applying a gamma correction of " + request.Gamma + " ...");

            foreach (FIBITMAP bitmap in libraryData.Bitmaps)
            {
                FreeImage.AdjustGamma(bitmap, request.Gamma);
            }
        }

    }
}
