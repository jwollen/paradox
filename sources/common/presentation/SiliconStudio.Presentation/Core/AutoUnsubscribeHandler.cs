﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;

namespace SiliconStudio.Presentation.Core
{
    /// <summary>
    /// A class wrapping an event handler that is capable to notify when it wants to unsubscribe the event.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class AutoUnsubscribeHandler<T> where T : EventArgs
    {
        private readonly Action<EventHandler<T>> unsubscribe;
        private readonly Func<object, T, bool> action;

        /// <summary>
        /// Initializes a new instance of the <see cref="AutoUnsubscribeHandler{T}"/> class.
        /// </summary>
        /// <param name="action">The actual event handler. This handler should return a boolean that indicates whether it should unsubscribe to the event after its execution.</param>
        /// <param name="unsubscribe">An action that unsubscribe the handler from the event.</param>
        public AutoUnsubscribeHandler(Func<object, T, bool> action, Action<EventHandler<T>> unsubscribe)
        {
            this.unsubscribe = unsubscribe;
            this.action = action;
        }

        /// <summary>
        /// Retrieves the actual event handler to use for subscription to the event.
        /// </summary>
        /// <param name="instance">The instance of <see cref="AutoUnsubscribeHandler{T}"/>.</param>
        /// <returns>An EventHandler that can subscribe to the event.</returns>
        public static implicit operator EventHandler<T>(AutoUnsubscribeHandler<T> instance)
        {
            return instance.Handler;
        }

        private void Handler(object sender, T e)
        {
            if (action(sender, e))
                unsubscribe(Handler);
        }
    }
}