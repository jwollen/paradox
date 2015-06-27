﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Audio
{
    /// <summary>
    /// Processor in charge of creating and updating the <see cref="AudioListener"/> data associated to the scene <see cref="AudioListenerComponent"/>s.
    /// </summary>
    /// <remarks>
    /// The processor updates only <see cref="AudioListener"/> associated to <see cref="AudioListenerComponent"/>s 
    /// added to the <see cref="AudioSystem"/> via the <see cref="AudioSystem.AddListener"/> function.
    /// The processor is subscribing to the <see cref="audioSystem"/> <see cref="AudioListenerComponent"/> collection events to be informed of required <see cref="AudioEmitter"/> updates.
    /// When a <see cref="AudioListenerComponent"/> is added to the <see cref="audioSystem"/>, the processor set the associated <see cref="AudioEmitter"/>.
    /// When a <see cref="AudioListenerComponent"/> is removed from the entity system, 
    /// the processor set the <see cref="AudioEmitter"/> reference of the <see cref="AudioSystem"/> to null 
    /// but do not remove the <see cref="AudioListenerComponent"/> from its collection.
    /// </remarks>
    public class AudioListenerProcessor : EntityProcessor<AudioListenerProcessor.AssociatedData>
    {
        /// <summary>
        /// Reference to the <see cref="AudioSystem"/> of the game instance.
        /// </summary>
        private AudioSystem audioSystem;

        // expose internal member to allow debug (use directly matchingEntities if not for debug uses)
        internal Dictionary<Entity, AssociatedData> MatchingEntitiesForDebug { get { return matchingEntities; } } 

        /// <summary>
        /// Create a new instance of AudioListenerProcessor.
        /// </summary>
        public AudioListenerProcessor()
            : base(new PropertyKey[] { AudioListenerComponent.Key, TransformComponent.Key })
        {
        }

        protected override AssociatedData GenerateAssociatedData(Entity entity)
        {
            // Initialize TransformComponent and ListenerComponent fields of the matchingEntities' AssociatedData.
            // other fields are initialized in OnEntityAdded or OnListenerCollectionChanged
            return new AssociatedData { TransformComponent = entity.Get(TransformComponent.Key),
                                        ListenerComponent = entity.Get(AudioListenerComponent.Key) };
        }

        protected internal override void OnSystemAdd()
        {
            base.OnSystemAdd();

            audioSystem = Services.GetServiceAs<AudioSystem>();

            audioSystem.Listeners.CollectionChanged += OnListenerCollectionChanged;
        }

        protected internal override void OnSystemRemove()
        {
            base.OnSystemRemove();

            audioSystem.Listeners.CollectionChanged -= OnListenerCollectionChanged;

            // ensure that all associated AudioEmitter of the AudioSystem are put to null since not updated anymore.
            foreach (var audioListenerComp in audioSystem.Listeners.Keys)
                audioSystem.Listeners[audioListenerComp] = null;
        }

        protected override void OnEntityAdding(Entity entity, AssociatedData data)
        {
            base.OnEntityAdding(entity, data);

            // initialize the AudioEmitter and mark it for update if it is present in the AudioSystem collection.
            if (audioSystem.Listeners.ContainsKey(data.ListenerComponent))
            {
                InitializeAudioEmitter(data);
                data.ShouldBeComputed = true;
            }
        }

        private void InitializeAudioEmitter(AssociatedData data)
        {
            // initialize emitter position
            data.TransformComponent.UpdateWorldMatrix(); // ensure that value of the worldMatrix is correct
            data.AudioListener = new AudioListener { Position = data.TransformComponent.WorldMatrix.TranslationVector }; // we need a valid value of Position for the first Update (Velocity computation).

            if (!audioSystem.Listeners.ContainsKey(data.ListenerComponent))
                throw new AudioEngineInternalExceptions("Initialized AudioListenerComponent was not in AudioSystem.ListenerList");

            // set reference to the AudioEmitter of AudioSytem.
            audioSystem.Listeners[data.ListenerComponent] = data.AudioListener;
        }

        protected override void OnEntityRemoved(Entity entity, AssociatedData data)
        {
            base.OnEntityRemoved(entity, data);

            // set the reference to the AudioEmitter of AudioSystem to null since not valid anymore.
            if (audioSystem.Listeners.ContainsKey(data.ListenerComponent))
            {
                audioSystem.Listeners[data.ListenerComponent] = null;
            }
        }

        public override void Draw(RenderContext context)
        {
            base.Draw(context);

            foreach (var listenerData in matchingEntities.Values)
            {
                if(!listenerData.ShouldBeComputed)  // skip all updates if the listener is not used.
                    continue;
                
                var worldMatrix = listenerData.TransformComponent.WorldMatrix;
                var listener = listenerData.AudioListener;
                var newPosition = worldMatrix.TranslationVector;

                listener.Velocity = newPosition - listener.Position; // estimate velocity from last and new position
                listener.Position = newPosition;
                listener.Forward = Vector3.Normalize((Vector3)worldMatrix.Row3);
                listener.Up = Vector3.Normalize((Vector3)worldMatrix.Row2);
            }
        }

        /// <summary>
        /// The <see cref="AudioSystem"/> listeners collection has been modified.
        /// Mark AudioEmitter not for update if removed from the list.
        /// Create the AudioEmitter data and mark it for update  if added to the list.
        /// </summary>
        /// <param name="o"></param>
        /// <param name="args"></param>
        private void OnListenerCollectionChanged(object o, TrackingCollectionChangedEventArgs args)
        {
            if (!args.CollectionChanged) // no keys have been added or removed, only one of the values changed
                return;

            var listenersData = matchingEntities.Values.Where(x => x.ListenerComponent == args.Key);

            if (args.Action == NotifyCollectionChangedAction.Add)   // A new listener have been added
            {
                foreach (var listenerData in listenersData)
                {
                    InitializeAudioEmitter(listenerData);
                    listenerData.ShouldBeComputed = true;
                }
            }
            else if(args.Action == NotifyCollectionChangedAction.Remove) // A listener have been removed
            {
                foreach (var listenerData in listenersData)
                    listenerData.ShouldBeComputed = false;
            }
        }
        
        public class AssociatedData
        {
            /// <summary>
            /// Boolean indicating whether the AudioEmitter need to be updated for the current loop turn or not.
            /// </summary>
            public bool ShouldBeComputed;       

            /// <summary>
            /// The <see cref="Audio.AudioListener"/> associated to the below <see cref="AudioListenerComponent"/>.
            /// </summary>
            public AudioListener AudioListener;

            /// <summary>
            /// The <see cref="AudioListenerComponent"/> associated to the entity.
            /// </summary>
            public AudioListenerComponent ListenerComponent;

            /// <summary>
            /// The <see cref="TransformComponent"/> associated to the entity.
            /// </summary>
            public TransformComponent TransformComponent;
        }
    }
}
