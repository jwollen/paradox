﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Linq;
using System.Collections.Generic;

using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Graphics.Font
{
    /// <summary>
    /// The system managing the fonts.
    /// </summary>
    public class FontSystem : IFontFactory
    {
        internal int FrameCount { get; private set; }
        internal FontManager FontManager { get; private set; }
        internal GraphicsDevice GraphicsDevice { get; private set; }
        internal FontCacheManager FontCacheManager { get; private set; }
        internal readonly HashSet<SpriteFont> AllocatedSpriteFonts = new HashSet<SpriteFont>();

        /// <summary>
        /// Create a new instance of <see cref="FontSystem" /> base on the provided <see cref="GraphicsDevice" />.
        /// </summary>
        public FontSystem()
        {
        }

        /// <summary>
        /// Load this system.
        /// </summary>
        /// <param name="graphicsDevice">The graphics device.</param>
        /// <exception cref="System.ArgumentNullException">graphicsDevice</exception>
        public void Load(GraphicsDevice graphicsDevice)
        {
            // TODO possibly load cached character bitmaps from the disk
            if (graphicsDevice == null) throw new ArgumentNullException("graphicsDevice");
            GraphicsDevice = graphicsDevice;
            FontManager = new FontManager();
            FontCacheManager = new FontCacheManager(this);
        }

        public void Draw()
        {
            ++FrameCount;
        }

        public void Unload()
        {
            // TODO possibly save generated characters bitmaps on the disk

            // Dispose create sprite fonts
            foreach (var allocatedSpriteFont in AllocatedSpriteFonts.ToArray())
                allocatedSpriteFont.Dispose();
        }

        public SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Image> images, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            var font = new StaticSpriteFont(size, glyphs, null, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter) { FontSystem = this };

            // affects the textures from the images.
            foreach (var image in images)
                font.StaticTextures.Add(Texture.New(GraphicsDevice, image).DisposeBy(font));

            return font;
        }

        public SpriteFont NewStatic(float size, IList<Glyph> glyphs, IList<Texture> textures, float baseOffset, float defaultLineSpacing, IList<Kerning> kernings = null, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            return new StaticSpriteFont(size, glyphs, textures, baseOffset, defaultLineSpacing, kernings, extraSpacing, extraLineSpacing, defaultCharacter) { FontSystem = this };
        }
        
        public SpriteFont NewDynamic(float defaultSize, string fontName, FontStyle style, FontAntiAliasMode antiAliasMode = FontAntiAliasMode.Default, bool useKerning = false, float extraSpacing = 0, float extraLineSpacing = 0, char defaultCharacter = ' ')
        {
            var font = new DynamicSpriteFont
            {
                Size = defaultSize,
                FontName = fontName,
                Style = style,
                AntiAlias = antiAliasMode,
                UseKerning = useKerning,
                ExtraSpacing = extraSpacing,
                ExtraLineSpacing = extraLineSpacing,
                DefaultCharacter = defaultCharacter,
                FontSystem = this
            };

            return font;
        }
    }
}