﻿using System;
using System.Collections;
using System.Collections.Specialized;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interactivity;

using SiliconStudio.Presentation.Collections;

namespace SiliconStudio.Presentation.Behaviors
{
    /// <summary>
    /// This static class gives some control on the instances of the <see cref="BindableSelectedItemsBehavior{T}"/>.
    /// </summary>
    public static class BindableSelectedItemsControl
    {
        /// <summary>
        /// Allows to disable <see cref="BindableSelectedItemsBehavior{T}"/> instances during specific view operations.
        /// </summary>
        public static bool DisableBindings;
    }

    /// <summary>
    /// A behavior that allows to bind and synchronize a collection of selected items in a control with an equivalent collection in a view model. In most
    /// control, a collection of selected items is available as a property but is either not a dependency property, nor a read-only dependency property
    /// and thus is not directly bindable. This behavior is abstract and must be implemented for each control, since there is no base class/interface
    /// that contains a selected items collection in the framework.
    /// </summary>
    /// <typeparam name="T">The type of control that is associated with this behavior.</typeparam>
    public abstract class BindableSelectedItemsBehavior<T> : Behavior<T> where T : Control
    {
        private bool updatingCollection;
        
        /// <summary>
        /// Identifies the <see cref="SelectedItems"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedItemsProperty = DependencyProperty.Register("SelectedItems", typeof(ObservableList<object>), typeof(BindableSelectedItemsBehavior<T>), new PropertyMetadata(null, SelectedItemsChanged));

        /// <summary>
        /// Identifies the <see cref="GiveFocusOnSelectionChange"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty GiveFocusOnSelectionChangeProperty = DependencyProperty.Register("GiveFocusOnSelectionChange", typeof(bool), typeof(BindableSelectedItemsBehavior<T>), new PropertyMetadata(true));

        /// <summary>
        /// Gets or sets the view model collection that should be bound to the selected item collection of the control.
        /// </summary>
        /// <remarks>The view model collection must be an instance of the <see cref="ObservableList{Object}"/> class.</remarks>
        public ObservableList<object> SelectedItems { get { return (ObservableList<object>)GetValue(SelectedItemsProperty); } set { SetValue(SelectedItemsProperty, value); } }

        /// <summary>
        /// Gets or sets whether changes in the selected item collection of the view model should give the focus to the control. The focus is not given if the selection is cleared.
        /// </summary>
        public bool GiveFocusOnSelectionChange { get { return (bool)GetValue(GiveFocusOnSelectionChangeProperty); } set { SetValue(GiveFocusOnSelectionChangeProperty, value); } }

        /// <summary>
        /// Represents the collection of selected items in the associated control. This property must be set in an override of the <see cref="OnAttached"/>
        /// method, before invoking the base OnAttached.
        /// </summary>
        protected IList SelectedItemsInAssociatedObject;

        /// <inheritdoc/>
        protected override void OnAttached()
        {
            SanityCheck();

            // This will modify the SelectedItemsInAssociatedObject collection to match the SelectedItems collection.
            if (SelectedItems != null)
            {
                object[] currentlySelectedItems = SelectedItemsInAssociatedObject.Cast<object>().ToArray();
                foreach (var itemToRemove in currentlySelectedItems.Where(x => !SelectedItems.Contains(x)))
                {
                    SelectedItemsInAssociatedObject.Remove(itemToRemove);
                }

                foreach (var itemToAdd in SelectedItems.Where(x => !currentlySelectedItems.Contains(x)))
                {
                    SelectedItemsInAssociatedObject.Add(itemToAdd);
                }
            }

            base.OnAttached();
        }

        /// <summary>
        /// Scrolls the items control to make the given item visible. This method should be overriden in implementations of this behavior.
        /// </summary>
        /// <param name="dataItem">The item to include</param>
        protected abstract void ScrollIntoView(object dataItem);
        
