﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

using SiliconStudio.ActionStack;
using SiliconStudio.Core.Reflection;
using SiliconStudio.Presentation.Commands;
using SiliconStudio.Presentation.ViewModel;
using SiliconStudio.Quantum;
using SiliconStudio.Quantum.Commands;

namespace SiliconStudio.Presentation.Quantum
{
    public class ModelNodeCommandWrapper : NodeCommandWrapperBase
    {
        private class ModelNodeToken
        {
            public readonly UndoToken Token;
            public readonly UndoToken AdditionalToken;

            public ModelNodeToken(UndoToken token, UndoToken additionalToken)
            {
                Token = token;
                AdditionalToken = additionalToken;
            }
        }

        public readonly ModelNodePath NodePath;
        protected readonly ModelContainer ModelContainer;
        protected readonly ObservableViewModelService Service;
        protected readonly ObservableViewModelIdentifier Identifier;

        public ModelNodeCommandWrapper(IViewModelServiceProvider serviceProvider, INodeCommand nodeCommand, string observableNodePath, ObservableViewModel owner, ModelNodePath nodePath, IEnumerable<IDirtiableViewModel> dirtiables)
            : base(serviceProvider, dirtiables)
        {
            if (nodeCommand == null) throw new ArgumentNullException("nodeCommand");
            if (owner == null) throw new ArgumentNullException("owner");
            NodePath = nodePath;
            // Note: the owner should not be stored in the command because we want it to be garbage collectable
            Identifier = owner.Identifier;
            ModelContainer = owner.ModelContainer;
            NodeCommand = nodeCommand;
            Service = serviceProvider.Get<ObservableViewModelService>();
            ObservableNodePath = observableNodePath;
        }

        public override string Name { get { return NodeCommand.Name; } }

        public override CombineMode CombineMode { get { return NodeCommand.CombineMode; } }

        public virtual CancellableCommand AdditionalCommand { get; set; }
        
        public INodeCommand NodeCommand { get; private set; }

        protected override UndoToken Redo(object parameter, bool creatingActionItem)
        {
            UndoToken token;
            object index;
            var modelNode = NodePath.GetSourceNode(out index);
            if (modelNode == null)
                throw new InvalidOperationException("Unable to retrieve the node on which to apply the redo operation.");

            var currentValue = modelNode.GetValue(index);
            var newValue = NodeCommand.Invoke(currentValue, parameter, out token);
            modelNode.SetValue(newValue, index);
            Refresh(modelNode, index);

            var additionalToken = new UndoToken();
            if (AdditionalCommand != null)
            {
                additionalToken = AdditionalCommand.ExecuteCommand(null, false);
            }
            return new UndoToken(token.CanUndo, new ModelNodeToken(token, additionalToken));
        }

        protected override void Undo(object parameter, UndoToken token)
        {
            object index;
            var modelNode = NodePath.GetSourceNode(out index);
            if (modelNode == null)
                throw new InvalidOperationException("Unable to retrieve the node on which to apply the undo operation.");

            var modelNodeToken = (ModelNodeToken)token.TokenValue;
            var currentValue = modelNode.GetValue(index);
            var newValue = NodeCommand.Undo(currentValue, modelNodeToken.Token);
            modelNode.SetValue(newValue, index);
            Refresh(modelNode, index);

            if (AdditionalCommand != null)
            {
                AdditionalCommand.UndoCommand(null, modelNodeToken.AdditionalToken);
            }
        }

        /// <summary>
        /// Refreshes the <see cref="ObservableNode"/> corresponding to the given <see cref="IModelNode"/>, if an <see cref="ObservableViewModel"/>
        /// is available in the current.<see cref="IViewModelServiceProvider"/>.
        /// </summary>
        /// <param name="modelNode">The model node to use to fetch a corresponding <see cref="ObservableNode"/>.</param>
        /// <param name="index">The index at which the actual value to update is stored.</param>
        protected virtual void Refresh(IModelNode modelNode, object index)
        {
            if (modelNode == null) throw new ArgumentNullException("modelNode");

            var observableNode = Service.ResolveObservableNode(Identifier, ObservableNodePath) as ObservableModelNode;
            // No node matches this model node
            if (observableNode == null)
                return;

            var newValue = modelNode.GetValue(index);

            observableNode.ForceSetValue(newValue);
            observableNode.Owner.NotifyNodeChanged(observableNode.Path);
        }
    }
}
