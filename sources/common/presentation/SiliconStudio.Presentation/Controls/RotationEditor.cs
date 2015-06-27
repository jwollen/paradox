using System;
using System.Windows;

using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Presentation.Controls
{
    public class RotationEditor : VectorEditor<Quaternion>
    {
        private Vector3 decomposedRotation;

        /// <summary>
        /// Identifies the <see cref="X"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty XProperty = DependencyProperty.Register("X", typeof(float), typeof(RotationEditor), new FrameworkPropertyMetadata(.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Y"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty YProperty = DependencyProperty.Register("Y", typeof(float), typeof(RotationEditor), new FrameworkPropertyMetadata(.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// Identifies the <see cref="Z"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ZProperty = DependencyProperty.Register("Z", typeof(float), typeof(RotationEditor), new FrameworkPropertyMetadata(.0f, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault, OnComponentPropertyChanged, CoerceComponentValue));

        /// <summary>
        /// The X component (in Cartesian coordinate system) of the <see cref="Vector3"/> associated to this control.
        /// </summary>
        public float X { get { return (float)GetValue(XProperty); } set { SetValue(XProperty, value); } }

        /// <summary>
        /// The Y component (in Cartesian coordinate system) of the <see cref="Vector3"/> associated to this control.
        /// </summary>
        public float Y { get { return (float)GetValue(YProperty); } set { SetValue(YProperty, value); } }

        /// <summary>
        /// The Y component (in Cartesian coordinate system) of the <see cref="Vector3"/> associated to this control.
        /// </summary>
        public float Z { get { return (float)GetValue(ZProperty); } set { SetValue(ZProperty, value); } }

        /// <inheritdoc/>
        protected override void UpdateComponentsFromValue(Quaternion value)
        {
            Matrix rotationMatrix = Matrix.RotationQuaternion(value);
            rotationMatrix.DecomposeXYZ(out decomposedRotation);
            SetCurrentValue(XProperty, MathUtil.RadiansToDegrees(decomposedRotation.X));
            SetCurrentValue(YProperty, MathUtil.RadiansToDegrees(decomposedRotation.Y));
            SetCurrentValue(ZProperty, MathUtil.RadiansToDegrees(decomposedRotation.Z));
        }

        /// <inheritdoc/>
        protected override Quaternion UpdateValueFromComponent(DependencyProperty property)
        {
            if (property == XProperty)
                decomposedRotation = new Vector3(MathUtil.DegreesToRadians(X), decomposedRotation.Y, decomposedRotation.Z);
            else if (property == YProperty)
                decomposedRotation = new Vector3(decomposedRotation.X, MathUtil.DegreesToRadians(Y), decomposedRotation.Z);
            else if (property == ZProperty)
                decomposedRotation = new Vector3(decomposedRotation.X, decomposedRotation.Y, MathUtil.DegreesToRadians(Z));
            else
                throw new ArgumentException("Property unsupported by method UpdateValueFromComponent.");

            Quaternion quatX, quatY, quatZ;
            Quaternion.RotationX(decomposedRotation.X, out quatX);
            Quaternion.RotationY(decomposedRotation.Y, out quatY);
            Quaternion.RotationZ(decomposedRotation.Z, out quatZ);
            return quatX * quatY * quatZ;
        }

        /// <inheritdoc/>
        protected override Quaternion UpateValueFromFloat(float value)
        {
            var radian = MathUtil.DegreesToRadians(value);
            decomposedRotation = new Vector3(radian);
            Quaternion quatX, quatY, quatZ;
            Quaternion.RotationX(decomposedRotation.X, out quatX);
            Quaternion.RotationY(decomposedRotation.Y, out quatY);
            Quaternion.RotationZ(decomposedRotation.Z, out quatZ);
            return quatX * quatY * quatZ;
        }
    }
}