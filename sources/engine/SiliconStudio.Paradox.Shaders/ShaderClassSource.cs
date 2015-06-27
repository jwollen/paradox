// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;

using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;

namespace SiliconStudio.Paradox.Shaders
{
    /// <summary>
    /// A shader class used for mixin.
    /// </summary>
    [DataContract("ShaderClassSource")]
    public sealed class ShaderClassSource : ShaderSource, IEquatable<ShaderClassSource>
    {
        /// <summary>
        /// Gets the name of the class.
        /// </summary>
        /// <value>The name of the class.</value>
        public string ClassName { get; set; }

        /// <summary>
        /// Gets the generic parameters.
        /// </summary>
        /// <value>The generic parameters.</value>
        [DefaultValue(null)]
        public string[] GenericArguments { get; set; }

        [DefaultValue(null)]
        public Dictionary<string, string> GenericParametersArguments { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        public ShaderClassSource()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        public ShaderClassSource(string className)
            : this(className, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="genericArguments">The generic parameters.</param>
        public ShaderClassSource(string className, params string[] genericArguments)
        {
            ClassName = className;
            GenericArguments = genericArguments;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderClassSource"/> class.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <param name="genericArguments">The generic parameters.</param>
        public ShaderClassSource(string className, params object[] genericArguments)
        {
            ClassName = className;
            if (genericArguments != null)
            {
                GenericArguments = new string[genericArguments.Length];
                for (int i = 0; i < genericArguments.Length; ++i)
                {
                    var genArg = genericArguments[i];
                    if (genArg is bool)
                        GenericArguments[i] = ((bool)genArg).ToString().ToLower();
                    else
                        GenericArguments[i] = genArg == null ? "null": genArg.ToString();
                }
            }
        }

        /// <summary>
        /// Returns a class name as a <see cref="System.String" /> that represents this instance.
        /// </summary>
        /// <returns>A class name as a <see cref="System.String" /> that represents this instance.</returns>
        public string ToClassName()
        {
            if (GenericArguments == null)
                return ClassName;

            var result = new StringBuilder();
            result.Append(ClassName);
            if (GenericArguments != null && GenericArguments.Length > 0)
            {
                result.Append('<');
                result.Append(string.Join(",", GenericArguments));
                result.Append('>');
            }

            return result.ToString();
        }

        public bool Equals(ShaderClassSource shaderClassSource)
        {
            if (ReferenceEquals(null, shaderClassSource)) return false;
            if (ReferenceEquals(this, shaderClassSource)) return true;
            return string.Equals(ClassName, shaderClassSource.ClassName) && Utilities.Compare(GenericArguments, shaderClassSource.GenericArguments);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ShaderClassSource)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((ClassName != null ? ClassName.GetHashCode() : 0) * 397) ^ Utilities.GetHashCode(GenericArguments);
            }
        }

        public override object Clone()
        {
            return new ShaderClassSource(ClassName, GenericArguments = GenericArguments != null ? GenericArguments.ToArray() : null);
        }
        
        public override string ToString()
        {
            return ToClassName();
        }

        /// <summary>
        /// Performs an implicit conversion from <see cref="System.String"/> to <see cref="ShaderClassSource"/>.
        /// </summary>
        /// <param name="className">Name of the class.</param>
        /// <returns>The result of the conversion.</returns>
        public static implicit operator ShaderClassSource(string className)
        {
            return new ShaderClassSource(className);
        }
    }
}