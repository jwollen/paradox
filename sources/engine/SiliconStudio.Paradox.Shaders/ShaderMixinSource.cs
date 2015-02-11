// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Shaders
{
    /// <summary>
    /// A mixin performing a combination of <see cref="ShaderClassSource"/> and other mixins.
    /// </summary>
    [DataContract]
    public sealed class ShaderMixinSource : ShaderSource, IEquatable<ShaderMixinSource>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ShaderMixinSource"/> class.
        /// </summary>
        public ShaderMixinSource()
        {
            Mixins = new List<ShaderClassSource>();
            Compositions = new Core.Collections.SortedList<string, ShaderSource>();
            Macros = new List<ShaderMacro>();
        }

        /// <summary>
        /// Gets or sets the name of this mixin source (if this ShaderMixinSource was generated from a <see cref="ShaderMixinGeneratorSource"/>,
        /// it contains the name of <see cref="ShaderMixinGeneratorSource.Name"/>.
        /// </summary>
        /// <value>The name.</value>
        //public string Name { get; set; }

        /// <summary>
        /// Gets or sets the mixins.
        /// </summary>
        /// <value>The mixins.</value>
        public List<ShaderClassSource> Mixins { get; set; }

        /// <summary>
        /// Gets or sets the macros.
        /// </summary>
        /// <value>The macros.</value>
        public List<ShaderMacro> Macros { get; set; }

        /// <summary>
        /// Gets or sets the compositions.
        /// </summary>
        /// <value>The compositions.</value>
        public Core.Collections.SortedList<string, ShaderSource> Compositions { get; set; }

        /// <summary>
        /// Adds a composition to this mixin.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="shaderSource">The shader source.</param>
        public void AddComposition(string name, ShaderSource shaderSource)
        {
            Compositions[name] = shaderSource;
        }

        /// <summary>
        /// Adds a composition to this mixin.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="shaderSourceElement">The shader source element.</param>
        /// <returns>Returns the index of the composition in the array.</returns>
        public int AddCompositionToArray(string name, ShaderSource shaderSourceElement)
        {
            ShaderSource shaderSource;
            if (!Compositions.TryGetValue(name, out shaderSource))
                Compositions.Add(name, shaderSource = new ShaderArraySource());

            var shaderArraySource = (ShaderArraySource)shaderSource;
            shaderArraySource.Add(shaderSourceElement);
            return shaderArraySource.Values.Count - 1;
        }

        /// <summary>
        /// Adds a macro to this mixin.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <param name="value">The value.</param>
        public void AddMacro(string name, object value)
        {
            Macros.Add(new ShaderMacro(name, value));
        }

        /// <summary>
        /// Clones from the specified <see cref="ShaderMixinSource"/>.
        /// </summary>
        /// <param name="parent">The parent mixin to clone from.</param>
        /// <exception cref="System.ArgumentNullException">parent</exception>
        public void CloneFrom(ShaderMixinSource parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent", string.Format("Cannot clone mixin [{0}] from a null parent"));

            Mixins.AddRange(parent.Mixins);
            Macros.AddRange(parent.Macros);
            foreach (var shaderBasic in parent.Compositions)
            {
                Compositions[shaderBasic.Key] = shaderBasic.Value;
            }
        }

        /// <summary>
        /// Clones from the specified <see cref="ShaderMixinSource"/>. Clones members too.
        /// </summary>
        /// <param name="parent">The parent mixin to clone from.</param>
        /// <exception cref="System.ArgumentNullException">parent</exception>
        public void DeepCloneFrom(ShaderMixinSource parent)
        {
            if (parent == null)
                throw new ArgumentNullException("parent", string.Format("Cannot deep clone mixin [{0}] from a null parent"));

            foreach (var mixin in parent.Mixins)
                Mixins.Add((ShaderClassSource)mixin.Clone());
            Macros.AddRange(parent.Macros);
            foreach (var shaderBasic in parent.Compositions)
            {
                Compositions[shaderBasic.Key] = (ShaderSource)shaderBasic.Value.Clone();
            }
        }

        public override bool Equals(object against)
        {
            if (ReferenceEquals(null, against)) return false;
            if (ReferenceEquals(this, against)) return true;
            if (against.GetType() != this.GetType()) return false;
            return Equals((ShaderMixinSource)against);
        }

        public bool Equals(ShaderMixinSource other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;

            // Doesn't check for Parent for Children
            return Utilities.Compare(Mixins, other.Mixins) && Utilities.Compare(Macros, other.Macros) && Utilities.Compare<string, ShaderSource>(Compositions, other.Compositions);
        }
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = 0;
                hashCode = (hashCode * 397) ^ Utilities.GetHashCode(Mixins);
                hashCode = (hashCode * 397) ^ Utilities.GetHashCode(Macros);
                hashCode = (hashCode * 397) ^ Utilities.GetHashCode(Compositions);
                return hashCode;
            }
        }
        
        public override object Clone()
        {
            var newMixin = (ShaderMixinSource)MemberwiseClone();
            newMixin.Compositions = Compositions == null ? null : ToSortedList(Compositions.Select(x => new KeyValuePair<string, ShaderSource>(x.Key, (ShaderSource)x.Value.Clone())));
            newMixin.Mixins = Mixins == null ? null : Mixins.Select(x => (ShaderClassSource)x.Clone()).ToList();
            newMixin.Macros = Macros == null ? null : new List<ShaderMacro>(Macros.ToArray());
            return newMixin;
        }

        private static Core.Collections.SortedList<TKey, TValue> ToSortedList<TKey, TValue>(IEnumerable<KeyValuePair<TKey, TValue>> list)
        {
            var values = new Core.Collections.SortedList<TKey, TValue>();
            foreach(var item in list)
                values.Add(item.Key, item.Value);
            return values;
        }

        public override string ToString()
        {
            var result = new StringBuilder();

            result.Append("mixin");

            if (Mixins != null && Mixins.Count > 0)
            {
                result.Append(" ");
                for (int i = 0; i < Mixins.Count; i++)
                {
                    if (i > 0)
                        result.Append(", ");
                    result.Append(Mixins[i]);
                }
            }

            if (Compositions != null && Compositions.Count > 0)
            {
                result.Append(" [");
                var keys = Compositions.Keys.ToList();
                keys.Sort();
                for (int i = 0; i < keys.Count; i++)
                {
                    var key = keys[i];
                    if (i > 0)
                        result.Append(", ");
                    result.AppendFormat("{{{0} = {1}}}", key, Compositions[key]);
                }
                result.Append("]");
            }
            return result.ToString();
        }
    }
}