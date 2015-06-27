﻿using System;
using System.Runtime.CompilerServices;

namespace SiliconStudio.Core.Serialization
{
    /// <summary>
    /// Manage object reference information externally, not stored in the object but in a separate <see cref="AttachedReference"/> object.
    /// </summary>
    public static class AttachedReferenceManager
    {
        private static ConditionalWeakTable<object, AttachedReference> attachedReferences = new ConditionalWeakTable<object, AttachedReference>();

        /// <summary>
        /// Gets the URL of a given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static string GetUrl(object obj)
        {
            AttachedReference attachedReference;
            return attachedReferences.TryGetValue(obj, out attachedReference) ? attachedReference.Url : null;
        }

        /// <summary>
        /// Sets the URL of a given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <param name="url">The URL.</param>
        public static void SetUrl(object obj, string url)
        {
            var attachedReference = attachedReferences.GetValue(obj, x => new AttachedReference());
            attachedReference.Url = url;
        }

        /// <summary>
        /// Gets the object reference info of a given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static AttachedReference GetAttachedReference(object obj)
        {
            AttachedReference attachedReference;
            attachedReferences.TryGetValue(obj, out attachedReference);
            return attachedReference;
        }

        /// <summary>
        /// Gets or creates the object reference info of a given object.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns></returns>
        public static AttachedReference GetOrCreateAttachedReference(object obj)
        {
            return attachedReferences.GetValue(obj, x => new AttachedReference());
        }

        /// <summary>
        /// Creates a proxy object with <see cref="AttachedReference" /> designing it as a proxy with a given id and location (that can be used with <see cref="Assets.AssetManager" />). This allows to construct and save object references without actually loading them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="contentReference">The content reference.</param>
        /// <returns>T.</returns>
        /// <exception cref="System.ArgumentNullException">contentReference</exception>
        public static T CreateSerializableVersion<T>(IContentReference contentReference) where T : class, new()
        {
            if (contentReference == null) throw new ArgumentNullException("contentReference");
            return CreateSerializableVersion<T>(contentReference.Id, contentReference.Location);
        }

        /// <summary>
        /// Creates a proxy object with <see cref="AttachedReference"/> designing it as a proxy with a given id and location (that can be used with <see cref="Assets.AssetManager"/>). This allows to construct and save object references without actually loading them.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="id">The identifier.</param>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        public static T CreateSerializableVersion<T>(Guid id, string location) where T : class, new()
        {
            var result = new T();
            var attachedReference = GetOrCreateAttachedReference(result);
            attachedReference.Id = id;
            attachedReference.Url = location;
            attachedReference.IsProxy = true;
            return result;
        }

        /// <summary>
        /// Creates a proxy object with <see cref="AttachedReference"/> designing it as a proxy with a given id and location (that can be used with <see cref="Assets.AssetManager"/>). This allows to construct and save object references without actually loading them.
        /// </summary>
        /// <param name="type">The type.</param>
        /// <param name="id">The identifier.</param>
        /// <param name="location">The location.</param>
        /// <returns></returns>
        public static object CreateSerializableVersion(Type type, Guid id, string location)
        {
            var result = Activator.CreateInstance(type);
            var attachedReference = GetOrCreateAttachedReference(result);
            attachedReference.Id = id;
            attachedReference.Url = location;
            attachedReference.IsProxy = true;
            return result;
        }
    }
}