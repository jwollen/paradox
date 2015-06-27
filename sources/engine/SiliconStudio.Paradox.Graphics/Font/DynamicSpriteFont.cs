﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Paradox.Graphics.Font
{
    /// <summary>
    /// A dynamic font. That is a font that generate its character bitmaps at execution.
    /// </summary>
    [DataSerializerGlobal(typeof(ReferenceSerializer<DynamicSpriteFont>), Profile = "Asset")]
    [ContentSerializer(typeof(DynamicSpriteFontContentSerializer))]
    [DataSerializer(typeof(DynamicSpriteFontSerializer))]
    internal class DynamicSpriteFont : SpriteFont
    {
        /// <summary>
        /// Input the family name of the (TrueType) font.
        /// </summary>
        internal string FontName;

        /// <summary>
        /// Style for the font. 'regular', 'bold' or 'italic'. Default is 'regular
        /// </summary>
        internal FontStyle Style;

        /// <summary>
        /// Specifies whether to use kerning information when rendering the font. Default value is false (NOT SUPPORTED YET).
        /// </summary>
        internal bool UseKerning;

        /// <summary>
        /// The alias mode of the font
        /// </summary>
        internal FontAntiAliasMode AntiAlias;

        /// <summary>
        /// The character specifications cached to avoid re-allocations
        /// </summary>
        private readonly Dictionary<CharacterKey, CharacterSpecification> sizedCharacterToCharacterData = new Dictionary<CharacterKey, CharacterSpecification>();

        [DataMemberIgnore]
        internal FontManager FontManager
        {
            get { return FontSystem != null? FontSystem.FontManager: null; }
        }

        [DataMemberIgnore]
        internal FontCacheManager FontCacheManager
        {
            get { return FontSystem != null? FontSystem.FontCacheManager: null; }
        }

        [DataMemberIgnore]
        internal int FrameCount
        {
            get { return FontSystem != null ? FontSystem.FrameCount : 0; }
        }

        [DataMemberIgnore]
        internal override FontSystem FontSystem
        {
            set
            {
                if (FontSystem == value)
                    return;

                base.FontSystem = value;
                
                // retrieve needed info from the font
                float relativeLineSpacing;
                float relativeBaseOffsetY;
                float relativeMaxWidth;
                float relativeMaxHeight;
                FontManager.GetFontInfo(FontName, Style, out relativeLineSpacing, out relativeBaseOffsetY, out relativeMaxWidth, out relativeMaxHeight);

                // set required base properties
                DefaultLineSpacing = relativeLineSpacing * Size;
                BaseOffsetY = relativeBaseOffsetY * Size;
                Textures = FontCacheManager.Textures;
                Swizzle = SwizzleMode.RRRR;
            }
        }

        public DynamicSpriteFont()
        {
            IsDynamic = true;
        }

        public override bool IsCharPresent(char c)
        {
            return FontManager.DoesFontContains(FontName, Style, c);
        }

        protected override Glyph GetGlyph(char character, ref Vector2 fontSize, bool uploadGpuResources)
        {
            // Add a safe guard to prevent the system to generate characters too big for the dynamic font cache texture
            fontSize.X = Math.Min(fontSize.X, 1024);
            fontSize.Y = Math.Min(fontSize.Y, 1024);

            // get the character data associated to the provided character and size
            var characterData = GetOrCreateCharacterData(fontSize, character);
            
            // generate the bitmap if it does not exist
            if(characterData.Bitmap == null)
                FontManager.GenerateBitmap(characterData, false);

            // upload the character to the GPU font texture and create the glyph if does not exists
            if (uploadGpuResources && characterData.Bitmap != null && !characterData.IsBitmapUploaded)
                FontCacheManager.UploadCharacterBitmap(characterData);

            // update the character usage info
            FontCacheManager.NotifyCharacterUtilization(characterData);

            return characterData.Glyph;
        }

        internal override void PreGenerateGlyphs(ref StringProxy text, ref Vector2 size)
        {
            for (int i = 0; i < text.Length; i++)
            {
                // get the character data associated to the provided character and size
                var characterData = GetOrCreateCharacterData(size, text[i]);

                // force asynchronous generation of the bitmap if it does not exist
                if (characterData.Bitmap == null)
                    FontManager.GenerateBitmap(characterData, true);
            }
        }

        private CharacterSpecification GetOrCreateCharacterData(Vector2 size, char character)
        {
            // build the dictionary look up key
            var lookUpKey = new CharacterKey(character, size);

            // get the entry (creates it if it does not exist)
            CharacterSpecification characterData;
            if (!sizedCharacterToCharacterData.TryGetValue(lookUpKey, out characterData))
            {
                characterData = new CharacterSpecification(character, FontName, size, Style, AntiAlias);
                sizedCharacterToCharacterData[lookUpKey] = characterData;
            }
            return characterData;
        }

        private struct CharacterKey : IEquatable<CharacterKey>
        {
            private readonly char character;

            private readonly Vector2 size;

            public CharacterKey(char character, Vector2 size)
            {
                this.character = character;
                this.size = size;
            }

            public bool Equals(CharacterKey other)
            {
                return character == other.character && size == other.size;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is CharacterKey && Equals((CharacterKey)obj);
            }

            public override int GetHashCode()
            {
                return character.GetHashCode();
            }
        }
    }
}