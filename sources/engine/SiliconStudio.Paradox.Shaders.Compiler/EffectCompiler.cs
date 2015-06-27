﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.Storage;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders.Parser;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Ast.Hlsl;
using SiliconStudio.Shaders.Utility;
using Encoding = System.Text.Encoding;
using LoggerResult = SiliconStudio.Core.Diagnostics.LoggerResult;

namespace SiliconStudio.Paradox.Shaders.Compiler
{
    /// <summary>
    /// An <see cref="IEffectCompiler"/> which will compile effect into multiple shader code, and compile them with a <see cref="IShaderCompiler"/>.
    /// </summary>
    public class EffectCompiler : EffectCompilerBase
    {
        private bool d3dCompilerLoaded = false;
        private static readonly Object WriterLock = new Object();

        private ShaderMixinParser shaderMixinParser;

        private readonly object shaderMixinParserLock = new object();

        public List<string> SourceDirectories { get; private set; }

        public Dictionary<string, string> UrlToFilePath { get; private set; }

        public override IVirtualFileProvider FileProvider { get; set; }
        public bool UseFileSystem { get; set; }

        public EffectCompiler()
        {
            NativeLibrary.PreloadLibrary("d3dcompiler_47.dll");
            SourceDirectories = new List<string>();
            UrlToFilePath = new Dictionary<string, string>();
        }

        public override ObjectId GetShaderSourceHash(string type)
        {
            return GetMixinParser().SourceManager.GetShaderSourceHash(type);
        }

        /// <summary>
        /// Remove cached files for modified shaders
        /// </summary>
        /// <param name="modifiedShaders"></param>
        public override void ResetCache(HashSet<string> modifiedShaders)
        {
            GetMixinParser().DeleteObsoleteCache(modifiedShaders);
        }

        private ShaderMixinParser GetMixinParser()
        {
            lock (shaderMixinParserLock)
            {
                // Generate the AST from the mixin description
                if (shaderMixinParser == null)
                {
                    shaderMixinParser = new ShaderMixinParser(FileProvider ?? AssetManager.FileProvider);
                    shaderMixinParser.SourceManager.LookupDirectoryList.AddRange(SourceDirectories); // TODO: temp
                    shaderMixinParser.SourceManager.UseFileSystem = UseFileSystem;
                    shaderMixinParser.SourceManager.UrlToFilePath = UrlToFilePath; // TODO: temp
                }
                return shaderMixinParser;
            }
        }

