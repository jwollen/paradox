﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Core.Serialization.Contents
{
    /// <summary>
    /// Allows customization of IContentSerializer through an attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface, AllowMultiple = true, Inherited = false)]
    public class ContentSerializerAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ContentSerializerAttribute"/> class.
        /// </summary>
        /// <param name="contentSerializerType">Type of the content serializer.</param>
        public ContentSerializerAttribute(Type contentSerializerType)
        {
            ContentSerializerType = contentSerializerType;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ContentSerializerAttribute"/> class.
        /// </summary>
        public ContentSerializerAttribute()
        {
        }

        /// <summary>
        /// Gets the type of the content serializer.
        /// </summary>
        /// <value>
        /// The type of the content serializer.
        /// </value>
        public Type ContentSerializerType { get; private set; }
    }

    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true)]
    public class ContentSerializerExtensionAttribute : Attribute
    {
        public ContentSerializerExtensionAttribute(string supportedExtension)
        {
            SupportedExtension = supportedExtension;
        }

        public string SupportedExtension { get; private set; }
    }
}