﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.IO;
using SiliconStudio.Core;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.ReferenceCounting;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Paradox.Engine.Design;
using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders;
using SiliconStudio.Paradox.Shaders.Compiler;

namespace SiliconStudio.Paradox.Rendering
{
    /// <summary>
    /// The effect system.
    /// </summary>
    public class EffectSystem : GameSystemBase
    {
        private readonly static Logger Log = GlobalLogger.GetLogger("EffectSystem");

        private IGraphicsDeviceService graphicsDeviceService;
        private EffectCompilerBase compiler;
        private readonly Dictionary<string, List<CompilerResults>> earlyCompilerCache = new Dictionary<string, List<CompilerResults>>();
        private Dictionary<EffectBytecode, Effect> cachedEffects = new Dictionary<EffectBytecode, Effect>();
        private DirectoryWatcher directoryWatcher;
        private bool isInitialized;

        /// <summary>
        /// Called each time a non-cached effect is requested.
        /// </summary>
        internal Action<EffectCompileRequest> EffectUsed;

        private readonly HashSet<string> recentlyModifiedShaders = new HashSet<string>();
        private bool clearNextFrame = false;

        public IEffectCompiler Compiler { get { return compiler; } set { compiler = (EffectCompilerBase)value; } }

        /// <summary>
        /// Gets or sets the database file provider, to use for loading effects and shader sources.
        /// </summary>
        /// <value>
        /// The database file provider.
        /// </value>
        public IVirtualFileProvider FileProvider
        {
            get { return compiler.FileProvider ?? AssetManager.FileProvider; }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="EffectSystem"/> class.
        /// </summary>
        /// <param name="services">The services.</param>
        public EffectSystem(IServiceRegistry services)
            : base(services)
        {
            Services.AddService(typeof(EffectSystem), this);
        }

        public override void Initialize()
        {
            base.Initialize();

            isInitialized = true;

            // Get graphics device service
            graphicsDeviceService = Services.GetSafeServiceAs<IGraphicsDeviceService>();

#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
            Enabled = true;
            directoryWatcher = new DirectoryWatcher("*.pdxsl");
            directoryWatcher.Modified += FileModifiedEvent;
            // TODO: pdxfx too
#endif

            // Make sure default compiler is created (local if possible otherwise none) if nothing else was explicitely set/requested (i.e. by GameSettings)
            if (Compiler == null)
                Compiler = CreateEffectCompiler();
        }

        protected override void Destroy()
        {
            // Mark effect system as destroyed (so that async effect compilation are ignored)
            lock (cachedEffects)
            {
                // Clear effects
                foreach (var effect in cachedEffects)
                {
                    effect.Value.ReleaseInternal();
                }
                cachedEffects.Clear();

                // Mark as not initialized anymore
                isInitialized = false;
            }

            base.Destroy();
        }

        /// <summary>
        /// Creates an effect compiler, with either specificed <see cref="effectCompiler"/> or default one, wrapped in an <see cref="EffectCompilerCache"/>.
        /// </summary>
        /// <param name="effectCompiler">The effect compiler.</param>
        /// <param name="taskSchedulerSelector">The task scheduler selector.</param>
        /// <returns></returns>
        public static IEffectCompiler CreateEffectCompiler(TaskSchedulerSelector taskSchedulerSelector = null)
        {
            return CreateEffectCompiler(null, null, EffectCompilationMode.Local, false);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            UpdateEffects();
        }

        public bool IsValid(Effect effect)
        {
            lock (cachedEffects)
            {
                return cachedEffects.ContainsKey(effect.Bytecode);
            }
        }

        /// <summary>
        /// Loads the effect.
        /// </summary>
        /// <param name="effectName">Name of the effect.</param>
        /// <param name="compilerParameters">The compiler parameters.</param>
        /// <returns>A new instance of an effect.</returns>
        /// <exception cref="System.InvalidOperationException">Could not compile shader. Need fallback.</exception>
        public TaskOrResult<Effect> LoadEffect(string effectName, CompilerParameters compilerParameters)
        {
            ParameterCollection usedParameters;
            return LoadEffect(effectName, compilerParameters, out usedParameters);
        }

        /// <summary>
        /// Loads the effect.
        /// </summary>
        /// <param name="effectName">Name of the effect.</param>
        /// <param name="compilerParameters">The compiler parameters.</param>
        /// <param name="usedParameters">The used parameters.</param>
        /// <returns>A new instance of an effect.</returns>
        /// <exception cref="System.InvalidOperationException">Could not compile shader. Need fallback.</exception>
        public TaskOrResult<Effect> LoadEffect(string effectName, CompilerParameters compilerParameters, out ParameterCollection usedParameters)
        {
            if (effectName == null) throw new ArgumentNullException("effectName");
            if (compilerParameters == null) throw new ArgumentNullException("compilerParameters");

            // Get the compiled result
            var compilerResult = GetCompilerResults(effectName, compilerParameters);
            CheckResult(compilerResult);

            // Only take the sub-effect
            var bytecode = compilerResult.Bytecode;
            usedParameters = compilerResult.UsedParameters;

            if (bytecode.Task != null && !bytecode.Task.IsCompleted)
            {
                // Result was async, keep it async
                return bytecode.Task.ContinueWith(x => CreateEffect(effectName, x.Result, compilerResult));
            }
            else
            {
                return CreateEffect(effectName, bytecode.WaitForResult(), compilerResult);
            }
        }

        // TODO: THIS IS JUST A WORKAROUND, REMOVE THIS

        private static void CheckResult(LoggerResult compilerResult)
        {
            // Check errors
            if (compilerResult.HasErrors)
            {
                throw new InvalidOperationException("Could not compile shader. See error messages." + compilerResult.ToText());
            }
        }

        private Effect CreateEffect(string effectName, EffectBytecodeCompilerResult effectBytecodeCompilerResult, CompilerResults compilerResult)
        {
            Effect effect;
            lock (cachedEffects)
            {
                if (!isInitialized)
                    throw new InvalidOperationException("EffectSystem has been disposed. This Effect compilation has been cancelled.");

                var usedParameters = compilerResult.UsedParameters;

                if (effectBytecodeCompilerResult.CompilationLog.HasErrors)
                {
                    // Unregister result (or should we keep it so that failure never change?)
                    List<CompilerResults> effectCompilerResults;
                    if (earlyCompilerCache.TryGetValue(effectName, out effectCompilerResults))
                    {
                        effectCompilerResults.Remove(compilerResult);
                    }
                }

                CheckResult(effectBytecodeCompilerResult.CompilationLog);

                var bytecode = effectBytecodeCompilerResult.Bytecode;
                if (bytecode == null)
                    throw new InvalidOperationException("EffectCompiler returned no shader and no compilation error.");

                if (!cachedEffects.TryGetValue(bytecode, out effect))
                {
                    effect = new Effect(graphicsDeviceService.GraphicsDevice, bytecode, usedParameters) { Name = effectName };
                    cachedEffects.Add(bytecode, effect);

#if SILICONSTUDIO_PLATFORM_WINDOWS_DESKTOP
                    foreach (var type in bytecode.HashSources.Keys)
                    {
                        // TODO: the "/path" is hardcoded, used in ImportStreamCommand and ShaderSourceManager. Find a place to share this correctly.
                        using (var pathStream = FileProvider.OpenStream(EffectCompilerBase.GetStoragePathFromShaderType(type) + "/path", VirtualFileMode.Open, VirtualFileAccess.Read))
                        using (var reader = new StreamReader(pathStream))
                        {
                            var path = reader.ReadToEnd();
                            directoryWatcher.Track(path);
                        }
                    }
#endif
                }
            }
            return effect;
        }

        private CompilerResults GetCompilerResults(string effectName, CompilerParameters compilerParameters)
        {
            compilerParameters.Profile = GraphicsDevice.ShaderProfile.HasValue ? GraphicsDevice.ShaderProfile.Value : GraphicsDevice.Features.Profile;
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLCORE
            compilerParameters.Platform = GraphicsPlatform.OpenGL;
#endif
#if SILICONSTUDIO_PARADOX_GRAPHICS_API_OPENGLES 
            compilerParameters.Platform = GraphicsPlatform.OpenGLES;
#endif

            // Compile shader
            var isPdxfx = ShaderMixinManager.Contains(effectName);

            // getting the effect from the used parameters only makes sense when the source files are the same
            // TODO: improve this by updating earlyCompilerCache - cache can still be relevant

            CompilerResults compilerResult = null;

            if (isPdxfx)
            {
                // perform an early test only based on the parameters
                compilerResult = GetShaderFromParameters(effectName, compilerParameters);
            }

            if (compilerResult == null)
            {
                var source = isPdxfx ? new ShaderMixinGeneratorSource(effectName) : (ShaderSource)new ShaderClassSource(effectName);
                compilerResult = compiler.Compile(source, compilerParameters);

                var effectRequested = EffectUsed;
                if (effectRequested != null)
                {
                    effectRequested(new EffectCompileRequest(effectName, compilerResult.UsedParameters));
                }
                
                if (!compilerResult.HasErrors && isPdxfx)
                {
                    lock (earlyCompilerCache)
                    {
                        List<CompilerResults> effectCompilerResults;
                        if (!earlyCompilerCache.TryGetValue(effectName, out effectCompilerResults))
                        {
                            effectCompilerResults = new List<CompilerResults>();
                            earlyCompilerCache.Add(effectName, effectCompilerResults);
                        }

                        // Register bytecode used parameters so that they are checked when another effect is instanced
                        effectCompilerResults.Add(compilerResult);
                    }
                }
            }

            foreach (var message in compilerResult.Messages)
            {
                Log.Log(message);
            }

            return compilerResult;
        }

        private void UpdateEffects()
        {
            lock (recentlyModifiedShaders)
            {
                if (recentlyModifiedShaders.Count == 0)
                {
                    return;
                }

                // Clear cache for recently modified shaders
                compiler.ResetCache(recentlyModifiedShaders);

                var bytecodeRemoved = new List<EffectBytecode>();

                lock (cachedEffects)
                {
                    foreach (var shaderSourceName in recentlyModifiedShaders)
                    {
                        // TODO: cache keys in a HashSet instead of ToHashSet
                        var bytecodes = new HashSet<EffectBytecode>(cachedEffects.Keys);
                        foreach (var bytecode in bytecodes)
                        {
                            if (bytecode.HashSources.ContainsKey(shaderSourceName))
                            {
                                bytecodeRemoved.Add(bytecode);

                                // Dispose previous effect
                                var effect = cachedEffects[bytecode];
                                effect.Dispose();

                                // Remove effect from cache
                                cachedEffects.Remove(bytecode);
                            }
                        }
                    }
                }

                lock (earlyCompilerCache)
                {
                    foreach (var effectCompilerResults in earlyCompilerCache.Values)
                    {
                        foreach (var bytecode in bytecodeRemoved)
                        {
                            effectCompilerResults.RemoveAll(results => results.Bytecode.GetCurrentResult().Bytecode == bytecode);
                        }
                    }
                }

                recentlyModifiedShaders.Clear();
            }
        }

        private void FileModifiedEvent(object sender, FileEvent e)
        {
            if (e.ChangeType == FileEventChangeType.Changed || e.ChangeType == FileEventChangeType.Renamed)
            {
                lock (recentlyModifiedShaders)
                {
                    recentlyModifiedShaders.Add(Path.GetFileNameWithoutExtension(e.Name));
                }
            }
        }

        /// <summary>
        /// Get the shader from the database based on the parameters used for its compilation.
        /// </summary>
        /// <param name="effectName">Name of the effect.</param>
        /// <param name="parameters">The parameters.</param>
        /// <returns>The EffectBytecode if found.</returns>
        protected CompilerResults GetShaderFromParameters(string effectName, CompilerParameters parameters)
        {
            lock (earlyCompilerCache)
            {
                List<CompilerResults> compilerResultsList;
                if (!earlyCompilerCache.TryGetValue(effectName, out compilerResultsList))
                    return null;

                // TODO: Optimize it so that search is not linear?
                // Probably not trivial for subset testing
                foreach (var compiledResults in compilerResultsList)
                {
                    if (parameters.Contains(compiledResults.UsedParameters))
                    {
                        return compiledResults;
                    }
                }
            }

            return null;
        }

        internal static IEffectCompiler CreateEffectCompiler(EffectSystem effectSystem, Guid? packageId, EffectCompilationMode effectCompilationMode, bool recordEffectRequested)
        {
            EffectCompilerBase compiler = null;

#if SILICONSTUDIO_PARADOX_EFFECT_COMPILER
            if ((effectCompilationMode & EffectCompilationMode.Local) != 0)
            {
                // Local allowed and available, let's use that
                compiler = new EffectCompiler
                {
                    SourceDirectories = { EffectCompilerBase.DefaultSourceShaderFolder },
                };
            }
#endif               

            // Nothing to do remotely
            bool needRemoteCompiler = (compiler == null && (effectCompilationMode & EffectCompilationMode.Remote) != 0);
            if (needRemoteCompiler || recordEffectRequested)
            {
                // Create the object that handles the connection
                var shaderCompilerTarget = new RemoteEffectCompilerClient(packageId);

                if (recordEffectRequested)
                {
                    // Let's notify effect compiler server for each new effect requested
                    effectSystem.EffectUsed += shaderCompilerTarget.NotifyEffectUsed;
                }

                // Use remote only if nothing else was found before (i.e. a local compiler)
                if (needRemoteCompiler)
                {
                    // Create a remote compiler
                    compiler = new RemoteEffectCompiler(shaderCompilerTarget);
                }
            }

            // Local not possible or allowed, and remote not allowed either => switch back to null compiler
            if (compiler == null)
            {
                compiler = new NullEffectCompiler();
            }

            return new EffectCompilerCache(compiler);
        }
    }
}