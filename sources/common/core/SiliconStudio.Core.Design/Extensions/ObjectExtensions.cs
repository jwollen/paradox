﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace SiliconStudio.Core.Extensions
{
    /// <summary>
    /// A static class that provides extension methods on the <see cref="object"/> type.
    /// </summary>
    public static class ObjectExtensions
    {
        /// <summary>
        /// An extension method that checks for nullity before invoking <see cref="object.ToString"/> on a given object and catches exception thrown by this method.
        /// </summary>
        /// <param name="obj">The object.</param>
        /// <returns>The return value of <see cref="object.ToString"/>, or "(null)" if <see ref="obj"/> is null, or (ExceptionInToString)" if <see cref="object.ToString"/> thrown an exception.</returns>
        public static string ToStringSafe(this object obj)
        {
            try
            {
                return obj != null ? obj.ToString() : "(null)";
            }
            catch
            {
                return "(ExceptionInToString)";
            }
        }

        /// <summary>
        /// Returns an <see cref="IEnumerable{T}"/> that contains the given object as its single item.
        /// </summary>
        /// <typeparam name="T">The type argument for the <see cref="IEnumerable{T}"/> to generate</typeparam>
        /// <param name="obj">The object to yield.</param>
        /// <returns>An <see cref="IEnumerable{T}"/> that contains the given object as its single item.</returns>
        /// <remarks>This method uses <b>yield return</b> to return the given object as an enumerable.</remarks>
        public static IEnumerable<T> Yield<T>(this T obj)
        {
            yield return obj;
        }

        /// <summary>
        /// Returns the given object if it is an enumerable. Otherwise, returns an <see cref="IEnumerable{T}"/> that contains the given object as its single item.
        /// </summary>
        /// <typeparam name="T">The type argument for the <see cref="IEnumerable{T}"/> to generate</typeparam>
        /// <param name="obj">The object to convert to an <see cref="IEnumerable{T}"/>.</param>
        /// <returns>the given object if it is an enumerable, an <see cref="IEnumerable{T}"/> that contains the given object as its single item otherwise.</returns>
        /// <remarks>This method uses <see cref="Yield{T}"/> to return the given object as an enumerable.</remarks>
        public static IEnumerable<T> ToEnumerable<T>(this object obj)
        {
            if (obj is IEnumerable<T>)
                return (IEnumerable<T>)obj;

            var enumerable = obj as IEnumerable;
            return enumerable != null ? enumerable.Cast<T>() : Yield((T)obj);
        }

        /// <summary>
        /// This method checks if the given <c>this</c> object is <c>null</c>, and throws a <see cref="ArgumentNullException"/> with the given argument name if so.
        /// It returns the given this object.
        /// </summary>
        /// <typeparam name="T">The type of object to test.</typeparam>
        /// <param name="obj">The object to test.</param>
        /// <param name="argumentName">The name of the argument, in case an <see cref="ArgumentNullException"/> must be thrown.</param>
        /// <returns>The given object.</returns>
        /// <remarks>This method can be used to test for null argument when forwarding members of the object to the <c>base</c> or <c>this</c> constructor.</remarks>
        public static T SafeArgument<T>(this T obj, string argumentName) where T : class
        {
            if (argumentName == null) throw new ArgumentNullException("argumentName");
            if (obj == null) throw new ArgumentNullException(argumentName);
            return obj;
        }
    }
}
