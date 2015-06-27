// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Specialized;
using System.ComponentModel;
using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;

namespace SiliconStudio.Paradox.Engine
{
    /// <summary>
    /// Defines Position, Rotation and Scale of its <see cref="Entity"/>.
    /// </summary>
    [DataContract("TransformComponent")]
    [DataSerializerGlobal(null, typeof(TrackingCollection<TransformComponent>))]
    [Display(10, "Transform")]
    public sealed class TransformComponent : EntityComponent //, IEnumerable<TransformComponent> Check why this is not working
    {
        public readonly static PropertyKey<TransformComponent> Key = new PropertyKey<TransformComponent>("Key", typeof(TransformComponent),
            new AccessorMetadata((ref PropertyContainer props) => ((Entity)props.Owner).Transform, (ref PropertyContainer props, object value) => ((Entity)props.Owner).Transform = (TransformComponent)value));

        // When false, transformation should be computed in TransformProcessor (no dependencies).
        // When true, transformation is computed later by another system.
        // This is useful for scenario such as binding a node to a bone, where it first need to run TransformProcessor for the hierarchy,
        // run MeshProcessor to update ModelViewHierarchy, copy Node/Bone transformation to another Entity with special root and then update its children transformations.
        internal bool isSpecialRoot = false;
        private bool useTRS = true;
        private TransformComponent parent;

        /// <summary>
        /// The world matrix.
        /// Use <see cref="UpdateWorldMatrix"/> to ensure it is updated.
        /// </summary>
        [DataMemberIgnore]
        public Matrix WorldMatrix = Matrix.Identity;

        /// <summary>
        /// The local matrix.
        /// Use <see cref="UpdateLocalMatrix"/> to ensure it is updated.
        /// </summary>
        [DataMemberIgnore]
        public Matrix LocalMatrix = Matrix.Identity;

        /// <summary>
        /// The translation relative to the parent transformation.
        /// </summary>
        [DataMember(10)]
        public Vector3 Position;

        /// <summary>
        /// The rotation relative to the parent transformation.
        /// </summary>
        [DataMember(20)]
        public Quaternion Rotation;

        /// <summary>
        /// The scaling relative to the parent transformation.
        /// </summary>
        [DataMember(30)]
        public Vector3 Scale;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransformComponent" /> class.
        /// </summary>
        public TransformComponent()
        {
            var children = new TrackingCollection<TransformComponent>();
            children.CollectionChanged += ChildrenCollectionChanged;

            Children = children;

            UseTRS = true;
            Scale = Vector3.One;
            Rotation = Quaternion.Identity;
        }

        /// <summary>
        /// Gets or sets a value indicating whether to use the Translation/Rotation/Scale.
        /// </summary>
        /// <value><c>true</c> if [use TRS]; otherwise, <c>false</c>.</value>
        [DataMemberIgnore]
        [Browsable(false)]
        [DefaultValue(true)]
        public bool UseTRS
        {
            get { return useTRS; }
            set { useTRS = value; }
        }
        
        /// <summary>
        /// Gets the children of this <see cref="TransformComponent"/>.
        /// </summary>
        /// <value>
        /// The children.
        /// </value>
        public FastCollection<TransformComponent> Children { get; private set; }

