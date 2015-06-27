﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

using NUnit.Framework;

using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.Storage;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Shaders.Compiler;

namespace SiliconStudio.Paradox.Shaders.Tests
{
    [TestFixture]
    public class TestParallelShaderMixer
    {
        private static EffectCompiler compiler;

        private static int NumThreads = 15;

        public static void Main3()
        {
            // Create and mount database file system
            var objDatabase = new ObjectDatabase("/data/db");
            var assetIndexMap = AssetIndexMap.Load();
            var databaseFileProvider = new DatabaseFileProvider(assetIndexMap, objDatabase);
            AssetManager.GetFileProvider = () => databaseFileProvider;

            compiler = new EffectCompiler();
            compiler.SourceDirectories.Add("shaders");
            var shaderMixinSource = new ShaderMixinSource();
            shaderMixinSource.Mixins.Add(new ShaderClassSource("ShaderBase"));
            shaderMixinSource.Mixins.Add(new ShaderClassSource("TransformationWVP"));
            shaderMixinSource.Mixins.Add(new ShaderClassSource("ShadingBase"));

            var shaderMixinSource2 = new ShaderMixinSource();
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("ShaderBase"));
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("TransformationWVP"));
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("ShadingBase"));
            shaderMixinSource2.Mixins.Add(new ShaderClassSource("ShadingOverlay"));

            var allThreads = new List<Thread>();

            for (int i = 0; i < NumThreads; ++i)
            {
                CompilerThread compilerThread;
                if (i % 2 == 0)
                    compilerThread = new CompilerThread(compiler, shaderMixinSource);
                else
                    compilerThread = new CompilerThread(compiler, shaderMixinSource2);
                allThreads.Add(new Thread(compilerThread.Compile));
            }

            foreach (var thread in allThreads)
            {
                thread.Start();
            }
        }
        
    }

    public class CompilerThread
    {
        private volatile EffectCompiler effectCompiler;

        private volatile ShaderMixinSource mixinSource;

        public CompilerThread(EffectCompiler compiler, ShaderMixinSource source)
        {
            effectCompiler = compiler;
            mixinSource = source;
        }

        public void Compile()
        {
            Console.WriteLine(@"Inside Thread");
            
            var parameters = new ShaderMixinParameters();
            parameters.Set(CompilerParameters.GraphicsPlatformKey, GraphicsPlatform.Direct3D11);
            parameters.Set(CompilerParameters.GraphicsProfileKey, GraphicsProfile.Level_11_0);

            var mixinTree = new ShaderMixinSource() { Name = "TestParallelMix", UsedParameters = parameters };

            var result = effectCompiler.Compile(mixinTree, new CompilerParameters()).WaitForResult();

            Assert.IsFalse(result.CompilationLog.HasErrors);
            Assert.IsNotNull(result);

            Console.WriteLine(@"Thread end");
        }
    }
}
