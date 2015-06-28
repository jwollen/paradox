﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGL
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenTK.Graphics;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Extensions;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Paradox.Shaders;
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
using OpenTK.Graphics.ES30;
#if !SILICONSTUDIO_PLATFORM_MONO_MOBILE
using ProgramParameter = OpenTK.Graphics.ES30.GetProgramParameterName;
#endif
#else
using OpenTK.Graphics.OpenGL;
#endif


namespace SiliconStudio.Paradox.Graphics
{
    internal partial class EffectProgram
    {
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
        // The ProgramParameter.ActiveUniformBlocks enum is not defined in OpenTK for OpenGL ES
        public const ProgramParameter PdxActiveUniformBlocks = (ProgramParameter)0x8A36;
#else
        public const ProgramParameter PdxActiveUniformBlocks = ProgramParameter.ActiveUniformBlocks;
#endif

        private LoggerResult reflectionResult = new LoggerResult();

        private readonly EffectBytecode effectBytecode;

        private EffectInputSignature inputSignature;

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
        // Fake cbuffer emulation binding
        internal struct Uniform
        {
            public ActiveUniformType Type;
            public int UniformIndex;
            public int Offset;
            public int Count;
            public int CompareSize;
        }

        internal byte[] BoundUniforms;
        internal List<Uniform> Uniforms = new List<Uniform>();
#endif

        internal struct Texture
        {
            public int TextureUnit;

            public Texture(int textureUnit)
            {
                TextureUnit = textureUnit;
            }
        }
        
        internal List<Texture> Textures = new List<Texture>();

        private EffectProgram(GraphicsDevice device, EffectBytecode bytecode)
            : base(device)
        {
            effectBytecode = bytecode;
            CreateShaders();
        }

        private void CreateShaders()
        {
            using (GraphicsDevice.UseOpenGLCreationContext())
            {
                resourceId = GL.CreateProgram();

                // Attach shaders
                foreach (var shader in effectBytecode.Stages)
                {
                    ShaderType shaderStage;
                    switch (shader.Stage)
                    {
                        case ShaderStage.Vertex:
                            shaderStage = ShaderType.VertexShader;
                            // We can't use VS only, since various attributes might get optimized when linked with a specific PS
                            // Maybe we should unify signature after checking attributes
                            //inputSignature = EffectInputSignature.GetOrCreateLayout(new EffectInputSignature(shader.Id, shader.Data));
                            inputSignature = new EffectInputSignature(shader.Id, shader.Data);
                            break;
                        case ShaderStage.Pixel:
                            shaderStage = ShaderType.FragmentShader;
                            break;
                        default:
                            throw new Exception("Unsupported shader stage");
                            break;
                    }

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                    var shaderSources = BinarySerialization.Read<ShaderLevelBytecode>(shader.Data);
                    var shaderSource = GraphicsDevice.IsOpenGLES2 ? shaderSources.DataES2 : shaderSources.DataES3;
#else
                    var shaderSource = shader.GetDataAsString();
#endif
                    var shaderId = GL.CreateShader(shaderStage);
                    GL.ShaderSource(shaderId, shaderSource);
                    GL.CompileShader(shaderId);

                    int compileStatus;
                    GL.GetShader(shaderId, ShaderParameter.CompileStatus, out compileStatus);
                    if (compileStatus != 1)
                    {
                        var glErrorMessage = GL.GetShaderInfoLog(shaderId);
                        throw new InvalidOperationException("Error while compiling GLSL shader. [{0}]".ToFormat(glErrorMessage));
                    }

                    GL.AttachShader(resourceId, shaderId);
                }

#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                // Mark program as retrievable (necessary for later GL.GetProgramBinary).
                GL.ProgramParameter(resourceId, AssemblyProgramParameterArb.ProgramBinaryRetrievableHint, 1);
#endif

                // Link OpenGL program
                GL.LinkProgram(resourceId);

                // Check link results
                int linkStatus;
                GL.GetProgram(resourceId, ProgramParameter.LinkStatus, out linkStatus);
                if (linkStatus != 1)
                {
                    var infoLog = GL.GetProgramInfoLog(resourceId);
                    throw new InvalidOperationException("Error while linking GLSL shaders.\n" + infoLog);
                }

                if (inputSignature.Attributes.Count == 0) // the shader wasn't analyzed yet
                {
                    // Build attributes list for shader signature
                    int activeAttribCount;
                    GL.GetProgram(resourceId, ProgramParameter.ActiveAttributes, out activeAttribCount);

                    for (int activeAttribIndex = 0; activeAttribIndex < activeAttribCount; ++activeAttribIndex)
                    {
                        int size;
                        ActiveAttribType type;
                        var attribName = GL.GetActiveAttrib(resourceId, activeAttribIndex, out size, out type);
#if SILICONSTUDIO_PLATFORM_ANDROID
                        var attribIndex = GL.GetAttribLocation(resourceId, new StringBuilder(attribName));
#else
                        var attribIndex = GL.GetAttribLocation(resourceId, attribName);
#endif
                        inputSignature.Attributes.Add(attribName, attribIndex);
                    }
                }

                CreateReflection(effectBytecode.Reflection, effectBytecode.Stages[0].Stage); // need to regenerate the Uniforms on OpenGL ES

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                // Allocate a buffer that can cache all the bound parameters
                BoundUniforms = new byte[effectBytecode.Reflection.ConstantBuffers[0].Size];
#endif
            }

            // output the gathered errors
            foreach (var message in reflectionResult.Messages)
                Console.WriteLine(message);
            if (reflectionResult.HasErrors)
                throw new Exception("Exception");
        }

