﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Core.Serialization.Converters;
using SiliconStudio.Paradox.Effects;
using SiliconStudio.Paradox.Graphics.Internals;
using SiliconStudio.Paradox.Shaders;
using SiliconStudio.Paradox.Shaders.Compiler;

namespace SiliconStudio.Paradox.Graphics
{
    [ContentSerializer(typeof(DataContentConverterSerializer<Effect>))]
    public class Effect : ComponentBase
    {
        private readonly GraphicsDevice graphicsDeviceDefault;
        private EffectProgram program;
        private ShaderParameterUpdaterDefinition updaterDefinition;
        private EffectParameterResourceBinding[] resourceBindings;
        private ParameterCollection defaultParameters;
        private EffectReflection reflection;
        private EffectInputSignature inputSignature;

        private readonly EffectBytecode bytecode;
        private const int DefaultParameterCollectionCount = 2;

        private EffectStateBindings effectStateBindings;

        public static readonly ParameterKey<RasterizerState> RasterizerStateKey = ParameterKeys.New<RasterizerState>();
        public static readonly ParameterKey<DepthStencilState> DepthStencilStateKey = ParameterKeys.New<DepthStencilState>();
        public static readonly ParameterKey<BlendState> BlendStateKey = ParameterKeys.New<BlendState>();

        internal ParameterCollection CompilationParameters;
        internal ParameterCollection DefaultCompilationParameters;

        static Effect()
        {
            ConverterContext.RegisterConverter(new EffectConverter());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Effect"/> class.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="bytecode">The bytecode.</param>
        /// <param name="usedParameters">The parameters used to create this shader (from a pdxfx).</param>
        /// <exception cref="System.ArgumentNullException">
        /// device
        /// or
        /// bytecode
        /// </exception>
        public Effect(GraphicsDevice device, EffectBytecode bytecode, ParameterCollection usedParameters = null)
        {
            if (device == null) throw new ArgumentNullException("device");
            if (bytecode == null) throw new ArgumentNullException("bytecode");

            this.graphicsDeviceDefault = device;
            this.bytecode = bytecode;
            Initialize(usedParameters);
        }

        /// <summary>
        /// Gets the input signature of this effect.
        /// </summary>
        /// <value>The input signature.</value>
        public EffectInputSignature InputSignature
        {
            get
            {
                return inputSignature;
            }
        }

        /// <summary>
        /// Gets the bytecode.
        /// </summary>
        /// <value>The bytecode.</value>
        public EffectBytecode Bytecode
        {
            get
            {
                return bytecode;
            }
        }

        public void Apply(bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, applyEffectStates);
        }

        public void Apply(ParameterCollection paramCollection1, bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, paramCollection1, applyEffectStates);
        }

