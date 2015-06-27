﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Runtime.CompilerServices;
using SiliconStudio.Core.Storage;

namespace SiliconStudio.Core.Serialization
{
    /// <summary>
    /// Describes how to serialize and deserialize an object without knowing its type.
    /// Used as a common base class for all data serializers.
    /// </summary>
    public abstract class DataSerializer
    {
        // Binary format version, needs to be bumped in case of big changes in serialization formats (i.e. primitive types).
        public const int BinaryFormatVersion = 5;

        public ObjectId SerializationTypeId;

        /// <inheritdoc/>
        public abstract Type SerializationType { get; }

        /// <inheritdoc/>
        public abstract bool IsBlittable { get; }

        /// <inheritdoc/>
        public abstract void Serialize(ref object obj, ArchiveMode mode, SerializationStream stream);

        /// <inheritdoc/>
        public abstract void PreSerialize(ref object obj, ArchiveMode mode, SerializationStream stream);
    }

    /// <summary>
    /// Describes how to serialize and deserialize an object of a given type.
    /// </summary>
    /// <typeparam name="T">The type of object to serialize or deserialize.</typeparam>
    public abstract class DataSerializer<T> : DataSerializer
    {
        /// <inheritdoc/>
        public override Type SerializationType { get { return typeof(T); } }

        /// <inheritdoc/>
        public override bool IsBlittable { get { return false; } }

        /// <inheritdoc/>
        public override void Serialize(ref object obj, ArchiveMode mode, SerializationStream stream)
        {
            var objT = (obj == null ? default(T) : (T)obj);
            Serialize(ref objT, mode, stream);
            obj = objT;
        }

        /// <inheritdoc/>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Serialize(T obj, SerializationStream stream)
        {
            Serialize(ref obj, ArchiveMode.Serialize, stream);
        }

        /// <inheritdoc/>
        public override void PreSerialize(ref object obj, ArchiveMode mode, SerializationStream stream)
        {
            var objT = (obj == null ? default(T) : (T)obj);
            PreSerialize(ref objT, mode, stream);
            obj = objT;
        }

        /// <inheritdoc/>
        public virtual void PreSerialize(ref T obj, ArchiveMode mode, SerializationStream stream)
        {
        }

        /// <inheritdoc/>
        public abstract void Serialize(ref T obj, ArchiveMode mode, SerializationStream stream);
    }
}