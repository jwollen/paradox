﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Rendering.Images;
using ComponentBase = SiliconStudio.Core.ComponentBase;
using IServiceRegistry = SiliconStudio.Core.IServiceRegistry;

namespace SiliconStudio.Paradox.Rendering
{
    /// <summary>
    /// Rendering context.
    /// </summary>
    public sealed class RenderContext : ComponentBase
    {
        private const string SharedImageEffectContextKey = "__SharedRenderContext__";
        private readonly Dictionary<Type, DrawEffect> sharedEffects = new Dictionary<Type, DrawEffect>();
        private readonly GraphicsResourceAllocator allocator;

        private readonly Stack<ParameterCollection> parametersStack;

        /// <summary>
        /// Initializes a new instance of the <see cref="RenderContext" /> class.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <param name="allocator">The allocator.</param>
        /// <exception cref="System.ArgumentNullException">services</exception>
        private RenderContext(IServiceRegistry services, GraphicsResourceAllocator allocator = null)
        {
            if (services == null) throw new ArgumentNullException("services");
            Services = services;
            Effects = services.GetSafeServiceAs<EffectSystem>();
            this.allocator = allocator ?? new GraphicsResourceAllocator(Services).DisposeBy(this);
            GraphicsDevice = services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
            parametersStack = new Stack<ParameterCollection>();
            PushParameters(new ParameterCollection());
        }

        /// <summary>
        /// Occurs when a renderer is initialized.
        /// </summary>
        public event Action<IGraphicsRendererCore> RendererInitialized;

        /// <summary>
        /// Gets the content manager.
        /// </summary>
        /// <value>The content manager.</value>
        public EffectSystem Effects { get; private set; }

        /// <summary>
        /// Gets or sets the graphics device.
        /// </summary>
        /// <value>The graphics device.</value>
        public GraphicsDevice GraphicsDevice { get; private set; }

        /// <summary>
        /// Gets the services registry.
        /// </summary>
        /// <value>The services registry.</value>
        public IServiceRegistry Services { get; private set; }

        /// <summary>
        /// Gets the parameters shared with all <see cref="ImageEffect"/> instance.
        /// </summary>
        /// <value>The parameters.</value>
        public ParameterCollection Parameters { get; private set; }

        public void PushParameters(ParameterCollection parameters)
        {
            if (parameters == null) throw new ArgumentNullException("parameters");
            parametersStack.Push(parameters);
            Parameters = parameters;
        }

        public ParameterCollection PopParameters()
        {
            if (parametersStack.Count == 1)
            {
                throw new InvalidOperationException("Cannot Pop more than push");
            }
            var previous = parametersStack.Pop();
            Parameters = parametersStack.Peek();
            return previous;
        }

        /// <summary>
        /// Gets the time.
        /// </summary>
        /// <value>The time.</value>
        public GameTime Time { get; internal set; }

        /// <summary>
        /// Gets the <see cref="GraphicsResource"/> allocator.
        /// </summary>
        /// <value>The allocator.</value>
        public GraphicsResourceAllocator Allocator
        {
            get
            {
                return allocator;
            }
        }

        /// <summary>
        /// Gets or creates a shared effect.
        /// </summary>
        /// <typeparam name="T">Type of the shared effect (mush have a constructor taking a <see cref="RenderContext"/></typeparam>
        /// <returns>A singleton instance of <typeparamref name="T"/></returns>
        public T GetSharedEffect<T>() where T : DrawEffect, new()
        {
            // TODO: Add a way to support custom constructor
            lock (sharedEffects)
            {
                DrawEffect effect;
                if (!sharedEffects.TryGetValue(typeof(T), out effect))
                {
                    effect = new T();
                    sharedEffects.Add(typeof(T), effect);
                    effect.Initialize(this);
                }

                return (T)effect;
            }
        }

        /// <summary>
        /// Gets a global shared context.
        /// </summary>
        /// <param name="services">The services.</param>
        /// <returns>RenderContext.</returns>
        public static RenderContext GetShared(IServiceRegistry services)
        {
            if (services == null) throw new ArgumentNullException("services");

            // Store RenderContext shared into the GraphicsDevice
            var graphicsDevice = services.GetSafeServiceAs<IGraphicsDeviceService>().GraphicsDevice;
            return graphicsDevice.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, SharedImageEffectContextKey, d => new RenderContext(services));
        }

        internal void OnRendererInitialized(IGraphicsRendererCore obj)
        {
            Action<IGraphicsRendererCore> handler = RendererInitialized;
            if (handler != null) handler(obj);
        }
    }
}
