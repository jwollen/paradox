﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;

using SiliconStudio.Quantum.References;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// A class describing the path of a node, relative to a root node. The path can cross references, array, etc.
    /// </summary>
    /// <remarks>This class is immutable.</remarks>
    public class ModelNodePath
    {
        /// <summary>
        /// An enum that describes the type of an item of a model node path.
        /// </summary>
        public enum ElementType
        {
            /// <summary>
            /// This item is a member (child) of the previous node
            /// </summary>
            Member,
            /// <summary>
            /// This item is the target of the object reference of the previous node.
            /// </summary>
            Target,
            /// <summary>
            /// This item is the target of a enumerable reference of the previous node corresponding to the associated index.
            /// </summary>
            Index,
        }

        private class NodePathElement
        {
            public ElementType Type;
            public object Value;
            public override string ToString()
            {
                switch (Type)
                {
                    case ElementType.Member:
                        return string.Format(".{0}", Value);
                    case ElementType.Target:
                        return "-> (Target)";
                    case ElementType.Index:
                        return string.Format("[{0}]", Value);
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private readonly List<NodePathElement> path = new List<NodePathElement>();
        private readonly IModelNode rootNode;
        private readonly bool targetIsRootNode;

        private ModelNodePath(IModelNode rootNode, bool targetIsRootNode)
        {
            this.rootNode = rootNode;
            this.targetIsRootNode = targetIsRootNode;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelNodePath"/> with the given root node.
        /// </summary>
        /// <param name="rootNode">The root node to represent with this instance of <see cref="ModelNodePath"/>.</param>
        /// <remarks>This constructor should be used for path to a root node only. To create a path to a child node, use <see cref="GetChildPath"/>.</remarks>
        public ModelNodePath(IModelNode rootNode)
            : this(rootNode, true)
        {
        }

        /// <summary>
        /// Gets whether this path is a valid path.
        /// </summary>
        public bool IsValid { get { return path.Count > 0 || targetIsRootNode; } }

        /// <summary>
        /// Gets the source node corresponding to this path.
        /// </summary>
        /// <param name="targetIndex">The index to the target node, if applicable.</param>
        /// <returns>The node corresponding to this path.</returns>
        /// <exception cref="InvalidOperationException">The path is invalid.</exception>
        public IModelNode GetSourceNode(out object targetIndex)
        {
            if (!IsValid)
                throw new InvalidOperationException("The node path is invalid.");

            IModelNode node = rootNode;
            targetIndex = null;
            foreach (var itemPath in path)
            {
                targetIndex = null;
                switch (itemPath.Type)
                {
                    case ElementType.Member:
                        var name = (string)itemPath.Value;
                        node = node.Children.Single(x => x.Name == name);
                        break;
                    case ElementType.Target:
                        if (itemPath != path[path.Count - 1])
                        {
                            var objectRefererence = (ObjectReference)node.Content.Reference;
                            node = objectRefererence.TargetNode;
                        }
                        break;
                    case ElementType.Index:
                        if (itemPath != path[path.Count - 1])
                        {
                            var enumerableReference = (ReferenceEnumerable)node.Content.Reference;
                            var objectRefererence = enumerableReference.Single(x => Equals(x.Index, itemPath.Value));
                            node = objectRefererence.TargetNode;
                        }
                        targetIndex = itemPath.Value;
                        break;
                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
            return node;
        }
        
        /// <summary>
        /// Gets the source node corresponding to this path.
        /// </summary>
        /// <returns>The node corresponding to this path.</returns>
        /// <exception cref="InvalidOperationException">The path is invalid.</exception>
        public IModelNode GetSourceNode()
        {
            object index;
            return GetSourceNode(out index);
        }

        /// <summary>
        /// Computes a <see cref="ModelNodePath"/> corresponding to the given <see cref="target"/> node, which must be a direct child or a direct reference of the <see cref="parentNode"/>.
        /// </summary>
        /// <param name="parentNode">The parent node which must be a direct child or a direct reference of the <see cref="target"/>.</param>
        /// <param name="target">The target node for which to build a <see cref="ModelNodePath"/> instance.</param>
        /// <returns></returns>
        public ModelNodePath GetChildPath(IModelNode parentNode, IModelNode target)
        {
            if (parentNode == target)
                return Clone();

            var result = Clone(rootNode, false);
            
            var member = parentNode.Children.FirstOrDefault(x => x == target);
            if (member != null)
            {
                // The target is a direct member of the parent.
                result.path.Add(new NodePathElement { Type = ElementType.Member, Value = member.Name });
                return result;
            }
            var objectReference = parentNode.Content.Reference as ObjectReference;
            if (objectReference != null && objectReference.TargetNode == target)
            {
                // The target is the node referenced by the parent.
                result.path.Add(new NodePathElement { Type = ElementType.Target });
                return result;
            }

            member = parentNode.Children.FirstOrDefault(x => x.Content.Reference is ObjectReference && ((ObjectReference)x.Content.Reference).TargetNode == target);
            if (member != null)
            {
                // The target is the node referenced by one of the children of the parent.
                result.path.Add(new NodePathElement { Type = ElementType.Member, Value = member.Name });
                result.path.Add(new NodePathElement { Type = ElementType.Target });
                return result;
            }

            var enumerableReference = parentNode.Content.Reference as ReferenceEnumerable;
            if (enumerableReference != null)
            {
                ObjectReference reference = enumerableReference.FirstOrDefault(x => x.TargetNode == target);
                if (reference != null)
                {
                    // The target is the node referenced by the parent at a given index.
                    result.path.Add(new NodePathElement { Type = ElementType.Index, Value = reference.Index });
                    return result;
                }
            }
            
            foreach (var child in parentNode.Children)
            {
                enumerableReference = child.Content.Reference as ReferenceEnumerable;
                if (enumerableReference != null)
                {
                    ObjectReference reference = enumerableReference.FirstOrDefault(x => x.TargetNode == target);
                    if (reference != null)
                    {
                        // The target is the node referenced by one of the children of the parent at a given index.
                        result.path.Add(new NodePathElement { Type = ElementType.Member, Value = child.Name });
                        result.path.Add(new NodePathElement { Type = ElementType.Index, Value = reference.Index });
                        return result;
                    }
                }
            }
            return null;
        }


        /// <inheritdoc/>
        public override string ToString()
        {
            return IsValid ? "(root)" + path.Select(x => x.ToString()).Aggregate((current, next) => current + next) : "(invalid)";
        }

        public ModelNodePath Clone(IModelNode newRoot)
        {
            return Clone(newRoot, targetIsRootNode);
        }

        public ModelNodePath Clone()
        {
            return Clone(rootNode, targetIsRootNode);
        }

        public ModelNodePath PushElement(object elementValue, ElementType type)
        {
            var result = Clone();
            switch (type)
            {
                case ElementType.Member:
                    if (!(elementValue is string)) throw new ArgumentException("The value must be a string when type is ElementType.Member.");
                    break;
                case ElementType.Target:
                    if (elementValue != null) throw new ArgumentException("The value must be null when type is ElementType.Target.");
                    break;
                case ElementType.Index:
                    if (elementValue == null) throw new ArgumentException("The value must be non-null when type is ElementType.Target.");
                    break;
                default:
                    throw new ArgumentOutOfRangeException("type");
            }
            result.path.Add(new NodePathElement { Type = type, Value = elementValue });
            return result;
        }

        private ModelNodePath Clone(IModelNode newRoot, bool newTargetIsRootNode)
        {
            var clone = new ModelNodePath(newRoot, newTargetIsRootNode);
            clone.path.AddRange(path);
            return clone;
        }
    }
}
