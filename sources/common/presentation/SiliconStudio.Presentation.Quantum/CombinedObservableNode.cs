﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;

using SiliconStudio.Core.Extensions;
using SiliconStudio.Presentation.Commands;
using SiliconStudio.Quantum;

namespace SiliconStudio.Presentation.Quantum
{
    public abstract class CombinedObservableNode : ObservableNode
    {
        private readonly List<SingleObservableNode> combinedNodes;
        private readonly List<object> combinedNodeInitialValues;
        private readonly HashSet<object> distinctCombinedNodeInitialValues;
        private readonly int? order;

        protected static readonly HashSet<CombinedObservableNode> ChangedNodes = new HashSet<CombinedObservableNode>();
        protected static bool ChangeInProgress;

        protected CombinedObservableNode(ObservableViewModel ownerViewModel, string name, IEnumerable<SingleObservableNode> combinedNodes, object index)
            : base(ownerViewModel, index)
        {
            this.combinedNodes = new List<SingleObservableNode>(combinedNodes);
            Name = name;
            DisplayName = this.combinedNodes.First().DisplayName;

            combinedNodeInitialValues = new List<object>();
            distinctCombinedNodeInitialValues = new HashSet<object>();

            bool isReadOnly = false;
            bool isVisible = false;
            bool nullOrder = false;

            foreach (var node in this.combinedNodes)
            {
                if (node.IsReadOnly)
                    isReadOnly = true;

                if (node.IsVisible)
                    isVisible = true;

                if (node.Order == null)
                    nullOrder = true;

                if (order == node.Order || (!nullOrder && order == null))
                    order = node.Order;

                combinedNodeInitialValues.Add(node.Value);
                distinctCombinedNodeInitialValues.Add(node.Value);
                node.PropertyChanged += NodePropertyChanged;
            }
            IsReadOnly = isReadOnly;
            IsVisible = isVisible;

            ResetInitialValues = new AnonymousCommand(ServiceProvider, () => { Owner.BeginCombinedAction(); CombinedNodes.Zip(combinedNodeInitialValues).ForEach(x => x.Item1.Value = x.Item2); Refresh(); Owner.EndCombinedAction(Owner.FormatCombinedUpdateMessage(this, null), Path, null); });
        }

        internal void Initialize()
        {
            var commandGroups = new Dictionary<string, List<ModelNodeCommandWrapper>>();
            foreach (var node in combinedNodes)
            {
                foreach (var command in node.Commands)
                {
                    var list = commandGroups.GetOrCreateValue(command.Name);
                    list.Add((ModelNodeCommandWrapper)command);
                }
            }

            foreach (var commandGroup in commandGroups)
            {
                var mode = commandGroup.Value.First().CombineMode;
                if (commandGroup.Value.Any(x => x.CombineMode != mode))
                    throw new InvalidOperationException(string.Format("Inconsistent combine mode among command {0}", commandGroup.Key));

                var shouldCombine = mode != CombineMode.DoNotCombine && (mode == CombineMode.AlwaysCombine || commandGroup.Value.Count == combinedNodes.Count);

                if (shouldCombine)
                {
                    var command = new CombinedNodeCommandWrapper(ServiceProvider, commandGroup.Key, Path, Owner.Identifier, commandGroup.Value);
                    AddCommand(command);
                }
            }

            if (!HasList || HasDictionary)
            {
                var commonChildren = GetCommonChildren();
                GenerateChildren(commonChildren);
            }
            else
            {
                var allChildren = GetAllChildrenByValue();
                if (allChildren != null)
                {
                    // TODO: Disable list children for now - they need to be improved a lot (resulting combinaison is very random, especially for list of ints
                    //GenerateListChildren(allChildren);
                }
            }
            foreach (var key in AssociatedData.Keys.ToList())
            {
                RemoveAssociatedData(key);
            }

            // TODO: we add associatedData added to SingleObservableNode this way, but it's a bit dangerous. Maybe we should check that all combined nodes have this data entry, and all with the same value.
            foreach (var singleData in CombinedNodes.SelectMany(x => x.AssociatedData).Where(x => !AssociatedData.ContainsKey(x.Key)))
            {
                AddAssociatedData(singleData.Key, singleData.Value);
            }

            FinalizeChildrenInitialization();

            CheckDynamicMemberConsistency();
        }

        private void NodePropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (ChangeInProgress && e.PropertyName == "Value")
            {
                ChangedNodes.Add(this);
            }
        }