        public EffectInputSignature InputSignature
        {
            get { return inputSignature; }
        }

        /// <inheritdoc/>
        protected internal override bool OnRecreate()
        {
            base.OnRecreate();
            CreateShaders();
            return true;
        }

        /// <inheritdoc/>
        protected override void Destroy()
        {
            using (GraphicsDevice.UseOpenGLCreationContext())
            {
                GL.DeleteProgram(resourceId);
            }

            resourceId = 0;

            base.Destroy();
        }

        public void Apply(GraphicsDevice device)
        {
#if DEBUG
            device.EnsureContextActive();
#endif

            device.effectProgram = this;
            device.BindProgram(ResourceId);
        }

        /// <summary>
        /// Create or updates the reflection for this shader
        /// </summary>
        /// <param name="effectReflection">the reflection from the hlsl</param>
        /// <param name="stage">the shader pipeline stage</param>
        private void CreateReflection(EffectReflection effectReflection, ShaderStage stage)
        {
            int currentProgram;
            GL.GetInteger(GetPName.CurrentProgram, out currentProgram);
            GL.UseProgram(resourceId);

            int uniformBlockCount;
            GL.GetProgram(resourceId, PdxActiveUniformBlocks, out uniformBlockCount);

            for (int uniformBlockIndex = 0; uniformBlockIndex < uniformBlockCount; ++uniformBlockIndex)
            {
                // TODO: get previous name to find te actual constant buffer in the reflexion
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                const int sbCapacity = 128;
                int length;
                var sb = new StringBuilder(sbCapacity);
                GL.GetActiveUniformBlockName(resourceId, uniformBlockIndex, sbCapacity, out length, sb);
                var constantBufferName = sb.ToString();
#else
                var constantBufferName = GL.GetActiveUniformBlockName(resourceId, uniformBlockIndex);
#endif

                var constantBufferDescriptionIndex = effectReflection.ConstantBuffers.FindIndex(x => x.Name == constantBufferName);
                if (constantBufferDescriptionIndex == -1)
                {
                    reflectionResult.Error("Unable to find the constant buffer description [{0}]", constantBufferName);
                    return;
                }
                var constantBufferIndex = effectReflection.ResourceBindings.FindIndex(x => x.Param.RawName == constantBufferName);
                if (constantBufferIndex == -1)
                {
                    reflectionResult.Error("Unable to find the constant buffer [{0}]", constantBufferName);
                    return;
                }

                var constantBufferDescription = effectReflection.ConstantBuffers[constantBufferDescriptionIndex];
                var constantBuffer = effectReflection.ResourceBindings[constantBufferIndex];

                GL.GetActiveUniformBlock(resourceId, uniformBlockIndex, ActiveUniformBlockParameter.UniformBlockDataSize, out constantBufferDescription.Size);
                
                int uniformCount;
                GL.GetActiveUniformBlock(resourceId, uniformBlockIndex, ActiveUniformBlockParameter.UniformBlockActiveUniforms, out uniformCount);

                // set the binding
                GL.UniformBlockBinding(resourceId, uniformBlockIndex, uniformBlockIndex);

                // Read uniforms desc
                var uniformIndices = new int[uniformCount];
                var uniformOffsets = new int[uniformCount];
                var uniformTypes = new int[uniformCount];
                var uniformNames = new string[uniformCount];
                GL.GetActiveUniformBlock(resourceId, uniformBlockIndex, ActiveUniformBlockParameter.UniformBlockActiveUniformIndices, uniformIndices);
                GL.GetActiveUniforms(resourceId, uniformIndices.Length, uniformIndices, ActiveUniformParameter.UniformOffset, uniformOffsets);
                GL.GetActiveUniforms(resourceId, uniformIndices.Length, uniformIndices, ActiveUniformParameter.UniformType, uniformTypes);
                
                for (int uniformIndex = 0; uniformIndex < uniformIndices.Length; ++uniformIndex)
                {
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                    int size;
                    ActiveUniformType aut;
                    GL.GetActiveUniform(resourceId, uniformIndices[uniformIndex], sbCapacity, out length, out size, out aut, sb);
                    uniformNames[uniformIndex] = sb.ToString();
#else
                    uniformNames[uniformIndex] = GL.GetActiveUniformName(resourceId, uniformIndices[uniformIndex]);
#endif
                }

                // Reoder by offset
                var indexMapping = uniformIndices.Select((x, i) => new UniformMergeInfo { Offset = uniformOffsets[i], Type = (ActiveUniformType)uniformTypes[i], Name = uniformNames[i], NextOffset = 0 }).OrderBy(x => x.Offset).ToArray();
                indexMapping.Last().NextOffset = constantBufferDescription.Size;

                // Fill next offsets
                for (int i = 1; i < indexMapping.Length; ++i)
                {
                    indexMapping[i - 1].NextOffset = indexMapping[i].Offset;
                }

                // Group arrays/structures into one variable (std140 layout is enough for offset determinism inside arrays/structures)
                indexMapping = indexMapping.GroupBy(x =>
                    {
                        // Use only first part of name (ignore structure/array part)
                        var name = x.Name;
                        if (name.Contains(".")) { name = name.Substring(0, name.IndexOf('.')); }
                        if (name.Contains("[")) { name = name.Substring(0, name.IndexOf('[')); }
                        return name;
                    })
                                           .Select(x =>
                                               {
                                                   var result = x.First();
                                                   result.NextOffset = x.Last().NextOffset;

                                                   // Check weither it's an array or a struct
                                                   int dotIndex = result.Name.IndexOf('.');
                                                   int arrayIndex = result.Name.IndexOf('[');

                                                   if (x.Count() > 1 && arrayIndex == -1 && dotIndex == -1)
                                                       throw new InvalidOperationException();

                                                   // TODO: Type processing

                                                   result.Name = x.Key;
                                                   return result;
                                               }).ToArray();

                foreach (var variableIndexGroup in indexMapping)
                {
                    var variableIndex = -1;
                    for (var tentativeIndex = 0; tentativeIndex < constantBufferDescription.Members.Length; ++tentativeIndex)
                    {
                        if (constantBufferDescription.Members[tentativeIndex].Param.RawName == variableIndexGroup.Name)
                        {
                            variableIndex = tentativeIndex;
                            break;
                        }
                    }

                    if (variableIndex == -1)
                    {
                        reflectionResult.Error("Unable to find uniform [{0}] in constant buffer [{1}]", variableIndexGroup.Name, constantBufferName);
                        continue;
                    }
                    var variable = constantBufferDescription.Members[variableIndex];
                    variable.Param.Type = GetTypeFromActiveUniformType(variableIndexGroup.Type);
                    variable.Offset = variableIndexGroup.Offset;
                    variable.Size = variableIndexGroup.NextOffset - variableIndexGroup.Offset;

                    constantBufferDescription.Members[variableIndex] = variable;
                }

                constantBufferDescription.Stage = stage;
                constantBufferDescription.Type = ConstantBufferType.ConstantBuffer;

                constantBuffer.SlotCount = 1; // constant buffers are not arrays
                constantBuffer.SlotStart = uniformBlockIndex;
                constantBuffer.Stage = stage;

                // store the new values
                effectReflection.ConstantBuffers[constantBufferDescriptionIndex] = constantBufferDescription;
                effectReflection.ResourceBindings[constantBufferIndex] = constantBuffer;
            }
//#endif

            // Register textures, samplers, etc...
            //TODO: (?) non texture/buffer uniform outside of a block
            {
                // Register "NoSampler", required by HLSL=>GLSL translation to support HLSL such as texture.Load().
                var noSampler = new EffectParameterResourceData { Param = { RawName = "NoSampler", KeyName = "NoSampler", Class = EffectParameterClass.Sampler }, SlotStart = -1 };
                effectBytecode.Reflection.ResourceBindings.Add(noSampler);
                bool usingSamplerNoSampler = false;

                int activeUniformCount;
                GL.GetProgram(resourceId, ProgramParameter.ActiveUniforms, out activeUniformCount);
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                var uniformTypes = new int[activeUniformCount];
                GL.GetActiveUniforms(resourceId, activeUniformCount, Enumerable.Range(0, activeUniformCount).ToArray(), ActiveUniformParameter.UniformType, uniformTypes);
#endif

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                if (GraphicsDevice.IsOpenGLES2)
                {
                    // Register global "fake" cbuffer
                    //var constantBuffer = new ShaderReflectionConstantBuffer
                    //    {
                    //        Name = "$Globals",
                    //        Variables = new List<ShaderReflectionVariable>(),
                    //        Type = ConstantBufferType.ConstantBuffer
                    //    };
                    //shaderReflection.ConstantBuffers.Add(constantBuffer);
                    //shaderReflection.BoundResources.Add(new InputBindingDescription { BindPoint = 0, BindCount = 1, Name = constantBuffer.Name, Type = ShaderInputType.ConstantBuffer });

                    // reset the size of the constant buffers
                    foreach (var constantBuffer in effectReflection.ConstantBuffers)
                        constantBuffer.Size = 0;

                    // set the state of the constant buffers
                    foreach (var constantBuffer in effectReflection.ConstantBuffers)
                        constantBuffer.Stage = stage;
                    for (int i = 0; i < effectReflection.ResourceBindings.Count; i++)
                    {
                        if (effectReflection.ResourceBindings[i].Param.Class != EffectParameterClass.ConstantBuffer)
                            continue;

                        var globalConstantBufferCopy = effectReflection.ResourceBindings[i];
                        globalConstantBufferCopy.Stage = stage;
                        effectReflection.ResourceBindings[i] = globalConstantBufferCopy;
                    }

                    //Create a Globals constant buffer if necessary
                    var globalConstantBufferDescriptionIndex = effectReflection.ConstantBuffers.FindIndex(x => x.Name == "Globals");
                    var globalConstantBufferIndex = effectReflection.ResourceBindings.FindIndex(x => x.Param.RawName == "Globals");
                    if (globalConstantBufferDescriptionIndex == -1 && globalConstantBufferIndex == -1)
                    {
                        var newConstantBufferDescription = new ShaderConstantBufferDescription
                        {
                            Name = "Globals",
                            Stage = stage,
                            Type = ConstantBufferType.ConstantBuffer,
                            Size = 0,
                            Members = new EffectParameterValueData[0],
                        };
                        var newConstantBuffer = new EffectParameterResourceData
                        {
                            Stage = stage,
                            SlotStart = 0,
                            SlotCount = 1,
                            Param = { RawName = "Globals", KeyName = "Globals", Type = EffectParameterType.ConstantBuffer, Class = EffectParameterClass.ConstantBuffer }
                        };

                        effectReflection.ConstantBuffers.Add(newConstantBufferDescription);
                        effectReflection.ResourceBindings.Add(newConstantBuffer);

                        globalConstantBufferDescriptionIndex = effectReflection.ConstantBuffers.Count - 1;
                        globalConstantBufferIndex = effectReflection.ResourceBindings.Count - 1;
                    }

                    // Merge all the variables in the Globals constant buffer
                    if (globalConstantBufferDescriptionIndex != -1 && globalConstantBufferIndex != -1)
                    {
                        var globalConstantBufferDescription = effectReflection.ConstantBuffers[globalConstantBufferDescriptionIndex];
                        for (int cstDescrIndex = 0; cstDescrIndex < effectReflection.ConstantBuffers.Count; ++cstDescrIndex)
                        {
                            if (cstDescrIndex == globalConstantBufferDescriptionIndex)
                                continue;

                            var currentConstantBufferDescription = effectReflection.ConstantBuffers[cstDescrIndex];

                            globalConstantBufferDescription.Members = ArrayExtensions.Concat(
                                globalConstantBufferDescription.Members, currentConstantBufferDescription.Members);

                            effectReflection.ResourceBindings.RemoveAll(x => x.Param.RawName == currentConstantBufferDescription.Name);
                        }

                        // only keep the active uniforms
                        globalConstantBufferDescription.Members =
                            globalConstantBufferDescription.Members.Where(x => GL.GetUniformLocation(resourceId,
#if SILICONSTUDIO_PLATFORM_ANDROID
                            new StringBuilder(x.Param.RawName)
#else
                                x.Param.RawName
#endif
                                ) >= 0).ToArray();

                        // remove all the constant buffers and their resource bindings except the Globals one
                        effectReflection.ConstantBuffers.Clear();
                        effectReflection.ConstantBuffers.Add(globalConstantBufferDescription);
                    }
                    else if (globalConstantBufferDescriptionIndex != -1 && globalConstantBufferIndex == -1)
                    {
                        reflectionResult.Error("Globals constant buffer has a description and no resource binding");
                    }
                    else if (globalConstantBufferDescriptionIndex == -1 && globalConstantBufferIndex != -1)
                    {
                        reflectionResult.Error("Globals constant buffer has a description and no resource binding");
                    }
                }
#endif

                int textureUnitCount = 0;

                const int sbCapacity = 128;
                int length;
                var sb = new StringBuilder(sbCapacity);

                for (int activeUniformIndex = 0; activeUniformIndex < activeUniformCount; ++activeUniformIndex)
                {
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                    var uniformType = (ActiveUniformType)uniformTypes[activeUniformIndex];
                    var uniformName = GL.GetActiveUniformName(resourceId, activeUniformIndex);
#else
                    ActiveUniformType uniformType;
                    int uniformCount;
                    GL.GetActiveUniform(resourceId, activeUniformIndex, sbCapacity, out length, out uniformCount, out uniformType, sb);
                    var uniformName = sb.ToString();
                    //int uniformSize;
                    //GL.GetActiveUniform(resourceId, activeUniformIndex, out uniformSize, ActiveUniformType.Float);
#endif

                    switch (uniformType)
                    {
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                        case ActiveUniformType.Bool:
                        case ActiveUniformType.Int:
                            AddUniform(effectReflection, sizeof(int) * 1, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.BoolVec2:
                        case ActiveUniformType.IntVec2:
                            AddUniform(effectReflection, sizeof(int) * 2, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.BoolVec3:
                        case ActiveUniformType.IntVec3:
                            AddUniform(effectReflection, sizeof(int) * 3, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.BoolVec4:
                        case ActiveUniformType.IntVec4:
                            AddUniform(effectReflection, sizeof(int) * 4, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.Float:
                            AddUniform(effectReflection, sizeof(float) * 1, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.FloatVec2:
                            AddUniform(effectReflection, sizeof(float) * 2, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.FloatVec3:
                            AddUniform(effectReflection, sizeof(float) * 3, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.FloatVec4:
                            AddUniform(effectReflection, sizeof(float) * 4, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.FloatMat4:
                            AddUniform(effectReflection, sizeof(float) * 4 * 4, uniformCount, uniformName, uniformType);
                            break;
                        case ActiveUniformType.FloatMat2:
                        case ActiveUniformType.FloatMat3:
                            throw new NotImplementedException();
#endif
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                        case ActiveUniformType.Sampler1D:
                        case ActiveUniformType.Sampler1DShadow:
#endif
                        case ActiveUniformType.Sampler2D:
                        case ActiveUniformType.Sampler3D: // TODO: remove Texture3D that is not available in OpenGL ES 2
                        case ActiveUniformType.SamplerCube:
                        case ActiveUniformType.Sampler2DShadow:
#if SILICONSTUDIO_PLATFORM_ANDROID
                            var uniformIndex = GL.GetUniformLocation(resourceId, new StringBuilder(uniformName));
#else
                            var uniformIndex = GL.GetUniformLocation(resourceId, uniformName);
#endif

                            // Temporary way to scan which texture and sampler created this texture_sampler variable (to fix with new HLSL2GLSL converter)

                            var startIndex = -1;
                            var textureReflectionIndex = -1;
                            var samplerReflectionIndex = -1;
                            do
                            {
                                int middlePart = uniformName.IndexOf('_', startIndex + 1);
                                var textureName = middlePart != -1 ? uniformName.Substring(0, middlePart) : uniformName;
                                var samplerName = middlePart != -1 ? uniformName.Substring(middlePart + 1) : null;

                                textureReflectionIndex =
                                    effectReflection.ResourceBindings.FindIndex(x => x.Param.RawName == textureName);
                                samplerReflectionIndex =
                                    effectReflection.ResourceBindings.FindIndex(x => x.Param.RawName == samplerName);

                                if (textureReflectionIndex != -1 && samplerReflectionIndex != -1)
                                    break;

                                startIndex = middlePart;
                            } while (startIndex != -1);

                            if (startIndex == -1 || textureReflectionIndex == -1 || samplerReflectionIndex == -1)
                            {
                                reflectionResult.Error("Unable to find sampler and texture corresponding to [{0}]", uniformName);
                                continue; // Error
                            }

                            var textureReflection = effectReflection.ResourceBindings[textureReflectionIndex];
                            var samplerReflection = effectReflection.ResourceBindings[samplerReflectionIndex];

                            // Contrary to Direct3D, samplers and textures are part of the same object in OpenGL
                            // Since we are exposing the Direct3D representation, a single sampler parameter key can be used for several textures, a single texture can be used with several samplers.
                            // When such a case is detected, we need to duplicate the resource binding.
                            textureReflectionIndex = GetReflexionIndex(textureReflection, textureReflectionIndex, effectReflection.ResourceBindings);
                            samplerReflectionIndex = GetReflexionIndex(samplerReflection, samplerReflectionIndex, effectReflection.ResourceBindings);

                            // Update texture uniform mapping
                            GL.Uniform1(uniformIndex, textureUnitCount);
                            
                            textureReflection.Stage = stage;
                            //textureReflection.Param.RawName = uniformName;
                            textureReflection.Param.Type = GetTypeFromActiveUniformType(uniformType);
                            textureReflection.SlotStart = textureUnitCount;
                            textureReflection.SlotCount = 1; // TODO: texture arrays
                            textureReflection.Param.Class = EffectParameterClass.ShaderResourceView;

                            samplerReflection.Stage = stage;
                            samplerReflection.SlotStart = textureUnitCount;
                            samplerReflection.SlotCount = 1; // TODO: texture arrays
                            samplerReflection.Param.Class = EffectParameterClass.Sampler;

                            effectReflection.ResourceBindings[textureReflectionIndex] = textureReflection;
                            effectReflection.ResourceBindings[samplerReflectionIndex] = samplerReflection;

                            Textures.Add(new Texture(textureUnitCount));
                            
                            textureUnitCount++;
                            break;
                    }
                }

                // Remove any optimized resource binding
                effectReflection.ResourceBindings.RemoveAll(x => x.SlotStart == -1);
            }

            GL.UseProgram(currentProgram);
        }

#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES

        private void AddUniform(EffectReflection effectReflection, int uniformSize, int uniformCount, string uniformName, ActiveUniformType uniformType)
        {
            // clean the name
            if (uniformName.Contains("."))
            {
                uniformName = uniformName.Substring(0, uniformName.IndexOf('.'));
            }
            if (uniformName.Contains("["))
            {
                uniformName = uniformName.Substring(0, uniformName.IndexOf('['));
            }

            if (GraphicsDevice.IsOpenGLES2)
            {
                var indexOfConstantBufferDescription = effectReflection.ConstantBuffers.FindIndex(x => x.Name == "Globals");
                var indexOfConstantBuffer = effectReflection.ResourceBindings.FindIndex(x => x.Param.RawName == "Globals");

                if (indexOfConstantBufferDescription == -1 || indexOfConstantBuffer == -1)
                {
                    reflectionResult.Error("Unable to find uniform [{0}] in any constant buffer", uniformName);
                    return;
                }

                var constantBufferDescription = effectReflection.ConstantBuffers[indexOfConstantBufferDescription];
                var constantBuffer = effectReflection.ResourceBindings[indexOfConstantBuffer];

                var elementSize = uniformSize;

                // For array, each element is rounded to register size
                if (uniformSize%16 != 0 && uniformCount > 1)
                {
                    constantBufferDescription.Size = (constantBufferDescription.Size + 15)/16*16;
                    uniformSize = (uniformSize + 15)/16*16;
                }

                // Check if it can fits in the same register, otherwise starts at the next one
                if (uniformCount == 1 && constantBufferDescription.Size/16 != (constantBufferDescription.Size + uniformSize - 1)/16)
                    constantBufferDescription.Size = (constantBufferDescription.Size + 15)/16*16;

                var indexOfUniform = -1;
                for (var tentativeIndex = 0; tentativeIndex < constantBufferDescription.Members.Length; ++tentativeIndex)
                {
                    if (constantBufferDescription.Members[tentativeIndex].Param.RawName == uniformName)
                    {
                        indexOfUniform = tentativeIndex;
                        break;
                    }
                }

                var variable = constantBufferDescription.Members[indexOfUniform];

                variable.Param.Type = GetTypeFromActiveUniformType(uniformType);
                //variable.SourceOffset = variableIndexGroup.Offset;
                variable.Offset = constantBufferDescription.Size;
                variable.Count = uniformCount;
                variable.Size = uniformSize*uniformCount;

                constantBufferDescription.Type = ConstantBufferType.ConstantBuffer;

                constantBuffer.SlotStart = 0;

                constantBufferDescription.Members[indexOfUniform] = variable;
                effectReflection.ResourceBindings[indexOfConstantBuffer] = constantBuffer;

                // No need to compare last element padding.
                // TODO: In case of float1/float2 arrays (rare) it is quite non-optimal to do a CompareMemory
                var compareSize = uniformSize*(uniformCount - 1) + elementSize;

                Uniforms.Add(new Uniform
                {
                    Type = uniformType,
                    Count = uniformCount,
                    CompareSize = compareSize,
                    Offset = constantBufferDescription.Size,
#if SILICONSTUDIO_PLATFORM_ANDROID
                    UniformIndex = GL.GetUniformLocation(resourceId, new StringBuilder(uniformName))
#else
                    UniformIndex = GL.GetUniformLocation(resourceId, uniformName)
#endif
                });
                constantBufferDescription.Size += uniformSize*uniformCount;
            }
            else
            {
                // check that this uniform is in a constant buffer
                foreach (var constantBuffer in effectReflection.ConstantBuffers)
                {
                    foreach (var member in constantBuffer.Members)
                    {
                        if (member.Param.RawName.Equals(uniformName))
                            return;
                    }
                }
                throw new Exception("The uniform value " + uniformName + " is defined outside of a uniform block in OpenGL ES 3, which is not supported by the engine.");
            }
        }
#endif

        /// <summary>
        /// Inserts the data in the list if this is a copy of a previously set one.
        /// </summary>
        /// <param name="data">The  data.</param>
        /// <param name="index">The index in the list.</param>
        /// <param name="bindings">The list of bindings.</param>
        /// <returns>The new index of the data.</returns>
        private static int GetReflexionIndex(EffectParameterResourceData data, int index, List<EffectParameterResourceData> bindings)
        {
            if (data.SlotCount != 0)
            {
                // slot count has been specified, this means that this resource was already configured
                // We have to create a new entry for the data
                var newIndex = bindings.Count;
                bindings.Add(data);
                return newIndex;
            }
            return index;
        }

        private static int GetCountFromActiveUniformType(ActiveUniformType type)
        {
            switch (type)
            {
                case ActiveUniformType.Int:
                case ActiveUniformType.Float:
                case ActiveUniformType.Bool:
                    return 1;
                case ActiveUniformType.IntVec2:
                case ActiveUniformType.UnsignedIntVec2:
                case ActiveUniformType.FloatVec2:
                case ActiveUniformType.BoolVec2:
                    return 2;
                case ActiveUniformType.IntVec3:
                case ActiveUniformType.UnsignedIntVec3:
                case ActiveUniformType.FloatVec3:
                case ActiveUniformType.BoolVec3:
                    return 3;
                case ActiveUniformType.IntVec4:
                case ActiveUniformType.UnsignedIntVec4:
                case ActiveUniformType.FloatVec4:
                case ActiveUniformType.BoolVec4:
                case ActiveUniformType.FloatMat2:
                    return 4;
                case ActiveUniformType.FloatMat2x3:
                case ActiveUniformType.FloatMat3x2:
                    return 6;
                case ActiveUniformType.FloatMat2x4:
                case ActiveUniformType.FloatMat4x2:
                    return 8;
                case ActiveUniformType.FloatMat3:
                    return 9;
                case ActiveUniformType.FloatMat3x4:
                case ActiveUniformType.FloatMat4x3:
                    return 12;
                case ActiveUniformType.FloatMat4:
                    return 16;
                
                case ActiveUniformType.Sampler2D:
                case ActiveUniformType.SamplerCube:
                case ActiveUniformType.Sampler3D:
                case ActiveUniformType.Sampler2DShadow:
                case ActiveUniformType.SamplerCubeShadow:
                case ActiveUniformType.IntSampler2D:
                case ActiveUniformType.IntSampler3D:
                case ActiveUniformType.IntSamplerCube:
                case ActiveUniformType.UnsignedIntSampler2D:
                case ActiveUniformType.UnsignedIntSampler3D:
                case ActiveUniformType.UnsignedIntSamplerCube:
                case ActiveUniformType.Sampler2DArray:
                case ActiveUniformType.Sampler2DArrayShadow:
                case ActiveUniformType.IntSampler2DArray:
                case ActiveUniformType.UnsignedIntSampler2DArray:
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1D:
                case ActiveUniformType.Sampler1DShadow:
                case ActiveUniformType.Sampler2DRect:
                case ActiveUniformType.Sampler2DRectShadow:
                case ActiveUniformType.IntSampler1D:
                case ActiveUniformType.IntSampler2DRect:
                case ActiveUniformType.UnsignedIntSampler1D:
                case ActiveUniformType.UnsignedIntSampler2DRect:
                case ActiveUniformType.Sampler1DArray:
                case ActiveUniformType.Sampler1DArrayShadow:
                case ActiveUniformType.IntSampler1DArray:
                case ActiveUniformType.UnsignedIntSampler1DArray:
#endif
                    return 1;
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                case ActiveUniformType.SamplerBuffer:
                case ActiveUniformType.IntSamplerBuffer:
                case ActiveUniformType.UnsignedIntSamplerBuffer:
                    return 1;
                case ActiveUniformType.Sampler2DMultisample:
                case ActiveUniformType.IntSampler2DMultisample:
                case ActiveUniformType.UnsignedIntSampler2DMultisample:
                    return 1;
                case ActiveUniformType.Sampler2DMultisampleArray:
                case ActiveUniformType.IntSampler2DMultisampleArray:
                case ActiveUniformType.UnsignedIntSampler2DMultisampleArray:
#endif
                    return 1;
                default:
                    //TODO: log error ?
                    return 0;
            }
        }

        private static EffectParameterClass GetClassFromActiveUniformType(ActiveUniformType type)
        {
            switch (type)
            {
                case ActiveUniformType.Int:
                case ActiveUniformType.Float:
                case ActiveUniformType.Bool:
                    return EffectParameterClass.Scalar;
                case ActiveUniformType.FloatVec2:
                case ActiveUniformType.FloatVec3:
                case ActiveUniformType.FloatVec4:
                case ActiveUniformType.IntVec2:
                case ActiveUniformType.IntVec3:
                case ActiveUniformType.IntVec4:
                case ActiveUniformType.BoolVec2:
                case ActiveUniformType.BoolVec3:
                case ActiveUniformType.BoolVec4:
                case ActiveUniformType.UnsignedIntVec2:
                case ActiveUniformType.UnsignedIntVec3:
                case ActiveUniformType.UnsignedIntVec4:
                    return EffectParameterClass.Vector;
                case ActiveUniformType.FloatMat2:
                case ActiveUniformType.FloatMat3:
                case ActiveUniformType.FloatMat4:
                case ActiveUniformType.FloatMat2x3:
                case ActiveUniformType.FloatMat2x4:
                case ActiveUniformType.FloatMat3x2:
                case ActiveUniformType.FloatMat3x4:
                case ActiveUniformType.FloatMat4x2:
                case ActiveUniformType.FloatMat4x3:
                    return EffectParameterClass.MatrixColumns;
                    //return EffectParameterClass.MatrixRows;
                    //return EffectParameterClass.Vector;
                case ActiveUniformType.Sampler2D:
                case ActiveUniformType.SamplerCube:
                case ActiveUniformType.Sampler3D:
                case ActiveUniformType.Sampler2DShadow:
                case ActiveUniformType.Sampler2DArray:
                case ActiveUniformType.Sampler2DArrayShadow:
                case ActiveUniformType.SamplerCubeShadow:
                case ActiveUniformType.IntSampler2D:
                case ActiveUniformType.IntSampler3D:
                case ActiveUniformType.IntSamplerCube:
                case ActiveUniformType.IntSampler2DArray:
                case ActiveUniformType.UnsignedIntSampler2D:
                case ActiveUniformType.UnsignedIntSampler3D:
                case ActiveUniformType.UnsignedIntSamplerCube:
                case ActiveUniformType.UnsignedIntSampler2DArray:
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1D:
                case ActiveUniformType.Sampler1DShadow:
                case ActiveUniformType.Sampler2DRect:
                case ActiveUniformType.Sampler2DRectShadow:
                case ActiveUniformType.Sampler1DArray:
                case ActiveUniformType.SamplerBuffer:
                case ActiveUniformType.Sampler1DArrayShadow:
                case ActiveUniformType.IntSampler1D:
                case ActiveUniformType.IntSampler2DRect:
                case ActiveUniformType.IntSampler1DArray:
                case ActiveUniformType.IntSamplerBuffer:
                case ActiveUniformType.UnsignedIntSampler1D:
                case ActiveUniformType.UnsignedIntSampler2DRect:
                case ActiveUniformType.UnsignedIntSampler1DArray:
                case ActiveUniformType.UnsignedIntSamplerBuffer:
                case ActiveUniformType.Sampler2DMultisample:
                case ActiveUniformType.IntSampler2DMultisample:
                case ActiveUniformType.UnsignedIntSampler2DMultisample:
                case ActiveUniformType.Sampler2DMultisampleArray:
                case ActiveUniformType.IntSampler2DMultisampleArray:
                case ActiveUniformType.UnsignedIntSampler2DMultisampleArray:
#endif
                    return EffectParameterClass.TextureBuffer;
                default:
                    //TODO: log error ?
                    return EffectParameterClass.Object;
            }
        }

        private static EffectParameterType GetTypeFromActiveUniformType(ActiveUniformType type)
        {
            switch (type)
            {
                case ActiveUniformType.Int:
                case ActiveUniformType.IntVec2:
                case ActiveUniformType.IntVec3:
                case ActiveUniformType.IntVec4:
                    return EffectParameterType.Int;
                case ActiveUniformType.Float:
                case ActiveUniformType.FloatVec2:
                case ActiveUniformType.FloatVec3:
                case ActiveUniformType.FloatVec4:
                case ActiveUniformType.FloatMat2:
                case ActiveUniformType.FloatMat3:
                case ActiveUniformType.FloatMat4:
                case ActiveUniformType.FloatMat2x3:
                case ActiveUniformType.FloatMat2x4:
                case ActiveUniformType.FloatMat3x2:
                case ActiveUniformType.FloatMat3x4:
                case ActiveUniformType.FloatMat4x2:
                case ActiveUniformType.FloatMat4x3:
                    return EffectParameterType.Float;
                case ActiveUniformType.Bool:
                case ActiveUniformType.BoolVec2:
                case ActiveUniformType.BoolVec3:
                case ActiveUniformType.BoolVec4:
                    return EffectParameterType.Bool;
                case ActiveUniformType.UnsignedIntVec2:
                case ActiveUniformType.UnsignedIntVec3:
                case ActiveUniformType.UnsignedIntVec4:
                    return EffectParameterType.UInt;
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1D:
                case ActiveUniformType.Sampler1DShadow:
                case ActiveUniformType.IntSampler1D:
                case ActiveUniformType.UnsignedIntSampler1D:
                    return EffectParameterType.Texture1D;
#endif
                case ActiveUniformType.Sampler2D:
                case ActiveUniformType.Sampler2DShadow:
                case ActiveUniformType.IntSampler2D:
                case ActiveUniformType.UnsignedIntSampler2D:
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler2DRect:
                case ActiveUniformType.Sampler2DRectShadow:
                case ActiveUniformType.IntSampler2DRect:
                case ActiveUniformType.UnsignedIntSampler2DRect:
#endif
                    return EffectParameterType.Texture2D;
                case ActiveUniformType.Sampler3D:
                case ActiveUniformType.IntSampler3D:
                case ActiveUniformType.UnsignedIntSampler3D:
                    return EffectParameterType.Texture3D;
                case ActiveUniformType.SamplerCube:
                case ActiveUniformType.SamplerCubeShadow:
                case ActiveUniformType.IntSamplerCube:
                case ActiveUniformType.UnsignedIntSamplerCube:
                    return EffectParameterType.TextureCube;
                case ActiveUniformType.Sampler2DArray:
                case ActiveUniformType.Sampler2DArrayShadow:
                case ActiveUniformType.IntSampler2DArray:
                case ActiveUniformType.UnsignedIntSampler2DArray:
                    return EffectParameterType.Texture2DArray;
#if !SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES
                case ActiveUniformType.Sampler1DArray:
                case ActiveUniformType.Sampler1DArrayShadow:
                case ActiveUniformType.IntSampler1DArray:
                case ActiveUniformType.UnsignedIntSampler1DArray:
                    return EffectParameterType.Texture1DArray;
                case ActiveUniformType.SamplerBuffer:
                case ActiveUniformType.IntSamplerBuffer:
                case ActiveUniformType.UnsignedIntSamplerBuffer:
                    return EffectParameterType.TextureBuffer;
                case ActiveUniformType.Sampler2DMultisample:
                case ActiveUniformType.IntSampler2DMultisample:
                case ActiveUniformType.UnsignedIntSampler2DMultisample:
                    return EffectParameterType.Texture2DMultisampled;
                case ActiveUniformType.Sampler2DMultisampleArray:
                case ActiveUniformType.IntSampler2DMultisampleArray:
                case ActiveUniformType.UnsignedIntSampler2DMultisampleArray:
                    return EffectParameterType.Texture2DMultisampledArray;
#endif
                default:
                    //TODO: log error ?
                    return EffectParameterType.Void;
            }
        }

        class UniformMergeInfo
        {
            public ActiveUniformType Type;
            public int Offset;
            public int NextOffset;
            public string Name;
        }
    }
}
#endif