        /// <summary>
        /// Notifies that the collection of selected items has changed in the control. Updates the collection of selected items in the view model.
        /// This method must be invoked by the implementations of the <see cref="BindableSelectedItemsBehavior{T}"/>.
        /// </summary>
        /// <param name="addedItems">The list of items that have been added to the selection.</param>
        /// <param name="removedItems">The list of items that have been removed from the selection.</param>
        protected void ControlSelectionChanged(IEnumerable addedItems, IList removedItems)
        {
            if (BindableSelectedItemsControl.DisableBindings)
                return;

            if (SelectedItems != null)
            {
                updatingCollection = true;
                if (removedItems != null)
                {
                    // Optimize removal if most of the selected items are removed
                    if (removedItems.Count > SelectedItems.Count / 2)
                    {
                        var remainingItems = SelectedItems.Where(x => !removedItems.Contains(x)).ToList();
                        SelectedItems.Clear();
                        SelectedItems.AddRange(remainingItems);
                    }
                    else
                    {
                        foreach (var removedItem in removedItems.Cast<object>())
                        {
                            SelectedItems.Remove(removedItem);
                        }
                    }
                }

                if (addedItems != null)
                {
                    SelectedItems.AddRange(addedItems.Cast<object>().Where(x => !SelectedItems.Contains(x)));
                }
                updatingCollection = false;
            }
        }

        protected void ControlSelectionCleared()
        {
            if (BindableSelectedItemsControl.DisableBindings)
                return;

            if (SelectedItems != null)
            {
                updatingCollection = true;
                SelectedItems.Clear();
                updatingCollection = false;
            }
        }

        /// <summary>
        /// Ensures that the <see cref="SelectedItemsInAssociatedObject"/> collection has been set.
        /// </summary>
        private void SanityCheck()
        {
            if (AssociatedObject != null && SelectedItemsInAssociatedObject == null)
                throw new InvalidOperationException("SelectedItemsInAssociatedObject not set in the behavior. This field must be set in OnAttached method before calling base.OnAttached() and unset in OnDetaching.");
        }

        /// <summary>
        /// Raised when the <see cref="SelectedItems"/> property changes. Properly handles
        /// the <see cref="INotifyCollectionChanged.CollectionChanged"/> event of the bound collection.
        /// </summary>
        /// <param name="d">The sender of the event (this behavior).</param>
        /// <param name="e">The arguments of the event.</param>
        private static void SelectedItemsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behavior = (BindableSelectedItemsBehavior<T>)d;

            behavior.SanityCheck();

            if (e.OldValue != null)
            {
                var oldList = (ObservableList<object>)e.OldValue;
                oldList.CollectionChanged -= behavior.CollectionSelectionChanged;
            }

            if (e.NewValue != null)
            {
                var newList = (ObservableList<object>)e.NewValue;
                newList.CollectionChanged += behavior.CollectionSelectionChanged;
                if (behavior.AssociatedObject != null)
                {
                    object[] currentlySelectedItems = behavior.SelectedItemsInAssociatedObject.Cast<object>().ToArray();
                    foreach (var currentlySelectedItem in currentlySelectedItems.Where(x => !newList.Contains(x)).ToList())
                    {
                        behavior.SelectedItemsInAssociatedObject.Remove(currentlySelectedItem);
                    }

                    foreach (var newlySelectedItem in newList.Where(x => !behavior.SelectedItemsInAssociatedObject.Contains(x)).ToList())
                    {
                        behavior.SelectedItemsInAssociatedObject.Add(newlySelectedItem);
                    }
                }
            }
        }

        /// <summary>
        /// Raised when the selection changes in the view model - updates the selection in the control.
        /// </summary>
        /// <param name="sender">The sender of the event.</param>
        /// <param name="e">The arguments of the event.</param>
        private void CollectionSelectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            SanityCheck();

            if (BindableSelectedItemsControl.DisableBindings)
                return;

            if (updatingCollection)
                return;

            if (AssociatedObject != null)
            {
                if (e.Action == NotifyCollectionChangedAction.Reset)
                {
                    SelectedItemsInAssociatedObject.Clear();
                }
                
                if (e.NewItems != null)
                {
                    object itemToScrollIntoView = null;

                    foreach (var addedItem in e.NewItems.Cast<object>().Where(x => !SelectedItemsInAssociatedObject.Contains(x)))
                    {
                        SelectedItemsInAssociatedObject.Add(addedItem);
                        itemToScrollIntoView = addedItem;
                    }

                    if (itemToScrollIntoView != null)
                        ScrollIntoView(itemToScrollIntoView);
                }

                if (e.OldItems != null)
                {
                    foreach (var removedItem in e.OldItems.Cast<object>().Where(x => SelectedItemsInAssociatedObject.Contains(x)))
                        SelectedItemsInAssociatedObject.Remove(removedItem);
                }

                if (SelectedItemsInAssociatedObject.Count > 0 && GiveFocusOnSelectionChange)
                {
                    AssociatedObject.Focus();
                }
            }
        }
    }
}
