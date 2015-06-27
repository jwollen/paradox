﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Threading;
using SiliconStudio.Core.Diagnostics;

namespace SiliconStudio.Core
{
    /// <summary>
    /// Base class for a framework component.
    /// </summary>
    [DataContract]
    public abstract class ComponentBase : DisposeBase, IComponent
    {
        private static int globalCounterId;
        private string name;

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentBase"/> class.
        /// </summary>
        protected ComponentBase()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ComponentBase"/> class.
        /// </summary>
        /// <param name="name">The name attached to this component</param>
        protected ComponentBase(string name)
        {
            Name = name ?? GetType().Name;
            Id = Interlocked.Increment(ref globalCounterId);

            // Track this component
            if (ComponentTracker.Enable) ComponentTracker.Track(this);
        }

        public int Id { get; private set; }

        /// <summary>
        /// Gets or sets the name of this component.
        /// </summary>
        /// <value>
        /// The name.
        /// </value>
        [DataMemberIgnore] // By default don't store it, unless derived class are overriding this member
        public virtual string Name
        {
            get
            {
                return name;
            }
            set
            {
                if (value == name) return;

                name = value;
                OnNameChanged();
            }
        }

        /// <summary>
        /// Called when <see cref="Name"/> property was changed.
        /// </summary>
        protected virtual void OnNameChanged()
        {
        }

        /// <summary>
        /// Disposes of object resources.
        /// </summary>
        protected override void Destroy()
        {
            // Untrack this object
            if (ComponentTracker.Enable) ComponentTracker.UnTrack(this);

            base.Destroy();
        }

        public override string ToString()
        {
            return string.Format("{0}: {1}", this.GetType().Name, Name);
        }

        protected override void OnAddReference()
        {
            if (ComponentTracker.Enable && ComponentTracker.EnableEvents)
                ComponentTracker.NotifyEvent(this, ComponentEventType.AddReference);
        }

        protected override void OnReleaseReference()
        {
            if (ComponentTracker.Enable && ComponentTracker.EnableEvents)
                ComponentTracker.NotifyEvent(this, ComponentEventType.Release);
        }
    }
}