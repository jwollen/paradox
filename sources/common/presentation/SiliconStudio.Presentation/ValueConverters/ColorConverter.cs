﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Globalization;
using System.Windows.Media;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Presentation.Extensions;

using Color = SiliconStudio.Core.Mathematics.Color;

namespace SiliconStudio.Presentation.ValueConverters
{
    /// <summary>
    /// This converter will convert any known type of color value to the target type, if the conversion is possible. Otherwise, a <see cref="NotSupportedException"/> will be thrown.
    /// </summary>
    public class ColorConverter : ValueConverterBase<ColorConverter>
    {
        /// <inheritdoc/>
        public override object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var brush = value as SolidColorBrush;
            if (brush != null)
                value = brush.Color;

            if (value is Color)
            {
                var color = (Color)value;
                if (targetType == typeof(System.Windows.Media.Color))
                    return color.ToSystemColor();
                if (targetType == typeof(Color))
                    return color;
                if (targetType == typeof(Color3))
                    return color.ToColor3();
                if (targetType == typeof(Color4))
                    return color.ToColor4();
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(color.ToSystemColor());
                if (targetType == typeof(string))
                    return ColorExtensions.RgbaToString(color.ToRgba());
            }
            if (value is Color3)
            {
                var color = (Color3)value;
                if (targetType == typeof(System.Windows.Media.Color))
                    return color.ToSystemColor();
                if (targetType == typeof(Color))
                    return (Color)color;
                if (targetType == typeof(Color3))
                    return color;
                if (targetType == typeof(Color4))
                    return color.ToColor4();
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(color.ToSystemColor());
                if (targetType == typeof(string))
                    return ColorExtensions.RgbToString(color.ToRgb());
            }
            if (value is Color4)
            {
                var color = (Color4)value;
                if (targetType == typeof(System.Windows.Media.Color))
                    return color.ToSystemColor();
                if (targetType == typeof(Color))
                    return (Color)color;
                if (targetType == typeof(Color3))
                    return color.ToColor3();
                if (targetType == typeof(Color4))
                    return color;
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(color.ToSystemColor());
                if (targetType == typeof(string))
                    return ColorExtensions.RgbaToString(color.ToRgba());
            }
            if (value is System.Windows.Media.Color)
            {
                var wpfColor = (System.Windows.Media.Color)value;
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                    return new SolidColorBrush(wpfColor);

                var color = new Color(wpfColor.R, wpfColor.G, wpfColor.B, wpfColor.A);
                if (targetType == typeof(System.Windows.Media.Color))
                    return color;
                if (targetType == typeof(Color))
                    return color;
                if (targetType == typeof(Color3))
                    return color.ToColor3();
                if (targetType == typeof(Color4))
                    return color.ToColor4();
                if (targetType == typeof(string))
                    return ColorExtensions.RgbaToString(color.ToRgba());
            }
            if (value is string)
            {
                var stringColor = value as string;
                uint intValue = 0xFF000000;
                if (stringColor.StartsWith("#"))
                {
                    if (stringColor.Length == "#000".Length && UInt32.TryParse(stringColor.Substring(1, 3), NumberStyles.HexNumber, null, out intValue))
                    {
                        intValue = ((intValue & 0x00F) << 16)
                                 | ((intValue & 0x00F) << 20)
                                 | ((intValue & 0x0F0) << 4)
                                 | ((intValue & 0x0F0) << 8)
                                 | ((intValue & 0xF00) >> 4)
                                 | ((intValue & 0xF00) >> 8)
                                 | (0xFF000000);
                    }
                    if (stringColor.Length == "#000000".Length && UInt32.TryParse(stringColor.Substring(1, 6), NumberStyles.HexNumber, null, out intValue))
                    {
                        intValue = ((intValue & 0x000000FF) << 16)
                                 | (intValue & 0x0000FF00)
                                 | ((intValue & 0x00FF0000) >> 16)
                                 | (0xFF000000);
                    }
                    if (stringColor.Length == "#00000000".Length && UInt32.TryParse(stringColor.Substring(1, 8), NumberStyles.HexNumber, null, out intValue))
                    {
                        intValue = ((intValue & 0x000000FF) << 16)
                                 | (intValue & 0x0000FF00)
                                 | ((intValue & 0x00FF0000) >> 16)
                                 | (intValue & 0xFF000000);
                    }
                }

                if (targetType == typeof(Color))
                    return Color.FromRgba(intValue);
                if (targetType == typeof(Color3))
                    return new Color3(intValue);
                if (targetType == typeof(Color4))
                    return new Color4(intValue);
                if (targetType == typeof(System.Windows.Media.Color))
                {
                    return System.Windows.Media.Color.FromArgb(
                        (byte)((intValue >> 24) & 255),
                        (byte)(intValue & 255),
                        (byte)((intValue >> 8) & 255),
                        (byte)((intValue >> 16) & 255));
                }
                if (targetType.IsAssignableFrom(typeof(SolidColorBrush)))
                {
                    return new SolidColorBrush(System.Windows.Media.Color.FromArgb(
                        (byte)((intValue >> 24) & 255),
                        (byte)(intValue & 255),
                        (byte)((intValue >> 8) & 255),
                        (byte)((intValue >> 16) & 255)));
                }
                if (targetType == typeof(string))
                    return stringColor;
            }
            throw new NotSupportedException("Requested conversion is not supported.");
        }

        /// <inheritdoc/>
        public override object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (targetType == typeof(object))
                return value;

            return Convert(value, targetType, parameter, culture);
        }
    }
}
