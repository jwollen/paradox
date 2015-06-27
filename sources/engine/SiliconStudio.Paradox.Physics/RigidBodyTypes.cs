﻿// Copyright (c) 2014-2015 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

namespace SiliconStudio.Paradox.Physics
{
    public enum RigidBodyTypes
    {
        /// <summary>
        ///     They are supposed to never move, they are not automatically updated by the engine.
        ///     They can be moved tho by an explicit call to UpdateTransformation(), results are not realist for dynamic simulation
        ///     so use it wisely.
        ///     If you plan to move the entity it is advised to use Kinematic, which allows the normal dynamic simulation.
        /// </summary>
        Static,

        /// <summary>
        ///     The Physics engine is the authority for this kind of rigidbody, you should move them using forces and/or impulses,
        ///     never directly editing the Transformation
        /// </summary>
        Dynamic,

        /// <summary>
        ///     You can move this kind of rigidbody around and the physics engine will interpolate and perform dynamic interactions
        ///     with dynamic bodies
        ///     Notice that there is no dynamic interaction with static and other kinematic bodies
        /// </summary>
        Kinematic
    }
}