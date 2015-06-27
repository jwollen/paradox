﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

using SiliconStudio.Core.Reflection;
using SiliconStudio.Quantum.References;

namespace SiliconStudio.Quantum.Contents
{
    /// <summary>
    /// Content of a <see cref="IModelNode"/>.
    /// </summary>
    public interface IContent
    {
        /// <summary>
        /// Gets the expected type of <see cref="Value"/>.
        /// </summary>
        Type Type { get; }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        object Value { get; set; }

        /// <summary>
        /// Gets whether this content hold a primitive type value. If so, the node owning this content should have no children and modifying its value should not trigger any node refresh.
        /// </summary>
        /// <remarks>Types registered as primitive types in the <see cref="INodeBuilder"/> used to build this content are taken in account by this property.</remarks>
        bool IsPrimitive { get; }

        /// <summary>
        /// Gets or sets the type descriptor of this content
        /// </summary>
        ITypeDescriptor Descriptor { get; }

        /// <summary>
        /// Gets wheither this content holds a reference or is a direct value.
        /// </summary>
        bool IsReference { get; }

        /// <summary>
        /// Gets the reference hold by this content, if applicable.
        /// </summary>
        IReference Reference { get; }

        /// <summary>
        /// Gets whether the <see cref="Reference"/> contained in this content should lead to the creation of model node for the referenced object.
        /// </summary>
        bool ShouldProcessReference { get; }

        /// <summary>
        /// Gets or sets the loading state.
        /// </summary>
        ViewModelContentState LoadState { get; set; }

        /// <summary>
        /// Gets or sets the content flags.
        /// </summary>
        ViewModelContentFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets the serialization flags.
        /// </summary>
        ViewModelContentSerializeFlags SerializeFlags { get; set; }
    }
}