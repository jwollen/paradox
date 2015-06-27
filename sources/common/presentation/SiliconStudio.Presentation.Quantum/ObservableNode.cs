﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Dynamic;
using System.Linq;
using System.Windows;
using System.Windows.Input;

using SiliconStudio.Presentation.Collections;
using SiliconStudio.Presentation.Core;
using SiliconStudio.Presentation.ViewModel;
using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Contents;

using Expression = System.Linq.Expressions.Expression;

namespace SiliconStudio.Presentation.Quantum
{
    public abstract class ObservableNode : DispatcherViewModel, IObservableNode, IDynamicMetaObjectProvider
    {
        private readonly AutoUpdatingSortedObservableCollection<IObservableNode> children = new AutoUpdatingSortedObservableCollection<IObservableNode>(new AnonymousComparer<IObservableNode>(CompareChildren));
        private readonly ObservableCollection<INodeCommandWrapper> commands = new ObservableCollection<INodeCommandWrapper>();
        private readonly Dictionary<string, object> associatedData = new Dictionary<string, object>();
        private bool isVisible;
        private bool isReadOnly;
        private string displayName;
        private int visibleChildrenCount;
        
        private List<IObservableNode> initializingChildren = new List<IObservableNode>();

        protected ObservableNode(ObservableViewModel ownerViewModel, object index = null)
            : base(ownerViewModel.ServiceProvider)
        {
            Owner = ownerViewModel;
            Index = index;
            Guid = Guid.NewGuid();
            IsVisible = true;
            IsReadOnly = false;
        }

        /// <summary>
        /// Gets the <see cref="ObservableViewModel"/> that owns this node.
        /// </summary>
        public ObservableViewModel Owner { get; private set; }

        /// <summary>
        /// Gets or sets the name of this node. Note that the name can be used to access this node from its parent using a dynamic object.
        /// </summary>
        public string Name { get; protected set; }

        /// <summary>
        /// Gets or sets the name used to display the node to the user.
        /// </summary>
        public string DisplayName { get { return displayName; } set { SetValue(ref displayName, value); } }

        /// <summary>
        /// Gets the path of this node. The path is constructed from the name of all node from the root to this one, separated by periods.
        /// </summary>
        public string Path { get { return Parent != null ? Parent.Path + '.' + Name : Name; } }

        /// <summary>
        /// Gets the parent of this node.
        /// </summary>
        public IObservableNode Parent { get; private set; }

        /// <summary>
        /// Gets the root of this node.
        /// </summary>
        public IObservableNode Root { get { IObservableNode root = this; while (root.Parent != null) root = root.Parent; return root; } }

        /// <summary>
        /// Gets the expected type of <see cref="Value"/>.
        /// </summary>
        public abstract Type Type { get; }

        /// <summary>
        /// Gets whether this node contains a primitive value. A primitive value has no children node and does not need to refresh its hierarchy when its value is modified.
        /// </summary>
        public abstract bool IsPrimitive { get; }

        /// <summary>
        /// Gets or sets whether this node should be displayed in the view.
        /// </summary>
        public bool IsVisible { get { return isVisible; } set { SetValue(ref isVisible, value, () => { var handler = IsVisibleChanged; if (handler != null) handler(this, EventArgs.Empty); }); } }

        /// <summary>
        /// Gets or sets whether this node can be modified in the view.
        /// </summary>
        public bool IsReadOnly { get { return isReadOnly; } set { SetValue(ref isReadOnly, value); } }

        /// <summary>
        /// Gets or sets the value.
        /// </summary>
        public abstract object Value { get; set; }

        /// <summary>
        /// Gets or sets the index of this node, relative to its parent node when its contains a collection. Can be null of this node is not in a collection.
        /// </summary>
        public object Index { get; private set; }

        /// <summary>
        /// Gets a unique identifier for this observable node.
        /// </summary>
        public Guid Guid { get; private set; }

        /// <summary>
        /// Gets the list of children nodes.
        /// </summary>
        public IReadOnlyCollection<IObservableNode> Children { get { return initializingChildren != null ? (IReadOnlyCollection<IObservableNode>)initializingChildren : children; } }

        /// <summary>
        /// Gets the list of commands available in this node.
        /// </summary>
        public IEnumerable<INodeCommandWrapper> Commands { get { return commands; } }

