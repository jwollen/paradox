﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Reflection;

namespace SiliconStudio.Core.Reflection
{
    /// <summary>
    /// A <see cref="IMemberDescriptor"/> for a <see cref="FieldInfo"/>
    /// </summary>
    public class FieldDescriptor : MemberDescriptorBase
    {
        private readonly FieldInfo fieldInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="FieldDescriptor"/> class.
        /// </summary>
        /// <param name="fieldInfo">The property information.</param>
        public FieldDescriptor(ITypeDescriptorFactory factory, FieldInfo fieldInfo)
            : base(factory, fieldInfo)
        {
            if (fieldInfo == null) throw new ArgumentNullException("fieldInfo");

            this.fieldInfo = fieldInfo;
            this.TypeDescriptor = Factory.Find(fieldInfo.FieldType);
        }

        /// <summary>
        /// Gets the property information attached to this instance.
        /// </summary>
        /// <value>The property information.</value>
        public FieldInfo FieldInfo
        {
            get
            {
                return fieldInfo;
            }
        }

        public override Type Type
        {
            get
            {
                return fieldInfo.FieldType;
            }
        }

        public override object Get(object thisObject)
        {
            return fieldInfo.GetValue(thisObject);
        }

        public override void Set(object thisObject, object value)
        {
            fieldInfo.SetValue(thisObject, value);
        }

        public override IEnumerable<T> GetCustomAttributes<T>(bool inherit)
        {
            return fieldInfo.GetCustomAttributes<T>(inherit);
        }

        public override bool HasSet
        {
            get
            {
                return true;
            }
        }

        /// <summary>
        /// Returns a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A <see cref="System.String" /> that represents this instance.</returns>
        public override string ToString()
        {
            return string.Format("Field [{0}] from Type [{1}]", Name, FieldInfo.DeclaringType != null ? FieldInfo.DeclaringType.FullName : string.Empty);
        }
    }
}