﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Assets.SpriteFont
{
    /// <summary>
    /// Describes a range of consecutive characters that should be included in the font.
    /// </summary>
    [DataContract("CharacterRegion")]
    public struct CharacterRegion
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CharacterRegion"/> structure.
        /// </summary>
        /// <param name="start">The start.</param>
        /// <param name="end">The end.</param>
        /// <exception cref="System.ArgumentException"></exception>
        public CharacterRegion(char start, char end)
        {
            if (start > end)
                throw new ArgumentException();

            Start = start;
            End = end;
        }

        /// <summary>
        /// The first character to include in the region.
        /// </summary>
        /// <userdoc>
        /// The first character of the region.
        /// </userdoc>
        [DataMember(0)]
        public char Start;

        /// <summary>
        /// The second character to include in the region.
        /// </summary>
        /// <userdoc>
        /// The last character of the region.
        /// </userdoc>
        [DataMember(1)]
        public char End;

        // Flattens a list of character regions into a combined list of individual characters.
        public static IEnumerable<Char> Flatten(List<CharacterRegion> regions)
        {
            if (regions.Any())
            {
                // If we have any regions, flatten them and remove duplicates.
                return regions.SelectMany(region => region.GetCharacters()).Distinct();
            }

            // If no regions were specified, use the default.
            return Default.GetCharacters();
        }

        // Default to just the base ASCII character set.
        public static CharacterRegion Default = new CharacterRegion(' ', '~');

        // Enumerates all characters within the region.
        private IEnumerable<Char> GetCharacters()
        {
            for (char c = Start; c <= End; c++)
            {
                yield return c;
            }
        }
    }
}