        /// <summary>
        /// Gets additional data associated to this content. This can be used when the content itself does not contain enough information to be used as a view model.
        /// </summary>
        public IReadOnlyDictionary<string, object> AssociatedData { get { return associatedData; } }

        /// <summary>
        /// Gets the order number of this node in its parent.
        /// </summary>
        public abstract int? Order { get; }

        /// <summary>
        /// Gets whether this node contains a list
        /// </summary>
        public abstract bool HasList { get; }

        /// <summary>
        /// Gets whether this node contains a dictionary
        /// </summary>
        public abstract bool HasDictionary { get; }

        /// <inheritdoc/>
        public int VisibleChildrenCount { get { return visibleChildrenCount; } private set { SetValue(ref visibleChildrenCount, value); } }

        /// <inheritdoc/>
        public event EventHandler<EventArgs> ValueChanged;
        
        /// <inheritdoc/>
        public event EventHandler<EventArgs> IsVisibleChanged;
        
        /// <summary>
        /// Gets or sets the flags associated to this node.
        /// </summary>
        public ViewModelContentFlags Flags { get; set; }

        /// <summary>
        /// Gets or sets the serialization flags associated to this node.
        /// </summary>
        public ViewModelContentSerializeFlags SerializeFlags { get; set; }

        /// <summary>
        /// Gets or sets the state flags associated to this node.
        /// </summary>
        public ViewModelContentState LoadState { get; set; }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: [{1}]", Name, Value);
        }

        public void AddAssociatedData(string key, object value)
        {
            if (initializingChildren == null)
            {
                OnPropertyChanging(key);
            }
            associatedData.Add(key, value);
            if (initializingChildren == null)
            {
                OnPropertyChanged(key);
            }
        }

        public void AddOrUpdateAssociatedData(string key, object value)
        {
            if (initializingChildren == null)
            {
                OnPropertyChanging(key);
            }
            associatedData[key] = value;
            if (initializingChildren == null)
            {
                OnPropertyChanged(key);
            }
        }

        public bool RemoveAssociatedData(string key)
        {
            if (initializingChildren == null)
            {
                OnPropertyChanging(key);
            }
            var result = associatedData.Remove(key);
            if (initializingChildren == null)
            {
                OnPropertyChanged(key);
            }
            return result;
        }

        /// <summary>
        /// Indicates whether this node can be moved.
        /// </summary>
        /// <param name="newParent">The new parent of the node once moved.</param>
        /// <returns><c>true</c> if the node can be moved, <c>fals</c> otherwise.</returns>
        public bool CanMove(IObservableNode newParent)
        {
            if (newParent is CombinedObservableNode)
                return false;

            var parent = newParent;
            while (parent != null)
            {
                if (parent == this)
                    return false;
                parent = parent.Parent;
            }
            return true;
        }

        /// <summary>
        /// Moves the node by setting it a new parent.
        /// </summary>
        /// <param name="newParent">The new parent of the node once moved.</param>
        /// <param name="newName">The new name to give to the node once moved. This will modify its path. If <c>null</c>, it does not modify the name.</param>
        public void Move(IObservableNode newParent, string newName = null)
        {
            if (this is CombinedObservableNode)
                throw new InvalidOperationException("A CombinedObservableNode cannot be moved.");
            if (newParent is CombinedObservableNode)
                throw new ArgumentException("The new parent cannot be a CombinedObservableNode");

            var parent = (ObservableNode)newParent;
            while (parent != null)
            {
                if (parent == this)
                    throw new InvalidOperationException("A node cannot be moved into itself or one of its children.");
                parent = (ObservableNode)parent.Parent;
            }

            if (newParent.Children.Any(x => (newName == null && x.Name == Name) || x.Name == newName))
                throw new InvalidOperationException("Unable to move this node, a node with the same name already exists.");

            if (Parent != null)
            {
                parent = (ObservableNode)Parent;
                parent.RemoveChild(this);
            }

            if (newName != null)
            {
                Name = newName;
            }
            ((ObservableNode)newParent).AddChild(this);
            UpdateCommandPath();
        }
        
