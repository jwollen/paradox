﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Assets.Visitors
{
    /// <summary>
    /// Defines an item in a list.
    /// </summary>
    public sealed class DataVisitListItem : DataVisitNode
    {
        private readonly int index;

        /// <summary>
        /// Initializes a new instance of the <see cref="DataVisitListItem"/> class.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <param name="item">The item.</param>
        /// <param name="itemDescriptor">The item descriptor.</param>
        /// <exception cref="System.ArgumentNullException">
        /// list
        /// or
        /// descriptor
        /// or
        /// itemDescriptor
        /// </exception>
        public DataVisitListItem(int index, object item, ITypeDescriptor itemDescriptor) : base(item, itemDescriptor)
        {
            this.index = index;
        }

        public IList List
        {
            get
            {
                return (IList)(Parent != null ? Parent.Instance : null);
            }
        }

        public CollectionDescriptor Descriptor
        {
            get
            {
                return (CollectionDescriptor)(Parent != null ? Parent.InstanceDescriptor : null);
            }
        }

        public int Index
        {
            get
            {
                return index;
            }
        }

        public override string ToString()
        {
            return string.Format("[{0}] = {1}", index, Instance ?? "null");
        }

        public override DataVisitNode CreateWithEmptyInstance()
        {
            return new DataVisitListItem(index, null, InstanceDescriptor);
        }
    }
}