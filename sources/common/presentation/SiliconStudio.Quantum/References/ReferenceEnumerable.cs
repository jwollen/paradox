﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Quantum.References
{
    /// <summary>
    /// A class representing an enumeration of references to multiple objects.
    /// </summary>
    public sealed class ReferenceEnumerable : IReference, IEnumerable<ObjectReference>
    {
        private readonly List<ObjectReference> references = new List<ObjectReference>();
        private readonly List<object> indices = new List<object>();
        private readonly Type elementType;

        internal ReferenceEnumerable(IEnumerable enumerable, Type enumerableType, object index)
        {
            Reference.CheckReferenceCreationSafeGuard();
            Type = enumerableType;
            Index = index;

            if (enumerableType.HasInterface(typeof(IDictionary<,>)))
                elementType = enumerableType.GetInterface(typeof(IDictionary<,>)).GetGenericArguments()[1];
            else if (enumerableType.HasInterface(typeof(IEnumerable<>)))
                elementType = enumerableType.GetInterface(typeof(IEnumerable<>)).GetGenericArguments()[0];
            else
                elementType = typeof(object);
        }

        /// <inheritdoc/>
        public object ObjectValue { get; private set; }

        /// <inheritdoc/>
        public Type Type { get; private set; }

        /// <inheritdoc/>
        public object Index { get; private set; }

        /// <summary>
        /// Gets whether this reference enumerates a dictionary collection.
        /// </summary>
        public bool IsDictionary { get { return ObjectValue is IDictionary || ObjectValue.GetType().HasInterface(typeof(IDictionary<,>)); } }

        /// <inheritdoc/>
        public int Count { get { return references.Count; } }

        /// <inheritdoc/>
        public ObjectReference this[object index] { get { return references.Single(x => Equals(x.Index, index)); } }

        /// <inheritdoc/>
        public void Clear()
        {
            references.Clear();
        }

        /// <summary>
        /// Indicates whether this instance of <see cref="ReferenceEnumerable"/> contains an element which as the given index.
        /// </summary>
        /// <param name="index">The index to look for.</param>
        /// <returns><c>true</c> if an object with the given index exists in this instance, <c>false</c> otherwise.</returns>
        public bool ContainsIndex(object index)
        {
            return references.Any(x => Equals(x.Index, index));
        }

        /// <inheritdoc/>
        public void Refresh(object newObjectValue)
        {
            if (!(newObjectValue is IEnumerable)) throw new ArgumentException(@"The object is not an IEnumerable", "newObjectValue");

            ObjectValue = newObjectValue;

            references.Clear();
            references.AddRange(
                IsDictionary
                    ? ((IEnumerable)ObjectValue).Cast<object>().Select(x => (ObjectReference)Reference.CreateReference(GetValue(x), elementType, GetKey(x)))
                    : ((IEnumerable)ObjectValue).Cast<object>().Select((x, i) => (ObjectReference)Reference.CreateReference(x, elementType, i)));
            indices.Clear();
            foreach (var reference in references)
            {
                indices.Add(reference.Index);
            }
        }

        /// <inheritdoc/>
        public IEnumerator<ObjectReference> GetEnumerator()
        {
            return references.GetEnumerator();
        }

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return references.GetEnumerator();
        }

        /// <inheritdoc/>
        public bool Equals(IReference other)
        {
            var otherEnumerable = other as ReferenceEnumerable;
            return otherEnumerable != null && DesignExtensions.Equals<IReference>(references, otherEnumerable.references);
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            string text = "(" + references.Count + " references";
            if (references.Count > 0)
            {
                text += ": ";
                text += string.Join(", ", references);
            }
            text += ")";
            return text;
        }

        private static object GetKey(object keyValuePair)
        {
            var type = keyValuePair.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>)) throw new ArgumentException("The given object is not a KeyValuePair.");
            var keyProperty = type.GetProperty("Key");
            return keyProperty.GetValue(keyValuePair);
        }

        private static object GetValue(object keyValuePair)
        {
            var type = keyValuePair.GetType();
            if (!type.IsGenericType || type.GetGenericTypeDefinition() != typeof(KeyValuePair<,>)) throw new ArgumentException("The given object is not a KeyValuePair.");
            var valueProperty = type.GetProperty("Value");
            return valueProperty.GetValue(keyValuePair);
        }
    }
}