        internal static CombinedObservableNode Create(ObservableViewModel ownerViewModel, string name, CombinedObservableNode parent, Type contentType, IEnumerable<SingleObservableNode> combinedNodes, object index)
        {
            var node = (CombinedObservableNode)Activator.CreateInstance(typeof(CombinedObservableNode<>).MakeGenericType(contentType), ownerViewModel, name, combinedNodes, index);
            return node;
        }

        /// <inheritdoc/>
        public override sealed bool IsPrimitive { get { return CombinedNodes.All(x => x.IsPrimitive); } }

        public IReadOnlyCollection<SingleObservableNode> CombinedNodes { get { return combinedNodes; } }

        public bool HasMultipleValues { get { return ComputeHasMultipleValues(); } }

        public bool HasMultipleInitialValues { get { return ComputeHasMultipleInitialValues(); } }

        public ICommandBase ResetInitialValues { get; private set; }

        public IEnumerable<object> DistinctInitialValues { get { return distinctCombinedNodeInitialValues; } }

        public override int? Order { get { return order; } }

        /// <inheritdoc/>
        public override sealed bool HasList { get { return CombinedNodes.First().HasList; } }

        /// <inheritdoc/>
        public override sealed bool HasDictionary { get { return CombinedNodes.First().HasDictionary; } }

        public void Refresh()
        {
            if (Parent == null) throw new InvalidOperationException("The node to refresh can be a root node.");

            if (CombinedNodes.Any(x => x != null))
            {
                var parent = (CombinedObservableNode)Parent;
                parent.NotifyPropertyChanging(Name);
                NotifyNodeUpdating();

                if (AreCombinable(CombinedNodes))
                {
                    ClearCommands();

                    foreach (var child in Children.Cast<ObservableNode>().ToList())
                        RemoveChild(child);

                    foreach (var modelNode in CombinedNodes.OfType<ObservableModelNode>())
                        modelNode.ForceSetValue(modelNode.Value);

                    Initialize();
                }

                NotifyNodeUpdated();
                parent.NotifyPropertyChanged(Name);
            }
        }

        public static bool AreCombinable(IEnumerable<SingleObservableNode> nodes, bool ignoreNameConstraint = false)
        {
            bool firstNode = true;

            Type type = null;
            string name = null;
            object index = null;
            foreach (var node in nodes)
            {
                if (firstNode)
                {
                    type = node.Type;
                    name = node.Name;
                    index = node.Index;
                    firstNode = false;
                }
                else
                {
                    if (node.Type != type)
                        return false;
                    if (!ignoreNameConstraint && node.Name != name)
                        return false;
                    if (!Equals(node.Index, index))
                        return false;
                }
            }
            return true;
        }

        protected abstract void NotifyNodeUpdating();

        protected abstract void NotifyNodeUpdated();

        private void GenerateChildren(IEnumerable<KeyValuePair<string, List<SingleObservableNode>>> commonChildren)
        {
            foreach (var children in commonChildren)
            {
                var contentType = children.Value.First().Type;
                var index = children.Value.First().Index;
                CombinedObservableNode child = Create(Owner, children.Key, this, contentType, children.Value, index);
                AddChild(child);
                child.Initialize();
            }
        }

        private void GenerateListChildren(IEnumerable<KeyValuePair<object, List<SingleObservableNode>>> allChildren)
        {
            int currentIndex = 0;
            foreach (var children in allChildren)
            {
                if (!ShouldCombine(children.Value, CombinedNodes.Count, "(ListItem)", true))
                    continue;

                var contentType = children.Value.First().Type;
                var name = string.Format("Item {0}", currentIndex);
                CombinedObservableNode child = Create(Owner, name, this, contentType, children.Value, currentIndex);
                AddChild(child);
                child.Initialize();
                child.DisplayName = name;
                ++currentIndex;
            }
        }

        private IEnumerable<KeyValuePair<string, List<SingleObservableNode>>> GetCommonChildren()
        {
            var allChildNodes = new Dictionary<string, List<SingleObservableNode>>();
            foreach (var singleNode in CombinedNodes)
            {
                foreach (var observableNode in singleNode.Children)
                {
                    var child = (SingleObservableNode)observableNode;
                    var list = allChildNodes.GetOrCreateValue(child.Name);
                    list.Add(child);
                }
            }

            return allChildNodes.Where(x => ShouldCombine(x.Value, CombinedNodes.Count, x.Key));
        }

