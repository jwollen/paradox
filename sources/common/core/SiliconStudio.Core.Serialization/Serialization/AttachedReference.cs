﻿using System;

namespace SiliconStudio.Core.Serialization
{
    /// <summary>
    /// Stores the object reference information, so that it is easy to work on partially loaded or CPU version of assets with <see cref="Assets.AssetManager"/>.
    /// </summary>
    public class AttachedReference
    {
        /// <summary>
        /// The asset URL of the referenced data.
        /// </summary>
        public string Url;

        /// <summary>
        /// The asset unique identifier.
        /// </summary>
        public Guid Id;

        /// <summary>
        /// If yes, this object won't be recursively saved in a separate chunk by <see cref="Assets.AssetManager"/>.
        /// Use this if you only care about the Url reference.
        /// </summary>
        public bool IsProxy;

        /// <summary>
        /// Data representation (useful if your object is a GPU object but you want to manipulate a CPU version of it).
        /// This needs to be manually interpreted by a custom <see cref="DataSerializer{T}"/> implementation.
        /// </summary>
        public object Data;
    }
}