        /// <summary>
        /// Returns the child node with the matching name.
        /// </summary>
        /// <param name="name">The name of the <see cref="ObservableNode"/> to look for.</param>
        /// <returns>The corresponding child node, or <c>null</c> if no child with the given name exists.</returns>
        public ObservableNode GetChild(string name)
        {
            return (ObservableNode)Children.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Returns the command with the matching name.
        /// </summary>
        /// <param name="name">The name of the command to look for.</param>
        /// <returns>The corresponding command, or <c>null</c> if no command with the given name exists.</returns>
        public ICommand GetCommand(string name)
        {
            return Commands.FirstOrDefault(x => x.Name == name);
        }

        /// <summary>
        /// Returns the additionnal data with the matching name.
        /// </summary>
        /// <param name="name">The name of the additionnal data to look for.</param>
        /// <returns>The corresponding additionnal data, or <c>null</c> if no data with the given name exists.</returns>
        public object GetAssociatedData(string name)
        {
            return AssociatedData.FirstOrDefault(x => x.Key == name).Value;
        }

        /// <summary>
        /// Returns the child node, the command or the additional data with the matching name.
        /// </summary>
        /// <param name="name">The name of the object to look for.</param>
        /// <returns>The corresponding object, or <c>null</c> if no object with the given name exists.</returns>
        public object GetDynamicObject(string name)
        {
            return GetChild(name) ?? GetCommand(name) ?? GetAssociatedData(name) ?? DependencyProperty.UnsetValue;
        }

        /// <inheritdoc/>
        public DynamicMetaObject GetMetaObject(Expression parameter)
        {
            return new ObservableNodeDynamicMetaObject(parameter, this);
        }

        public void NotifyPropertyChanging(string propertyName)
        {
            OnPropertyChanging(propertyName, ObservableViewModel.HasChildPrefix + propertyName);
        }

        public void NotifyPropertyChanged(string propertyName)
        {
            OnPropertyChanged(propertyName, ObservableViewModel.HasChildPrefix + propertyName);
        }

        protected void FinalizeChildrenInitialization()
        {
            if (initializingChildren != null)
            {
                OnPropertyChanging("Children");
                foreach (var child in initializingChildren)
                {
                    children.Add(child);
                }
                initializingChildren = null;
                OnPropertyChanged("Children");
            }
        }

        protected void AddChild(ObservableNode node)
        {
            if (node == null) throw new ArgumentNullException("node");
            if (node.Parent != null) throw new InvalidOperationException("The node already have a parent.");
            if (Children.Contains(node)) throw new InvalidOperationException("The node is already in the children list of its parent.");
            if (initializingChildren == null)
            {
                NotifyPropertyChanging(node.Name);
            }
            node.Parent = this;

            if (initializingChildren == null)
            {
                children.Add(node);
                NotifyPropertyChanged(node.Name);
            }
            else
            {
                initializingChildren.Add(node);
            }
            if (node.IsVisible)
                ++VisibleChildrenCount;    
            node.IsVisibleChanged += ChildVisibilityChanged;
        }

        protected void RemoveChild(ObservableNode node)
        {
            if (node == null) throw new ArgumentNullException("node");
            if (!Children.Contains(node)) throw new InvalidOperationException("The node is not in the children list of its parent.");

            if (node.IsVisible)
                --VisibleChildrenCount;
            node.IsVisibleChanged -= ChildVisibilityChanged;

            if (initializingChildren == null)
            {
                NotifyPropertyChanging(node.Name);
            }

            node.Parent = null;
            if (initializingChildren == null)
            {
                children.Remove(node);
                NotifyPropertyChanged(node.Name);
            }
            else
            {
                initializingChildren.Remove(node);
            }
        }
        
        protected void AddCommand(INodeCommandWrapper command)
        {
            if (command == null) throw new ArgumentNullException("command");
            OnPropertyChanging(string.Format("{0}{1}", ObservableViewModel.HasCommandPrefix, command.Name));
            OnPropertyChanging(command.Name);
            commands.Add(command);
            OnPropertyChanged(command.Name);
            OnPropertyChanged(string.Format("{0}{1}", ObservableViewModel.HasCommandPrefix, command.Name));
        }

        protected void ClearCommands()
        {
            var commandNames = commands.Select(x => x.Name).ToList();
            foreach (string commandName in commandNames)
            {
                OnPropertyChanging(string.Format("{0}{1}", ObservableViewModel.HasCommandPrefix, commandName));
                OnPropertyChanging(commandName);
            }
            commands.Clear();
            for (int i = commandNames.Count - 1; i >= 0; --i)
            {
                OnPropertyChanged(commandNames[i]);
                OnPropertyChanged(string.Format("{0}{1}", ObservableViewModel.HasCommandPrefix, commandNames[i]));
            }
        }

        protected void OnValueChanged()
        {
            var handler = ValueChanged;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        protected void CheckDynamicMemberConsistency()
        {
            var memberNames = new HashSet<string>();
            foreach (var child in Children)
            {
                if (string.IsNullOrWhiteSpace(child.Name))
                    throw new InvalidOperationException("This node has a child with a null or blank name");

                if (child.Name.Contains('.'))
                    throw new InvalidOperationException(string.Format("This node has a child which contains a period (.) in its name: {0}", child.Name));

                if (memberNames.Contains(child.Name))
                    throw new InvalidOperationException(string.Format("This node contains several members named {0}", child.Name));

                memberNames.Add(child.Name);
            }

            foreach (var command in Commands.OfType<ModelNodeCommandWrapper>())
            {
                if (string.IsNullOrWhiteSpace(command.Name))
                    throw new InvalidOperationException("This node has a command with a null or blank name {0}");

                if (memberNames.Contains(command.Name))
                    throw new InvalidOperationException(string.Format("This node contains several members named {0}", command.Name));

                memberNames.Add(command.Name);
            }

            foreach (var associatedDataKey in AssociatedData.Keys)
            {
                if (string.IsNullOrWhiteSpace(associatedDataKey))
                    throw new InvalidOperationException("This node has associated data with a null or blank name {0}");

                if (memberNames.Contains(associatedDataKey))
                    throw new InvalidOperationException(string.Format("This node contains several members named {0}", associatedDataKey));

                memberNames.Add(associatedDataKey);
            }
        }

        private static bool DebugQuantumPropertyChanges = true;

        protected override void OnPropertyChanging(params string[] propertyNames)
        {
            if (DebugQuantumPropertyChanges && HasPropertyChangingSubscriber)
            {
                foreach (var property in propertyNames)
                {
                    Owner.Logger.Debug(@"Node Property changing: [{0}].{1}", Path, property);
                }
            }
            base.OnPropertyChanging(propertyNames);
        }

        protected override void OnPropertyChanged(params string[] propertyNames)
        {
            if (DebugQuantumPropertyChanges && HasPropertyChangedSubscriber)
            {
                foreach (var property in propertyNames)
                {
                    Owner.Logger.Debug(@"Node Property changed: [{0}].{1}", Path, property);
                }
            }
            base.OnPropertyChanged(propertyNames);
        }
        private void UpdateCommandPath()
        {
            foreach (var commandWrapper in Commands.OfType<NodeCommandWrapperBase>())
            {
                commandWrapper.ObservableNodePath = Path;
            }
            foreach (var child in Children.OfType<ObservableNode>())
            {
                child.UpdateCommandPath();
            }
        }

        private void ChildVisibilityChanged(object sender, EventArgs e)
        {
            var node = (IObservableNode)sender;
            if (node.IsVisible)
                ++VisibleChildrenCount;
            else
                --VisibleChildrenCount;
        }

        private static int CompareChildren(IObservableNode a, IObservableNode b)
        {
            // Order has the best priority for comparison, if set.
            if (a.Order != null && b.Order != null)
                return ((int)a.Order).CompareTo(b.Order);

            // If one has order and not the other one, consider the one with order as more prioritary
            if (a.Order != null)
                return -1;
            if (b.Order != null)
                return 1;

            // Then we use index, if they are set and comparable.
            if (a.Index != null && b.Index != null)
            {
                if (a.Index.GetType() == b.Index.GetType() && a.Index is IComparable)
                {
                    return ((IComparable)a.Index).CompareTo(b.Index);
                }
            }

            // Then we use name, only if both orders are unset.
            if (a.Order == null && b.Order == null)
                return string.Compare(a.Name, b.Name, StringComparison.InvariantCultureIgnoreCase);

            // Otherwise, the first child would be the one who have an order value.
            return a.Order == null ? 1 : -1;
        }
    }
}