        /// <summary>
        /// Gets or sets the euler rotation, with XYZ order.
        /// Not stable: setting value and getting it again might return different value as it is internally encoded as a <see cref="Quaternion"/> in <see cref="Rotation"/>.
        /// </summary>
        /// <value>
        /// The euler rotation.
        /// </value>
        [DataMemberIgnore]
        public Vector3 RotationEulerXYZ
        {
            get
            {
                var rotation = Rotation;
                Vector3 rotationEuler;

                // Equivalent to:
                //  Matrix rotationMatrix;
                //  Matrix.Rotation(ref cachedRotation, out rotationMatrix);
                //  rotationMatrix.DecomposeXYZ(out rotationEuler);

                float xx = rotation.X * rotation.X;
                float yy = rotation.Y * rotation.Y;
                float zz = rotation.Z * rotation.Z;
                float xy = rotation.X * rotation.Y;
                float zw = rotation.Z * rotation.W;
                float zx = rotation.Z * rotation.X;
                float yw = rotation.Y * rotation.W;
                float yz = rotation.Y * rotation.Z;
                float xw = rotation.X * rotation.W;

                rotationEuler.Y = (float)Math.Asin(2.0f * (yw - zx));
                double test = Math.Cos(rotationEuler.Y);
                if (test > 1e-6f)
                {
                    rotationEuler.Z = (float)Math.Atan2(2.0f * (xy + zw), 1.0f - (2.0f * (yy + zz)));
                    rotationEuler.X = (float)Math.Atan2(2.0f * (yz + xw), 1.0f - (2.0f * (yy + xx)));
                }
                else
                {
                    rotationEuler.Z = (float)Math.Atan2(2.0f * (zw - xy), 2.0f * (zx + yw));
                    rotationEuler.X = 0.0f;
                }
                return rotationEuler;
            }
            set
            {
                // Equilvalent to:
                //  Quaternion quatX, quatY, quatZ;
                //  
                //  Quaternion.RotationX(value.X, out quatX);
                //  Quaternion.RotationY(value.Y, out quatY);
                //  Quaternion.RotationZ(value.Z, out quatZ);
                //  
                //  rotation = quatX * quatY * quatZ;

                var halfAngles = value * 0.5f;

                var fSinX = (float)Math.Sin(halfAngles.X);
                var fCosX = (float)Math.Cos(halfAngles.X);
                var fSinY = (float)Math.Sin(halfAngles.Y);
                var fCosY = (float)Math.Cos(halfAngles.Y);
                var fSinZ = (float)Math.Sin(halfAngles.Z);
                var fCosZ = (float)Math.Cos(halfAngles.Z);

                var fCosXY = fCosX * fCosY;
                var fSinXY = fSinX * fSinY;

                Rotation.X = fSinX * fCosY * fCosZ - fSinZ * fSinY * fCosX;
                Rotation.Y = fSinY * fCosX * fCosZ + fSinZ * fSinX * fCosY;
                Rotation.Z = fSinZ * fCosXY - fSinXY * fCosZ;
                Rotation.W = fCosZ * fCosXY + fSinXY * fSinZ;
            }
        }

        /// <summary>
        /// Gets or sets the parent of this <see cref="TransformComponent"/>.
        /// </summary>
        /// <value>
        /// The parent.
        /// </value>
        [DataMemberIgnore]
        public TransformComponent Parent
        {
            get { return parent; }
            set
            {
                var oldParent = Parent;
                if (oldParent == value)
                    return;

                if (oldParent != null)
                    oldParent.Children.Remove(this);
                if (value != null)
                    value.Children.Add(this);
            }
        }

        /// <summary>
        /// Updates the local matrix.
        /// If <see cref="UseTRS"/> is true, <see cref="LocalMatrix"/> will be updated from <see cref="Position"/>, <see cref="Rotation"/> and <see cref="Scale"/>.
        /// </summary>
        public void UpdateLocalMatrix()
        {
            if (UseTRS)
            {
                CreateMatrixTRS(ref Position, ref Rotation, ref Scale, out LocalMatrix);
            }
        }

        /// <summary>
        /// Updates the world matrix.
        /// It will first call <see cref="UpdateLocalMatrix"/> on self, and <see cref="UpdateWorldMatrix"/> on <see cref="Parent"/> if not null.
        /// Then <see cref="WorldMatrix"/> will be updated by multiplying <see cref="LocalMatrix"/> and parent <see cref="WorldMatrix"/> (if any).
        /// </summary>
        public void UpdateWorldMatrix()
        {
            UpdateLocalMatrix();

            if (Parent != null && !isSpecialRoot)
            {
                Parent.UpdateWorldMatrix();
                Matrix.Multiply(ref LocalMatrix, ref Parent.WorldMatrix, out WorldMatrix);
            }
            else
            {
                WorldMatrix = LocalMatrix;
            }
        }

