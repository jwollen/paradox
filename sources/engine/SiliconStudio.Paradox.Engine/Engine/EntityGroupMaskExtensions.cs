﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Runtime.CompilerServices;

namespace SiliconStudio.Paradox.Engine
{
    /// <summary>
    /// Extensions for <see cref="EntityGroupMask"/>
    /// </summary>
    public static class EntityGroupMaskExtensions
    {
        /// <summary>
        /// Determines whether the group mask contains the specified group.
        /// </summary>
        /// <param name="mask">The mask.</param>
        /// <param name="group">The group.</param>
        /// <returns><c>true</c> if the group mask contains the specified group; otherwise, <c>false</c>.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Contains(this EntityGroupMask mask, EntityGroup group)
        {
            return ((uint)mask & (1 << (int)group)) != 0;
        }
    }
}