﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

using System.Reflection;

namespace SiliconStudio.Paradox.UI.Renderers
{
    /// <summary>
    /// The class in charge to manage the renderer of the different <see cref="UIElement"/>s.
    /// Once registered into the manager, a renderer is owned by the manager.
    /// </summary>
    public class RendererManager: IRendererManager, IDisposable
    {
        private readonly IElementRendererFactory defaultFactory;

        private readonly Dictionary<Type, IElementRendererFactory> typesToUserFactories = new Dictionary<Type, IElementRendererFactory>();

        // Note: use Id instead of element instance in order to avoid to keep dead UIelement alive.
        private readonly Dictionary<uint, ElementRenderer> elementIdToRenderer = new Dictionary<uint, ElementRenderer>();

        /// <summary> 
        /// Create a new instance of <see cref="RendererManager"/> with provided DefaultFactory
        /// </summary>
        /// <param name="defaultFactory"></param>
        public RendererManager(IElementRendererFactory defaultFactory)
        {
            this.defaultFactory = defaultFactory;
        }

        public ElementRenderer GetRenderer(UIElement element)
        {
            ElementRenderer elementRenderer;
            elementIdToRenderer.TryGetValue(element.ID, out elementRenderer);
            if (elementRenderer == null)
            {
                // try to get the renderer from the user registered class factory
                var currentType = element.GetType();
                while (elementRenderer == null && currentType != null)
                {
                    if (typesToUserFactories.ContainsKey(currentType))
                        elementRenderer = typesToUserFactories[currentType].TryCreateRenderer(element);

                    currentType = currentType.GetTypeInfo().BaseType;
                }

                // try to get the renderer from the default renderer factory.
                if (elementRenderer == null && defaultFactory != null)
                    elementRenderer = defaultFactory.TryCreateRenderer(element);

                if (elementRenderer == null)
                    throw new InvalidOperationException(string.Format("No renderer found for element {0}", element));

                // cache the renderer for future uses.
                elementIdToRenderer[element.ID] = elementRenderer;
            }

            return elementRenderer;
        }

        public void RegisterRendererFactory(Type uiElementType, IElementRendererFactory factory)
        {
            if (uiElementType == null) throw new ArgumentNullException("uiElementType");
            if (factory == null) throw new ArgumentNullException("factory");

            if(!typeof(UIElement).GetTypeInfo().IsAssignableFrom(uiElementType.GetTypeInfo()))
                throw new InvalidOperationException(uiElementType + " is not a descendant of UIElement.");

            typesToUserFactories[uiElementType] = factory;
        }

        public void RegisterRenderer(UIElement element, ElementRenderer renderer)
        {
            if (element == null) throw new ArgumentNullException("element");
            if (renderer == null) throw new ArgumentNullException("renderer");

            elementIdToRenderer[element.ID] = renderer;
        }

        public void Dispose()
        {
            foreach (var renderer in elementIdToRenderer.Values)
            {
                if(!renderer.IsDisposed)
                    renderer.Dispose();
            }
            elementIdToRenderer.Clear();
        }
    }
}