        public override TaskOrResult<EffectBytecodeCompilerResult> Compile(ShaderMixinSource mixinTree, CompilerParameters compilerParameters)
        {
            var log = new LoggerResult();

            // Load D3D compiler dll
            // Note: No lock, it's probably fine if it gets called from multiple threads at the same time.
            if (Platform.IsWindowsDesktop && !d3dCompilerLoaded)
            {
                NativeLibrary.PreloadLibrary("d3dcompiler_47.dll");
                d3dCompilerLoaded = true;
            }

            var shaderMixinSource = mixinTree;
            var fullEffectName = mixinTree.Name;
            var usedParameters = mixinTree.UsedParameters;

            // Make a copy of shaderMixinSource. Use deep clone since shaderMixinSource can be altered during compilation (e.g. macros)
            var shaderMixinSourceCopy = new ShaderMixinSource();
            shaderMixinSourceCopy.DeepCloneFrom(shaderMixinSource);
            shaderMixinSource = shaderMixinSourceCopy;

            // Generate platform-specific macros
            var platform = usedParameters.Get(CompilerParameters.GraphicsPlatformKey);
            switch (platform)
            {
                case GraphicsPlatform.Direct3D11:
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D", 1);
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D11", 1);
                    break;
                case GraphicsPlatform.OpenGL:
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGL", 1);
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLCORE", 1);
                    break;
                case GraphicsPlatform.OpenGLES:
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGL", 1);
                    shaderMixinSource.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES", 1);
                    break;
                default:
                    throw new NotSupportedException();
            }

            var parsingResult = GetMixinParser().Parse(shaderMixinSource, shaderMixinSource.Macros.ToArray());

            // Copy log from parser results to output
            CopyLogs(parsingResult, log);

            // Return directly if there are any errors
            if (parsingResult.HasErrors)
            {
                return new EffectBytecodeCompilerResult(null, log);
            }

            // Convert the AST to HLSL
            var writer = new SiliconStudio.Shaders.Writer.Hlsl.HlslWriter
            {
                EnablePreprocessorLine = true // Allow to output links to original pdxsl via #line pragmas
            };
            writer.Visit(parsingResult.Shader);
            var shaderSourceText = writer.Text;

            if (string.IsNullOrEmpty(shaderSourceText))
            {
                log.Error("No code generated for effect [{0}]", fullEffectName);
                return new EffectBytecodeCompilerResult(null, log);
            }

            // -------------------------------------------------------
            // Save shader log
            // TODO: TEMP code to allow debugging generated shaders on Windows Desktop
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
            var shaderId = ObjectId.FromBytes(Encoding.UTF8.GetBytes(shaderSourceText));

            var logDir = "log";
            if (!Directory.Exists(logDir))
            {
                Directory.CreateDirectory(logDir);
            }
            var shaderSourceFilename = Path.Combine(logDir, "shader_" +  fullEffectName.Replace('.', '_') + "_" + shaderId + ".hlsl");
            lock (WriterLock) // protect write in case the same shader is created twice
            {
                // Write shader before generating to make sure that we are having a trace before compiling it (compiler may crash...etc.)
                if (!File.Exists(shaderSourceFilename))
                {
                    File.WriteAllText(shaderSourceFilename, shaderSourceText);
                }
            }
#else
            string shaderSourceFilename = null;
#endif
            // -------------------------------------------------------

            var bytecode = new EffectBytecode { Reflection = parsingResult.Reflection, HashSources = parsingResult.HashSources };

            // Select the correct backend compiler
            IShaderCompiler compiler;
            switch (platform)
            {
#if SILICONSTUDIO_PLATFORM_WINDOWS
                case GraphicsPlatform.Direct3D11:
                    compiler = new Direct3D.ShaderCompiler();
                    break;
#endif
                case GraphicsPlatform.OpenGL:
                case GraphicsPlatform.OpenGLES:
                    // get the number of render target outputs
                    var rtOutputs = 0;
                    var psOutput = parsingResult.Shader.Declarations.OfType<StructType>().FirstOrDefault(x => x.Name.Text == "PS_OUTPUT");
                    if (psOutput != null)
                    {
                        foreach (var rto in psOutput.Fields)
                        {
                            var sem = rto.Qualifiers.OfType<Semantic>().FirstOrDefault();
                            if (sem != null)
                            {
                                // special case SV_Target
                                if (rtOutputs == 0 && sem.Name.Text == "SV_Target")
                                {
                                    rtOutputs = 1;
                                    break;
                                }
                                for (var i = rtOutputs; i < 8; ++i)
                                {
                                    if (sem.Name.Text == ("SV_Target" + i))
                                    {
                                        rtOutputs = i + 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    compiler = new OpenGL.ShaderCompiler(rtOutputs);
                    break;
                default:
                    throw new NotSupportedException();
            }

            var shaderStageBytecodes = new List<ShaderBytecode>();

#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
            var stageStringBuilder = new StringBuilder();
#endif
            // if the shader (non-compute) does not have a pixel shader, we should add it on OpenGL ES.
            if (platform == GraphicsPlatform.OpenGLES && !parsingResult.EntryPoints.ContainsKey(ShaderStage.Pixel) && !parsingResult.EntryPoints.ContainsKey(ShaderStage.Compute))
            {
                parsingResult.EntryPoints.Add(ShaderStage.Pixel, null);
            }

            foreach (var stageBinding in parsingResult.EntryPoints)
            {
                // Compile
                // TODO: We could compile stages in different threads to improve compiler throughput?
                var result = compiler.Compile(shaderSourceText, stageBinding.Value, stageBinding.Key, usedParameters, bytecode.Reflection, shaderSourceFilename);
                result.CopyTo(log);

                if (result.HasErrors)
                {
                    continue;
                }

                // -------------------------------------------------------
                // Append bytecode id to shader log
#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
                stageStringBuilder.AppendLine("@G    {0} => {1}".ToFormat(stageBinding.Key, result.Bytecode.Id));
#endif
                // -------------------------------------------------------

                shaderStageBytecodes.Add(result.Bytecode);

                // When this is a compute shader, there is no need to scan other stages
                if (stageBinding.Key == ShaderStage.Compute)
                    break;
            }

            // In case of Direct3D, we can safely remove reflection data as it is entirely resolved at compile time.
            if (platform == GraphicsPlatform.Direct3D11)
            {
                CleanupReflection(bytecode.Reflection);
            }
            bytecode.Stages = shaderStageBytecodes.ToArray();

#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
            lock (WriterLock) // protect write in case the same shader is created twice
            {
                var builder = new StringBuilder();
                builder.AppendLine("/**************************");
                builder.AppendLine("***** Used Parameters *****");
                builder.AppendLine("***************************");
                builder.Append("@P EffectName: ");
                builder.AppendLine(fullEffectName ?? "");
                builder.Append(usedParameters.ToStringDetailed());
                builder.AppendLine("***************************");

                if (bytecode.Reflection.ConstantBuffers.Count > 0)
                {
                    builder.AppendLine("****  ConstantBuffers  ****");
                    builder.AppendLine("***************************");
                    foreach (var cBuffer in bytecode.Reflection.ConstantBuffers)
                    {
                        builder.AppendFormat("cbuffer {0} [Stage: {1}, Size: {2}]", cBuffer.Name, cBuffer.Stage, cBuffer.Size).AppendLine();
                        foreach (var parameter in cBuffer.Members)
                        {
                            builder.AppendFormat("@C    {0} => {1}", parameter.Param.RawName, parameter.Param.KeyName).AppendLine();
                        }
                    }
                    builder.AppendLine("***************************");
                }

                if (bytecode.Reflection.ResourceBindings.Count > 0)
                {
                    builder.AppendLine("******  Resources    ******");
                    builder.AppendLine("***************************");
                    foreach (var resource in bytecode.Reflection.ResourceBindings)
                    {
                        var parameter = resource.Param;
                        builder.AppendFormat("@R    {0} => {1} [Stage: {2}, Slot: ({3}-{4})]", parameter.RawName, parameter.KeyName, resource.Stage, resource.SlotStart, resource.SlotStart + resource.SlotCount - 1).AppendLine();
                    }
                    builder.AppendLine("***************************");
                }

                if (bytecode.HashSources.Count > 0)
                {
                    builder.AppendLine("*****     Sources     *****");
                    builder.AppendLine("***************************");
                    foreach (var hashSource in bytecode.HashSources)
                    {
                        builder.AppendFormat("@S    {0} => {1}", hashSource.Key, hashSource.Value).AppendLine();
                    }
                    builder.AppendLine("***************************");
                }

                if (bytecode.Stages.Length > 0)
                {
                    builder.AppendLine("*****     Stages      *****");
                    builder.AppendLine("***************************");
                    builder.Append(stageStringBuilder);
                    builder.AppendLine("***************************");
                }
                builder.AppendLine("*************************/");

                // Re-append the shader with all informations
                builder.Append(shaderSourceText);

                File.WriteAllText(shaderSourceFilename, builder.ToString());
            }
#endif

            return new EffectBytecodeCompilerResult(bytecode, log);
        }

        private static void CopyLogs(SiliconStudio.Shaders.Utility.LoggerResult inputLog, LoggerResult outputLog)
        {
            foreach (var inputMessage in inputLog.Messages)
            {
                var logType = LogMessageType.Info;
                switch (inputMessage.Level)
                {
                    case ReportMessageLevel.Error:
                        logType = LogMessageType.Error;
                        break;
                    case ReportMessageLevel.Info:
                        logType = LogMessageType.Info;
                        break;
                    case ReportMessageLevel.Warning:
                        logType = LogMessageType.Warning;
                        break;
                }
                var outputMessage = new LogMessage(inputMessage.Span.ToString(), logType, string.Format(" {0}: {1}", inputMessage.Code, inputMessage.Text));
                outputLog.Log(outputMessage);
            }
            outputLog.HasErrors = inputLog.HasErrors;
        }

        private static void CleanupReflection(EffectReflection reflection)
        {
            for (int i = reflection.ConstantBuffers.Count - 1; i >= 0; i--)
            {
                var cBuffer = reflection.ConstantBuffers[i];
                if (cBuffer.Stage == ShaderStage.None)
                {
                    reflection.ConstantBuffers.RemoveAt(i);
                }
            }

            for (int i = reflection.ResourceBindings.Count - 1; i >= 0; i--)
            {
                var resourceBinding = reflection.ResourceBindings[i];
                if (resourceBinding.Stage == ShaderStage.None)
                {
                    reflection.ResourceBindings.RemoveAt(i);
                }
            }
        }
    }
}