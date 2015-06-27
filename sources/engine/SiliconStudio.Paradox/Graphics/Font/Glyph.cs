﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Graphics.Font
{
    /// <summary>
    /// Description of a glyph (a single character)
    /// </summary>
    [DataContract]
    public class Glyph
    {
        /// <summary>
        /// Unicode codepoint.
        /// </summary>
        public int Character;

        /// <summary>
        /// Glyph image data (may only use a portion of a larger bitmap).
        /// </summary>
        public Rectangle Subrect;

        /// <summary>
        /// Layout information.
        /// </summary>
        public Vector2 Offset;

        /// <summary>
        /// Advance X
        /// </summary>
        public float XAdvance;

        /// <summary>
        /// Index of the bitmap. 
        /// </summary>
        public int BitmapIndex;
    } 
}