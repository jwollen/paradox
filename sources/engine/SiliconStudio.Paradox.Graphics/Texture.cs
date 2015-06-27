// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
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
using System.Diagnostics;
using System.IO;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.ReferenceCounting;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Paradox.Graphics.Data;
using Utilities = SiliconStudio.Core.Utilities;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// Class used for all Textures (1D, 2D, 3D, DepthStencil, RenderTargets...etc.)
    /// </summary>
    [DataSerializerGlobal(typeof(ReferenceSerializer<Texture>), Profile = "Asset")]
    [ContentSerializer(typeof(DataContentSerializer<Texture>))]
    [DebuggerDisplay("Texture {ViewWidth}x{ViewHeight}x{ViewDepth} {Format} ({ViewFlags})")]
    [DataSerializer(typeof(TextureSerializer))]
    public sealed partial class Texture : GraphicsResource
    {
        internal const int DepthStencilReadOnlyFlags = 16;

        private TextureDescription textureDescription;
        private TextureViewDescription textureViewDescription;

        /// <summary>
        /// Common description for the original texture. See remarks.
        /// </summary>
        /// <remarks>
        /// This field and the properties in TextureDessciption must be considered as readonly when accessing from this instance.
        /// </remarks>
        public TextureDescription Description
        {
            get
            {
                return textureDescription;
            }
        }

        /// <summary>
        /// Gets the view description.
        /// </summary>
        /// <value>The view description.</value>
        public TextureViewDescription ViewDescription
        {
            get
            {
                return textureViewDescription;
            }
        }

        /// <summary>
        /// The dimension of a texture.
        /// </summary>
        public TextureDimension Dimension
        {
            get
            {
                return textureDescription.Dimension;
            }
        }

        /// <summary>
        /// The width of this texture view.
        /// </summary>
        /// <value>The width of the view.</value>
        public int ViewWidth { get; private set; }

        /// <summary>
        /// The height of this texture view.
        /// </summary>
        /// <value>The height of the view.</value>
        public int ViewHeight { get; private set; }

        /// <summary>
        /// The depth of this texture view.
        /// </summary>
        /// <value>The view depth.</value>
        public int ViewDepth { get; private set; }

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The view format.</value>
        public PixelFormat ViewFormat
        {
            get
            {
                return textureViewDescription.Format;
            }
        }

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The type of the view.</value>
        public TextureFlags ViewFlags
        {
            get
            {
                return textureViewDescription.Flags;
            }
        }

        /// <summary>
        /// The format of this texture view.
        /// </summary>
        /// <value>The type of the view.</value>
        public ViewType ViewType
        {
            get
            {
                return textureViewDescription.Type;
            }
        }

        /// <summary>
        /// The miplevel index of this texture view.
        /// </summary>
        /// <value>The mip level.</value>
        public int MipLevel
        {
            get
            {
                return textureViewDescription.MipLevel;
            }
        }

        /// <summary>
        /// The array index of this texture view.
        /// </summary>
        /// <value>The array slice.</value>
        public int ArraySlice
        {
            get
            {
                return textureViewDescription.ArraySlice;
            }
        }

        /// <summary>
        /// The width of the texture.
        /// </summary>
        /// <value>The width.</value>
        public int Width
        {
            get
            {
                return textureDescription.Width;
            }
        }

        /// <summary>
        /// The height of the texture.
        /// </summary>
        /// <value>The height.</value>
        public int Height
        {
            get
            {
                return textureDescription.Height;
            }
        }

        /// <summary>
        /// The depth of the texture.
        /// </summary>
        /// <value>The depth.</value>
        public int Depth
        {
            get
            {
                return textureDescription.Depth;
            }
        }

        /// <summary>
        /// Number of textures in the array.
        /// </summary>
        /// <value>The size of the array.</value>
        /// <remarks>This field is only valid for 1D, 2D and Cube <see cref="Texture" />.</remarks>
        public int ArraySize
        {
            get
            {
                return textureDescription.ArraySize;
            }
        }

        /// <summary>
        /// The maximum number of mipmap levels in the texture.
        /// </summary>
        /// <value>The mip levels.</value>
        public int MipLevels
        {
            get
            {
                return textureDescription.MipLevels;
            }
        }

        /// <summary>
        /// Texture format (see <see cref="PixelFormat" />)
        /// </summary>
        /// <value>The format.</value>
        public PixelFormat Format
        {
            get
            {
                return textureDescription.Format;
            }
        }

        /// <summary>
        /// Structure that specifies multisampling parameters for the texture.
        /// </summary>
        /// <value>The multi sample level.</value>
        /// <remarks>This field is only valid for a 2D <see cref="Texture" />.</remarks>
        public MSAALevel MultiSampleLevel
        {
            get
            {
                return textureDescription.MultiSampleLevel;
            }
        }

        /// <summary>	
        /// Value that identifies how the texture is to be read from and written to.
        /// </summary>	
        public GraphicsResourceUsage Usage
        {
            get
            {
                return textureDescription.Usage;
            }
        }

        /// <summary>	
        /// Texture flags.
        /// </summary>	
        public TextureFlags Flags
        {
            get
            {
                return textureDescription.Flags;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a render target.
        /// </summary>
        /// <value><c>true</c> if this instance is render target; otherwise, <c>false</c>.</value>
        public bool IsRenderTarget
        {
            get
            {
                return (ViewFlags & TextureFlags.RenderTarget) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a depth stencil.
        /// </summary>
        /// <value><c>true</c> if this instance is a depth stencil; otherwise, <c>false</c>.</value>
        public bool IsDepthStencil
        {
            get
            {
                return (ViewFlags & TextureFlags.DepthStencil) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a depth stencil readonly.
        /// </summary>
        /// <value><c>true</c> if this instance is a depth stencil readonly; otherwise, <c>false</c>.</value>
        public bool IsDepthStencilReadOnly
        {
            get
            {
                return (ViewFlags & TextureFlags.DepthStencilReadOnly) == TextureFlags.DepthStencilReadOnly;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
        public bool IsShaderResource
        {
            get
            {
                return (ViewFlags & TextureFlags.ShaderResource) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a shader resource.
        /// </summary>
        /// <value><c>true</c> if this instance is a shader resource; otherwise, <c>false</c>.</value>
        public bool IsUnorderedAccess
        {
            get
            {
                return (ViewFlags & TextureFlags.UnorderedAccess) != 0;
            }
        }

        /// <summary>
        /// Gets a value indicating whether this instance is a multi sample texture.
        /// </summary>
        /// <value><c>true</c> if this instance is multi sample texture; otherwise, <c>false</c>.</value>
        public bool IsMultiSample
        {
            get
            {
                return this.MultiSampleLevel > MSAALevel.None;
            }
        }
        
        /// <summary>
        /// Gets a boolean indicating whether this <see cref="Texture"/> is a using a block compress format (BC1, BC2, BC3, BC4, BC5, BC6H, BC7).
        /// </summary>
        public bool IsBlockCompressed { get; private set; }

        /// <summary>
        /// Gets the size of this texture.
        /// </summary>
        /// <value>The size.</value>
        public Size3 Size
        {
            get
            {
                return new Size3(ViewWidth, ViewHeight, ViewDepth);
            }
        }

        /// <summary>
        /// The width stride in bytes (number of bytes per row).
        /// </summary>
        private int RowStride { get; set; }

        /// <summary>
        /// The depth stride in bytes (number of bytes per depth slice).
        /// </summary>
        private int DepthStride { get; set; }

        /// <summary>
        /// The underlying parent texture (if this is a view).
        /// </summary>
        private Texture ParentTexture { get; set; }

        private MipMapDescription[] mipmapDescriptions;

        public Texture()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Texture"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        internal Texture(GraphicsDevice device) : base(device)
        {
        }

        protected override void Destroy()
        {
            base.Destroy();
            DestroyImpl();
            if (ParentTexture != null)
            {
                ParentTexture.ReleaseInternal();
            }
        }

        protected internal override bool OnRecreate()
        {
            base.OnRecreate();
            OnRecreateImpl();
            return true;
        }

        internal Texture InitializeFrom(TextureDescription description, DataBox[] textureDatas = null)
        {
            return InitializeFrom(null, description, new TextureViewDescription(), textureDatas);
        }

        internal Texture InitializeFrom(TextureDescription description, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(null, description, viewDescription, textureDatas);
        }

        internal Texture InitializeFrom(Texture parentTexture, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            return InitializeFrom(parentTexture, parentTexture.Description, viewDescription, textureDatas);
        }

        internal Texture InitializeFrom(Texture parentTexture, TextureDescription description, TextureViewDescription viewDescription, DataBox[] textureDatas = null)
        {
            ParentTexture = parentTexture;
            if (ParentTexture != null)
            {
                ParentTexture.AddReferenceInternal();
            }

            textureDescription = description;
            textureViewDescription = viewDescription;
            IsBlockCompressed = description.Format.IsCompressed();
            RowStride = this.Width * description.Format.SizeInBytes();
            DepthStride = RowStride * this.Height;
            mipmapDescriptions = Image.CalculateMipMapDescription(description);

            ViewWidth = Math.Max(1, Width >> MipLevel);
            ViewHeight = Math.Max(1, Height >> MipLevel);
            ViewDepth = Math.Max(1, Depth >> MipLevel);
            if (ViewFormat == PixelFormat.None)
            {
                textureViewDescription.Format = description.Format;
            }
            if (ViewFlags == TextureFlags.None)
            {
                textureViewDescription.Flags = description.Flags;
            }

            // Check that the view is compatible with the parent texture
            var filterViewFlags = (TextureFlags)((int)ViewFlags & (~DepthStencilReadOnlyFlags));
            if ((Flags & filterViewFlags) != filterViewFlags)
            {
                throw new NotSupportedException("Cannot create a texture view with flags [{0}] from the parent texture [{1}] as the parent texture must include all flags defined by the view".ToFormat(ViewFlags, Flags));
            }

            InitializeFromImpl(textureDatas);

            return this;
        }


        /// <summary>
        /// Gets a view on this texture for a particular <see cref="ViewType" />, array index (or zIndex for Texture3D), and mipmap index.
        /// </summary>
        /// <param name="viewDescription">The view description.</param>
        /// <returns>A new texture object that is bouded to the requested view.</returns>
        public Texture ToTextureView(TextureViewDescription viewDescription)
        {
            return new Texture(GraphicsDevice).InitializeFrom(ParentTexture ?? this, viewDescription);
        }

        /// <summary>
        /// Gets the mipmap description of this instance for the specified mipmap level.
        /// </summary>
        /// <param name="mipmap">The mipmap.</param>
        /// <returns>A description of a particular mipmap for this texture.</returns>
        public MipMapDescription GetMipMapDescription(int mipmap)
        {
            return mipmapDescriptions[mipmap];
        }

        /// <summary>
        /// Calculates the size of a particular mip.
        /// </summary>
        /// <param name="size">The size.</param>
        /// <param name="mipLevel">The mip level.</param>
        /// <returns>System.Int32.</returns>
        public static int CalculateMipSize(int size, int mipLevel)
        {
            mipLevel = Math.Min(mipLevel, Image.CountMips(size));
            return Math.Max(1, size >> mipLevel);
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 1D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                int maxMips = CountMips(width);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException(String.Format("MipLevels must be <= {0}", maxMips));
            }
            else if (mipLevels == 0)
            {
                mipLevels = CountMips(width);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 2D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, int height, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                int maxMips = CountMips(width, height);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException(String.Format("MipLevels must be <= {0}", maxMips));
            }
            else if (mipLevels == 0)
            {
                mipLevels = CountMips(width, height);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Calculates the number of miplevels for a Texture 2D.
        /// </summary>
        /// <param name="width">The width of the texture.</param>
        /// <param name="height">The height of the texture.</param>
        /// <param name="depth">The depth of the texture.</param>
        /// <param name="mipLevels">A <see cref="MipMapCount"/>, set to true to calculates all mipmaps, to false to calculate only 1 miplevel, or > 1 to calculate a specific amount of levels.</param>
        /// <returns>The number of miplevels.</returns>
        public static int CalculateMipLevels(int width, int height, int depth, MipMapCount mipLevels)
        {
            if (mipLevels > 1)
            {
                if (!MathUtil.IsPow2(width) || !MathUtil.IsPow2(height) || !MathUtil.IsPow2(depth))
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                int maxMips = CountMips(width, height, depth);
                if (mipLevels > maxMips)
                    throw new InvalidOperationException(String.Format("MipLevels must be <= {0}", maxMips));
            }
            else if (mipLevels == 0)
            {
                if (!MathUtil.IsPow2(width) || !MathUtil.IsPow2(height) || !MathUtil.IsPow2(depth))
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");

                mipLevels = CountMips(width, height, depth);
            }
            else
            {
                mipLevels = 1;
            }
            return mipLevels;
        }

        /// <summary>
        /// Gets the absolute sub-resource index from the array and mip slice.
        /// </summary>
        /// <param name="arraySlice">The array slice index.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <returns>A value equals to arraySlice * Description.MipLevels + mipSlice.</returns>
        public int GetSubResourceIndex(int arraySlice, int mipSlice)
        {
            return arraySlice * MipLevels + mipSlice;
        }

        /// <summary>
        /// Calculates the expected width of a texture using a specified type.
        /// </summary>
        /// <typeparam name="TData">The type of the T pixel data.</typeparam>
        /// <returns>The expected width</returns>
        /// <exception cref="System.ArgumentException">If the size is invalid</exception>
        public int CalculateWidth<TData>(int mipLevel = 0) where TData : struct
        {
            var widthOnMip = CalculateMipSize((int)Width, mipLevel);
            var rowStride = widthOnMip * Format.SizeInBytes();

            var dataStrideInBytes = Utilities.SizeOf<TData>() * widthOnMip;
            var width = ((double)rowStride / dataStrideInBytes) * widthOnMip;
            if (Math.Abs(width - (int)width) > Double.Epsilon)
                throw new ArgumentException("sizeof(TData) / sizeof(Format) * Width is not an integer");

            return (int)width;
        }

        /// <summary>
        /// Calculates the number of pixel data this texture is requiring for a particular mip level.
        /// </summary>
        /// <typeparam name="TData">The type of the T pixel data.</typeparam>
        /// <param name="mipLevel">The mip level.</param>
        /// <returns>The number of pixel data.</returns>
        /// <remarks>This method is used to allocated a texture data buffer to hold pixel datas: var textureData = new T[ texture.CalculatePixelCount&lt;T&gt;() ] ;.</remarks>
        public int CalculatePixelDataCount<TData>(int mipLevel = 0) where TData : struct
        {
            return CalculateWidth<TData>(mipLevel) * CalculateMipSize(Height, mipLevel) * CalculateMipSize(Depth, mipLevel);
        }

        /// <summary>
        /// Gets the content of this texture to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <returns>The texture data.</returns>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public TData[] GetData<TData>(int arraySlice = 0, int mipSlice = 0) where TData : struct
        {
            var toData = new TData[this.CalculatePixelDataCount<TData>(mipSlice)];
            GetData(toData, arraySlice, mipSlice);
            return toData;
        }

        /// <summary>
        /// Copies the content of this texture to an array of data.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="toData">The destination buffer to receive a copy of the texture datas.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// This method creates internally a stagging resource if this texture is not already a stagging resouce, copies to it and map it to memory. Use method with explicit staging resource
        /// for optimal performances.</remarks>
        public bool GetData<TData>(TData[] toData, int arraySlice = 0, int mipSlice = 0, bool doNotWait = false) where TData : struct
        {
            // Get data from this resource
            if (Usage == GraphicsResourceUsage.Staging)
            {
                // Directly if this is a staging resource
                return GetData(this, toData, arraySlice, mipSlice, doNotWait);
            }
            else
            {
                // Unefficient way to use the Copy method using dynamic staging texture
                using (var throughStaging = this.ToStaging())
                    return GetData(throughStaging, toData, arraySlice, mipSlice, doNotWait);
            }
        }

        /// <summary>
        /// Copies the content of this texture from GPU memory to an array of data on CPU memory using a specific staging resource.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="stagingTexture">The staging texture used to transfer the texture to.</param>
        /// <param name="toData">To data.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe bool GetData<TData>(Texture stagingTexture, TData[] toData, int arraySlice = 0, int mipSlice = 0, bool doNotWait = false) where TData : struct
        {
            return GetData(stagingTexture, new DataPointer((IntPtr)Interop.Fixed(toData), toData.Length * Utilities.SizeOf<TData>()), arraySlice, mipSlice, doNotWait);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this texture into GPU memory.
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="region">Destination region</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working on the main graphics device. Use method with explicit graphics device to set data on a deferred context.
        /// See also unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData<TData>(TData[] fromData, int arraySlice = 0, int mipSlice = 0, ResourceRegion? region = null) where TData : struct
        {
            SetData(GraphicsDevice, fromData, arraySlice, mipSlice, region);
        }

        /// <summary>
        /// Copies the content an data on CPU memory to this texture into GPU memory using the specified <see cref="GraphicsDevice"/> (The graphics device could be deffered).
        /// </summary>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="region">Destination region</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working on the main graphics device. Use method with explicit graphics device to set data on a deferred context.
        /// See also unmanaged documentation about Map/UnMap for usage and restrictions.
        /// </remarks>
        public void SetData(DataPointer fromData, int arraySlice = 0, int mipSlice = 0, ResourceRegion? region = null)
        {
            SetData(GraphicsDevice, fromData, arraySlice, mipSlice, region);
        }

        /// <summary>
        /// Copies the content an array of data on CPU memory to this texture into GPU memory using the specified <see cref="GraphicsDevice"/> (The graphics device could be deffered).
        /// </summary>
        /// <typeparam name="TData">The type of the T data.</typeparam>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="region">Destination region</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// See unmanaged documentation for usage and restrictions.
        /// </remarks>
        public unsafe void SetData<TData>(GraphicsDevice device, TData[] fromData, int arraySlice = 0, int mipSlice = 0, ResourceRegion? region = null) where TData : struct
        {
            SetData(device, new DataPointer((IntPtr)Interop.Fixed(fromData), fromData.Length * Utilities.SizeOf<TData>()), arraySlice, mipSlice, region);
        }

        /// <summary>
        /// Copies the content of this texture from GPU memory to a pointer on CPU memory using a specific staging resource.
        /// </summary>
        /// <param name="stagingTexture">The staging texture used to transfer the texture to.</param>
        /// <param name="toData">The pointer to data in CPU memory.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="doNotWait">if set to <c>true</c> this method will return immediately if the resource is still being used by the GPU for writing. Default is false</param>
        /// <returns><c>true</c> if data was correctly retrieved, <c>false</c> if <see cref="doNotWait"/> flag was true and the resource is still being used by the GPU for writing.</returns>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// This method is only working when called from the main thread that is accessing the main <see cref="GraphicsDevice"/>.
        /// </remarks>
        public unsafe bool GetData(Texture stagingTexture, DataPointer toData, int arraySlice = 0, int mipSlice = 0, bool doNotWait = false)
        {
            if (stagingTexture == null) throw new ArgumentNullException("stagingTexture");
            var device = GraphicsDevice;
            //var deviceContext = device.NativeDeviceContext;

            // Get mipmap description for the specified mipSlice
            var mipmap = this.GetMipMapDescription(mipSlice);

            // Copy height, depth
            int height = mipmap.HeightPacked;
            int depth = mipmap.Depth;

            // Calculate depth stride based on mipmap level
            int rowStride = mipmap.RowStride;

            // Depth Stride
            int textureDepthStride = mipmap.DepthStride;

            // MipMap Stride
            int mipMapSize = mipmap.MipmapSize;

            // Check size validity of data to copy to
            if (toData.Size > mipMapSize)
                throw new ArgumentException(String.Format("Size of toData ({0} bytes) is not compatible expected size ({1} bytes) : Width * Height * Depth * sizeof(PixelFormat) size in bytes", toData.Size, mipMapSize));

            // Copy the actual content of the texture to the staging resource
            if (!ReferenceEquals(this, stagingTexture))
                device.Copy(this, stagingTexture);

            // Calculate the subResourceIndex for a Texture
            int subResourceIndex = this.GetSubResourceIndex(arraySlice, mipSlice);

            // Map the staging resource to a CPU accessible memory
            var mappedResource = device.MapSubresource(stagingTexture, subResourceIndex, MapMode.Read, doNotWait);

            // Box can be empty if DoNotWait is set to true, return false if empty
            var box = mappedResource.DataBox;
            if (box.IsEmpty)
            {
                return false;
            }

            // If depth == 1 (Texture, Texture or TextureCube), then depthStride is not used
            var boxDepthStride = this.Depth == 1 ? box.SlicePitch : textureDepthStride;

            var isFlippedTexture = IsFlippedTexture();

            // The fast way: If same stride, we can directly copy the whole texture in one shot
            if (box.RowPitch == rowStride && boxDepthStride == textureDepthStride && !isFlippedTexture)
            {
                Utilities.CopyMemory(toData.Pointer, box.DataPointer, mipMapSize);
            }
            else
            {
                // Otherwise, the long way by copying each scanline
                var sourcePerDepthPtr = (byte*)box.DataPointer;
                var destPtr = (byte*)toData.Pointer;

                // Iterate on all depths
                for (int j = 0; j < depth; j++)
                {
                    var sourcePtr = sourcePerDepthPtr;
                    // Iterate on each line

                    if (isFlippedTexture)
                    {
                        sourcePtr = sourcePtr + box.RowPitch * (height - 1);
                        for (int i = height - 1; i >= 0; i--)
                        {
                            // Copy a single row
                            Utilities.CopyMemory(new IntPtr(destPtr), new IntPtr(sourcePtr), rowStride);
                            sourcePtr -= box.RowPitch;
                            destPtr += rowStride;
                        }
                    }
                    else
                    {
                        for (int i = 0; i < height; i++)
                        {
                            // Copy a single row
                            Utilities.CopyMemory(new IntPtr(destPtr), new IntPtr(sourcePtr), rowStride);
                            sourcePtr += box.RowPitch;
                            destPtr += rowStride;
                        }
                    }
                    sourcePerDepthPtr += box.SlicePitch;
                }
            }

            // Make sure that we unmap the resource in case of an exception
            device.UnmapSubresource(mappedResource);

            return true;
        }

        /// <summary>
        /// Copies the content an data on CPU memory to this texture into GPU memory.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="fromData">The data to copy from.</param>
        /// <param name="arraySlice">The array slice index. This value must be set to 0 for Texture 3D.</param>
        /// <param name="mipSlice">The mip slice index.</param>
        /// <param name="region">Destination region</param>
        /// <exception cref="System.ArgumentException">When strides is different from optimal strides, and TData is not the same size as the pixel format, or Width * Height != toData.Length</exception>
        /// <remarks>
        /// See unmanaged documentation for usage and restrictions.
        /// </remarks>
        public unsafe void SetData(GraphicsDevice device, DataPointer fromData, int arraySlice = 0, int mipSlice = 0, ResourceRegion? region = null)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (region.HasValue && this.Usage != GraphicsResourceUsage.Default)
                throw new ArgumentException("Region is only supported for textures with ResourceUsage.Default");

            // Get mipmap description for the specified mipSlice
            var mipMapDesc = this.GetMipMapDescription(mipSlice);

            int width = mipMapDesc.Width;
            int height = mipMapDesc.Height;
            int depth = mipMapDesc.Depth;

            // If we are using a region, then check that parameters are fine
            if (region.HasValue)
            {
                int newWidth = region.Value.Right - region.Value.Left;
                int newHeight = region.Value.Bottom - region.Value.Top;
                int newDepth = region.Value.Back - region.Value.Front;
                if (newWidth > width)
                    throw new ArgumentException(String.Format("Region width [{0}] cannot be greater than mipmap width [{1}]", newWidth, width), "region");
                if (newHeight > height)
                    throw new ArgumentException(String.Format("Region height [{0}] cannot be greater than mipmap height [{1}]", newHeight, height), "region");
                if (newDepth > depth)
                    throw new ArgumentException(String.Format("Region depth [{0}] cannot be greater than mipmap depth [{1}]", newDepth, depth), "region");

                width = newWidth;
                height = newHeight;
                depth = newDepth;
            }

            // Size per pixel
            var sizePerElement = Format.SizeInBytes();

            // Calculate depth stride based on mipmap level
            int rowStride;

            // Depth Stride
            int textureDepthStride;

            // Compute Actual pitch
            Image.ComputePitch(this.Format, width, height, out rowStride, out textureDepthStride, out width, out height);

            // Size Of actual texture data
            int sizeOfTextureData = textureDepthStride * depth;

            // Check size validity of data to copy to
            if (fromData.Size != sizeOfTextureData)
                throw new ArgumentException(String.Format("Size of toData ({0} bytes) is not compatible expected size ({1} bytes) : Width * Height * Depth * sizeof(PixelFormat) size in bytes", fromData.Size, sizeOfTextureData));

            // Calculate the subResourceIndex for a Texture
            int subResourceIndex = this.GetSubResourceIndex(arraySlice, mipSlice);

            // If this texture is declared as default usage, we use UpdateSubresource that supports sub resource region.
            if (this.Usage == GraphicsResourceUsage.Default)
            {
                // If using a specific region, we need to handle this case
                if (region.HasValue)
                {
                    var regionValue = region.Value;
                    var sourceDataPtr = fromData.Pointer;

                    // Workaround when using region with a deferred context and a device that does not support CommandList natively
                    // see http://blogs.msdn.com/b/chuckw/archive/2010/07/28/known-issue-direct3d-11-updatesubresource-and-deferred-contexts.aspx
                    if (device.NeedWorkAroundForUpdateSubResource)
                    {
                        if (IsBlockCompressed)
                        {
                            regionValue.Left /= 4;
                            regionValue.Right /= 4;
                            regionValue.Top /= 4;
                            regionValue.Bottom /= 4;
                        }
                        sourceDataPtr = new IntPtr((byte*)sourceDataPtr - (regionValue.Front * textureDepthStride) - (regionValue.Top * rowStride) - (regionValue.Left * sizePerElement));
                    }
                    device.UpdateSubresource(this, subResourceIndex, new DataBox(sourceDataPtr, rowStride, textureDepthStride), regionValue);
                }
                else
                {
                    device.UpdateSubresource(this, subResourceIndex, new DataBox(fromData.Pointer, rowStride, textureDepthStride));
                }
            }
            else
            {
                var mappedResource = device.MapSubresource(this, subResourceIndex, this.Usage == GraphicsResourceUsage.Dynamic ? MapMode.WriteDiscard : MapMode.Write);
                var box = mappedResource.DataBox;

                // If depth == 1 (Texture, Texture or TextureCube), then depthStride is not used
                var boxDepthStride = this.Depth == 1 ? box.SlicePitch : textureDepthStride;

                // The fast way: If same stride, we can directly copy the whole texture in one shot
                if (box.RowPitch == rowStride && boxDepthStride == textureDepthStride)
                {
                    Utilities.CopyMemory(box.DataPointer, fromData.Pointer, sizeOfTextureData);
                }
                else
                {
                    // Otherwise, the long way by copying each scanline
                    var destPerDepthPtr = (byte*)box.DataPointer;
                    var sourcePtr = (byte*)fromData.Pointer;

                    // Iterate on all depths
                    for (int j = 0; j < depth; j++)
                    {
                        var destPtr = destPerDepthPtr;
                        // Iterate on each line
                        for (int i = 0; i < height; i++)
                        {
                            Utilities.CopyMemory((IntPtr)destPtr, (IntPtr)sourcePtr, rowStride);
                            destPtr += box.RowPitch;
                            sourcePtr += rowStride;
                        }
                        destPerDepthPtr += box.SlicePitch;
                    }

                }
                device.UnmapSubresource(mappedResource);
            }
        }

        /// <summary>
        /// Makes a copy of this texture.
        /// </summary>
        /// <remarks>
        /// This method doesn't copy the content of the texture.
        /// </remarks>
        /// <returns>
        /// A copy of this texture.
        /// </returns>
        public Texture Clone()
        {
            return new Texture(GraphicsDevice).InitializeFrom(textureDescription.ToCloneableDescription(), ViewDescription);
        }

        /// <summary>
        /// Return an equivalent staging texture CPU read-writable from this instance.
        /// </summary>
        /// <returns></returns>
        public Texture ToStaging()
        {
            return new Texture(this.GraphicsDevice).InitializeFrom(textureDescription.ToStagingDescription(), ViewDescription.ToStagingDescription());
        }

        /// <summary>
        /// Loads a texture from a stream.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice"/>.</param>
        /// <param name="stream">The stream to load the texture from.</param>
        /// <param name="textureFlags">True to load the texture with unordered access enabled. Default is false.</param>
        /// <param name="usage">Usage of the resource. Default is <see cref="GraphicsResourceUsage.Immutable"/> </param>
        /// <returns>A texture</returns>
        public static Texture Load(GraphicsDevice device, Stream stream, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            using (var image = Image.Load(stream))
                return New(device, image, textureFlags, usage);
        }

        /// <summary>
        /// Loads a texture from a stream.
        /// </summary>
        /// <param name="device">The <see cref="GraphicsDevice" />.</param>
        /// <param name="image">The image.</param>
        /// <param name="textureFlags">True to load the texture with unordered access enabled. Default is false.</param>
        /// <param name="usage">Usage of the resource. Default is <see cref="GraphicsResourceUsage.Immutable" /></param>
        /// <returns>A texture</returns>
        /// <exception cref="System.InvalidOperationException">Dimension not supported</exception>
        public static Texture New(GraphicsDevice device, Image image, TextureFlags textureFlags = TextureFlags.ShaderResource, GraphicsResourceUsage usage = GraphicsResourceUsage.Immutable)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (image == null) throw new ArgumentNullException("image");

            return New(device, image.Description, image.ToDataBox());
        }

        /// <summary>
        /// Creates a new texture with the specified generic texture description.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="description">The description.</param>
        /// <param name="boxes">The data boxes.</param>
        /// <returns>A Texture instance, either a RenderTarget or DepthStencilBuffer or Texture, depending on Binding flags.</returns>
        /// <exception cref="System.ArgumentNullException">graphicsDevice</exception>
        public static Texture New(GraphicsDevice graphicsDevice, TextureDescription description, params DataBox[] boxes)
        {
            return New(graphicsDevice, description, new TextureViewDescription(), boxes);
        }

        /// <summary>
        /// Creates a new texture with the specified generic texture description.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <param name="description">The description.</param>
        /// <param name="viewDescription">The view description.</param>
        /// <param name="boxes">The data boxes.</param>
        /// <returns>A Texture instance, either a RenderTarget or DepthStencilBuffer or Texture, depending on Binding flags.</returns>
        /// <exception cref="System.ArgumentNullException">graphicsDevice</exception>
        public static Texture New(GraphicsDevice graphicsDevice, TextureDescription description, TextureViewDescription viewDescription, params DataBox[] boxes)
        {
            if (graphicsDevice == null)
            {
                throw new ArgumentNullException("graphicsDevice");
            }

            return new Texture(graphicsDevice).InitializeFrom(description, viewDescription, boxes);
        }

        /// <summary>
        /// Saves this texture to a stream with a specified format.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="fileType">Type of the image file.</param>
        public void Save(Stream stream, ImageFileType fileType)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            using (var staging = ToStaging())
                Save(stream, staging, fileType);
        }

        /// <summary>
        /// Gets the GPU content of this texture as an <see cref="Image"/> on the CPU.
        /// </summary>
        public Image GetDataAsImage()
        {
            using (var stagingTexture = ToStaging())
                return GetDataAsImage(stagingTexture);
        }

        /// <summary>
        /// Gets the GPU content of this texture to an <see cref="Image"/> on the CPU.
        /// </summary>
        /// <param name="stagingTexture">The staging texture used to temporary transfer the image from the GPU to CPU.</param>
        /// <exception cref="ArgumentException">If stagingTexture is not a staging texture.</exception>
        public Image GetDataAsImage(Texture stagingTexture)
        {
            if (stagingTexture == null) throw new ArgumentNullException("stagingTexture");
            if (stagingTexture.Usage != GraphicsResourceUsage.Staging)
                throw new ArgumentException("Invalid texture used as staging. Must have Usage = GraphicsResourceUsage.Staging", "stagingTexture");

            var image = Image.New(stagingTexture.Description);
            try {
                for (int arrayIndex = 0; arrayIndex < image.Description.ArraySize; arrayIndex++)
                {
                    for (int mipLevel = 0; mipLevel < image.Description.MipLevels; mipLevel++)
                    {
                        var pixelBuffer = image.PixelBuffer[arrayIndex, mipLevel];
                        GetData(stagingTexture, new DataPointer(pixelBuffer.DataPointer, pixelBuffer.BufferStride), arrayIndex, mipLevel);
                    }
                }

            } catch (Exception)
            {
                // If there was an exception, free the allocated image to avoid any memory leak.
                image.Dispose();
                throw;
            }
            return image;
        }

        /// <summary>
        /// Saves this texture to a stream with a specified format.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="stagingTexture">The staging texture used to temporary transfer the image from the GPU to CPU.</param>
        /// <param name="fileType">Type of the image file.</param>
        /// <exception cref="ArgumentException">If stagingTexture is not a staging texture.</exception>
        public void Save(Stream stream, Texture stagingTexture, ImageFileType fileType)
        {
            using (var image = GetDataAsImage(stagingTexture))
                image.Save(stream, fileType);
        }

        /// <summary>
        /// Calculates the mip map count from a requested level.
        /// </summary>
        /// <param name="requestedLevel">The requested level.</param>
        /// <param name="width">The width.</param>
        /// <param name="height">The height.</param>
        /// <param name="depth">The depth.</param>
        /// <returns>The resulting mipmap count (clamp to [1, maxMipMapCount] for this texture)</returns>
        internal static int CalculateMipMapCount(MipMapCount requestedLevel, int width, int height = 0, int depth = 0)
        {
            int size = Math.Max(Math.Max(width, height), depth);
            int maxMipMap = 1 + (int)Math.Ceiling(Math.Log(size) / Math.Log(2.0));

            return requestedLevel  == 0 ? maxMipMap : Math.Min(requestedLevel, maxMipMap);
        }

        protected static DataBox GetDataBox<T>(PixelFormat format, int width, int height, int depth, T[] textureData, IntPtr fixedPointer) where T : struct
        {
            // Check that the textureData size is correct
            if (textureData == null) throw new ArgumentNullException("textureData");
            int rowPitch;
            int slicePitch;
            int widthCount;
            int heightCount;
            Image.ComputePitch(format, width, height, out rowPitch, out slicePitch, out widthCount, out heightCount);
            if (Utilities.SizeOf(textureData) != (slicePitch * depth)) throw new ArgumentException("Invalid size for Image");

            return new DataBox(fixedPointer, rowPitch, slicePitch);
        }

        internal void GetViewSliceBounds(ViewType viewType, ref int arrayOrDepthIndex, ref int mipIndex, out int arrayOrDepthCount, out int mipCount)
        {
            int arrayOrDepthSize = this.Depth > 1 ? this.Depth : this.ArraySize;

            switch (viewType)
            {
                case ViewType.Full:
                    arrayOrDepthIndex = 0;
                    mipIndex = 0;
                    arrayOrDepthCount = arrayOrDepthSize;
                    mipCount = this.MipLevels;
                    break;
                case ViewType.Single:
                    arrayOrDepthCount = 1;
                    mipCount = 1;
                    break;
                case ViewType.MipBand:
                    arrayOrDepthCount = arrayOrDepthSize - arrayOrDepthIndex;
                    mipCount = 1;
                    break;
                case ViewType.ArrayBand:
                    arrayOrDepthCount = 1;
                    mipCount = MipLevels - mipIndex;
                    break;
                default:
                    arrayOrDepthCount = 0;
                    mipCount = 0;
                    break;
            }
        }

        internal int GetViewCount()
        {
            int arrayOrDepthSize = this.Depth > 1 ? this.Depth : this.ArraySize;
            return GetViewIndex((ViewType)4, arrayOrDepthSize, this.MipLevels);
        }

        internal int GetViewIndex(ViewType viewType, int arrayOrDepthIndex, int mipIndex)
        {
            int arrayOrDepthSize = this.Depth > 1 ? this.Depth : this.ArraySize;
            return (((int)viewType) * arrayOrDepthSize + arrayOrDepthIndex) * this.MipLevels + mipIndex;
        }

        internal static GraphicsResourceUsage GetUsageWithFlags(GraphicsResourceUsage usage, TextureFlags flags)
        {
            // If we have a texture supporting render target or unordered access, force to UsageDefault
            if ((flags & TextureFlags.RenderTarget) != 0 || (flags & TextureFlags.UnorderedAccess) != 0)
                return GraphicsResourceUsage.Default;
            return usage;
        }

        private static int CountMips(int width)
        {
            int mipLevels = 1;

            // TODO: Use Math.Log2 or a loop?
            while (width > 1)
            {
                ++mipLevels;

                if (width > 1)
                    width >>= 1;
            }

            return mipLevels;
        }

        private static int CountMips(int width, int height)
        {
            int mipLevels = 1;

            // TODO: Use Math.Log2 or a loop?
            while (height > 1 || width > 1)
            {
                ++mipLevels;

                if (height > 1)
                    height >>= 1;

                if (width > 1)
                    width >>= 1;
            }

            return mipLevels;
        }

        private static int CountMips(int width, int height, int depth)
        {
            int mipLevels = 1;

            // TODO: Use Math.Log2 or a loop?
            while (height > 1 || width > 1 || depth > 1)
            {
                ++mipLevels;

                if (height > 1)
                    height >>= 1;

                if (width > 1)
                    width >>= 1;

                if (depth > 1)
                    depth >>= 1;
            }

            return mipLevels;
        }
    }
}