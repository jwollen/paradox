﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;

using SiliconStudio.Core.Extensions;
using SiliconStudio.Quantum.Contents;
using SiliconStudio.Quantum.References;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// A container used to store models and resolve references between them.
    /// </summary>
    public class ModelContainer
    {
        private readonly Dictionary<Guid, IModelNode> modelsByGuid = new Dictionary<Guid, IModelNode>();
        private readonly IGuidContainer guidContainer;
        private readonly object lockObject = new object();

        /// <summary>
        /// Create a new instance of <see cref="ModelContainer"/>.
        /// </summary>
        /// <param name="instantiateGuidContainer">Indicate whether to create a <see cref="GuidContainer"/> to store Guid per data object. This can be useful to retrieve an existing model from a data object.</param>
        public ModelContainer(bool instantiateGuidContainer = true)
        {
            if (instantiateGuidContainer)
                guidContainer = new GuidContainer();
            NodeBuilder = CreateDefaultNodeBuilder();
        }
        
        /// <summary>
        /// Create a new instance of <see cref="ModelContainer"/>. This constructor allows to provide a <see cref="IGuidContainer"/>,
        /// in order to share object <see cref="Guid"/> between different <see cref="ModelContainer"/>.
        /// </summary>
        /// <param name="guidContainer">A <see cref="IGuidContainer"/> to use to ensure the unicity of guid associated to data objects. Cannot be <c>null</c></param>
        public ModelContainer(IGuidContainer guidContainer)
        {
            if (guidContainer == null) throw new ArgumentNullException("guidContainer");
            this.guidContainer = guidContainer;
            NodeBuilder = CreateDefaultNodeBuilder();
        }

        /// <summary>
        /// Gets an enumerable of the registered models.
        /// </summary>
        public IEnumerable<IModelNode> Models { get { return modelsByGuid.Values; } }

        /// <summary>
        /// Gets an enumerable of the registered models.
        /// </summary>
        public IEnumerable<Guid> Guids { get { return modelsByGuid.Keys; } }

        /// <summary>
        /// Gets or set the visitor to use to create models. Default value is a <see cref="DefaultModelBuilder"/> constructed with default parameters.
        /// </summary>
        public INodeBuilder NodeBuilder { get; set; }

        /// <summary>
        /// Gets the model associated to a data object, if it exists. If the ModelContainer has been constructed without <see cref="IGuidContainer"/>, this method will throw an exception.
        /// </summary>
        /// <param name="rootObject">The data object.</param>
        /// <returns>The <see cref="IModelNode"/> associated to the given object if available, or <c>null</c> otherwise.</returns>
        public IModelNode GetModelNode(object rootObject)
        {
            lock (lockObject)
            {
                if (guidContainer == null) throw new InvalidOperationException("This ModelContainer has no GuidContainer and can't retrieve Guid associated to a data object.");
                Guid guid = guidContainer.GetGuid(rootObject);
                return guid == Guid.Empty ? null : GetModelNode(guid);
            }
        }

        /// <summary>
        /// Gets the model associated to the given Guid, if it exists.
        /// </summary>
        /// <param name="guid">The Guid.</param>
        /// <returns>The <see cref="IModelNode"/> associated to the given Guid if available, or <c>null</c> otherwise.</returns>
        public IModelNode GetModelNode(Guid guid)
        {
            lock (lockObject)
            {
                IModelNode result;
                if (modelsByGuid.TryGetValue(guid, out result))
                {
                    if (result != null)
                        UpdateReferences(result);
                }
                return result;
            }
        }

        /// <summary>
        /// Gets the <see cref="Guid"/> associated to a data object, if it exists. If the ModelContainer has been constructed without <see cref="IGuidContainer"/>, this method will throw an exception.
        /// </summary>
        /// <param name="rootObject">The data object.</param>
        /// <param name="type">Thetype of the data object.</param>
        /// <returns>The <see cref="Guid"/> associated to the given object if available, or <see cref="Guid.Empty"/> otherwise.</returns>
        public Guid GetGuid(object rootObject, Type type)
        {
            lock (lockObject)
            {
                if (guidContainer == null) throw new InvalidOperationException("This ModelContainer has no GuidContainer and can't retrieve Guid associated to a data object.");
                return guidContainer.GetGuid(rootObject);
            }
        }

        /// <summary>
        /// Gets the model associated to a data object, if it exists, or create a new model for the object otherwise.
        /// </summary>
        /// <param name="rootObject">The data object.</param>
        /// <param name="type">The type of the data object.</param>
        /// <param name="updateReferencesIfExists">Update references contained in the result node, if it already exists.</param>
        /// <param name="referencer">The referencer (optional, just here to help having some context when building nodes).</param>
        /// <returns>The <see cref="IModelNode"/> associated to the given object.</returns>
        // TODO: Remove the type argument here
        public IModelNode GetOrCreateModelNode(object rootObject, Type type, bool updateReferencesIfExists = true, IModelNode referencer = null)
        {
            lock (lockObject)
            {
                IModelNode result = null;
                if (guidContainer != null && (rootObject == null || !rootObject.GetType().IsValueType))
                {
                    result = GetModelNode(rootObject);
                }

                return result ?? CreateModelNode(rootObject, type, referencer);
            }
        }

        /// <summary>
        /// Removes all models that were previously registered.
        /// </summary>
        public void Clear()
        {
            lock (lockObject)
            {
                if (guidContainer != null)
                {
                    guidContainer.Clear();
                }
                modelsByGuid.Clear();
            }
        }

        /// <summary>
        /// Refresh all references contained in the given node, creating new models for newly referenced objects.
        /// </summary>
        /// <param name="node">The node to update</param>
        internal void UpdateReferences(IModelNode node)
        {
            lock (lockObject)
            {
                // If the node was holding a reference, refresh the reference
                if (node.Content.IsReference)
                {
                    node.Content.Reference.Refresh(node.Content.Value);
                    UpdateOrCreateReferenceTarget(node.Content.Reference, node);
                }
                else
                {
                    // Otherwise refresh potential references in its children.
                    foreach (var child in node.Children.SelectDeep(x => x.Children).Where(x => x.Content.IsReference))
                    {
                        child.Content.Reference.Refresh(child.Content.Value);
                        UpdateOrCreateReferenceTarget(child.Content.Reference, child);
                    }
                }
            }
        }

        /// <summary>
        /// Creates the model node.
        /// </summary>
        /// <param name="rootObject">The root object.</param>
        /// <param name="type">The type.</param>
        /// <param name="referencer">The referencer (optional, just here to help having some context when building nodes).</param>
        /// <returns></returns>
        /// <exception cref="System.ArgumentException">@The given type does not match the given object.;rootObject</exception>
        private IModelNode CreateModelNode(object rootObject, Type type, IModelNode referencer)
        {
            if (rootObject != null && !type.IsInstanceOfType(rootObject)) throw new ArgumentException(@"The given type does not match the given object.", "rootObject");

            Guid guid = Guid.NewGuid();

            // Retrieve results
            if (guidContainer != null && rootObject != null && !rootObject.GetType().IsValueType)
                guid = guidContainer.GetOrCreateGuid(rootObject);

            var result = (ModelNode)NodeBuilder.Build(referencer, rootObject, type, guid);

            if (result != null && (result.Flags & ModelNodeFlags.DoNotCache) == 0)
            {
                // Register reference objects
                modelsByGuid.Add(result.Guid, result);

                // Create or update model for referenced objects
                UpdateReferences(result);
            }

            return result;
        }

        private void UpdateOrCreateReferenceTarget(IReference reference, IModelNode modelNode, Stack<object> indices = null)
        {
            if (reference == null) throw new ArgumentNullException("reference");
            if (modelNode == null) throw new ArgumentNullException("modelNode");

            var content = (ContentBase)modelNode.Content;

            var referenceEnumerable = reference as ReferenceEnumerable;
            if (referenceEnumerable != null)
            {
                if (indices == null)
                    indices = new Stack<object>();

                foreach (var itemReference in referenceEnumerable)
                {
                    indices.Push(itemReference.Index);
                    UpdateOrCreateReferenceTarget(itemReference, modelNode, indices);
                    indices.Pop();
                }
            }
            else
            {
                if (content.ShouldProcessReference)
                {
                    var singleReference = ((ObjectReference)reference);
                    if (singleReference.TargetNode != null && singleReference.TargetNode.Content.Value != reference.ObjectValue)
                    {
                        singleReference.Clear();
                    }

                    if (singleReference.TargetNode == null)
                    {
                        // This call will recursively update the references.
                        IModelNode node = singleReference.SetTarget(this);
                        if (node != null)
                        {                 
                            var structContent = node.Content as BoxedContent;
                            if (structContent != null)
                            {
                                structContent.BoxedStructureOwner = content;
                                structContent.BoxedStructureOwnerIndices = indices != null ? indices.Reverse().ToArray() : null;
                            }
                        }
                        else
                        {
                            content.ShouldProcessReference = false;
                        }
                    }
                }
            }
        }

        private INodeBuilder CreateDefaultNodeBuilder()
        {
            var nodeBuilder = new DefaultModelBuilder(this);
            return nodeBuilder;
        }
    }
}
