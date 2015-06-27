﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.ComponentModel;

using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Assets.Textures
{
    /// <summary>
    /// Describes a texture asset.
    /// </summary>
    [DataContract("Texture")]
    [AssetDescription(FileExtension)]
    [AssetCompiler(typeof(TextureAssetCompiler))]
    [ThumbnailCompiler(PreviewerCompilerNames.TextureThumbnailCompilerQualifiedName, true, Priority = -10000)]
    [Display(105, "Texture", "A texture")]
    [CategoryOrder(10, "Size")]
    [CategoryOrder(20, "Format")]
    [CategoryOrder(30, "Transparency")]
    public sealed class TextureAsset : AssetImport
    {
        /// <summary>
        /// The default file extension used by the <see cref="TextureAsset"/>.
        /// </summary>
        public const string FileExtension = ".pdxtex";

        /// <summary>
        /// Initializes a new instance of the <see cref="TextureAsset"/> class.
        /// </summary>
        public TextureAsset()
        {
            SetDefaults();
        }

        /// <summary>
        /// Gets or sets the width.
        /// </summary>
        /// <value>The width.</value>
        /// <userdoc>
        /// The width of the texture in-game. Depending on the value of the IsSizeInPercentage property, the value might represent either percent (%) or actual pixel.
        /// </userdoc>
        [DataMember(20)]
        [DefaultValue(100.0f)]
        [DataMemberRange(0, 10000, 1, 10)]
        [Display(null, null, "Size")]
        public float Width { get; set; }

        /// <summary>
        /// Gets or sets the height.
        /// </summary>
        /// <value>The height.</value>
        /// <userdoc>
        /// The height of the texture in-game. Depending on the value of the IsSizeInPercentage property, the value might represent either percent (%) or actual pixel.
        /// </userdoc>
        [DataMember(30)]
        [DefaultValue(100.0f)]
        [DataMemberRange(0, 10000, 1, 10)]
        [Display(null, null, "Size")]
        public float Height { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether this instance is using size in percentage. Default is true. See remarks.
        /// </summary>
        /// <value><c>true</c> if this instance is dimension absolute; otherwise, <c>false</c>.</value>
        /// <remarks>
        /// When this property is true (by default), <see cref="Width"/> and <see cref="Height"/> are epxressed 
        /// in percentage, with 100.0f being 100% of the current size, and 50.0f half of the current size, otherwise
        /// the size is in absolute pixels.
        /// </remarks>
        /// <userdoc>
        /// If checked, the values of the Width and Height properties will represent percent (%). Otherwise they would represent actual pixel.
        /// </userdoc>
        [DataMember(40)]
        [DefaultValue(true)]
        [Display(null, null, "Size")]
        public bool IsSizeInPercentage { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to enable color key. Default is false.
        /// </summary>
        /// <value><c>true</c> to enable color key; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// If checked, all pixels of the color set in the ColorKeyColor property will be replaced by transparent black.
        /// </userdoc>
        [DataMember(43)]
        [DefaultValue(false)]
        [Display(null, null, "Transparency")]
        public bool ColorKeyEnabled { get; set; }

        /// <summary>
        /// Gets or sets the color key used when color keying for a texture is enabled. When color keying, all pixels of a specified color are replaced with transparent black.
        /// </summary>
        /// <value>The color key.</value>
        /// <userdoc>
        /// If ColorKeyEnabled is true, All pixels of the color set to this property are replaced with transparent black.
        /// </userdoc>
        [DataMember(45)]
        [Display(null, null, "Transparency")]
        public Color ColorKeyColor { get; set; }

        /// <summary>
        /// Gets or sets the texture format.
        /// </summary>
        /// <value>The texture format.</value>
        /// <userdoc>
        /// The format to use for the texture. If Compressed, the final texture size must be a multiple of 4.
        /// </userdoc>
        [DataMember(50)]
        [DefaultValue(TextureFormat.Compressed)]
        [Display(null, null, "Format")]
        public TextureFormat Format { get; set; }

        /// <summary>
        /// Gets or sets the hint to indicate the type of texture. See remarks.
        /// </summary>
        /// <value>The hint.</value>
        /// <remarks>This hint helps the texture compressor to select the appropriate format based on the HW Level and 
        /// platform.</remarks>
        /// <userdoc>A hint to indicate the usage/type of texture. This hint helps the texture compressor to select the 
        /// appropriate format based on the HW Level and platform.</userdoc>
        [DataMember(51)]
        [DefaultValue(TextureHint.Color)]
        [Display(null, null, "Format")]
        public TextureHint Hint { get; set; }

        /// <summary>
        /// Gets or sets the alpha format.
        /// </summary>
        /// <value>The alpha format.</value>
        /// <userdoc>
        /// The format to use for alpha in the texture.
        /// </userdoc>
        [DataMember(55)]
        [DefaultValue(AlphaFormat.None)]
        [Display(null, null, "Transparency")]
        public AlphaFormat Alpha { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to generate mipmaps.
        /// </summary>
        /// <value><c>true</c> if mipmaps are generated; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// If checked, Mipmaps will be pre-generated for this texture.
        /// </userdoc>
        [DataMember(60)]
        [DefaultValue(true)]
        [Display(null, null, "Format")]
        public bool GenerateMipmaps { get; set; }

        /// <summary>
        /// Gets or sets the value indicating whether the output texture is encoded into the standard RGB color space.
        /// </summary>
        /// <userdoc>
        /// If checked, the input image is considered as an sRGB image. This should be default for colored texture
        /// with a HDR/gamma correct rendering.
        /// </userdoc>
        [DataMember(70)]
        [DefaultValue(false)]
        [Display("sRGB", null, "Format")]
        public bool SRgb { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether to convert the texture in premultiply alpha.
        /// </summary>
        /// <value><c>true</c> to convert the texture in premultiply alpha.; otherwise, <c>false</c>.</value>
        /// <userdoc>
        /// If checked, The color values will be pre-multiplied by the alpha value.
        /// </userdoc>
        [DataMember(80)]
        [DefaultValue(true)]
        [Display(null, null, "Transparency")]
        public bool PremultiplyAlpha { get; set; }

        public override void SetDefaults()
        {
            Width = 100.0f;
            Height = 100.0f;
            Format = TextureFormat.Compressed;
            Hint = TextureHint.Color;
            Alpha = AlphaFormat.None;
            ColorKeyColor = new Color(255, 0, 255);
            ColorKeyEnabled = false;
            IsSizeInPercentage = true;
            GenerateMipmaps = true;
            PremultiplyAlpha = true;
        }
    }
}