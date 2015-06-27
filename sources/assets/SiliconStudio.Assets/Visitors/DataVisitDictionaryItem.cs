﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Assets.Visitors
{
    /// <summary>
    /// Defines a dictionary item (key-value).
    /// </summary>
    public sealed class DataVisitDictionaryItem : DataVisitNode
    {
        private readonly object key;

        private readonly ITypeDescriptor keyDescriptor;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataVisitDictionaryItem"/> class.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <param name="keyDescriptor">The key descriptor.</param>
        /// <param name="value">The value.</param>
        /// <param name="valueDescriptor">The value descriptor.</param>
        public DataVisitDictionaryItem(object key, ITypeDescriptor keyDescriptor, object value, ITypeDescriptor valueDescriptor) : base(value, valueDescriptor)
        {
            if (keyDescriptor == null) throw new ArgumentNullException("keyDescriptor");
            this.key = key;
            this.keyDescriptor = keyDescriptor;
        }

        /// <summary>
        /// Gets the dictionary.
        /// </summary>
        /// <value>The dictionary.</value>
        public IDictionary Dictionary
        {
            get
            {
                return (IDictionary)(Parent != null ? Parent.Instance : null);
            }
        }

        /// <summary>
        /// Gets the descriptor.
        /// </summary>
        /// <value>The descriptor.</value>
        public DictionaryDescriptor Descriptor
        {
            get
            {
                return (DictionaryDescriptor)(Parent != null ? Parent.InstanceDescriptor : null);
            }
        }

        /// <summary>
        /// Gets the key.
        /// </summary>
        /// <value>The key.</value>
        public object Key
        {
            get
            {
                return key;
            }
        }

        /// <summary>
        /// Gets the key descriptor.
        /// </summary>
        /// <value>The key descriptor.</value>
        public ITypeDescriptor KeyDescriptor
        {
            get
            {
                return keyDescriptor;
            }
        }

        /// <summary>
        /// Gets the value descriptor.
        /// </summary>
        /// <value>The value descriptor.</value>
        public ITypeDescriptor ValueDescriptor
        {
            get
            {
                return InstanceDescriptor;
            }
        }

        public override string ToString()
        {
            return string.Format("{0} => {1}", key, Instance ?? "null");
        }

        public override DataVisitNode CreateWithEmptyInstance()
        {
            return new DataVisitDictionaryItem(key, KeyDescriptor, null, ValueDescriptor);
        }
    }
}