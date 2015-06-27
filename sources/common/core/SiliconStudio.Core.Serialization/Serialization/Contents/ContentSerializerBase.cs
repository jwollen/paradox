﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SiliconStudio.Core.Serialization.Contents
{
    /// <summary>
    /// Base class for Content Serializer with empty virtual implementation.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class ContentSerializerBase<T> : IContentSerializer<T>
    {
        static readonly bool hasParameterlessConstructor = typeof(T).GetTypeInfo().DeclaredConstructors.Any(x => !x.IsStatic && x.IsPublic && !x.GetParameters().Any());

        /// <inheritdoc/>
        public virtual Type SerializationType
        {
            get { return typeof(T); }
        }

        /// <inheritdoc/>
        public virtual Type ActualType
        {
            get { return typeof(T); }
        }
        
        /// <inheritdoc/>
        public virtual object Construct(ContentSerializerContext context)
        {
            return hasParameterlessConstructor ? Activator.CreateInstance<T>() : default(T);
        }

        /// <inheritdoc/>
        public virtual void Serialize(ContentSerializerContext context, SerializationStream stream, T obj)
        {
        }

        /// <inheritdoc/>
        public void Serialize(ContentSerializerContext context, SerializationStream stream, object obj)
        {
            var objT = (T)obj;
            Serialize(context, stream, objT);
        }
    }
}