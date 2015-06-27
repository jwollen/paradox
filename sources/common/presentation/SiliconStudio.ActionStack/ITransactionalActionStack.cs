﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

namespace SiliconStudio.ActionStack
{
    /// <summary>
    /// Base interface for a transactional action stack.
    /// </summary>
    /// <remarks>
    /// A transactional action stack is an action stack that generates <see cref="IAggregateActionItem"/> from the action items that are added after a
    /// transaction is started and before it is finished. A transaction can also be cancelled (undone) or discarded instead of creating an aggregate action item.
    /// Multiple transactions can be created at the same time, each transaction that ends will become a single item of the parent transaction in progress.
    /// </remarks>
    public interface ITransactionalActionStack : IActionStack
    {
        /// <summary>
        /// Gets whether a transaction is in progress.
        /// </summary>
        bool TransactionInProgress { get; }

        /// <summary>
        /// Raised when a transaction is started.
        /// </summary>
        event EventHandler<EventArgs> TransactionStarted;

        /// <summary>
        /// Raised when a transaction has ended.
        /// </summary>
        event EventHandler<ActionItemsEventArgs<IActionItem>> TransactionEnded;

        /// <summary>
        /// Raised when a transaction is cancelled.
        /// </summary>
        event EventHandler<ActionItemsEventArgs<IActionItem>> TransactionCancelled;

        /// <summary>
        /// Raised when a transaction is discarded.
        /// </summary>
        event EventHandler<ActionItemsEventArgs<IActionItem>> TransactionDiscarded;

        /// <summary>
        /// Creates a BeginTransaction-EndTransaction subscription.
        /// Use it with a using statement to ensure balanced state integrity.
        /// </summary>
        /// <param name="name">The name given to the transaction at the end.</param>
        /// <returns>An EndTransaction subscription.</returns>
        /// <seealso cref="BeginTransaction"/>
        /// <seealso cref="EndTransaction(string)"/>
        IDisposable BeginEndTransaction(string name);

        /// <summary>
        /// Creates a BeginTransaction-EndTransaction subscription.
        /// Use it with a using statement to ensure balanced state integrity.
        /// </summary>
        /// <param name="getName">A delegate that late-evaluate the name given to the transaction at the end.</param>
        /// <returns>Returns a end transaction subscription.</returns>
        /// <seealso cref="BeginTransaction"/>
        /// <seealso cref="EndTransaction(string)"/>
        IDisposable BeginEndTransaction(Func<string> getName);

        /// <summary>
        /// Creates a BeginTransaction-CancelTransaction transaction subscription.
        /// Use it with a using statement to ensure balanced state integrity.
        /// </summary>
        /// <returns>Returns a cancel transaction subscription.</returns>
        /// <seealso cref="BeginTransaction"/>
        /// <seealso cref="CancelTransaction"/>
        IDisposable BeginCancelTransaction();

        /// <summary>
        /// Creates a BeginTransaction-DiscardTransaction transaction subscription.
        /// Use it with a using statement to ensure balanced state integrity.
        /// </summary>
        /// <returns>Returns a discard transaction subscription.</returns>
        /// <seealso cref="BeginTransaction"/>
        /// <seealso cref="DiscardTransaction"/>
        IDisposable BeginDiscardTransaction();

        /// <summary>
        /// Begins a transaction. <see cref="IActionItem"/> added after a call to BeginTransaction are stored in a temporary transaction stack,
        /// until a call to <see cref="EndTransaction(string)"/>, <see cref="CancelTransaction"/>, or <see cref="DiscardTransaction"/> is done.
        /// </summary>
        void BeginTransaction();

        /// <summary>
        /// Ends a transaction started with <see cref="BeginTransaction"/>.
        /// </summary>
        /// <param name="displayName"></param>
        /// <remarks>Once the transaction is ended, an aggregate action is created with all action items that were added during the transaction. This aggregate is added to the action stack.</remarks>
        void EndTransaction(string displayName);

        /// <summary>
        /// Ends a transaction started with <see cref="BeginTransaction"/>.
        /// </summary>
        /// <param name="displayName"></param>
        /// <param name="aggregateActionItems">A function that will aggregate an enumeration of action items into a single action item.</param>
        /// <remarks>Once the transaction is ended, an aggregate action is created with all action items that were added during the transaction. This aggregate is added to the action stack.</remarks>
        void EndTransaction(string displayName, Func<IReadOnlyCollection<IActionItem>, IActionItem> aggregateActionItems);

        /// <summary>
        /// Cancels a transaction started with <see cref="BeginTransaction"/>. Every action from the cancelled transaction will be undone.
        /// </summary>
        /// <remarks>This method will undo every action item of the transaction and then discard them.</remarks>
        void CancelTransaction();

        /// <summary>
        /// Discard a transaction started with <see cref="BeginTransaction"/>.
        /// </summary>
        /// <remarks>This method will ends the transaction and discard every action item it contains.</remarks>
        void DiscardTransaction();

        /// <summary>
        /// Gets the action items in the current transaction.
        /// </summary>
        /// <returns>The action items in the current transaction.</returns>
        IReadOnlyCollection<IActionItem> GetCurrentTransactions();
}
}