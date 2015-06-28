﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using SharpDiff;
using SiliconStudio.Assets.Visitors;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Assets.Diff
{
    /// <summary>
    /// Class AssetDiff. This class cannot be inherited.
    /// </summary>
    public sealed class AssetDiff
    {
        private readonly static List<DataVisitNode> EmptyNodes = new List<DataVisitNode>();

        private readonly Asset baseAsset;
        private readonly Asset asset1;
        private readonly Asset asset2;
        private readonly NodeEqualityComparer equalityComparer;
        private Diff3Node computed;

        /// <summary>
        /// Initializes a new instance of the <see cref="AssetDiff"/> class.
        /// </summary>
        /// <param name="baseAsset">The base asset.</param>
        /// <param name="asset1">The asset1.</param>
        /// <param name="asset2">The asset2.</param>
        public AssetDiff(Asset baseAsset, Asset asset1, Asset asset2)
        {
            // TODO handle some null values (no asset2....etc.)
            this.baseAsset = baseAsset;
            this.asset1 = asset1;
            this.asset2 = asset2;
            this.equalityComparer = new NodeEqualityComparer(this);
        }

        public Asset BaseAsset
        {
            get
            {
                return baseAsset;
            }
        }

        public Asset Asset1
        {
            get
            {
                return asset1;
            }
        }

        public Asset Asset2
        {
            get
            {
                return asset2;
            }
        }

        public void Reset()
        {
            computed = null;
        }

        public static Diff3Node Compute(Asset baseAsset, Asset asset1, Asset asset2)
        {
            var diff3 = new AssetDiff(baseAsset, asset1, asset2);
            return diff3.Compute();
        }

        /// <summary>
        /// Computes the diff3 between <see cref="BaseAsset" />, <see cref="Asset1" /> and <see cref="Asset2" />.
        /// </summary>
        /// <param name="forceRecompute">if set to <c>true</c> force to recompute the diff.</param>
        /// <returns>The result of the diff. This result is cached so next call will return it directly.</returns>
        public Diff3Node Compute(bool forceRecompute = false)
        {
            if (computed != null && !forceRecompute)
            {
                return computed;
            }

            // If asset implement IDiffResolver, run callback
            if (baseAsset is IDiffResolver)
            {
                ((IDiffResolver)baseAsset).BeforeDiff(baseAsset, asset1, asset2);
            }

            var baseNodes = DataVisitNodeBuilder.Run(TypeDescriptorFactory.Default, baseAsset);
            var asset1Nodes = DataVisitNodeBuilder.Run(TypeDescriptorFactory.Default, asset1);
            var asset2Nodes = DataVisitNodeBuilder.Run(TypeDescriptorFactory.Default, asset2);
            computed =  DiffNode(baseNodes, asset1Nodes, asset2Nodes);
            return computed;
        }

        private Diff3Node DiffNode(DataVisitNode baseNode, DataVisitNode asset1Node, DataVisitNode asset2Node)
        {
            var diff3 = new Diff3Node(baseNode, asset1Node, asset2Node);

            var baseNodeDesc = GetNodeDescription(baseNode);
            var asset1NodeDesc = GetNodeDescription(asset1Node);
            var asset2NodeDesc = GetNodeDescription(asset2Node);

            if (asset1NodeDesc.Type == asset2NodeDesc.Type)
            {
                if (baseNodeDesc.Type == asset1NodeDesc.Type)
                {
                    // If all types are the same, perform a normal diff.
                    return DiffNodeWithUniformType(baseNode, asset1Node, asset2Node);
                }
                else
                {
                    // If base has a different type, but asset1 and asset2 are equal, use them. Otherwise there is a conflict with base.
                    var temp = DiffNodeWithUniformType(asset1Node, asset1Node, asset2Node);
                    diff3.ChangeType = temp.ChangeType == Diff3ChangeType.None ? Diff3ChangeType.MergeFromAsset1And2 : Diff3ChangeType.Conflict;
                    diff3.InstanceType = asset1NodeDesc.Type;
                }
            }
            else if (baseNodeDesc.Type == asset1NodeDesc.Type)
            {
                // If base and asset 1 are equal, use asset 2.
                var temp = DiffNodeWithUniformType(baseNode, asset1Node, asset1Node);
                diff3.ChangeType = temp.ChangeType == Diff3ChangeType.None ? Diff3ChangeType.MergeFromAsset2 : Diff3ChangeType.Conflict;
                diff3.InstanceType = asset2NodeDesc.Type;
            }
            else if (baseNodeDesc.Type == asset2NodeDesc.Type)
            {
                // If base and asset 2 are equal, use asset 1.
                var temp = DiffNodeWithUniformType(baseNode, asset2Node, asset2Node);
                diff3.ChangeType = temp.ChangeType == Diff3ChangeType.None ? Diff3ChangeType.MergeFromAsset1 : Diff3ChangeType.Conflict;
                diff3.InstanceType = asset1NodeDesc.Type;
            }
            else
            {
                // If one asset is unspecified, use the other.
                // If all types are different, there is a type conflict.
                if (asset1Node == null)
                {
                    diff3.ChangeType = Diff3ChangeType.MergeFromAsset2;
                    diff3.InstanceType = asset2NodeDesc.Type;
                }
                else if (asset2Node == null)
                {
                    diff3.ChangeType = Diff3ChangeType.MergeFromAsset1;
                    diff3.InstanceType = asset1NodeDesc.Type;
                }
                else
                {
                    diff3.ChangeType = Diff3ChangeType.ConflictType;
                }
            }

            return diff3;
        }

        private Diff3Node DiffNodeWithUniformType(DataVisitNode baseNode, DataVisitNode asset1Node, DataVisitNode asset2Node)
        {
            var baseNodeDesc = GetNodeDescription(baseNode);
            var asset1NodeDesc = GetNodeDescription(asset1Node);
            var asset2NodeDesc = GetNodeDescription(asset2Node);

            var node = baseNode ?? asset1Node ?? asset2Node;
            var type = baseNodeDesc.Type ?? asset1NodeDesc.Type ?? asset2NodeDesc.Type;

            var diff3 = new Diff3Node(baseNode, asset1Node, asset2Node) { InstanceType = type };

            if (IsComparableType(node.HasMembers, type))
            {
                DiffValue(diff3, ref baseNodeDesc, ref asset1NodeDesc, ref asset2NodeDesc);
            }
            else
            {
                DiffMembers(diff3, baseNode, asset1Node, asset2Node);

                if (DictionaryDescriptor.IsDictionary(type))
                {
                    DiffDictionary(diff3, baseNode, asset1Node, asset2Node);
                }
                else if (CollectionDescriptor.IsCollection(type))
                {
                    DiffCollection(diff3, baseNode, asset1Node, asset2Node);
                }
                else if (type.IsArray)
                {
                    DiffArray(diff3, baseNode, asset1Node, asset2Node);
                }
            }

            return diff3;
        }

        private static bool IsComparableType(bool hasMembers, Type type)
        {
            // A comparable type doesn't have any members, is not a collection or dictionary or array.
            bool isComparableType = !hasMembers && !CollectionDescriptor.IsCollection(type) && !DictionaryDescriptor.IsDictionary(type) && !type.IsArray;
            return isComparableType;
        }

        private static void DiffValue(Diff3Node diff3, ref NodeDescription baseNodeDesc, ref NodeDescription asset1NodeDesc, ref NodeDescription asset2NodeDesc)
        {
            var node = diff3.Asset1Node ?? diff3.Asset2Node ?? diff3.BaseNode;
            var dataVisitMember = node as DataVisitMember;
            if (dataVisitMember != null)
            {
                var specificAssetAttribute = dataVisitMember.MemberDescriptor.GetCustomAttributes<DiffUseSpecificAssetAttribute>(true).FirstOrDefault();
                if (specificAssetAttribute != null)
                {
                    if (specificAssetAttribute is DiffUseAsset1Attribute)
                        diff3.ChangeType = Diff3ChangeType.MergeFromAsset1;
                    else if (specificAssetAttribute is DiffUseAsset2Attribute)
                        diff3.ChangeType = Diff3ChangeType.MergeFromAsset2;
                    else
                        throw new InvalidOperationException();
                    return;
                }
            }

            var baseAsset1Equals = Equals(baseNodeDesc.Instance, asset1NodeDesc.Instance);
            var baseAsset2Equals = Equals(baseNodeDesc.Instance, asset2NodeDesc.Instance);
            var asset1And2Equals = Equals(asset1NodeDesc.Instance, asset2NodeDesc.Instance);

            diff3.ChangeType = baseAsset1Equals && baseAsset2Equals
                ? Diff3ChangeType.None
                : baseAsset2Equals ? Diff3ChangeType.MergeFromAsset1 : baseAsset1Equals ? Diff3ChangeType.MergeFromAsset2 : asset1And2Equals ? Diff3ChangeType.MergeFromAsset1And2 : Diff3ChangeType.Conflict;
        }

        private void DiffMembers(Diff3Node diff3, DataVisitNode baseNode, DataVisitNode asset1Node, DataVisitNode asset2Node)
        {
            var baseMembers = baseNode != null ? baseNode.Members : null;
            var asset1Members = asset1Node != null ? asset1Node.Members : null;
            var asset2Members = asset2Node != null ? asset2Node.Members : null;
            int memberCount = 0;

            if (baseMembers != null) memberCount = baseMembers.Count;
            else if (asset1Members != null) memberCount = asset1Members.Count;
            else if (asset2Members != null) memberCount = asset2Members.Count;

            for (int i = 0; i < memberCount; i++)
            {
                AddMember(diff3, DiffNode(baseMembers == null ? null : baseMembers[i],
                    asset1Members == null ? null : asset1Members[i],
                    asset2Members == null ? null : asset2Members[i]));
            }
        }

        private void DiffCollection(Diff3Node diff3, DataVisitNode baseNode, DataVisitNode asset1Node, DataVisitNode asset2Node)
        {
            diff3.Type = Diff3NodeType.Collection;

            var baseItems = baseNode != null ? baseNode.Items ?? EmptyNodes : EmptyNodes;
            var asset1Items = asset1Node != null ? asset1Node.Items ?? EmptyNodes : EmptyNodes;
            var asset2Items = asset2Node != null ? asset2Node.Items ?? EmptyNodes : EmptyNodes;

            var itemEqualityComparer = equalityComparer;

            var node = diff3.Asset1Node ?? diff3.Asset2Node ?? diff3.BaseNode;

            IEnumerable<Diff3Change> changes;
            bool recurseDiff = false;

            // Find an item in any of the list
            var firstItem = baseItems.FirstOrDefault() ?? asset1Items.FirstOrDefault() ?? asset2Items.FirstOrDefault();

            // If we have a DiffUseAsset1Attribute, list of Asset1Node becomes authoritative.
            var dataVisitMember = node as DataVisitMember;
            var specificAssetAttribute = dataVisitMember != null ? dataVisitMember.MemberDescriptor.GetCustomAttributes<DiffUseSpecificAssetAttribute>(true).FirstOrDefault() : null;
            if (specificAssetAttribute != null)
            {
                var isFromAsset2 = specificAssetAttribute is DiffUseAsset2Attribute;
                var diffChange = isFromAsset2
                    ? new Diff3Change { ChangeType = SharpDiff.Diff3ChangeType.MergeFrom2, From2 = new Span(0, asset2Items.Count - 1) }
                    : new Diff3Change { ChangeType = SharpDiff.Diff3ChangeType.MergeFrom1, From1 = new Span(0, asset1Items.Count - 1) };

                changes = new[] { diffChange };

                // TODO: Try to merge back data of matching nodes
            }
            else if (firstItem != null && typeof(IDiffKey).IsAssignableFrom(firstItem.InstanceType))
            {
                // If item implement IDataDiffKey, we will use that as equality key
                changes = Diff3.Compare(
                    baseItems.Select(x => ((IDiffKey)x.Instance).GetDiffKey()).ToList(),
                    asset1Items.Select(x => ((IDiffKey)x.Instance).GetDiffKey()).ToList(),
                    asset2Items.Select(x => ((IDiffKey)x.Instance).GetDiffKey()).ToList());
                recurseDiff = true;
            }
            else
            {
                // Otherwise, do a full node comparison
                itemEqualityComparer.Reset();
                changes = Diff3.Compare(baseItems, asset1Items, asset2Items, itemEqualityComparer);
            }

            foreach (var change in changes)
            {
                switch (change.ChangeType)
                {
                    case SharpDiff.Diff3ChangeType.Equal:
                        for (int i = 0; i < change.Base.Length; i++)
                        {
                            var diff3Node = recurseDiff
                                ? DiffNode(baseItems[change.Base.From + i], asset1Items[change.From1.From + i], asset2Items[change.From2.From + i])
                                : new Diff3Node(baseItems[change.Base.From + i], asset1Items[change.From1.From + i], asset2Items[change.From2.From + i]) { ChangeType = Diff3ChangeType.None };
                            AddItem(diff3, diff3Node, change.From1.From != 0);
                        }
                        break;

                    case SharpDiff.Diff3ChangeType.MergeFrom1:
                        for (int i = 0; i < change.From1.Length; i++)
                        {
                            var diff3Node = new Diff3Node(null, asset1Items[change.From1.From + i], null) { ChangeType = Diff3ChangeType.MergeFromAsset1 };
                            AddItem(diff3, diff3Node, change.From1.From != 0);
                        }
                        break;

                    case SharpDiff.Diff3ChangeType.MergeFrom2:
                        for (int i = 0; i < change.From2.Length; i++)
                        {
                            var diff3Node = new Diff3Node(null, null, asset2Items[change.From2.From + i]) { ChangeType = Diff3ChangeType.MergeFromAsset2 };
                            AddItem(diff3, diff3Node, true);
                        }
                        break;

                    case SharpDiff.Diff3ChangeType.MergeFrom1And2:
                        for (int i = 0; i < change.From2.Length; i++)
                        {
                            var diff3Node = recurseDiff
                                ? DiffNode(null, asset1Items[change.From1.From + i], asset2Items[change.From2.From + i])
                                : new Diff3Node(null, asset1Items[change.From1.From + i], asset2Items[change.From2.From + i]) { ChangeType = Diff3ChangeType.MergeFromAsset1And2 };
                            AddItem(diff3, diff3Node, change.From1.From != 0);
                        }
                        break;

                    case SharpDiff.Diff3ChangeType.Conflict:
                        int baseIndex = change.Base.IsValid ? change.Base.From : -1;
                        int from1Index = change.From1.IsValid ? change.From1.From : -1;
                        int from2Index = change.From2.IsValid ? change.From2.From : -1;

                        // If there are changes only from 1 or 2 or base.Length == list1.Length == list2.Length, then try to make a diff per item
                        // else output the conflict as a full conflict
                        bool tryResolveConflict = false;
                        if (baseIndex >= 0)
                        {
                            if (from1Index >= 0 && from2Index >= 0)
                            {
                                if ((change.Base.Length == change.From1.Length && change.Base.Length == change.From2.Length)
                                    || (change.From1.Length == change.From2.Length))
                                {
                                    tryResolveConflict = true;
                                }
                            }
                            else if (from1Index >= 0)
                            {
                                tryResolveConflict = change.Base.Length == change.From1.Length;
                            }
                            else if (from2Index >= 0)
                            {
                                tryResolveConflict = change.Base.Length == change.From2.Length;
                            }
                            else
                            {
                                tryResolveConflict = true;
                            }
                        }

                        // Iterate on items
                        while ((baseIndex >= 0 && baseItems.Count > 0) || (from1Index >= 0 && asset1Items.Count > 0) || (from2Index >= 0 && asset2Items.Count > 0))
                        {
                            var baseItem = GetSafeFromList(baseItems, ref baseIndex, ref change.Base);
                            var asset1Item = GetSafeFromList(asset1Items, ref from1Index, ref change.From1);
                            var asset2Item = GetSafeFromList(asset2Items, ref from2Index, ref change.From2);

                            var diff3Node = tryResolveConflict || recurseDiff ? 
                                DiffNode(baseItem, asset1Item, asset2Item) :
                                new Diff3Node(baseItem, asset1Item, asset2Item) { ChangeType = Diff3ChangeType.Conflict };
                            AddItem(diff3, diff3Node, true);
                        }
                        break;
                }
            }

            // Any missing item? (we can detect this only at the end)
            var newItemCount = diff3.Items != null ? diff3.Items.Count : 0;
            if (asset1Items.Count != newItemCount)
            {
                diff3.ChangeType = Diff3ChangeType.Children;
            }
        }

        private static DataVisitNode GetSafeFromList(List<DataVisitNode> nodes, ref int index, ref Span span)
        {
            if (nodes == null || index < 0) return null;
            if (index >= nodes.Count || (span.IsValid && index > span.To))
            {
                index = -1;
                return null;
            }
            var value = nodes[index];
            index++;
            if (index >= nodes.Count) index = -1;
            return value;
        }

        private void DiffDictionary(Diff3Node diff3, DataVisitNode baseNode, DataVisitNode asset1Node, DataVisitNode asset2Node)
        {
            diff3.Type = Diff3NodeType.Dictionary;

            var baseItems = baseNode != null ? baseNode.Items : null;
            var asset1Items = asset1Node != null ? asset1Node.Items : null;
            var asset2Items = asset2Node != null ? asset2Node.Items : null;

            // Build dictionary: key => base, v1, v2
            var keyNodes = new Dictionary<object, Diff3DictionaryItem>();
            Diff3DictionaryItem diff3Item;
            if (baseItems != null)
            {
                foreach (var dataVisitNode in baseItems.OfType<DataVisitDictionaryItem>())
                {
                    keyNodes.Add(dataVisitNode.Key, new Diff3DictionaryItem() { Base = dataVisitNode });
                }
            }
            if (asset1Items != null)
            {
                foreach (var dataVisitNode in asset1Items.OfType<DataVisitDictionaryItem>())
                {
                    keyNodes.TryGetValue(dataVisitNode.Key, out diff3Item);
                    diff3Item.Asset1 = dataVisitNode;
                    keyNodes[dataVisitNode.Key] = diff3Item;
                }
            }
            if (asset2Items != null)
            {
                foreach (var dataVisitNode in asset2Items.OfType<DataVisitDictionaryItem>())
                {
                    keyNodes.TryGetValue(dataVisitNode.Key, out diff3Item);
                    diff3Item.Asset2 = dataVisitNode;
                    keyNodes[dataVisitNode.Key] = diff3Item;
                }
            }

            // Perform merge on dictionary
            foreach (var keyNode in keyNodes)
            {
                var valueNode = keyNode.Value;

                Diff3Node diffValue;

                //  base     v1      v2     action
                //  ----     --      --     ------
                //   a        b       c     Diff(a,b,c)
                //  null      b       c     Diff(null, b, c)
                if (valueNode.Asset1 != null && valueNode.Asset2 != null)
                {
                    diffValue = DiffNode(valueNode.Base, valueNode.Asset1, valueNode.Asset2);
                }
                else if (valueNode.Asset1 == null)
                {
                    //   a       null     c     MergeFrom1 (unchanged)
                    //  null     null     c     MergeFrom2
                    //   a       null    null   MergeFrom1 (unchanged)
                    diffValue = new Diff3Node(valueNode.Base, null, valueNode.Asset2) { ChangeType = valueNode.Base == null ? Diff3ChangeType.MergeFromAsset2 : Diff3ChangeType.MergeFromAsset1 };
                }
                else
                {
                    //   a        a      null   MergeFrom2 (removed)
                    //   a        b      null   Conflict
                    //  null      b      null   MergeFrom1 (unchanged)
                    var changeType = Diff3ChangeType.MergeFromAsset1;
                    if (valueNode.Base != null)
                    {
                        var diffNode = DiffNode(valueNode.Base, valueNode.Asset1, valueNode.Base);
                        changeType = diffNode.FindDifferences().Any()
                            ? Diff3ChangeType.Conflict
                            : Diff3ChangeType.MergeFromAsset2;
                    }

                    diffValue = new Diff3Node(valueNode.Base, valueNode.Asset1, null) { ChangeType = changeType };
                }

                AddItem(diff3, diffValue);
            }
        }

        private void DiffArray(Diff3Node diff3, DataVisitNode baseNode, DataVisitNode asset1Node, DataVisitNode asset2Node)
        {
            var baseItems = baseNode != null ? baseNode.Items : null;
            var asset1Items = asset1Node != null ? asset1Node.Items : null;
            var asset2Items = asset2Node != null ? asset2Node.Items : null;
            int itemCount = -1;

            if (baseItems != null)
            {
                itemCount = baseItems.Count;
            }

            if (asset1Items != null)
            {
                var newLength = asset1Items.Count;
                if (itemCount >= 0 && itemCount != newLength)
                {
                    diff3.ChangeType = Diff3ChangeType.ConflictArraySize;
                    return;
                }
                itemCount = newLength;
            }

            if (asset2Items != null)
            {
                var newLength = asset2Items.Count;
                if (itemCount >= 0 && itemCount != newLength)
                {
                    diff3.ChangeType = Diff3ChangeType.ConflictArraySize;
                    return;
                }
                itemCount = newLength;
            }

            for (int i = 0; i < itemCount; i++)
            {
                AddItem(diff3, DiffNode(baseItems == null ? null : baseItems[i],
                    asset1Items == null ? null : asset1Items[i],
                    asset2Items == null ? null : asset2Items[i]));
            }
        }


        /// <summary>
        /// Adds a member to this instance.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <param name="member">The member.</param>
        /// <exception cref="System.ArgumentNullException">member</exception>
        private static void AddMember(Diff3Node thisObject, Diff3Node member)
        {
            if (member == null) throw new ArgumentNullException("member");
            if (thisObject.Members == null)
                thisObject.Members = new List<Diff3Node>();

            member.Parent = thisObject;
            if (member.ChangeType != Diff3ChangeType.None)
            {
                thisObject.ChangeType = Diff3ChangeType.Children;
            }
            thisObject.Members.Add(member);
        }

        /// <summary>
        /// Adds an item (array, list or dictionary item) to this instance.
        /// </summary>
        /// <param name="thisObject">The this object.</param>
        /// <param name="item">The item.</param>
        /// <exception cref="System.ArgumentNullException">item</exception>
        private static void AddItem(Diff3Node thisObject, Diff3Node item, bool hasChildrenChanged = false)
        {
            if (item == null) throw new ArgumentNullException("item");
            if (thisObject.Items == null)
                thisObject.Items = new List<Diff3Node>();

            item.Parent = thisObject;
            if (item.ChangeType != Diff3ChangeType.None || hasChildrenChanged)
            {
                thisObject.ChangeType = Diff3ChangeType.Children;
            }
            item.Index = thisObject.Items.Count;
            thisObject.Items.Add(item);
        }

        private NodeDescription GetNodeDescription(DataVisitNode node)
        {
            if (node == null)
            {
                return new NodeDescription();
            }

            var instanceType = node.InstanceType;
            if (NullableDescriptor.IsNullable(instanceType))
            {
                instanceType = Nullable.GetUnderlyingType(instanceType);
            }

            return new NodeDescription(node.Instance, instanceType);
        }

        private struct NodeDescription
        {
            public NodeDescription(object instance, Type type)
            {
                Instance = instance;
                Type = type;
            }

            public readonly object Instance;

            public readonly Type Type;
        }

        private struct Diff3DictionaryItem
        {
            public DataVisitDictionaryItem Base;

            public DataVisitDictionaryItem Asset1;

            public DataVisitDictionaryItem Asset2;
        }

        private class NodeEqualityComparer : IEqualityComparer<DataVisitNode>
        {
            private Dictionary<KeyComparison, bool> equalityCache = new Dictionary<KeyComparison, bool>();
            private AssetDiff diffManager;

            public NodeEqualityComparer(AssetDiff diffManager)
            {
                if (diffManager == null) throw new ArgumentNullException("diffManager");
                this.diffManager = diffManager;
            }

            public void Reset()
            {
                equalityCache.Clear();
            }

            public bool Equals(DataVisitNode x, DataVisitNode y)
            {
                var key = new KeyComparison(x, y);
                bool result;
                if (equalityCache.TryGetValue(key, out result))
                {
                    return result;
                }

                var diff3 = diffManager.DiffNode(x, y, x);

                result = !diff3.FindDifferences().Any();
                equalityCache.Add(key, result);
                return result;
            }

            public int GetHashCode(DataVisitNode obj)
            {
                int hashCode = 0;

                foreach (var node in obj.Children(x => true))
                {
                    if (node.HasItems)
                        hashCode = hashCode * 17 + node.Items.Count;
                    else if (node.HasMembers)
                        hashCode = hashCode * 11 + node.Members.Count;
                    else if (IsComparableType(false, node.InstanceType) && node.InstanceType.IsPrimitive && node.Instance != null) // Ignore non-primitive types, to be safe (GetHashCode doesn't do deep comparison)
                        hashCode = hashCode * 13 + node.Instance.GetHashCode();
                }

                return hashCode;
            }

            private struct KeyComparison : IEquatable<KeyComparison>
            {
                public KeyComparison(DataVisitNode node1, DataVisitNode node2)
                {
                    Node1 = node1;
                    Node2 = node2;
                }

                public readonly DataVisitNode Node1;

                public readonly DataVisitNode Node2;


                public bool Equals(KeyComparison other)
                {
                    return ReferenceEquals(Node1, other.Node1) && ReferenceEquals(Node2, other.Node2);
                }

                public override bool Equals(object obj)
                {
                    if (ReferenceEquals(null, obj)) return false;
                    return obj is KeyComparison && Equals((KeyComparison)obj);
                }

                public override int GetHashCode()
                {
                    unchecked
                    {
                        return ((Node1 != null ? Node1.GetHashCode() : 0) * 397) ^ (Node2 != null ? Node2.GetHashCode() : 0);
                    }
                }

                public static bool operator ==(KeyComparison left, KeyComparison right)
                {
                    return left.Equals(right);
                }

                public static bool operator !=(KeyComparison left, KeyComparison right)
                {
                    return !left.Equals(right);
                }
            }
        }
    }
}