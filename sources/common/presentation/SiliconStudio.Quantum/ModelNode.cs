// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

using SiliconStudio.Quantum.Commands;
using SiliconStudio.Quantum.Contents;

namespace SiliconStudio.Quantum
{
    /// <summary>
    /// This class is the default implementation of the <see cref="IModelNode"/>.
    /// </summary>
    public class ModelNode : IModelNode
    {
        private readonly List<IModelNode> children = new List<IModelNode>();
        private readonly List<INodeCommand> commands = new List<INodeCommand>();
        private IContent content;
        private bool isSealed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ModelNode"/> class.
        /// </summary>
        /// <param name="name">The name of this node.</param>
        /// <param name="content">The content of this node.</param>
        /// <param name="guid">An unique identifier for this node.</param>
        public ModelNode(string name, IContent content, Guid guid)
        {
            if (name == null) throw new ArgumentNullException("name");
            if (content == null) throw new ArgumentNullException("content");
            if (guid == Guid.Empty) throw new ArgumentException(@"The guid must be differ from Guid.Empty.", "content");
            this.content = content;
            Name = name;
            Guid = guid;

            var updatableContent = content as IUpdatableContent;
            if (updatableContent != null)
            {
                updatableContent.RegisterOwner(this);
            }
        }

        /// <inheritdoc/>
        public string Name { get; private set; }

        /// <inheritdoc/>
        public Guid Guid { get; private set; }

        /// <inheritdoc/>
        public virtual IContent Content { get { return content; } set { content = value; } }

        /// <inheritdoc/>
        public virtual IModelNode Parent { get; private set; }

        /// <inheritdoc/>
        public IReadOnlyCollection<IModelNode> Children { get { return children; } }

        /// <inheritdoc/>
        public IReadOnlyCollection<INodeCommand> Commands { get { return commands; } }

        /// <summary>
        /// Gets the flags.
        /// </summary>
        /// <value>
        /// The flags.
        /// </value>
        public ModelNodeFlags Flags { get; set; }

        /// <summary>
        /// Add a child to this node. The node must not have been sealed yet.
        /// </summary>
        /// <param name="child">The child node to add.</param>
        /// <param name="allowIfReference">if set to <c>false</c> throw an exception if <see cref="IContent.Reference"/> is not null.</param>
        public void AddChild(ModelNode child, bool allowIfReference = false)
        {
            if (isSealed)
                throw new InvalidOperationException("Unable to add a child to a ModelNode that has been sealed");

            if (child.Parent != null)
                throw new ArgumentException(@"This node has already been registered to a different parent", "child");

            if (Content.Reference != null && !allowIfReference)
                throw new InvalidOperationException("A ModelNode cannot have children when its content hold a reference.");

            child.Parent = this;
            children.Add(child);
        }

        /// <summary>
        /// Add a command to this node. The node must not have been sealed yet.
        /// </summary>
        /// <param name="command">The node command to add.</param>
        public void AddCommand(INodeCommand command)
        {
            if (isSealed)
                throw new InvalidOperationException("Unable to add a command to a ModelNode that has been sealed");

            commands.Add(command);
        }

        /// <summary>
        /// Remove a command from this node. The node must not have been sealed yet.
        /// </summary>
        /// <param name="command">The node command to remove.</param>
        public void RemoveCommand(INodeCommand command)
        {
            if (isSealed)
                throw new InvalidOperationException("Unable to add a child to a ModelNode that has been sealed");

            commands.Remove(command);
        }

        /// <summary>
        /// Seal the node, indicating its construction is finished and that no more children or commands will be added.
        /// </summary>
        public void Seal()
        {
            isSealed = true;
        }

        /// <inheritdoc/>
        public override string ToString()
        {
            return string.Format("{0}: [{1}]", Name, Content.Value);
        }
    }
}