﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Reflection;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.Core.Serialization.Assets
{
    public class AssetSerializer
    {
        private readonly Dictionary<Type, List<IContentSerializer>> contentSerializers = new Dictionary<Type, List<IContentSerializer>>();

        public SerializerSelector LowLevelSerializerSelector { get; set; }
        public SerializerSelector LowLevelSerializerSelectorWithReuse { get; set; }

        public PropertyContainer SerializerContextTags;

        public AssetSerializer()
        {
            LowLevelSerializerSelector = SerializerSelector.Asset;
            LowLevelSerializerSelectorWithReuse = SerializerSelector.AssetWithReuse;
        }

        /// <summary>
        /// Registers a serializer with this AssetSerializer.
        /// </summary>
        /// <param name="serializer">The serializer.</param>
        public void RegisterSerializer(IContentSerializer serializer)
        {
            lock (contentSerializers)
            {
                var serializers1 = GetSerializers(serializer.SerializationType);
                var serializers2 = GetSerializers(serializer.ActualType);
                serializers1.Insert(0, serializer);
                serializers2.Insert(0, serializer);
            }
        }

        internal List<IContentSerializer> GetSerializers(Type objectType)
        {
            lock (contentSerializers)
            {
                List<IContentSerializer> contentSerializersForT;

                if (!contentSerializers.TryGetValue(objectType, out contentSerializersForT))
                {
                    contentSerializers[objectType] = contentSerializersForT = new List<IContentSerializer>();

                    // If type has a ContentSerializerAttribute, use it.
                    foreach (var contentSerializerAttribute in objectType.GetTypeInfo().GetCustomAttributes<ContentSerializerAttribute>(true))
                    {
                        if (contentSerializerAttribute.ContentSerializerType == null)
                            continue;

                        var contentSerializer = (IContentSerializer)Activator.CreateInstance(contentSerializerAttribute.ContentSerializerType);
                        contentSerializersForT.Add(contentSerializer);
                    }
                }

                return contentSerializersForT;
            }
        }

        internal IContentSerializer GetSerializer(Type storageType, Type objectType)
        {
            lock (contentSerializers)
            {
                // Process serializer attributes of objectType
                foreach (var contentSerializer in GetSerializers(objectType))
                {
                    if (objectType.GetTypeInfo().IsAssignableFrom(contentSerializer.ActualType.GetTypeInfo()) && (storageType == null || contentSerializer.SerializationType == storageType))
                        return contentSerializer;
                }

                // Process serializer attributes of storageType
                if (storageType != null)
                {
                    foreach (var contentSerializer in GetSerializers(storageType))
                    {
                        if (objectType.GetTypeInfo().IsAssignableFrom(contentSerializer.ActualType.GetTypeInfo()) && contentSerializer.SerializationType == storageType)
                            return contentSerializer;
                    }
                }

                //foreach (var contentSerializerGroup in contentSerializers)
                //{
                //    if (contentSerializerGroup.Key.GetTypeInfo().IsAssignableFrom(objectType.GetTypeInfo()))
                //    {
                //        return GetSerializer(contentSerializerGroup.Value, storageType);
                //    }
                //}
            }

            throw new Exception(string.Format("Could not find a serializer for the type [{0}, {1}]", storageType == null ? null : storageType.Name, objectType == null ? null : objectType.Name));
        }

        private static IContentSerializer GetSerializer(List<IContentSerializer> serializers, Type storageType)
        {
            foreach (var contentSerializer in serializers)
            {
                if ((storageType == null || contentSerializer.SerializationType == storageType))
                    return contentSerializer;
            }
            return null;
        }
    }
}
