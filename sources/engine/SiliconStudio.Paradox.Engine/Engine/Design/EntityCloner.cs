﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.IO;
using System.Linq;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Paradox.Animations;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Engine.Design
{
    /// <summary>
    /// Provides method for deep cloning of en <see cref="Entity"/>.
    /// </summary>
    [DataSerializerGlobal(typeof(CloneSerializer<Effect>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<SpriteGroup>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<BlendState>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<RasterizerState>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<SamplerState>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<DepthStencilState>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(CloneSerializer<Texture>), Profile = "Clone")]
    [DataSerializerGlobal(typeof(ContentReferenceCloneDataSerializer<>), typeof(ContentReference<>), DataSerializerGenericMode.GenericArguments, Profile = "Clone")]
    class EntityCloner
    {
        private static CloneContext cloneContext = new CloneContext();
        private static SerializerSelector cloneSerializerSelector = null;
        internal static PropertyKey<CloneContext> CloneContextProperty = new PropertyKey<CloneContext>("CloneContext", typeof(EntityCloner));

        /// <summary>
        /// Clones the specified entity.
        /// <see cref="Entity"/>, children <see cref="Entity"/> and their <see cref="EntityComponent"/> will be cloned.
        /// Other assets will be shared.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        public static Entity Clone(Entity entity)
        {
            var clonedObjects = new HashSet<object>();

            // Registers objects that should be cloned (Entity and their EntityComponent)
            foreach (var currentEntity in ParameterContainerExtensions.CollectEntityTree(entity))
            {
                clonedObjects.Add(currentEntity);
                foreach (var component in currentEntity.Components.Where(x => x.Value is EntityComponent))
                {
                    clonedObjects.Add(component.Value);
                }
            } 
            
            return Clone(clonedObjects, null, entity);
        }

        /// <summary>
        /// Clones the specified object, taking special care of <see cref="Entity"/>, <see cref="EntityComponent"/> and external assets.
        /// User can optionally provides list of cloned objects (list of data reference objects that should be cloned)
        /// and mapped objects (list of data reference objects that should be ducplicated using the given instance).
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="clonedObjects">The cloned objects.</param>
        /// <param name="mappedObjects">The mapped objects.</param>
        /// <param name="entity">The entity.</param>
        /// <returns></returns>
        private static T Clone<T>(HashSet<object> clonedObjects, TryGetValueFunction<object, object> mappedObjects, T entity) where T : class
        {
            if (cloneSerializerSelector == null)
            {
                cloneSerializerSelector = new SerializerSelector();
                cloneSerializerSelector.ReuseReferences = true;

                cloneSerializerSelector
                    .RegisterProfile("Default")
                    .RegisterProfile("Clone")
                    .RegisterSerializer(new CloneSerializer<string>())
                    .RegisterSerializer(new CloneSerializer<Effect>())
                    .RegisterSerializer(new CloneSerializer<Mesh>())
                    .RegisterSerializer(new CloneSerializer<Model>())
                    .RegisterSerializer(new CloneSerializer<AnimationClip>());
            }

            // Initialize CloneContext
            lock (cloneContext)
            {
                try
                {
                    cloneContext.EntitySerializerSelector = cloneSerializerSelector;

                    cloneContext.ClonedObjects = clonedObjects;
                    cloneContext.MappedObjects = mappedObjects;

                    // Serialize
                    var memoryStream = cloneContext.MemoryStream;
                    var writer = new BinarySerializationWriter(memoryStream);
                    writer.Context.SerializerSelector = cloneSerializerSelector;
                    writer.Context.Set(CloneContextProperty, cloneContext);
                    writer.SerializeExtended(entity, ArchiveMode.Serialize, null);

                    // Deserialization reuses this list and expect it to be empty at the beginning.
                    cloneContext.SerializedObjects.Clear();

                    // Deserialize
                    T result = null;
                    memoryStream.Seek(0, SeekOrigin.Begin);
                    var reader = new BinarySerializationReader(memoryStream);
                    reader.Context.SerializerSelector = cloneSerializerSelector;
                    reader.Context.Set(CloneContextProperty, cloneContext);
                    reader.SerializeExtended(ref result, ArchiveMode.Deserialize, null);

                    return result;
                }
                finally
                {
                    cloneContext.Cleanup();
                }
            }
        }

        public delegate bool TryGetValueFunction<in TKey, TResult>(TKey key, out TResult result);

        /// <summary>
        /// Helper class for cloning <see cref="Entity"/>.
        /// </summary>
        internal class CloneContext
        {
            public void Cleanup()
            {
                MemoryStream.SetLength(0);
                MappedObjects = null;
                SerializedObjects.Clear();
                ContentReferences.Clear();
                ClonedObjects = null;
                SharedObjects.Clear();
                EntitySerializerSelector = null;
            }

            public MemoryStream MemoryStream = new MemoryStream(4096);

            public TryGetValueFunction<object, object> MappedObjects;

            public readonly HashSet<object> SerializedObjects = new HashSet<object>();

            public readonly List<ContentReference> ContentReferences = new List<ContentReference>();

            /// <summary>
            /// Lists objects that should be cloned.
            /// </summary>
            public HashSet<object> ClonedObjects;

            /// <summary>
            /// Stores objects that should be reused in the new cloned instance.
            /// </summary>
            public readonly List<object> SharedObjects = new List<object>();

            /// <summary>
            /// Special serializer that goes through <see cref="EntitySerializerSelector"/> and <see cref="CloneEntityComponentSerializer{T}"/>.
            /// </summary>
            public SerializerSelector EntitySerializerSelector;
        }

    }
}