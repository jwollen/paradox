// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;

namespace SiliconStudio.Paradox.Engine.Design
{
    public class CloneEntityComponentSerializer<T> : DataSerializer<T> where T : EntityComponent, new()
    {
        public override void PreSerialize(ref T entityComponent, ArchiveMode mode, SerializationStream stream)
        {
            if (entityComponent == null)
                entityComponent = new T();
        }

        public override void Serialize(ref T entityComponent, ArchiveMode mode, SerializationStream stream)
        {
            if (mode == ArchiveMode.Serialize)
            {
                stream.Write(entityComponent.Entity);
                stream.Write(CloneEntityComponentData.GenerateEntityComponentData(entityComponent));
            }
            else if (mode == ArchiveMode.Deserialize)
            {
                var entity = stream.Read<Entity>();

                var data = stream.Read<CloneEntityComponentData>();
                CloneEntityComponentData.RestoreEntityComponentData(entityComponent, data);
            }
        }
    }

    [DataContract]
    internal class CloneEntityComponentData
    {
        // Used to store entity data while in merge/text mode
        public static PropertyKey<CloneEntityComponentData> Key = new PropertyKey<CloneEntityComponentData>("Key", typeof(CloneEntityComponentData));

        [DataMemberCustomSerializer]
        public Entity Entity;
        public List<EntityComponentProperty> Properties;
        //public List<EntityComponentProperty> Properties;

        public static void RestoreEntityComponentData(EntityComponent entityComponent, CloneEntityComponentData data)
        {
            foreach (var componentProperty in data.Properties)
            {
                switch (componentProperty.Type)
                {
                    case EntityComponentPropertyType.Field:
                        {
                            var field = entityComponent.GetType().GetTypeInfo().GetDeclaredField(componentProperty.Name);
                            if (field == null) // Field disappeared? should we issue a warning?
                                continue;
                            var result = MergeObject(field.GetValue(entityComponent), componentProperty.Value);
                            field.SetValue(entityComponent, result);
                        }
                        break;
                    case EntityComponentPropertyType.Property:
                        {
                            var property = entityComponent.GetType().GetTypeInfo().GetDeclaredProperty(componentProperty.Name);
                            if (property == null) // Property disappeared? should we issue a warning?
                                continue;
                            var result = MergeObject(property.GetValue(entityComponent, null), componentProperty.Value);
                            if (property.CanWrite)
                                property.SetValue(entityComponent, result, null);
                        }
                        break;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public static CloneEntityComponentData GenerateEntityComponentData(EntityComponent entityComponent)
        {
            var data = new CloneEntityComponentData { Properties = new List<EntityComponentProperty>() };
            foreach (var field in entityComponent.GetType().GetTypeInfo().DeclaredFields)
            {
                //if (!field.GetCustomAttributes(typeof(DataMemberConvertAttribute), true).Any())
                //    continue;

                data.Properties.Add(new EntityComponentProperty(EntityComponentPropertyType.Field, field.Name, field.GetValue(entityComponent)));
            }

            foreach (var property in entityComponent.GetType().GetTypeInfo().DeclaredProperties)
            {
                //if (!property.GetCustomAttributes(typeof(DataMemberConvertAttribute), true).Any())
                //    continue;

                data.Properties.Add(new EntityComponentProperty(EntityComponentPropertyType.Property, property.Name, property.GetValue(entityComponent, null)));
            }
            return data;
        }

        private static object MergeObject(object oldValue, object newValue)
        {
            if (oldValue is IList)
            {
                var oldList = (IList)oldValue;
                oldList.Clear();
                foreach (var item in (IEnumerable)newValue)
                {
                    oldList.Add(item);
                }
                return oldList;
            }
            if (oldValue is IDictionary)
            {
                var oldDictionary = (IDictionary)oldValue;
                oldDictionary.Clear();
                foreach (DictionaryEntry item in (IDictionary)newValue)
                {
                    oldDictionary.Add(item.Key, item.Value);
                }
                return oldDictionary;
            }

            return newValue;
        }
    }
}