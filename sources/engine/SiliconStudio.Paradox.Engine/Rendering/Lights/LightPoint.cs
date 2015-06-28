// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.ComponentModel;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    /// <summary>
    /// A point light.
    /// </summary>
    [DataContract("LightPoint")]
    [Display("Point")]
    public class LightPoint : DirectLightBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LightPoint"/> class.
        /// </summary>
        public LightPoint()
        {
            Radius = 1.0f;
            Shadow = new LightStandardShadowMap() { Importance = LightShadowImportance.Low };
        }

        /// <summary>
        /// Gets or sets the radius of influence of this light.
        /// </summary>
        /// <value>The range.</value>
        [DataMember(10)]
        [DefaultValue(1.0f)]
        public float Radius{ get; set; }

        [DataMemberIgnore]
        internal float InvSquareRadius;

        public override bool HasBoundingBox
        {
            get
            {
                return true;
            }
        }

        public override bool Update(LightComponent lightComponent)
        {
            var range = Math.Max(0.001f, Radius);
            InvSquareRadius = 1.0f / (range * range);
            return true;
        }

        public override BoundingBox ComputeBounds(Vector3 positionWS, Vector3 directionWS)
        {
            return new BoundingBox(positionWS - Radius, positionWS + Radius);
        }

        protected override float ComputeScreenCoverage(CameraComponent camera, Vector3 position, Vector3 direction, float width, float height)
        {
            // http://stackoverflow.com/questions/21648630/radius-of-projected-sphere-in-screen-space
            var targetPosition = new Vector4(position, 1.0f);
            Vector4 projectedTarget;
            Vector4.Transform(ref targetPosition, ref camera.ViewProjectionMatrix, out projectedTarget);

            var d = Math.Abs(projectedTarget.W) + 0.00001f;
            var r = Radius;
            var coTanFovBy2 = camera.ProjectionMatrix.M22;
            var pr = r * coTanFovBy2 / (Math.Sqrt(d * d - r * r) + 0.00001f);

            // Size on screen
            return (float)pr * Math.Max(width, height);
        }
    }
}