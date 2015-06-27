﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;

namespace SiliconStudio.Core.MicroThreading
{
    /// <summary>
    /// Provides microthread-local storage of data.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class MicroThreadLocal<T>
    {
        private readonly Func<T> valueFactory;
        private readonly Dictionary<MicroThread, T> values = new Dictionary<MicroThread, T>();

        /// <summary>
        /// The value return when we are not in a micro thread. That is the value return when 'Scheduler.CurrentMicroThread==null'
        /// </summary>
        private T valueOutOfMicrothread;

        /// <summary>
        /// Indicate if the value out of micro-thread have been set at least once or not.
        /// </summary>
        private bool valueOutOfMicrothreadSet;

        /// <summary>
        /// Initializes a new instance of the <see cref="MicroThreadLocal{T}"/> class.
        /// </summary>
        public MicroThreadLocal()
            : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="MicroThreadLocal{T}"/> class.
        /// </summary>
        /// <param name="valueFactory">The value factory invoked to create a value when <see cref="Value"/> is retrieved before having been previously initialized.</param>
        public MicroThreadLocal(Func<T> valueFactory)
        {
            this.valueFactory = valueFactory;
        }

        /// <summary>
        /// Gets or sets the value for the current microthread.
        /// </summary>
        /// <value>
        /// The value for the current microthread.
        /// </value>
        public T Value
        {
            get
            {
                T value;
                var microThread = Scheduler.CurrentMicroThread;

                lock (values)
                {
                    if (microThread == null)
                    {
                        if (!valueOutOfMicrothreadSet)
                            valueOutOfMicrothread = valueFactory != null ? valueFactory() : default(T);
                        value = valueOutOfMicrothread;
                    }
                    else if (!values.TryGetValue(microThread, out value))
                        values.Add(microThread, value = valueFactory != null ? valueFactory() : default(T));
                }

                return value;
            }
            set
            {
                var microThread = Scheduler.CurrentMicroThread;

                lock (values)
                {
                    if (microThread == null)
                    {
                        valueOutOfMicrothread = value;
                        valueOutOfMicrothreadSet = true;
                    }
                    else
                    {
                        values[microThread] = value;
                    }
                }
            }
        }

        public bool IsValueCreated
        {
            get
            {
                var microThread = Scheduler.CurrentMicroThread;

                lock (values)
                {
                    if (microThread == null)
                        return valueOutOfMicrothreadSet;

                    return values.ContainsKey(microThread);
                }
            }
        }

        public void ClearValue()
        {
            var microThread = Scheduler.CurrentMicroThread;

            lock (values)
            {
                if (microThread == null)
                {
                    valueOutOfMicrothread = default(T);
                    valueOutOfMicrothreadSet = false;
                }
                else
                {
                    values.Remove(microThread);
                }
            }
        }
    }
}