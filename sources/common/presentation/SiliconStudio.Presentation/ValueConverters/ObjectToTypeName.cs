﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Globalization;

namespace SiliconStudio.Presentation.ValueConverters
{
    /// <summary>
    /// This converter convert any object to a string representing the name of its type (without assembly or namespace qualification).
    /// It accepts null and will convert it to a string representation of null.
    /// </summary>
    /// <seealso cref="ObjectToFullTypeName"/>
    /// <seealso cref="ObjectToType"/>
    public class ObjectToTypeName : OneWayValueConverter<ObjectToTypeName>
    {
        /// <summary>
        /// The string representation of the type of a null object
        /// </summary>
        public const string NullObjectType = "(None)";

        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value == null ? NullObjectType : value.GetType().Name;
        }
    }
}
