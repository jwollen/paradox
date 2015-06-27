﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

using SharpYaml.Serialization;
using SharpYaml.Serialization.Serializers;

using SiliconStudio.Assets.Serializers;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Core.Yaml;
using SiliconStudio.Paradox.Rendering.Materials;
using SiliconStudio.Paradox.Rendering.Materials.ComputeColors;

using ITypeDescriptor = SharpYaml.Serialization.ITypeDescriptor;

namespace SiliconStudio.Paradox.Assets.Serializers
{
    [YamlSerializerFactory]
    internal class GenericDictionarySerializer : DictionarySerializer, IDataCustomVisitor
    {
        public override IYamlSerializable TryCreate(SerializerContext context, ITypeDescriptor typeDescriptor)
        {
            var type = typeDescriptor.Type;
            return CanVisit(type) ? this : null;
        }

        protected override void WriteDictionaryItems(ref ObjectContext objectContext)
        {
            //TODO: make SortKeyForMapping accessible in object context since it modifies the behavior of the serializer for children of the ComputeColorParameters
            var savedSettings = objectContext.Settings.SortKeyForMapping;
            objectContext.Settings.SortKeyForMapping = false;
            base.WriteDictionaryItems(ref objectContext);
            objectContext.Settings.SortKeyForMapping = savedSettings;
        }

        public bool CanVisit(Type type)
        {
            return typeof(ComputeColorParameters).IsAssignableFrom(type);
        }

        public void Visit(ref VisitorContext context)
        {
            // Visit a ComputeColorParameters without visiting properties
            context.Visitor.VisitObject(context.Instance, context.Descriptor, false);
        }
    }
}