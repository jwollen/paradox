﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.IO;
using System.Runtime.Serialization;
using System.Xml;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;

namespace SiliconStudio.BuildEngine
{
    public class ParadoxXmlObjectSerializer : XmlObjectSerializer
    {
        public override void WriteObject(XmlDictionaryWriter writer, object graph)
        {
            var data = EncodeObject(graph);
            writer.WriteStartElement("Data");
            writer.WriteBase64(data, 0, data.Length);
            writer.WriteEndElement();
        }

        public override void WriteStartObject(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteObjectContent(XmlDictionaryWriter writer, object graph)
        {
            throw new NotImplementedException();
        }

        public override void WriteEndObject(XmlDictionaryWriter writer)
        {
            throw new NotImplementedException();
        }

        public override object ReadObject(XmlDictionaryReader reader, bool verifyObjectName)
        {
            reader.ReadStartElement("Data");
            var data = reader.ReadContentAsBase64();
            reader.ReadEndElement();
            return DecodeObject(data);
        }

        public override bool IsStartObject(XmlDictionaryReader reader)
        {
            return reader.IsStartElement("Data");
        }

        private static byte[] EncodeObject(object obj)
        {
            var memoryStream = new MemoryStream();
            var writer = new BinarySerializationWriter(memoryStream);
            writer.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
            writer.Context.Set(ContentSerializerContext.SerializeAttachedReferenceProperty, ContentSerializerContext.AttachedReferenceSerialization.AsSerializableVersion);
            writer.SerializeExtended(ref obj, ArchiveMode.Serialize);

            return memoryStream.ToArray();
        }

        private static object DecodeObject(byte[] serializedObject)
        {
            var reader = new BinarySerializationReader(new MemoryStream(serializedObject));
            reader.Context.SerializerSelector = SerializerSelector.AssetWithReuse;
            reader.Context.Set(ContentSerializerContext.SerializeAttachedReferenceProperty, ContentSerializerContext.AttachedReferenceSerialization.AsSerializableVersion);
            object command = null;
            reader.SerializeExtended(ref command, ArchiveMode.Deserialize, null);
            return command;
        }
    }
}