        internal void UpdateWorldMatrixNonRecursive()
        {
            if (Parent != null && !isSpecialRoot)
            {
                Matrix.Multiply(ref LocalMatrix, ref Parent.WorldMatrix, out WorldMatrix);
            }
            else
            {
                WorldMatrix = LocalMatrix;
            }
        }

        private void AddItem(TransformComponent item)
        {
            if (item.Parent != null)
                throw new InvalidOperationException("This TransformComponent already has a Parent, detach it first.");

            item.parent = this;
        }

        private void RemoveItem(TransformComponent item)
        {
            if (item.Parent != this)
                throw new InvalidOperationException("This TransformComponent's parent is not the expected value.");

            item.parent = null;
        }

        private void ChildrenCollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    AddItem((TransformComponent)e.Item);
                    break;
                case NotifyCollectionChangedAction.Remove:
                    RemoveItem((TransformComponent)e.Item);
                    break;
                default:
                    throw new NotSupportedException();
            }
        }

        /// <summary>
        /// Creates a matrix that contains both the X, Y and Z rotation, as well as scaling and translation.
        /// </summary>
        /// <param name="translation">The translation.</param>
        /// <param name="rotation">Angle of rotation in radians. Angles are measured clockwise when looking along the rotation axis toward the origin.</param>
        /// <param name="scaling">The scaling.</param>
        /// <param name="result">When the method completes, contains the created rotation matrix.</param>
        public static void CreateMatrixTRS(ref Vector3 translation, ref Quaternion rotation, ref Vector3 scaling, out Matrix result)
        {
            // Equivalent to:
            //result =
            //    Matrix.Scaling(scaling)
            //    *Matrix.RotationX(rotation.X)
            //    *Matrix.RotationY(rotation.Y)
            //    *Matrix.RotationZ(rotation.Z)
            //    *Matrix.Position(translation);

            // Rotation
            float xx = rotation.X * rotation.X;
            float yy = rotation.Y * rotation.Y;
            float zz = rotation.Z * rotation.Z;
            float xy = rotation.X * rotation.Y;
            float zw = rotation.Z * rotation.W;
            float zx = rotation.Z * rotation.X;
            float yw = rotation.Y * rotation.W;
            float yz = rotation.Y * rotation.Z;
            float xw = rotation.X * rotation.W;

            result.M11 = 1.0f - (2.0f * (yy + zz));
            result.M12 = 2.0f * (xy + zw);
            result.M13 = 2.0f * (zx - yw);
            result.M21 = 2.0f * (xy - zw);
            result.M22 = 1.0f - (2.0f * (zz + xx));
            result.M23 = 2.0f * (yz + xw);
            result.M31 = 2.0f * (zx + yw);
            result.M32 = 2.0f * (yz - xw);
            result.M33 = 1.0f - (2.0f * (yy + xx));

            // Position
            result.M41 = translation.X;
            result.M42 = translation.Y;
            result.M43 = translation.Z;

            // Scale
            if (scaling.X != 1.0f)
            {
                result.M11 *= scaling.X;
                result.M12 *= scaling.X;
                result.M13 *= scaling.X;
            }
            if (scaling.Y != 1.0f)
            {
                result.M21 *= scaling.Y;
                result.M22 *= scaling.Y;
                result.M23 *= scaling.Y;
            }
            if (scaling.Z != 1.0f)
            {
                result.M31 *= scaling.Z;
                result.M32 *= scaling.Z;
                result.M33 *= scaling.Z;
            }

            result.M14 = 0.0f;
            result.M24 = 0.0f;
            result.M34 = 0.0f;
            result.M44 = 1.0f;
        }

        public override PropertyKey GetDefaultKey()
        {
            return Key;
        }

        //public IEnumerator<TransformComponent> GetEnumerator()
        //{
        //    return Children.GetEnumerator();
        //}

        //IEnumerator IEnumerable.GetEnumerator()
        //{
        //    return ((IEnumerable)Children).GetEnumerator();
        //}
    }
}