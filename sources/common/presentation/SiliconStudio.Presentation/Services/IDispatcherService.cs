﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Threading.Tasks;

namespace SiliconStudio.Presentation.Services
{
    /// <summary>
    /// This interface allows to dispatch execution of a portion of code in the thread where it was created, usually the Main thread.
    /// </summary>
    public interface IDispatcherService
    {
        /// <summary>
        /// Executes the given callback in the dispatcher thread. This method will block until the execution of the callback is completed.
        /// </summary>
        /// <param name="callback">The callback to execute in the dispatcher thread.</param>
        void Invoke(Action callback);

        /// <summary>
        /// Executes the given callback in the dispatcher thread. This method will block until the execution of the callback is completed.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the callback.</typeparam>
        /// <param name="callback">The callback to execute in the dispatcher thread.</param>
        /// <returns>The result returned by the executed callback.</returns>
        TResult Invoke<TResult>(Func<TResult> callback);

        /// <summary>
        /// Executes the given callback in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <param name="callback">The callback to execute in the dispatcher thread.</param>
        void BeginInvoke(Action callback);

        /// <summary>
        /// Executes the given asynchronous function in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <param name="callback">The asynchronous function to execute in the dispatcher thread.</param>
        /// <returns>A task corresponding to the asynchronous execution of the given function.</returns>
        Task InvokeAsync(Action callback);

        /// <summary>
        /// Executes the given asynchronous function in the dispatcher thread. This method will run asynchronously and return immediately.
        /// </summary>
        /// <typeparam name="TResult">The type of result returned by the task.</typeparam>
        /// <param name="callback">The asynchronous function to execute in the dispatcher thread.</param>
        /// <returns>A task corresponding to the asynchronous execution of the given task.</returns>
        Task<TResult> InvokeAsync<TResult>(Func<TResult> callback);

        /// <summary>
        /// Verifies that the current thread is the dispatcher thread.
        /// </summary>
        /// <returns><c>True</c> if the current thread is the dispatcher thread, <c>False</c> otherwise.</returns>
        bool CheckAccess();

        /// <summary>
        /// Ensures that the current thread is the dispatcher thread. This method will throw an exception if it is not the case.
        /// </summary>
        void EnsureAccess();
    }
}
