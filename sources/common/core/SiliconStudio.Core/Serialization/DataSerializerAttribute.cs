﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Core.Serialization
{
    /// <summary>
    /// Use this attribute on a class to specify its data serializer type.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
    public class DataSerializerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataSerializerAttribute"/> class.
        /// </summary>
        /// <param name="dataSerializerType">Type of the data serializer.</param>
        public DataSerializerAttribute(Type dataSerializerType)
        {
            DataSerializerType = dataSerializerType;
        }

        /// <summary>
        /// Gets the type of the data serializer.
        /// </summary>
        /// <value>
        /// The type of the data serializer.
        /// </value>
        public Type DataSerializerType;

        public DataSerializerGenericMode Mode;
    }
}
