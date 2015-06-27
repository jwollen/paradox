﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Engine.Design
{
    /// <summary>
    /// An attribute used to associate a default <see cref="IEntityComponentRenderer"/> to an entity component.
    /// </summary>
    public class DefaultEntityComponentRendererAttribute : DynamicTypeAttributeBase
    {
        private readonly int order;

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEntityComponentRendererAttribute"/> class.
        /// </summary>
        /// <param name="type">The type must derived from <see cref="IEntityComponentRenderer"/>.</param>
        public DefaultEntityComponentRendererAttribute(Type type) : base(type)
        {
            order = 0;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="DefaultEntityComponentRendererAttribute" /> class.
        /// </summary>
        /// <param name="type">The type must derived from <see cref="IEntityComponentRenderer" />.</param>
        /// <param name="order">The order.</param>
        public DefaultEntityComponentRendererAttribute(Type type, int order) : base(type)
        {
            this.order = order;
        }

        public int Order
        {
            get
            {
                return order;
            }
        }
    }
} 