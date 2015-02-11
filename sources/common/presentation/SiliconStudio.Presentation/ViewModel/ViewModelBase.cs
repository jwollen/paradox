﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace SiliconStudio.Presentation.ViewModel
{
    /// <summary>
    /// This abstract class represents a basic view model, implementing <see cref="INotifyPropertyChanging"/> and <see cref="INotifyPropertyChanged"/> and providing
    /// a set of <b>SetValue</b> helper methods to easly update a property and trigger the change notifications.
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanging, INotifyPropertyChanged
    {
#if DEBUG
        private readonly List<string> changingProperties = new List<string>();
#endif

        /// <summary>
        /// An <see cref="IViewModelServiceProvider"/> that allows to retrieve various service objects.
        /// </summary>
        public IViewModelServiceProvider ServiceProvider = ViewModelServiceProvider.NullServiceProvider;

        /// <summary>
        /// A list of couple of property names that are dependent. For each couple of this list, if the first property name is notified as being changed, then
        /// the second property name will also be notified has being changed.
        /// </summary>
        protected readonly Dictionary<string, string[]> DependentProperties = new Dictionary<string, string[]>();

        protected ViewModelBase()
        {
        }

        protected ViewModelBase(IViewModelServiceProvider serviceProvider)
        {
            if (serviceProvider == null) throw new ArgumentNullException("serviceProvider");
            ServiceProvider = serviceProvider;
        }

        /// <summary>
        /// Sets the value of a field to the given value. Both values are compared with the default <see cref="EqualityComparer{T}"/>, and if they are equals,
        /// this method does nothing. If they are different, the <see cref="PropertyChanging"/> event will be raised first, then the field value will be modified,
        /// and finally the <see cref="PropertyChanged"/> event will be raised.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">A reference to the field to set.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">The name of the property that must be notified as changing/changed. Can use <see cref="CallerMemberNameAttribute"/>.</param>
        /// <returns><c>True</c> if the field was modified and events were raised, <c>False</c> if the new value was equal to the old one and nothing was done.</returns>
        protected bool SetValue<T>(ref T field, T value, [CallerMemberName]string propertyName = null)
        {
            return SetValue(ref field, value, null, new[] { propertyName });
        }

        /// <summary>
        /// Sets the value of a field to the given value. Both values are compared with the default <see cref="EqualityComparer{T}"/>, and if they are equals,
        /// this method does nothing. If they are different, the <see cref="PropertyChanging"/> will be raised first, then the field value will be modified,
        /// and finally the <see cref="PropertyChanged"/> event will be raised.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">A reference to the field to set.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyNames">The names of the properties that must be notified as changing/changed. At least one property name must be provided.</param>
        /// <returns><c>True</c> if the field was modified and events were raised, <c>False</c> if the new value was equal to the old one and nothing was done.</returns>
        protected bool SetValue<T>(ref T field, T value, params string[] propertyNames)
        {
            return SetValue(ref field, value, null, propertyNames);
        }

        /// <summary>
        /// Sets the value of a field to the given value. Both values are compared with the default <see cref="EqualityComparer{T}"/>, and if they are equals,
        /// this method does nothing. If they are different, the <see cref="PropertyChanging"/> event will be raised first, then the field value will be modified.
        /// The given update action will be executed and finally the <see cref="PropertyChanged"/> event will be raised.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">A reference to the field to set.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="updateAction">The update action to execute after setting the value. Can be <c>null</c>.</param>
        /// <param name="propertyName">The name of the property that must be notified as changing/changed. Can use <see cref="CallerMemberNameAttribute"/>.</param>
        /// <returns><c>True</c> if the field was modified and events were raised, <c>False</c> if the new value was equal to the old one and nothing was done.</returns>
        protected bool SetValue<T>(ref T field, T value, Action updateAction, [CallerMemberName]string propertyName = null)
        {
            return SetValue(ref field, value, updateAction, new[] { propertyName });
        }

        /// <summary>
        /// Sets the value of a field to the given value. Both values are compared with the default <see cref="EqualityComparer{T}"/>, and if they are equals,
        /// this method does nothing. If they are different, the <see cref="PropertyChanging"/> event will be raised first, then the field value will be modified.
        /// The given update action will be executed and finally the <see cref="PropertyChanged"/> event will be raised.
        /// </summary>
        /// <typeparam name="T">The type of the field.</typeparam>
        /// <param name="field">A reference to the field to set.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="updateAction">The update action to execute after setting the value. Can be <c>null</c>.</param>
        /// <param name="propertyNames">The names of the properties that must be notified as changing/changed. At least one property name must be provided.</param>
        /// <returns><c>True</c> if the field was modified and events were raised, <c>False</c> if the new value was equal to the old one and nothing was done.</returns>
        protected virtual bool SetValue<T>(ref T field, T value, Action updateAction, params string[] propertyNames)
        {
            if (propertyNames.Length == 0)
                throw new ArgumentOutOfRangeException("propertyNames", @"This method must be invoked with at least one property name.");

            if (EqualityComparer<T>.Default.Equals(field, value) == false)
            {
                OnPropertyChanging(propertyNames);
                field = value;
                if (updateAction != null)
                    updateAction();
                OnPropertyChanged(propertyNames);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Manages a property modification and its notifications. This method will invoke the provided update action. The <see cref="PropertyChanging"/>
        /// event will be raised prior to the update action, and the <see cref="PropertyChanged"/> event will be raised after.
        /// </summary>
        /// <param name="updateAction">The update action that will actually manage the update of the property.</param>
        /// <param name="propertyName">The name of the property that must be notified as changing/changed. Can use <see cref="CallerMemberNameAttribute"/>.</param>
        /// <returns>This method always returns<c>True</c> since it always performs the update.</returns>
        protected bool SetValue(Action updateAction, [CallerMemberName]string propertyName = null)
        {
            return SetValue(null, updateAction, new[] { propertyName });
        }

        /// <summary>
        /// Manages a property modification and its notifications. This method will invoke the provided update action. The <see cref="PropertyChanging"/>
        /// event will be raised prior to the update action, and the <see cref="PropertyChanged"/> event will be raised after.
        /// </summary>
        /// <param name="updateAction">The update action that will actually manage the update of the property.</param>
        /// <param name="propertyNames">The names of the properties that must be notified as changing/changed. At least one property name must be provided.</param>
        /// <returns>This method always returns<c>True</c> since it always performs the update.</returns>
        protected bool SetValue(Action updateAction, params string[] propertyNames)
        {
            return SetValue(null, updateAction, propertyNames);
        }

        /// <summary>
        /// Manages a property modification and its notifications. A function is provided to check whether the new value is different from the current one.
        /// This function will be invoked by this method, and if it returns <c>True</c>, it will invoke the provided update action. The <see cref="PropertyChanging"/>
        /// event will be raised prior to the update action, and the <see cref="PropertyChanged"/> event will be raised after.
        /// </summary>
        /// <param name="hasChangedFunction">A function that check if the new value is different and therefore if the update must be actually done.</param>
        /// <param name="updateAction">The update action that will actually manage the update of the property.</param>
        /// <param name="propertyName">The name of the property that must be notified as changing/changed. Can use <see cref="CallerMemberNameAttribute"/>.</param>
        /// <returns><c>True</c> if the update was done and events were raised, <c>False</c> if <see cref="hasChangedFunction"/> is not <c>null</c> and returned false.</returns>
        protected bool SetValue(Func<bool> hasChangedFunction, Action updateAction, [CallerMemberName]string propertyName = null)
        {
            return SetValue(hasChangedFunction, updateAction, new[] { propertyName });
        }

        /// <summary>
        /// Manages a property modification and its notifications. The first parameter <see cref="hasChanged"/> should indicate whether the property
        /// should actuallybe updated. If this parameter is <c>True</c>, it will invoke the provided update action. The <see cref="PropertyChanging"/>
        /// event will be raised prior to the update action, and the <see cref="PropertyChanged"/> event will be raised after.
        /// </summary>
        /// <param name="hasChanged">A boolean that indicates whether the update must be actually done. If <c>null</c>, the update is always done.</param>
        /// <param name="updateAction">The update action that will actually manage the update of the property.</param>
        /// <param name="propertyName">The name of the property that must be notified as changing/changed. Can use <see cref="CallerMemberNameAttribute"/>.</param>
        /// <returns>The value provided in the <see cref="hasChanged"/> argument.</returns>
        protected bool SetValue(bool hasChanged, Action updateAction, [CallerMemberName]string propertyName = null)
        {
            return SetValue(() => hasChanged, updateAction, new[] { propertyName });
        }

        /// <summary>
        /// Manages a property modification and its notifications. The first parameter <see cref="hasChanged"/> should indicate whether the property
        /// should actuallybe updated. If this parameter is <c>True</c>, it will invoke the provided update action. The <see cref="PropertyChanging"/>
        /// event will be raised prior to the update action, and the <see cref="PropertyChanged"/> event will be raised after.
        /// </summary>
        /// <param name="hasChanged">A boolean that indicates whether the update must be actually done. If <c>null</c>, the update is always done.</param>
        /// <param name="updateAction">The update action that will actually manage the update of the property.</param>
        /// <param name="propertyNames">The names of the properties that must be notified as changing/changed. At least one property name must be provided.</param>
        /// <returns>The value provided in the <see cref="hasChanged"/> argument.</returns>
        protected bool SetValue(bool hasChanged, Action updateAction, params string[] propertyNames)
        {
            return SetValue(() => hasChanged, updateAction, propertyNames);
        }

        /// <summary>
        /// Manages a property modification and its notifications. A function is provided to check whether the new value is different from the current one.
        /// This function will be invoked by this method, and if it returns <c>True</c>, it will invoke the provided update action. The <see cref="PropertyChanging"/>
        /// event will be raised prior to the update action, and the <see cref="PropertyChanged"/> event will be raised after.
        /// </summary>
        /// <param name="hasChangedFunction">A function that check if the new value is different and therefore if the update must be actually done.</param>
        /// <param name="updateAction">The update action that will actually manage the update of the property.</param>
        /// <param name="propertyNames">The names of the properties that must be notified as changing/changed. At least one property name must be provided.</param>
        /// <returns><c>True</c> if the update was done and events were raised, <c>False</c> if <see cref="hasChangedFunction"/> is not <c>null</c> and returned false.</returns>
        protected virtual bool SetValue(Func<bool> hasChangedFunction, Action updateAction, params string[] propertyNames)
        {
            if (propertyNames.Length == 0)
                throw new ArgumentOutOfRangeException("propertyNames", @"This method must be invoked with at least one property name.");

            bool hasChanged = true;
            if (hasChangedFunction != null)
            {
                hasChanged = hasChangedFunction();
            }
            if (hasChanged)
            {
                OnPropertyChanging(propertyNames);
                if (updateAction != null)
                    updateAction();
                OnPropertyChanged(propertyNames);
            }
            return hasChanged;
        }

        /// <summary>
        /// This method will raise the <see cref="PropertyChanging"/> for each of the property name passed as argument.
        /// </summary>
        /// <param name="propertyNames">The names of the properties that is changing.</param>
        protected virtual void OnPropertyChanging(params string[] propertyNames)
        {
            var propertyChanging = PropertyChanging;

            foreach (string propertyName in propertyNames)
            {
#if DEBUG
                if (changingProperties.Contains(propertyName))
                    throw new InvalidOperationException(string.Format("OnPropertyChanging called twice for property '{0}' without invoking OnPropertyChanged between calls.", propertyName));

                changingProperties.Add(propertyName);
#endif

                if (propertyChanging != null)
                {
                    propertyChanging(this, new PropertyChangingEventArgs(propertyName));
                }

                string[] dependentProperties;
                if (DependentProperties.TryGetValue(propertyName, out dependentProperties))
                {
                    OnPropertyChanging(dependentProperties);
                }
            }
        }

        /// <summary>
        /// This method will raise the <see cref="PropertyChanged"/> for each of the property name passed as argument.
        /// </summary>
        /// <param name="propertyNames">The names of the properties that has changed.</param>
        protected virtual void OnPropertyChanged(params string[] propertyNames)
        {
            var propertyChanged = PropertyChanged;

            for (int i = 0 ; i < propertyNames.Length; ++i)
            {
                string propertyName = propertyNames[propertyNames.Length - 1 - i];
                string[] dependentProperties;
                if (DependentProperties.TryGetValue(propertyName, out dependentProperties))
                {
                    var reverseList = new string[dependentProperties.Length];
                    for (int j = 0; j < dependentProperties.Length; ++j)
                        reverseList[j] = dependentProperties[dependentProperties.Length - 1 - j];
                    OnPropertyChanged(reverseList);
                }
                if (propertyChanged != null)
                {
                    propertyChanged(this, new PropertyChangedEventArgs(propertyName));
                }

#if DEBUG
                if (!changingProperties.Contains(propertyName))
                    throw new InvalidOperationException(string.Format("OnPropertyChanged called for property '{0}' but OnPropertyChanging was not invoked before.", propertyName));

                changingProperties.Remove(propertyName);
#endif
            }
        }

        /// <inheritdoc/>
        public event PropertyChangingEventHandler PropertyChanging;

        /// <inheritdoc/>
        public event PropertyChangedEventHandler PropertyChanged;
    }
}
