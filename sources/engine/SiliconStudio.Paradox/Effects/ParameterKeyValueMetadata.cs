﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core;

namespace SiliconStudio.Paradox.Rendering
{
    public abstract class ParameterKeyValueMetadata : PropertyKeyMetadata
    {
        public abstract object GetDefaultValue();

        public abstract void SetupDefaultValue(ParameterCollection parameterCollection, ParameterKey parameterKey, bool addDependencies);

        public abstract ParameterDynamicValue DefaultDynamicValue { get; }
    }

    /// <summary>
    /// Metadata used for <see cref="ParameterKey"/>
    /// </summary>
    public class ParameterKeyValueMetadata<T> : ParameterKeyValueMetadata
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        /// </summary>
        public ParameterKeyValueMetadata()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKeyValueMetadata"/> class.
        /// </summary>
        /// <param name="setupDelegate">The setup delegate.</param>
        public ParameterKeyValueMetadata(T defaultValue)
        {
            DefaultValue = defaultValue;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterKeyValueMetadataValueMetadata{T}"/> class.
        /// </summary>
        /// <param name="defaultDynamicValue">The default dynamic value.</param>
        public ParameterKeyValueMetadata(ParameterDynamicValue<T> defaultDynamicValue)
        {
            DefaultDynamicValueT = defaultDynamicValue;
        }

        /// <summary>
        /// Gets the default value.
        /// </summary>
        public readonly T DefaultValue;

        protected ParameterDynamicValue<T> DefaultDynamicValueT { get; set; }

        public override ParameterDynamicValue DefaultDynamicValue
        {
            get { return DefaultDynamicValueT; }
        }

        public override object GetDefaultValue()
        {
            return DefaultValue;
        }

        public override void SetupDefaultValue(ParameterCollection parameterCollection, ParameterKey parameterKey, bool addDependencies)
        {
            if (DefaultDynamicValueT != null)
            {
                if (addDependencies)
                {
                    foreach (var dependencyKey in DefaultDynamicValueT.Dependencies)
                        parameterCollection.RegisterParameter(dependencyKey, addDependencies);
                }
                parameterCollection.AddDynamic((ParameterKey<T>)parameterKey, DefaultDynamicValueT);
            }
            else
            {
                parameterCollection.Set((ParameterKey<T>)parameterKey, DefaultValue);
            }
        }
    }
}

