﻿// Copyright (c) 2014-2015 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Physics
{
    /// <summary>
    ///     Generic contact between colliders, Always using Vector3 as the engine allows mixed 2D/3D contacts.
    ///     Note: As class because it is shared between the 2 Colliders.. maybe struct is faster?
    /// </summary>
    public class Contact
    {
        #region Constants and Fields

        public Collider ColliderA;

        public Collider ColliderB;

        public float Distance;

        public Vector3 Normal;

        public Vector3 PositionOnA;

        public Vector3 PositionOnB;

        #endregion
    }

    public struct CollisionArgs
    {
        #region Constants and Fields

        public Contact Contact;

        #endregion
    }
}