        private static bool ShouldCombine(List<SingleObservableNode> nodes, int combineCount, string name, bool ignoreNameConstraint = false)
        {
            CombineMode? combineMode = null;

            if (!AreCombinable(nodes, ignoreNameConstraint))
                return false;

            foreach (var node in nodes)
            {
                if (combineMode == null)
                    combineMode = node.CombineMode;

                if (combineMode != node.CombineMode)
                    throw new InvalidOperationException(string.Format("Inconsistent values of CombineMode in single nodes for child '{0}'", name));
            }

            if (combineMode == CombineMode.DoNotCombine)
                return false;

            return combineMode == CombineMode.AlwaysCombine || nodes.Count == combineCount;
        }

        private IEnumerable<KeyValuePair<object, List<SingleObservableNode>>> GetAllChildrenByValue()
        {
            var allChildNodes = new List<KeyValuePair<object, List<SingleObservableNode>>>();
            foreach (var singleNode in CombinedNodes)
            {
                var usedSlots = new List<List<SingleObservableNode>>();
                foreach (var observableNode in singleNode.Children)
                {
                    var child = (SingleObservableNode)observableNode;
                    if (!child.Type.IsValueType && child.Type != typeof(string))
                        return null;

                    var list = allChildNodes.FirstOrDefault(x => Equals(x.Key, child.Value) && !usedSlots.Contains(x.Value)).Value;
                    if (list == null)
                    {
                        list = new List<SingleObservableNode>();
                        allChildNodes.Add(new KeyValuePair<object, List<SingleObservableNode>>(child.Value, list));
                    }
                    list.Add(child);
                    usedSlots.Add(list);
                }
            }

            return allChildNodes;
        }

        private bool ComputeHasMultipleValues()
        {
            if (IsPrimitive)
                return CombinedNodes.Any(x => !Equals(x.Value, CombinedNodes.First().Value));

            return !AreAllValuesOfTheSameType(CombinedNodes.Select(x => x.Value));
        }

        private bool ComputeHasMultipleInitialValues()
        {
            if (IsPrimitive)
                return distinctCombinedNodeInitialValues.Count > 1;

            return !AreAllValuesOfTheSameType(distinctCombinedNodeInitialValues);
        }

        private static bool AreAllValuesOfTheSameType(IEnumerable<object> values)
        {
            bool first = true;
            bool isNull = false;
            Type type = null;

            foreach (var value in values)
            {
                // Check status of the first value
                if (first)
                {
                    first = false;
                    if (value == null)
                        isNull = true;
                    else
                        type = value.GetType();
                    continue;
                }

                // For every other values...
                if (value != null)
                {
                    // Check if it should be null
                    if (isNull)
                        return false;

                    // Check if its type matches
                    if (type != value.GetType())
                        return false;
                }
                else if (!isNull)
                {
                    // Check if it should be non-null
                    return false;
                }
            }
            return true;
        }
    }

    public class CombinedObservableNode<T> : CombinedObservableNode
    {
        public CombinedObservableNode(ObservableViewModel ownerViewModel, string name, IEnumerable<SingleObservableNode> combinedNodes, object index)
            : base(ownerViewModel, name, combinedNodes, index)
        {
            DependentProperties.Add("TypedValue", new[] { "Value" });
        }

        /// <summary>
        /// Gets or sets the value of this node through a correctly typed property, which is more adapted to binding.
        /// </summary>
        public T TypedValue
        {
            get
            {
                return HasMultipleValues ? default(T) : (T)CombinedNodes.First().Value;
            }
            set
            {
                Owner.BeginCombinedAction();
                NotifyNodeUpdating();
                ChangeInProgress = true;
                CombinedNodes.ForEach(x => x.Value = value);
                var changedNodes = ChangedNodes.Where(x => x != this).ToList();
                ChangedNodes.Clear();
                ChangeInProgress = false;
                if (!IsPrimitive)
                {
                    Refresh();
                }
                changedNodes.ForEach(x => x.Refresh());
                NotifyNodeUpdated();
                string displayName = Owner.FormatCombinedUpdateMessage(this, value);
                Owner.EndCombinedAction(displayName, Path, value);
            }
        }

        /// <inheritdoc/>
        public override Type Type { get { return typeof(T); } }

        /// <inheritdoc/>
        public override sealed object Value { get { return TypedValue; } set { TypedValue = (T)value; } }

        // TODO: use DependentProperties property
        protected override void NotifyNodeUpdating()
        {
            OnPropertyChanging("TypedValue", "HasMultipleValues", "IsPrimitive", "HasList", "HasDictionary");
        }

        protected override void NotifyNodeUpdated()
        {
            OnPropertyChanged("TypedValue", "HasMultipleValues", "IsPrimitive", "HasList", "HasDictionary");
            OnValueChanged();
        }
    }
}
