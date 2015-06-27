﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Windows;

using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Presentation.Controls
{
    public class Vector2Editor : VectorEditor<Vector2>
    {
        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(float), typeof(Vector2Editor), new FrameworkPropertyMetadata(.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(float), typeof(Vector2Editor), new FrameworkPropertyMetadata(.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Length"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty LengthProperty = DependencyProperty.Register("Length", typeof(float), typeof(Vector2Editor), new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceLengthValue));

        /// <summary>
        /// Identifies the <see cref="Angle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AngleProperty = DependencyProperty.Register("Angle", typeof(float), typeof(Vector2Editor), new FrameworkPropertyMetadata(0.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceAngleValue));

        /// <summary>
        /// Gets or sets the X component (in Cartesian coordinate system) of the <see cref="Vector2"/> associated to this control.
        /// </summary>
        public float X { get { return (float)GetValue(XProperty); } set { SetValue(XProperty, value); } }

        /// <summary>
        /// Gets or sets the Y component (in Cartesian coordinate system) of the <see cref="Vector2"/> associated to this control.
        /// </summary>
        public float Y { get { return (float)GetValue(YProperty); } set { SetValue(YProperty, value); } }

        /// <summary>
        /// Gets or sets the length (in polar coordinate system) of the <see cref="Vector2"/> associated to this control.
        /// </summary>
        public float Length { get { return (float)GetValue(LengthProperty); } set { SetValue(LengthProperty, value); } }

        /// <summary>
        /// Gets or sets the angle (in polar coordinate system) of the <see cref="Vector2"/> associated to this control.
        /// </summary>
        public float Angle { get { return (float)GetValue(AngleProperty); } set { SetValue(AngleProperty, value); } }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(Vector2 value)
        {
            SetCurrentValue(XProperty, value.X);
            SetCurrentValue(YProperty, value.Y);
            SetCurrentValue(LengthProperty, value.Length());
            SetCurrentValue(AngleProperty, MathUtil.RadiansToDegrees((float)Math.Atan2(value.Y, value.X)));
        }

        /// <inheritdoc/>
        protected override Vector2 UpdateValueFromComponent(DependencyProperty property)
        {
            if (property == LengthProperty)
            {
                var newValue = Value;
                newValue.Normalize();
                newValue *= Length;
                return newValue;
            }
            if (property == AngleProperty)
            {
                var angle = MathUtil.DegreesToRadians(Angle);
                return new Vector2((float)(Length * Math.Cos(angle)), (float)(Length * Math.Sin(angle)));
            }
            if (property == XProperty)
                return new Vector2(X, Value.Y);
            if (property == YProperty)
                return new Vector2(Value.X, Y);
              
            throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");
        }

        /// <inheritdoc/>
        protected override Vector2 UpateValueFromFloat(float value)
        {
            return new Vector2(value);
        }

        /// <summary>
        /// Coerce the value of the Length so it is always positive
        /// </summary>
        private static object CoerceLengthValue(DependencyObject sender, object baseValue)
        {
            baseValue = CoerceComponentValue(sender, baseValue);
            return Math.Max(0.0f, (float)baseValue);
        }

        /// <summary>
        /// Coerce the value of the Angle so it is always contained between 0 and 360
        /// </summary>
        private static object CoerceAngleValue(DependencyObject sender, object baseValue)
        {
            baseValue = CoerceComponentValue(sender, baseValue);
            return (float)baseValue % 360.0f;
        }
    }
}