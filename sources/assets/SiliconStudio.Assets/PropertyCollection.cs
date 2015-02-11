﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using SiliconStudio.Core;

namespace SiliconStudio.Assets
{
    /// <summary>
    /// A collection of properties.
    /// </summary>
    [DataContract("PropertyCollection")]
    public sealed class PropertyCollection : ConcurrentDictionary<PropertyKey, object>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyCollection"/> class.
        /// </summary>
        public PropertyCollection()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyCollection"/> class.
        /// </summary>
        /// <param name="dictionary">The dictionary.</param>
        public PropertyCollection(IEnumerable<KeyValuePair<PropertyKey, object>> dictionary)
            : base(dictionary)
        {
        }

        /// <summary>
        /// Gets a value for the specified key, null if not found.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>A value for the specified key, null if not found.</returns>
        public object Get(PropertyKey key)
        {
            object value;
            TryGetValue(key, out value);
            return value;
        }

        /// <summary>
        /// Gets a value for the specified key, null if not found.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">The key.</param>
        /// <returns>a value for the specified key, null if not found.</returns>
        public T Get<T>(PropertyKey<T> key)
        {
            var value = Get((PropertyKey)key);
            return value == null ? default(T) : (T)value;
        }

        /// <summary>
        /// Gets a value for the specified key, null if not found.
        /// </summary>
        /// <typeparam name="T">Type of the value</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        /// <returns>
        /// A value for the specified key, null if not found.
        /// </returns>
        public bool TryGet<T>(PropertyKey<T> key, out T value)
        {
            object valueObject;
            var result = TryGetValue((PropertyKey)key, out valueObject);
            value = valueObject == null ? default(T) : (T)valueObject;
            return result;
        }

        /// <summary>
        /// Sets a value for the specified key.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set(PropertyKey key, object value)
        {
            this[key] = value;
        }

        /// <summary>
        /// Sets a value for the specified key.
        /// </summary>
        /// <typeparam name="T">Type of the value.</typeparam>
        /// <param name="key">The key.</param>
        /// <param name="value">The value.</param>
        public void Set<T>(PropertyKey<T> key, T value)
        {
            this[key] = value;
        }

        /// <summary>
        /// Copies this properties to a output dictionary.
        /// </summary>
        /// <param name="properties">The dictionary to receive a copy of the properties of this instance.</param>
        /// <param name="overrideValues">if set to <c>true</c> [override values].</param>
        /// <exception cref="System.ArgumentNullException">properties</exception>
        public void CopyTo(IDictionary<PropertyKey, object> properties, bool overrideValues)
        {
            if (properties == null) throw new ArgumentNullException("properties");
            foreach (var propKeyValue in this)
            {
                if (!overrideValues && properties.ContainsKey(propKeyValue.Key))
                {
                    continue;
                }
                properties[propKeyValue.Key] = propKeyValue.Value;
            }
        }
    }
}