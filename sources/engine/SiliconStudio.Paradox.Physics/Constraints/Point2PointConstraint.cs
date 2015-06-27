﻿// Copyright (c) 2014-2015 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Physics
{
    public class Point2PointConstraint : Constraint
    {
        /// <summary>
        /// Gets or sets the pivot in a.
        /// </summary>
        /// <value>
        /// The pivot in a.
        /// </value>
        public Vector3 PivotInA 
        {
            get { return InternalPoint2PointConstraint.PivotInA; }
            set { InternalPoint2PointConstraint.SetPivotA(value); } 
        }

        /// <summary>
        /// Gets or sets the pivot in b.
        /// </summary>
        /// <value>
        /// The pivot in b.
        /// </value>
        public Vector3 PivotInB
        {
            get { return InternalPoint2PointConstraint.PivotInB; }
            set { InternalPoint2PointConstraint.SetPivotB(value); }
        }

        /// <summary>
        /// Gets or sets the damping.
        /// </summary>
        /// <value>
        /// The damping.
        /// </value>
        public float Damping
        {
            get { return InternalPoint2PointConstraint.Setting.Damping; }
            set { InternalPoint2PointConstraint.Setting.Damping = value; }
        }

        /// <summary>
        /// Gets or sets the impulse clamp.
        /// </summary>
        /// <value>
        /// The impulse clamp.
        /// </value>
        public float ImpulseClamp
        {
            get { return InternalPoint2PointConstraint.Setting.ImpulseClamp; }
            set { InternalPoint2PointConstraint.Setting.ImpulseClamp = value; }
        }

        /// <summary>
        /// Gets or sets the tau.
        /// </summary>
        /// <value>
        /// The tau.
        /// </value>
        public float Tau
        {
            get { return InternalPoint2PointConstraint.Setting.Tau; }
            set { InternalPoint2PointConstraint.Setting.Tau = value; }
        }

        internal BulletSharp.Point2PointConstraint InternalPoint2PointConstraint;
    }
}
