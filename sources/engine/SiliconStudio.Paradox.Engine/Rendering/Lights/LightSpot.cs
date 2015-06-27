// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.ComponentModel;

using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    /// <summary>
    /// A spot light.
    /// </summary>
    [DataContract("LightSpot")]
    [Display("Spot")]
    public class LightSpot : DirectLightBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightSpot"/> class.
        /// </summary>
        public LightSpot()
        {
            Range = 3.0f;
            AngleInner = 30.0f;
            AngleOuter = 35.0f;
            Shadow = new LightStandardShadowMap() { Importance = LightShadowImportance.Medium };
        }

        /// <summary>
        /// Gets or sets the range distance the light is affecting.
        /// </summary>
        /// <value>The range.</value>
        [DataMember(10)]
        [DefaultValue(3.0f)]
        public float Range { get; set; }

        /// <summary>
        /// Gets or sets the spot angle in degrees.
        /// </summary>
        /// <value>The spot angle in degrees.</value>
        [DataMember(20)]
        [DataMemberRange(0.01, 90, 1, 10, 1)]
        [DefaultValue(30.0f)]
        public float AngleInner { get; set; }

        /// <summary>
        /// Gets or sets the spot angle in degrees.
        /// </summary>
        /// <value>The spot angle in degrees.</value>
        [DataMember(30)]
        [DataMemberRange(0.01, 90, 1, 10, 1)]
        [DefaultValue(35.0f)]
        public float AngleOuter { get; set; }

        [DataMemberIgnore]
        internal float InvSquareRange;

        [DataMemberIgnore]
        internal float LightAngleScale;

        [DataMemberIgnore]
        internal float LightAngleOffset;

        internal float LightRadiusAtTarget;

        public override bool Update(LightComponent lightComponent)
        {
            var range = Math.Max(0.001f, Range);
            InvSquareRange = 1.0f / (range * range);
            var innerAngle = Math.Min(AngleInner, AngleOuter);
            var outerAngle = Math.Max(AngleInner, AngleOuter);
            var cosInner = (float)Math.Cos(MathUtil.DegreesToRadians(innerAngle / 2));
            var cosOuter = (float)Math.Cos(MathUtil.DegreesToRadians(outerAngle / 2));
            LightAngleScale = 1.0f / Math.Max(0.001f, cosInner - cosOuter);
            LightAngleOffset = -cosOuter * LightAngleScale;

            LightRadiusAtTarget = (float)Math.Abs(Range * Math.Sin(MathUtil.DegreesToRadians(outerAngle / 2.0f)));

            return true;
        }

        public override bool HasBoundingBox
        {
            get
            {
                return true;
            }
        }

        public override BoundingBox ComputeBounds(Vector3 position, Vector3 direction)
        {
            // Calculates the bouding box of the spot target
            var spotTarget = position + direction * Range;
            var r = LightRadiusAtTarget * 1.73205080f; // * length(vector3(r,r,r))
            var box = new BoundingBox(spotTarget - r, spotTarget + r);

            // Merge it with the start of the bounding box
            BoundingBox.Merge(ref box, ref position, out box);
            return box;
        }

        protected override float ComputeScreenCoverage(CameraComponent camera, Vector3 position, Vector3 direction, float width, float height)
        {
            // http://stackoverflow.com/questions/21648630/radius-of-projected-sphere-in-screen-space
            // Use a sphere at target point to compute the screen coverage. This is a very rough approximation.
            // We compute the sphere at target point where the size of light is the largest
            // TODO: Check if we can improve this calculation with a better model
            var targetPosition = new Vector4(position + direction * Range, 1.0f);
            Vector4 projectedTarget;
            Vector4.Transform(ref targetPosition, ref camera.ViewProjectionMatrix, out projectedTarget);

            var d = Math.Abs(projectedTarget.W) + 0.00001f;
            var r = Range * Math.Sin(MathUtil.DegreesToRadians(AngleOuter/2.0f));
            var coTanFovBy2 = camera.ProjectionMatrix.M22;
            var pr = r * coTanFovBy2 / (Math.Sqrt(d * d - r * r) + 0.00001f);

            // Size on screen
            return (float)pr * Math.Max(width, height);
        }
    }
}