﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Core.Reflection
{
    /// <summary>
    /// A Type of override used on a member value.
    /// </summary>
    [Flags]
    public enum OverrideType
    {
        /// <summary>
        /// The value is taken from a base value or this instance if no base (default).
        /// </summary>
        Base,

        /// <summary>
        /// The value is new and overridden locally. Base value is ignored.
        /// </summary>
        New = 1,

        /// <summary>
        /// The value is sealed and cannot be changed by 
        /// </summary>
        Sealed = 2,
    }

    /// <summary>
    /// Extensions for <see cref="OverrideType"/>.
    /// </summary>
    public static class OverrideTypeExtensions
    {
        /// <summary>
        /// Determines whether the specified type is sealed.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is sealed; otherwise, <c>false</c>.</returns>
        public static bool IsSealed(this OverrideType type)
        {
            return (type & OverrideType.Sealed) != 0;
        }

        /// <summary>
        /// Determines whether the specified type is base.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is base; otherwise, <c>false</c>.</returns>
        public static bool IsBase(this OverrideType type)
        {
            return (type & OverrideType.Base) != 0;
        }

        /// <summary>
        /// Determines whether the specified type is new.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <returns><c>true</c> if the specified type is new; otherwise, <c>false</c>.</returns>
        public static bool IsNew(this OverrideType type)
        {
            return (type & OverrideType.New) != 0;
        }
    }
}