﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;

namespace SiliconStudio.Paradox.Graphics.Font
{
    /// <summary>
    /// Serializer for <see cref="DynamicSpriteFont"/>.
    /// </summary>
    internal class StaticSpriteFontSerializer : DataSerializer<StaticSpriteFont>, IDataSerializerInitializer, IDataSerializerGenericInstantiation
    {
        private DataSerializer<SpriteFont> parentSerializer;

        public override void PreSerialize(ref StaticSpriteFont texture, ArchiveMode mode, SerializationStream stream)
        {
            // Do not create object during pre-serialize (OK because not recursive)
        }

        public void Initialize(SerializerSelector serializerSelector)
        {
            parentSerializer = serializerSelector.GetSerializer<SpriteFont>();
            if (parentSerializer == null)
            {
                throw new InvalidOperationException(string.Format("Could not find parent serializer for type {0}", "SiliconStudio.Paradox.Graphics.SpriteFont"));
            }
        }

        public override void Serialize(ref StaticSpriteFont font, ArchiveMode mode, SerializationStream stream)
        {
            SpriteFont spriteFont = font;
            parentSerializer.Serialize(ref spriteFont, mode, stream);
            font = (StaticSpriteFont)spriteFont;

            if (mode == ArchiveMode.Deserialize)
            {
                var services = stream.Context.Tags.Get(ServiceRegistry.ServiceRegistryKey);
                var fontSystem = services.GetSafeServiceAs<FontSystem>();

                font.CharacterToGlyph = stream.Read<Dictionary<char, Glyph>>();
                font.StaticTextures = stream.Read<List<Texture>>();

                font.FontSystem = fontSystem;
            }
            else
            {
                stream.Write(font.CharacterToGlyph);
                stream.Write(font.StaticTextures);
            }
        }

        public void EnumerateGenericInstantiations(SerializerSelector serializerSelector, IList<Type> genericInstantiations)
        {
            genericInstantiations.Add(typeof(Dictionary<char, Glyph>));
            genericInstantiations.Add(typeof(List<Texture>));
        }
    }
}