        public void Apply(ParameterCollection paramCollection1, ParameterCollection paramCollection2, bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, paramCollection1, paramCollection2, applyEffectStates);
        }

        public void Apply(ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, paramCollection1, paramCollection2, paramCollection3, applyEffectStates);
        }

        public void Apply(ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, ParameterCollection paramCollection4, bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, paramCollection1, paramCollection2, paramCollection3, paramCollection4, applyEffectStates);
        }

        public void Apply(ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, ParameterCollection paramCollection4, ParameterCollection paramCollection5, bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, paramCollection1, paramCollection2, paramCollection3, paramCollection4, paramCollection5, applyEffectStates);
        }

        public void Apply(ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, ParameterCollection paramCollection4, ParameterCollection paramCollection5, ParameterCollection paramCollection6, bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, paramCollection1, paramCollection2, paramCollection3, paramCollection4, paramCollection5, paramCollection6, applyEffectStates);
        }

        public void Apply(ParameterCollection[] parameterCollections, bool applyEffectStates = false)
        {
            Apply(graphicsDeviceDefault, parameterCollections, applyEffectStates);
        }

        public void UnbindResources()
        {
            UnbindResources(graphicsDeviceDefault);
        }

        internal EffectParameterResourceBinding GetParameterFastUpdater<T>(ParameterKey<T> value)
        {
            for (int i = 0; i < resourceBindings.Length; i++)
            {
                if (resourceBindings[i].Description.Param.Key == value)
                {
                    return resourceBindings[i];
                }
            }

            throw new ArgumentException("Parameter resource binding not found.", "value");
        }

        public void Apply(GraphicsDevice graphicsDevice, bool applyEffectStates)
        {
            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            stageStatus.ParameterCollections[1] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, DefaultParameterCollectionCount);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void Apply(GraphicsDevice graphicsDevice, ParameterCollection paramCollection1, bool applyEffectStates)
        {
            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            stageStatus.ParameterCollections[1] = paramCollection1;
            stageStatus.ParameterCollections[2] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, DefaultParameterCollectionCount + 1);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void Apply(GraphicsDevice graphicsDevice, ParameterCollection paramCollection1, ParameterCollection paramCollection2, bool applyEffectStates)
        {
            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            stageStatus.ParameterCollections[1] = paramCollection1;
            stageStatus.ParameterCollections[2] = paramCollection2;
            stageStatus.ParameterCollections[3] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, DefaultParameterCollectionCount + 2);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void Apply(GraphicsDevice graphicsDevice, ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, bool applyEffectStates)
        {
            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            stageStatus.ParameterCollections[1] = paramCollection1;
            stageStatus.ParameterCollections[2] = paramCollection2;
            stageStatus.ParameterCollections[3] = paramCollection3;
            stageStatus.ParameterCollections[4] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, DefaultParameterCollectionCount + 3);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void Apply(GraphicsDevice graphicsDevice, ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, ParameterCollection paramCollection4, bool applyEffectStates)
        {
            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            stageStatus.ParameterCollections[1] = paramCollection1;
            stageStatus.ParameterCollections[2] = paramCollection2;
            stageStatus.ParameterCollections[3] = paramCollection3;
            stageStatus.ParameterCollections[4] = paramCollection4;
            stageStatus.ParameterCollections[5] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, DefaultParameterCollectionCount + 4);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void Apply(GraphicsDevice graphicsDevice, ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, ParameterCollection paramCollection4, ParameterCollection paramCollection5, bool applyEffectStates)
        {
            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            stageStatus.ParameterCollections[1] = paramCollection1;
            stageStatus.ParameterCollections[2] = paramCollection2;
            stageStatus.ParameterCollections[3] = paramCollection3;
            stageStatus.ParameterCollections[4] = paramCollection4;
            stageStatus.ParameterCollections[5] = paramCollection5;
            stageStatus.ParameterCollections[6] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, DefaultParameterCollectionCount + 5);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void Apply(GraphicsDevice graphicsDevice, ParameterCollection paramCollection1, ParameterCollection paramCollection2, ParameterCollection paramCollection3, ParameterCollection paramCollection4, ParameterCollection paramCollection5, ParameterCollection paramCollection6, bool applyEffectStates)
        {
            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            stageStatus.ParameterCollections[1] = paramCollection1;
            stageStatus.ParameterCollections[2] = paramCollection2;
            stageStatus.ParameterCollections[3] = paramCollection3;
            stageStatus.ParameterCollections[4] = paramCollection4;
            stageStatus.ParameterCollections[5] = paramCollection5;
            stageStatus.ParameterCollections[6] = paramCollection6;
            stageStatus.ParameterCollections[7] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, DefaultParameterCollectionCount + 6);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void Apply<TList>(GraphicsDevice graphicsDevice, TList parameterCollections, bool applyEffectStates) where TList : class, IEnumerable<ParameterCollection>
        {
            if (parameterCollections == null) throw new ArgumentNullException("parameterCollections");

            PrepareApply(graphicsDevice);
            var stageStatus = graphicsDevice.StageStatus;

            stageStatus.ParameterCollections[0] = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            int i = DefaultParameterCollectionCount - 1;
            foreach (var parameterCollection in parameterCollections)
            {
                if (i >= stageStatus.ParameterCollections.Length)
                {
                    throw new ArgumentException(string.Format("Exceeding limit of number of parameter collections [{0}]", stageStatus.ParameterCollections.Length - DefaultParameterCollectionCount));
                }
                stageStatus.ParameterCollections[i] = parameterCollection;
                i++;
            }
            stageStatus.ParameterCollections[i] = graphicsDevice.Parameters; // GraphicsDevice.Parameters is overriding all parameters
            stageStatus.UpdateParameters(graphicsDevice, updaterDefinition, i + 1);
            stageStatus.Apply(graphicsDevice, resourceBindings, ref effectStateBindings, applyEffectStates);
        }

        public void UnbindResources(GraphicsDevice graphicsDevice)
        {
            var stageStatus = graphicsDevice.StageStatus;
            stageStatus.UnbindResources(graphicsDevice, resourceBindings);
        }

        public bool HasParameter(ParameterKey parameterKey)
        {
            return defaultParameters.ContainsKey(parameterKey);
        }

        private void PrepareApply(GraphicsDevice graphicsDevice)
        {
            if (graphicsDevice == null) throw new ArgumentNullException("graphicsDevice");

            program.Apply(graphicsDevice);
            graphicsDevice.CurrentEffect = this;
            graphicsDevice.ApplyPlatformSpecificParams(this);
        }

        private void Initialize(ParameterCollection usedParameters)
        {
            program = EffectProgram.New(graphicsDeviceDefault, bytecode);
            reflection = bytecode.Reflection;

            // prepare resource bindings used internally
            resourceBindings = new EffectParameterResourceBinding[bytecode.Reflection.ResourceBindings.Count];
            for (int i = 0; i < resourceBindings.Length; i++)
            {
                resourceBindings[i].Description = bytecode.Reflection.ResourceBindings[i];
            }
            defaultParameters = new ParameterCollection();
            inputSignature = program.InputSignature;
            LoadDefaultParameters();

            CompilationParameters = new ParameterCollection();
            DefaultCompilationParameters = new ParameterCollection();
            if (usedParameters != null)
            {
                foreach (var parameter in usedParameters)
                {
                    if (parameter.Key != CompilerParameters.DebugKey && parameter.Key != CompilerParameters.GraphicsPlatformKey && parameter.Key != CompilerParameters.GraphicsProfileKey)
                        CompilationParameters.SetObject(parameter.Key, parameter.Value);
                }
            }

            foreach (var key in CompilationParameters.Keys)
            {
                DefaultCompilationParameters.RegisterParameter(key, false);
            }
        }

        private void LoadDefaultParameters()
        {
            var shaderParameters = defaultParameters; // Default Parameters contains all registered Parameters used effectively by the effect
            var constantBufferKeys = new Dictionary<string, ParameterKey<ParameterConstantBuffer>>();

            // Create parameter bindings
            for (int i = 0; i < resourceBindings.Length; i++)
            {
                // Update binding key
                var key = UpdateResourceBindingKey(ref resourceBindings[i].Description);

                // ConstantBuffers are handled by next loop
                if (resourceBindings[i].Description.Param.Class != EffectParameterClass.ConstantBuffer)
                {
                    shaderParameters.RegisterParameter(key, false);
                }
            }

            // Create constant buffers from descriptions (previously generated from shader reflection)
            foreach (var constantBuffer in reflection.ConstantBuffers)
            {
                var constantBufferMembers = constantBuffer.Members;

                for (int i = 0; i < constantBufferMembers.Length; ++i)
                {
                    // Update binding key
                    var key = UpdateValueBindingKey(ref constantBufferMembers[i]);

                    // Register ParameterKey with this effect and store its index for direct access during rendering
                    shaderParameters.RegisterParameter(key, false);
                }

                // Handle ConstantBuffer. Share the same key ParameterConstantBuffer with all the stages
                var parameterConstantBuffer = new ParameterConstantBuffer(graphicsDeviceDefault, constantBuffer.Name, constantBuffer);
                var constantBufferKey = ParameterKeys.New(parameterConstantBuffer, constantBuffer.Name);
                shaderParameters.RegisterParameter(constantBufferKey, false);

                for (int i = 0; i < resourceBindings.Length; i++)
                {
                    if (resourceBindings[i].Description.Param.Class == EffectParameterClass.ConstantBuffer && resourceBindings[i].Description.Param.Key.Name == constantBuffer.Name)
                    {
                        resourceBindings[i].Description.Param.Key = constantBufferKey;
                    }
                }

                // Update constant buffer mapping (to avoid name clashes)
                constantBufferKeys[constantBuffer.Name] = constantBufferKey;
            }

            UpdateKeyIndices();

            // Once we have finished binding, we can fully prepare them
            graphicsDeviceDefault.StageStatus.PrepareBindings(resourceBindings);
        }

        private ParameterKey UpdateResourceBindingKey(ref EffectParameterResourceData binding)
        {
            var keyName = binding.Param.KeyName;

            switch (binding.Param.Class)
            {
                case EffectParameterClass.Sampler:
                    var newSamplerKey = (ParameterKey<SamplerState>)FindOrCreateResourceKey<SamplerState>(keyName);
                    binding.Param.Key = newSamplerKey;
                    var samplerBinding = reflection.SamplerStates.FirstOrDefault(x => x.KeyName == keyName);
                    if (samplerBinding != null)
                    {
                        samplerBinding.Key = newSamplerKey;
                        var samplerDescription = samplerBinding.Description;
                        defaultParameters.Set(newSamplerKey, SamplerState.New(graphicsDeviceDefault, samplerDescription));
                    }
                    break;
                case EffectParameterClass.ConstantBuffer:
                case EffectParameterClass.TextureBuffer:
                case EffectParameterClass.ShaderResourceView:
                case EffectParameterClass.UnorderedAccessView:
                    switch (binding.Param.Type)
                    {
                        case EffectParameterType.Buffer:
                        case EffectParameterType.ConstantBuffer:
                        case EffectParameterType.TextureBuffer:
                        case EffectParameterType.AppendStructuredBuffer:
                        case EffectParameterType.ByteAddressBuffer:
                        case EffectParameterType.ConsumeStructuredBuffer:
                        case EffectParameterType.StructuredBuffer:
                        case EffectParameterType.RWBuffer:
                        case EffectParameterType.RWStructuredBuffer:
                        case EffectParameterType.RWByteAddressBuffer:
                            binding.Param.Key = FindOrCreateResourceKey<Buffer>(keyName);
                            break;
                        case EffectParameterType.Texture:
                        case EffectParameterType.Texture1D:
                        case EffectParameterType.Texture1DArray:
                        case EffectParameterType.RWTexture1D:
                        case EffectParameterType.RWTexture1DArray:
                        case EffectParameterType.Texture2D:
                        case EffectParameterType.Texture2DArray:
                        case EffectParameterType.Texture2DMultisampled:
                        case EffectParameterType.Texture2DMultisampledArray:
                        case EffectParameterType.RWTexture2D:
                        case EffectParameterType.RWTexture2DArray:
                        case EffectParameterType.TextureCube:
                        case EffectParameterType.TextureCubeArray:
                        case EffectParameterType.RWTexture3D:
                        case EffectParameterType.Texture3D:
                            binding.Param.Key = FindOrCreateResourceKey<Texture>(keyName);
                            break;
                    }
                    break;
            }

            if (binding.Param.Key == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find/generate key [{0}] with unsupported type [{1}/{2}]", binding.Param.KeyName, binding.Param.Class, binding.Param.Type));
            }

            return binding.Param.Key;
        }

        private ParameterKey UpdateValueBindingKey(ref EffectParameterValueData binding)
        {
            switch (binding.Param.Class)
            {
                case EffectParameterClass.Scalar:
                    switch (binding.Param.Type)
                    {
                        case EffectParameterType.Bool:
                            binding.Param.Key = FindOrCreateValueKey<bool>(binding);
                            break;
                        case EffectParameterType.Int:
                            binding.Param.Key = FindOrCreateValueKey<int>(binding);
                            break;
                        case EffectParameterType.UInt:
                            binding.Param.Key = FindOrCreateValueKey<uint>(binding);
                            break;
                        case EffectParameterType.Float:
                            binding.Param.Key = FindOrCreateValueKey<float>(binding);
                            break;
                    }
                    break;
                case EffectParameterClass.Color:
                    {
                        var componentCount = binding.RowCount != 1 ? binding.RowCount : binding.ColumnCount;
                        switch (binding.Param.Type)
                        {
                            case EffectParameterType.Float:
                                binding.Param.Key = componentCount == 4
                                                        ? FindOrCreateValueKey<Color4>(binding)
                                                        : (componentCount == 3 ? (ParameterKey)FindOrCreateValueKey<Color3>(binding) : null);
                                break;
                        }
                    }
                    break;
                case EffectParameterClass.Vector:
                    {
                        var componentCount = binding.RowCount != 1 ? binding.RowCount : binding.ColumnCount;
                        switch (binding.Param.Type)
                        {
                            case EffectParameterType.Bool:
                            case EffectParameterType.Int:
                                binding.Param.Key = componentCount == 4 ? (ParameterKey)FindOrCreateValueKey<Int4>(binding) : (componentCount == 3 ? FindOrCreateValueKey<Int3>(binding) : null);
                                break;
                            case EffectParameterType.UInt:
                                binding.Param.Key = componentCount == 4 ? FindOrCreateValueKey<UInt4>(binding) : null;
                                break;
                            case EffectParameterType.Float:
                                binding.Param.Key = componentCount == 4
                                                        ? FindOrCreateValueKey<Vector4>(binding)
                                                        : (componentCount == 3 ? (ParameterKey)FindOrCreateValueKey<Vector3>(binding) : (componentCount == 2 ? FindOrCreateValueKey<Vector2>(binding) : null));
                                break;
                        }
                    }
                    break;
                case EffectParameterClass.MatrixRows:
                case EffectParameterClass.MatrixColumns:
                    binding.Param.Key = FindOrCreateValueKey<Matrix>(binding);
                    break;
                case EffectParameterClass.Struct:
                    binding.Param.Key = ParameterKeys.FindByName(binding.Param.KeyName);
                    break;
            }

            if (binding.Param.Key == null)
            {
                throw new InvalidOperationException(string.Format("Unable to find/generate key [{0}] with unsupported type [{1}/{2}]", binding.Param.KeyName, binding.Param.Class, binding.Param.Type));
            }

            if (binding.Count > 1)
            {
                // Unspecified array length: guess from shader and set default parameter with array matching shader size
                binding.Param.Key = binding.Param.Key.CloneLength(binding.Count);
            }

            return binding.Param.Key;
        }

        private ParameterKey FindOrCreateResourceKey<T>(string name)
        {
            return ParameterKeys.FindByName(name) ?? ParameterKeys.New<T>(name);
        }

        private ParameterKey FindOrCreateValueKey<T>(EffectParameterValueData binding) where T : struct
        {
            var name = binding.Param.KeyName;
            return ParameterKeys.FindByName(name) ?? (binding.Count > 1 ? (ParameterKey)ParameterKeys.New<T[]>(name) : ParameterKeys.New<T>(name));
        }

        private void UpdateKeyIndices()
        {
            // TODO: For now, rebuild indices after processing
            // This code is ugly (esp. constant buffer one), it needs to be done directly within processing (as soon as new system is adopted)
            var keys = new HashSet<ParameterKey>();

            var allParameterDependencies = new Dictionary<ParameterKey, ParameterDependency>();
            var parameterDependencies = new HashSet<ParameterDependency>();

            // Always add graphics states
            keys.Add(RasterizerStateKey);
            keys.Add(DepthStencilStateKey);
            keys.Add(BlendStateKey);

            // Handle dynamic values
            foreach (var dynamicValue in defaultParameters.DynamicValues)
            {
                allParameterDependencies[dynamicValue.Target] = new ParameterDependency { Destination = dynamicValue.Target, Dynamic = dynamicValue, Sources = dynamicValue.Dependencies };
            }

            // effectPass.DefaultParameters.Keys contains shader requested keys.
            // Compute dependencies and add them in "keys".
            foreach (var key in defaultParameters.Keys)
            {
                UpdateRequiredKeys(keys, allParameterDependencies, key, parameterDependencies);
            }

            // Make sure every key (in "keys") is set in DefaultParameters to have a valid fallback
            foreach (var key in keys)
            {
                defaultParameters.RegisterParameter(key, false);
            }

            updaterDefinition = new ShaderParameterUpdaterDefinition(keys, parameterDependencies);

            // Cache internal values by specified index in EffectPass parameters (since they will be used by a given EffectPass.ParameterUpdater)
            var keyMapping = updaterDefinition.SortedKeys.Select((x, i) => new { x, i }).ToDictionary(k => k.x, k => k.i);
            defaultParameters.SetKeyMapping(keyMapping);

            for (int i = 0; i < resourceBindings.Length; ++i)
            {
                resourceBindings[i].Description.Param.KeyIndex = Array.IndexOf(updaterDefinition.SortedKeys, resourceBindings[i].Description.Param.Key);
            }

            // Update Constant buffers description
            foreach (var internalValue in defaultParameters.InternalValues)
            {
                var cb = internalValue.Value.Object as ParameterConstantBuffer;
                if (cb != null)
                {
                    for (int i = 0; i < cb.ConstantBufferDesc.Members.Length; ++i)
                    {
                        var member = cb.ConstantBufferDesc.Members[i];
                        member.Param.KeyIndex = Array.IndexOf(updaterDefinition.SortedKeys, member.Param.Key);
                        cb.ConstantBufferDesc.Members[i] = member;
                    }
                }
            }

            // Update effect state bindings
            effectStateBindings.RasterizerStateKeyIndex = Array.IndexOf(updaterDefinition.SortedKeys, RasterizerStateKey);
            effectStateBindings.DepthStencilStateKeyIndex = Array.IndexOf(updaterDefinition.SortedKeys, DepthStencilStateKey);
            effectStateBindings.BlendStateKeyIndex = Array.IndexOf(updaterDefinition.SortedKeys, BlendStateKey);
        }

        private void UpdateRequiredKeys(HashSet<ParameterKey> requiredKeys, Dictionary<ParameterKey, ParameterDependency> allDependencies, ParameterKey key, HashSet<ParameterDependency> requiredDependencies)
        {
            if (requiredKeys.Add(key))
            {
                ParameterDependency dependency;
                if (allDependencies.TryGetValue(key, out dependency))
                {
                    requiredDependencies.Add(dependency);
                    foreach (var source in dependency.Sources)
                    {
                        // Add Dependencies (if not already overriden)
                        // This is done only at this level because top-level keys dependencies are supposed to be present.
                        var sourceMetadata = source.Metadatas.OfType<ParameterKeyValueMetadata>().FirstOrDefault();
                        if (sourceMetadata != null
                            && sourceMetadata.DefaultDynamicValue != null
                            && !allDependencies.ContainsKey(source))
                        {
                            allDependencies[source] = new ParameterDependency { Destination = source, Dynamic = sourceMetadata.DefaultDynamicValue, Sources = sourceMetadata.DefaultDynamicValue.Dependencies };
                        }
                        UpdateRequiredKeys(requiredKeys, allDependencies, source, requiredDependencies);
                    }
                }
            }
        }
    }
}