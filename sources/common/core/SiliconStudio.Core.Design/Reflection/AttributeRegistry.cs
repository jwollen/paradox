﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace SiliconStudio.Core.Reflection
{
    /// <summary>
    /// A default implementation for <see cref="IAttributeRegistry"/>. 
    /// This implementation allows to retrieve default attributes for a member or 
    /// to attach an attribute to a specific type/member.
    /// </summary>
    public class AttributeRegistry : IAttributeRegistry
    {
        private readonly Dictionary<MemberInfoKey, IReadOnlyCollection<Attribute>> cachedAttributes = new Dictionary<MemberInfoKey, IReadOnlyCollection<Attribute>>();
        private readonly Dictionary<MemberInfo, List<Attribute>> registeredAttributes = new Dictionary<MemberInfo, List<Attribute>>();

        /// <summary>
        /// Gets the attributes associated with the specified member.
        /// </summary>
        /// <param name="memberInfo">The reflection member.</param>
        /// <param name="inherit">if set to <c>true</c> includes inherited attributes.</param>
        /// <returns>An enumeration of <see cref="Attribute"/>.</returns>
        public virtual IReadOnlyCollection<Attribute> GetAttributes(MemberInfo memberInfo, bool inherit = true)
        {
            var key = new MemberInfoKey(memberInfo, inherit);

            // Use a cache of attributes
            IReadOnlyCollection<Attribute> attributes;
            lock (cachedAttributes)
            {
                if (cachedAttributes.TryGetValue(key, out attributes))
                {
                    return attributes;
                }

                // Else retrieve all default attributes
                var defaultAttributes = Attribute.GetCustomAttributes(memberInfo, inherit);
                var attributesToCache = defaultAttributes.ToList();

                // And add registered attributes
                List<Attribute> registered;
                if (registeredAttributes.TryGetValue(memberInfo, out registered))
                {
                    attributesToCache.AddRange(registered);
                }

                attributes = attributesToCache.AsReadOnly();

                // Add to the cache
                cachedAttributes.Add(key, attributes);
            }

            return attributes;
        }

        /// <summary>
        /// Registers an attribute for the specified member. Restriction: Attributes registered this way cannot be listed in inherited attributes.
        /// </summary>
        /// <param name="memberInfo">The member information.</param>
        /// <param name="attribute">The attribute.</param>
        public void Register(MemberInfo memberInfo, Attribute attribute)
        {
            lock (cachedAttributes)
            {
                List<Attribute> attributes;
                if (!registeredAttributes.TryGetValue(memberInfo, out attributes))
                {
                    attributes = new List<Attribute>();
                    registeredAttributes.Add(memberInfo, attributes);
                }
                attributes.Add(attribute);

                cachedAttributes.Remove(new MemberInfoKey(memberInfo, true));
                cachedAttributes.Remove(new MemberInfoKey(memberInfo, false));
            }
        }

        private struct MemberInfoKey : IEquatable<MemberInfoKey>
        {
            private readonly MemberInfo memberInfo;

            private readonly bool inherit;

            public MemberInfoKey(MemberInfo memberInfo, bool inherit)
            {
                this.memberInfo = memberInfo;
                this.inherit = inherit;
            }

            public bool Equals(MemberInfoKey other)
            {
                return memberInfo.Equals(other.memberInfo) && inherit.Equals(other.inherit);
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is MemberInfoKey && Equals((MemberInfoKey) obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (memberInfo.GetHashCode()*397) ^ inherit.GetHashCode();
                }
            }
        }
    }
}