﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Collections;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Core.Serialization.Serializers
{
    /// <summary>
    /// Data serializer for List{T}.
    /// </summary>
    /// <typeparam name="T">Generics type of List{T}.</typeparam>
    [DataSerializerGlobal(typeof(ListSerializer<>), typeof(List<>), DataSerializerGenericMode.GenericArguments)]
    public class ListSerializer<T> : DataSerializer<List<T>>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        public override void PreSerialize(ref List<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = new List<T>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref List<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                int count = stream.ReadInt32();
                obj.Capacity = count;
                for (int i = 0; i < count; ++i)
                {
                    T value = default(T);
                    itemDataSerializer.Serialize(ref value, mode, stream);
                    obj.Add(value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (T item in obj)
                {
                    itemDataSerializer.Serialize(item, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }
    }

    /// <summary>
    /// Data serializer for IList{T}.
    /// </summary>
    /// <typeparam name="T">Generics type of IList{T}.</typeparam>
    public class ListAllSerializer<TList, T> : DataSerializer<TList>, IDataSerializerInitializer, IDataSerializerGenericInstantiation where TList : class, IList<T>
    {
        private bool isInterface = typeof(TList).GetTypeInfo().IsInterface;
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        public override void PreSerialize(ref TList obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = isInterface ? (TList)(object)new List<T>() : Activator.CreateInstance<TList>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref TList obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                int count = stream.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    T value = default(T);
                    itemDataSerializer.Serialize(ref value, mode, stream);
                    obj.Add(value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (T item in obj)
                {
                    itemDataSerializer.Serialize(item, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }
    }


    /// <summary>
    /// Data serializer for SortedList{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in SortedList{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in SortedList{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(SortedListSerializer<,>), typeof(SiliconStudio.Core.Collections.SortedList<,>), DataSerializerGenericMode.GenericArguments)]
    public class SortedListSerializer<TKey, TValue> : DataSerializer<SiliconStudio.Core.Collections.SortedList<TKey, TValue>>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        public override void PreSerialize(ref SiliconStudio.Core.Collections.SortedList<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the SortedList size
                if (obj == null)
                    obj = new SiliconStudio.Core.Collections.SortedList<TKey, TValue>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref SiliconStudio.Core.Collections.SortedList<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                int count = stream.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    TKey key = default(TKey);
                    TValue value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }


    /// <summary>
    /// Data serializer for IList{T}.
    /// </summary>
    /// <typeparam name="T">Generics type of IList{T}.</typeparam>
    [DataSerializerGlobal(typeof(ListInterfaceSerializer<>), typeof(IList<>), DataSerializerGenericMode.GenericArguments)]
    public class ListInterfaceSerializer<T> : DataSerializer<IList<T>>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        public override void PreSerialize(ref IList<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                if (obj == null)
                    obj = new List<T>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref IList<T> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                int count = stream.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    T value = default(T);
                    itemDataSerializer.Serialize(ref value, mode, stream);
                    obj.Add(value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (T item in obj)
                {
                    itemDataSerializer.Serialize(item, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));

            // Force concrete type to be implemented (that's what will likely be used with this interface)
            genericInstantiations.Add(typeof(List<T>));
        }
    }

    /// <summary>
    /// Data serializer for T[].
    /// </summary>
    /// <typeparam name="T">Generics type of T[].</typeparam>
    public class ArraySerializer<T> : DataSerializer<T[]>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<T> itemDataSerializer;

        /// <inheritdoc/>
        public virtual void Initialize(SerializerSelector serializerSelector)
        {
            itemDataSerializer = MemberSerializer<T>.Create(serializerSelector);
        }

        public override void PreSerialize(ref T[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Length);
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                int length = stream.ReadInt32();
                obj = new T[length];
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref T[] obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                int count = obj.Length;
                for (int i = 0; i < count; ++i)
                {
                    itemDataSerializer.Serialize(ref obj[i], mode, stream);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                int count = obj.Length;
                for (int i = 0; i < count; ++i)
                {
                    itemDataSerializer.Serialize(ref obj[i], mode, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(T));
        }
    }

    /// <summary>
    /// Data serializer for blittable T[].
    /// </summary>
    /// <typeparam name="T">Generics type of T[].</typeparam>
    public class BlittableArraySerializer<T> : ArraySerializer<T>
    {
        private int elementSize;

        /// <inheritdoc/>
        public override void Initialize(SerializerSelector serializerSelector)
        {
            elementSize = Marshal.SizeOf(typeof(T));
        }

        /// <inheritdoc/>
        public unsafe override void Serialize(ref T[] obj, ArchiveMode mode, SerializationStream stream)
        {
            int size = obj.Length * elementSize;
            var objPinned = Interop.Fixed(obj);
            if (mode == ArchiveMode.Deserialize)
            {
                stream.NativeStream.Read((IntPtr)objPinned, size);
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.NativeStream.Write((IntPtr)objPinned, size);
            }
        }
    }

    /// <summary>
    /// Data serializer for KeyValuePair{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in KeyValuePair{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in KeyValuePair{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(KeyValuePairSerializer<,>), typeof(KeyValuePair<,>), DataSerializerGenericMode.GenericArguments)]
    public class KeyValuePairSerializer<TKey, TValue> : DataSerializer<KeyValuePair<TKey, TValue>>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        /// <inheritdoc/>
        public override void Serialize(ref KeyValuePair<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                TKey key = default(TKey);
                TValue value = default(TValue);
                keySerializer.Serialize(ref key, mode, stream);
                valueSerializer.Serialize(ref value, mode, stream);
                obj = new KeyValuePair<TKey, TValue>(key, value);
            }
            else if (mode == ArchiveMode.Serialize)
            {
                keySerializer.Serialize(obj.Key, stream);
                valueSerializer.Serialize(obj.Value, stream);
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }

    /// <summary>
    /// Data serializer for Dictionary{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in Dictionary{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in Dictionary{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(DictionarySerializer<,>), typeof(Dictionary<,>), DataSerializerGenericMode.GenericArguments)]
    public class DictionarySerializer<TKey, TValue> : DataSerializer<Dictionary<TKey, TValue>>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        public override void PreSerialize(ref Dictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the dictionary size
                if (obj == null)
                    obj = new Dictionary<TKey, TValue>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref Dictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                int count = stream.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    TKey key = default(TKey);
                    TValue value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }

    public class DictionaryAllSerializer<TDictionary, TKey, TValue> : DataSerializer<TDictionary>, IDataSerializerInitializer, IDataSerializerGenericInstantiation where TDictionary : IDictionary<TKey, TValue>
    {
        private bool isInterface = typeof(TDictionary).GetTypeInfo().IsInterface;
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        public override void PreSerialize(ref TDictionary obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the dictionary size
                if (obj == null)
                    obj = isInterface ? (TDictionary)(object)new Dictionary<TKey, TValue>() : Activator.CreateInstance<TDictionary>();
                else
                    obj.Clear();
            }
        }

        /// <inheritdoc/>
        public override void Serialize(ref TDictionary obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                int count = stream.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    TKey key = default(TKey);
                    TValue value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));
        }
    }

    /// <summary>
    /// Data serializer for IDictionary{TKey, TValue}.
    /// </summary>
    /// <typeparam name="TKey">The type of the key in IDictionary{TKey, TValue}.</typeparam>
    /// <typeparam name="TValue">The type of the value in IDictionary{TKey, TValue}.</typeparam>
    [DataSerializerGlobal(typeof(DictionaryInterfaceSerializer<,>), typeof(IDictionary<,>), DataSerializerGenericMode.GenericArguments)]
    public class DictionaryInterfaceSerializer<TKey, TValue> : DataSerializer<IDictionary<TKey, TValue>>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<TKey> keySerializer;
        private DataSerializer<TValue> valueSerializer;

        /// <inheritdoc/>
        public void Initialize(SerializerSelector serializerSelector)
        {
            // Key should never be null
            keySerializer = MemberSerializer<TKey>.Create(serializerSelector, false);
            valueSerializer = MemberSerializer<TValue>.Create(serializerSelector);
        }

        public override void PreSerialize(ref IDictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // TODO: Peek the dictionary size
                if (obj == null)
                    obj = new Dictionary<TKey, TValue>();
                else
                    obj.Clear();
            }
        }
        
        /// <inheritdoc/>
        public override void Serialize(ref IDictionary<TKey, TValue> obj, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Deserialize)
            {
                // Should be null if it was
                int count = stream.ReadInt32();
                for (int i = 0; i < count; ++i)
                {
                    TKey key = default(TKey);
                    TValue value = default(TValue);
                    keySerializer.Serialize(ref key, mode, stream);
                    valueSerializer.Serialize(ref value, mode, stream);
                    obj.Add(key, value);
                }
            }
            else if (mode == ArchiveMode.Serialize)
            {
                stream.Write(obj.Count);
                foreach (var item in obj)
                {
                    keySerializer.Serialize(item.Key, stream);
                    valueSerializer.Serialize(item.Value, stream);
                }
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(TKey));
            genericInstantiations.Add(typeof(TValue));

            // Force concrete type to be implemented (that's what will likely be used with this interface)
            genericInstantiations.Add(typeof(Dictionary<TKey, TValue>));
        }
    }
}
