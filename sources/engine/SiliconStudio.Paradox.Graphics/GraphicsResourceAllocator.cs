﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;

using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// A <see cref="GraphicsResource"/> allocator tracking usage reference and allowing to recycle unused resources based on a recycle policy. 
    /// </summary>
    /// <remarks>
    /// This class is threadsafe. Accessing a member will lock globally this instance.
    /// </remarks>
    public class GraphicsResourceAllocator : ComponentBase
    {
        // TODO: Check if we should introduce an enum for the kind of scope (per DrawCore, per Frame...etc.)
        // TODO: Add statistics method (number of objects allocated...etc.)

        private readonly object thisLock = new object();
        private readonly Dictionary<TextureDescription, List<GraphicsResourceLink>> textureCache = new Dictionary<TextureDescription, List<GraphicsResourceLink>>();
        private readonly Dictionary<BufferDescription, List<GraphicsResourceLink>> bufferCache = new Dictionary<BufferDescription, List<GraphicsResourceLink>>();

        /// <summary>
        /// Initializes a new instance of the <see cref="GraphicsResourceAllocator" /> class.
        /// </summary>
        /// <param name="serviceRegistry">The service registry.</param>
        public GraphicsResourceAllocator(IServiceRegistry serviceRegistry)
        {
            if (serviceRegistry == null) throw new ArgumentNullException("serviceRegistry");
            Services = serviceRegistry;
            GraphicsDevice = serviceRegistry.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
        }

        /// <summary>
        /// Gets or sets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        private GraphicsDevice GraphicsDevice { get; set; }

        /// <summary>
        /// Gets the services registry.
        /// </summary>
        /// <value>The services registry.</value>
        public IServiceRegistry Services { get; private set; }

        /// <summary>
        /// Gets or sets the default recycle policy.
        /// </summary>
        /// <value>The default recycle policy.</value>
        public GraphicsResourceRecyclePolicyDelegate DefaultRecyclePolicy { get; set; }

        /// <summary>
        /// Recycles unused resources (with a  <see cref="GraphicsResourceLink.ReferenceCount"/> == 0 ) with the <see cref="DefaultRecyclePolicy"/>. By Default, no recycle policy installed.
        /// </summary>
        public void Recycle()
        {
            if (DefaultRecyclePolicy != null)
            {
                Recycle(DefaultRecyclePolicy);
            }
        }

        /// <summary>
        /// Recycles unused resource with the specified recycle policy.
        /// </summary>
        /// <param name="recyclePolicy">The recycle policy.</param>
        /// <exception cref="System.ArgumentNullException">recyclePolicy</exception>
        public void Recycle(GraphicsResourceRecyclePolicyDelegate recyclePolicy)
        {
            if (recyclePolicy == null) throw new ArgumentNullException("recyclePolicy");

            // Global lock to be threadsafe. 
            lock (thisLock)
            {
                Recycle(textureCache, recyclePolicy);
                Recycle(bufferCache, recyclePolicy);
            }
        }

        /// <summary>
        /// Gets a texture for the specified description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>A texture</returns>
        public Texture GetTemporaryTexture(TextureDescription description)
        {
            // Global lock to be threadsafe. 
            lock (thisLock)
            {
                return GetTemporaryResource(textureCache, description, CreateTexture, GetTextureDefinition);
            }
        }

        /// <summary>
        /// Gets a texture for the specified description.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>A texture</returns>
        public Buffer GetTemporaryBuffer(BufferDescription description)
        {
            // Global lock to be threadsafe. 
            lock (thisLock)
            {
                return GetTemporaryResource(bufferCache, description, CreateBuffer, GetBufferDescription);
            }
        }

        /// <summary>
        /// Increments the reference to a temporary resource.
        /// </summary>
        /// <param name="resource"></param>
        public void AddReference(GraphicsResource resource)
        {
            // Global lock to be threadsafe. 
            lock (thisLock)
            {
                UpdateReference(resource, 1);
            }
        }

        /// <summary>
        /// Decrements the reference to a temporary resource.
        /// </summary>
        /// <param name="resource"></param>
        public void ReleaseReference(GraphicsResource resource)
        {
            // Global lock to be threadsafe. 
            lock (thisLock)
            {
                UpdateReference(resource, -1);
            }
        }

        /// <summary>
        /// Creates a texture for output.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>Texture.</returns>
        protected virtual Texture CreateTexture(TextureDescription description)
        {
            return Texture.New(GraphicsDevice, description);
        }

        /// <summary>
        /// Creates a tempoary buffer.
        /// </summary>
        /// <param name="description">The description.</param>
        /// <returns>Buffer.</returns>
        protected virtual Buffer CreateBuffer(BufferDescription description)
        {
            return Buffer.New(GraphicsDevice, description);
        }

        protected override void Destroy()
        {
            lock (thisLock)
            {
                DisposeCache(textureCache, true);
                DisposeCache(bufferCache, true);
            }

            base.Destroy();
        }

        private void DisposeCache<TKey>(Dictionary<TKey, List<GraphicsResourceLink>> cache, bool clearKeys)
        {
            foreach (var resourceList in cache.Values)
            {
                foreach (var resource in resourceList)
                {
                    resource.Resource.Dispose();
                }
                if (!clearKeys)
                {
                    resourceList.Clear();
                }
            }
            if (clearKeys)
            {
                cache.Clear();
            }
        }

        private BufferDescription GetBufferDescription(Buffer buffer)
        {
            return buffer.Description;
        }

        private TextureDescription GetTextureDefinition(Texture texture)
        {
            return texture.Description;
        }

        private TResource GetTemporaryResource<TResource, TKey>(Dictionary<TKey, List<GraphicsResourceLink>> cache, TKey description, Func<TKey, TResource> creator, Func<TResource, TKey> getDefinition)
            where TResource : GraphicsResource
            where TKey : struct
        {
            // For a specific description, get allocated textures
            List<GraphicsResourceLink> resourceLinks = null;
            if (!cache.TryGetValue(description, out resourceLinks))
            {
                resourceLinks = new List<GraphicsResourceLink>();
                cache.Add(description, resourceLinks);
            }

            // Find a texture available
            foreach (var resourceLink in resourceLinks)
            {
                if (resourceLink.ReferenceCount == 0)
                {
                    UpdateCounter(resourceLink, 1);
                    return (TResource)resourceLink.Resource;
                }
            }

            // If no texture available, then creates a new one
            var newResource = creator(description);
            newResource.Name = string.Format("{0}{1}-{2}", Name == null ? string.Empty : string.Format("{0}-", Name), newResource.Name == null ? newResource.GetType().Name : Name, resourceLinks.Count);

            // Description may be altered when creating a resource (based on HW limitations...etc.) so we get the actual description
            var realDescription = getDefinition(newResource);

            // For a specific description, get allocated textures
            if (!cache.TryGetValue(realDescription, out resourceLinks))
            {
                resourceLinks = new List<GraphicsResourceLink>();
                cache.Add(description, resourceLinks);
            }

            // Add the texture to the allocated textures
            // Start RefCount == 1, because we don't want this texture to be available if a post FxProcessor is calling
            // several times this GetTemporaryTexture method.
            var newResourceLink = new GraphicsResourceLink(newResource) { ReferenceCount = 1 };
            resourceLinks.Add(newResourceLink);

            return newResource;
        }

        /// <summary>
        /// Recycles the specified cache.
        /// </summary>
        /// <typeparam name="TKey">The type of the t key.</typeparam>
        /// <param name="cache">The cache.</param>
        /// <param name="recyclePolicy">The recycle policy.</param>
        private void Recycle<TKey>(Dictionary<TKey, List<GraphicsResourceLink>> cache, GraphicsResourceRecyclePolicyDelegate recyclePolicy)
        {
            foreach (var resourceList in cache.Values)
            {
                for (int i = resourceList.Count - 1; i >= 0; i--)
                {
                    var resourceLink = resourceList[i];
                    if (resourceLink.ReferenceCount == 0)
                    {
                        if (recyclePolicy(resourceLink))
                        {
                            resourceLink.Resource.Dispose();
                            resourceList.RemoveAt(i);
                        }
                        // Reset the access count
                        resourceLink.AccessCountSinceLastRecycle = 0;
                    }
                }
            }
        }

        private void UpdateReference(GraphicsResource resource, int referenceDelta)
        {
            if (resource == null)
            {
                return;
            }

            var texture = resource as Texture;
            bool resourceFound;
            if (texture != null)
            {
                resourceFound = UpdateReferenceCount(textureCache, texture, GetTextureDefinition, referenceDelta);
            }
            else
            {
                var buffer = resource as Buffer;
                if (buffer == null)
                {
                    throw new ArgumentException("Unsupported GraphicsResource - Only Texture and Buffer", "resource");
                }
                resourceFound = UpdateReferenceCount(bufferCache, buffer, GetBufferDescription, referenceDelta);
            }

            if (!resourceFound)
            {
                throw new ArgumentException("The resource was not allocated by this allocator");
            }
        }

        private bool UpdateReferenceCount<TKey, TResource>(Dictionary<TKey, List<GraphicsResourceLink>> cache, TResource resource, Func<TResource, TKey> getDefinition, int deltaCount)
            where TResource : GraphicsResource
            where TKey : struct
        {
            if (resource == null)
            {
                return false;
            }

            List<GraphicsResourceLink> resourceLinks = null;
            if (cache.TryGetValue(getDefinition(resource), out resourceLinks))
            {
                foreach (var resourceLink in resourceLinks)
                {
                    if (resourceLink.Resource == resource)
                    {
                        UpdateCounter(resourceLink, deltaCount);
                        return true;
                    }
                }
            }
            return false;
        }

        private void UpdateCounter(GraphicsResourceLink resourceLink, int deltaCount)
        {
            if ((resourceLink.ReferenceCount + deltaCount) < 0)
            {
                throw new InvalidOperationException("Invalid decrement on reference count (must be >=0 after decrement). Current reference count: [{0}] Decrement: [{1}]".ToFormat(resourceLink.ReferenceCount, deltaCount));
            }

            resourceLink.ReferenceCount += deltaCount;
            resourceLink.AccessTotalCount++;
            resourceLink.AccessCountSinceLastRecycle++;
            resourceLink.LastAccessTime = DateTime.Now;
        }
    }
}