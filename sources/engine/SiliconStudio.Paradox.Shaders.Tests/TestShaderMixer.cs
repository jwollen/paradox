﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
/*
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using NUnit.Framework;

using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Engine.Shaders.Mixins;
using SiliconStudio.Core.IO;
using SiliconStudio.Paradox.Shaders.Compiler;
using SiliconStudio.Shaders.Ast;

namespace SiliconStudio.Shaders.Tests
{
    [TestFixture]
    internal class TestShaderMixer
    {
        private ShaderSourceManager sourceManager;
        private ShaderLoader shaderLoader;

        [SetUp]
        public void Init()
        {
            sourceManager = new ShaderSourceManager();
            sourceManager.LookupDirectoryList.Add(@"..\..\Shaders");
            shaderLoader = new ShaderLoader(sourceManager);
        }

        [Test]
        public void TestRenameBasic() // simple mix with inheritance
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("Parent"),
                    new ShaderClassSource("Child")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixer = new ParadoxShaderMixer(mcm.Mixins["Child"], mcm.Mixins, null);
            mixer.Mix();

            //var childMixinInfo = mcm.Mixins["Child"].ParsingInfo;
            //Assert.AreEqual("Child_AddBaseValue", childMixinInfo.MethodDeclarations.First().Name.Text);
            //Assert.AreEqual("Parent_AddBaseValue", (childMixinInfo.BaseMethodCalls.First().Target as VariableReferenceExpression).Name.Text);
            //Assert.AreEqual("Parent_baseValue", childMixinInfo.VariableReferenceExpressions[0].Name.Text);
        }

        [Test]
        public void TestRenameStatic() // mix with call to a static method
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StaticMixin"),
                    new ShaderClassSource("StaticCallMixin")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixer = new ParadoxShaderMixer(mcm.Mixins["StaticCallMixin"], mcm.Mixins, null);
            mixer.Mix();

            //var staticCallMixinInfo = mcm.Mixins["StaticCallMixin"].ParsingInfo;
            //Assert.AreEqual("StaticMixin_staticCall", (staticCallMixinInfo.MethodCalls.First().Target as VariableReferenceExpression).Name.Text);
            //Assert.AreEqual("StaticMixin_staticMember", ((staticCallMixinInfo.StaticMemberReferences.First().Node as UnaryExpression).Expression as VariableReferenceExpression).Name.Text);
        }

        [Test]
        public void TestBasicExternMix() // mix with an extern class
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("ExternTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var externDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            externDictionary.Add(mcmFinal.Mixins["ExternTest"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["ExternMixin"].DeepClone() });

            var mixer = new ParadoxShaderMixer(mcmFinal.Mixins["ExternTest"], mcmFinal.Mixins, externDictionary);
            mixer.Mix();

            //var externTestMixinInfo = mcmFinal.Mixins["ExternTest"].ParsingInfo;
            //Assert.AreEqual("ExternTest_myExtern_ExternMixin_externFunc", (externTestMixinInfo.ExternMethodCalls.First().MethodInvocation.Target as VariableReferenceExpression).Name.Text);
            //Assert.AreEqual("ExternTest_myExtern_ExternMixin_externMember", ((externTestMixinInfo.ExternMemberReferences.First().Node as ReturnStatement).Value as VariableReferenceExpression).Name.Text);
        }

        [Test]
        public void TestDeepMix() // mix with multiple levels of extern classes
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("DeepExtern"),
                    new ShaderClassSource("DeepExternTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var depext = mcm.Mixins["DeepExtern"].DeepClone();
            var deepDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            deepDictionary.Add(mcmFinal.Mixins["DeepExternTest"].VariableDependencies.First().Key, new List<ModuleMixin> { depext });
            deepDictionary.Add(depext.VariableDependencies.First().Key, new List<ModuleMixin> { mcm.Mixins["ExternMixin"].DeepClone() });
            var mixer = new ParadoxShaderMixer(mcmFinal.Mixins["DeepExternTest"], mcmFinal.Mixins, deepDictionary);
            mixer.Mix();

            //var externDeepTest = mcmFinal.Mixins["DeepExternTest"].ParsingInfo;
            //Assert.AreEqual("DeepExternTest_myExtern_DeepExtern_myExtern_ExternMixin_externFunc", (externDeepTest.ExternMethodCalls.First().MethodInvocation.Target as VariableReferenceExpression).Name.Text);
            //Assert.AreEqual("DeepExternTest_myExtern_DeepExtern_myExtern_ExternMixin_externMember", ((externDeepTest.ExternMemberReferences.First().Node as ReturnStatement).Value as VariableReferenceExpression).Name.Text);
        }

        [Test]
        public void TestMultipleStatic() // check that static calls only written once
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StaticMixin"),
                    new ShaderClassSource("StaticCallMixin"),
                    new ShaderClassSource("TestMultipleStatic"),
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["TestMultipleStatic"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["StaticCallMixin"].DeepClone() });
            var mixer = new ParadoxShaderMixer(mcmFinal.Mixins["TestMultipleStatic"], mcmFinal.Mixins, extDictionary);
            mixer.Mix();

            //Assert.AreEqual(1, mixer.MixedShader.Members.OfType<MethodDeclaration>().Count(x => x.Name.Text == "StaticMixin_staticCall"));
            //Assert.AreEqual(1, mixer.MixedShader.Members.OfType<Variable>().Count(x => x.Name.Text == "StaticMixin_staticMember"));
        }

        [Test]
        public void TestStageCall()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StageBase"),
                    new ShaderClassSource("StageCallExtern"),
                    new ShaderClassSource("StaticStageCallTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["StaticStageCallTest"].VariableDependencies.First().Key, new List<ModuleMixin>{mcm.Mixins["StageCallExtern"].DeepClone()});
            var mixer = new ParadoxShaderMixer(mcmFinal.Mixins["StaticStageCallTest"], mcmFinal.Mixins, extDictionary);
            mixer.Mix();

            //var extPI = mcmExtern.Mixins["StageCallExtern"].ParsingInfo;
            //var finalPI = mcmFinal.Mixins["StaticStageCallTest"].ParsingInfo;

            //Assert.AreEqual(1, extPI.StageMethodCalls.Count);
            //Assert.AreEqual(1, finalPI.MethodDeclarations.Count);
            //Assert.AreEqual(finalPI.MethodDeclarations[0], extPI.StageMethodCalls[0].Target.TypeInference.Declaration);
            //Assert.AreEqual(finalPI.MethodDeclarations[0].Name.Text, (extPI.StageMethodCalls[0].Target as VariableReferenceExpression).Name.Text);

            //Assert.AreEqual(1, extPI.VariableReferenceExpressions.Count);
            //Assert.AreEqual(2, finalPI.Variables.Count);
            //Assert.AreEqual(finalPI.Variables[1], extPI.VariableReferenceExpressions[0].TypeInference.Declaration);
            //Assert.AreEqual(finalPI.Variables[1].Name.Text, extPI.VariableReferenceExpressions[0].TypeInference.Declaration.Name.Text);
        }

        [Test]
        public void TestMergeSemantics()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("SemanticTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixer = new ParadoxShaderMixer(mcm.Mixins["SemanticTest"], mcm.Mixins, null);
            mixer.Mix();

            //Assert.AreEqual(1, mixer.MixedShader.Members.OfType<Variable>().Count());
        }

        [Test]
        public void TestStreams()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StreamTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixer = new ParadoxShaderMixer(mcm.Mixins["StreamTest"], mcm.Mixins, null);
            mixer.Mix();
        }

        [Test]
        public void TestStageAssignement()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StageValueReference"),
                    new ShaderClassSource("StageValueTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["StageValueTest"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["StageValueReference"].DeepClone() });
            var mixerFinal = new ParadoxShaderMixer(mcmFinal.Mixins["StageValueTest"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Test]
        public void TestClone()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("CloneTestBase"),
                    new ShaderClassSource("CloneTestRoot"),
                    new ShaderClassSource("CloneTestExtern")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            var keys = mcmFinal.Mixins["CloneTestRoot"].VariableDependencies.Keys.ToList();
            extDictionary.Add(keys[0], new List<ModuleMixin>{ mcm.Mixins["CloneTestExtern"].DeepClone() });
            extDictionary.Add(keys[1], new List<ModuleMixin>{ mcm.Mixins["CloneTestExtern"].DeepClone() });
            var mixerFinal = new ParadoxShaderMixer(mcmFinal.Mixins["CloneTestRoot"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Test]
        public void TestBaseThis()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("BaseTestChild"),
                    new ShaderClassSource("BaseTestInter"),
                    new ShaderClassSource("BaseTestParent")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new ParadoxShaderMixer(mcm.Mixins["BaseTestChild"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Test]
        public void TestForEachStatementExpand()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ForEachTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new ParadoxShaderMixer(mcm.Mixins["ForEachTest"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Test]
        public void TestStreamSolver()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StreamChild"),
                    new ShaderClassSource("StreamParent0"),
                    new ShaderClassSource("StreamParent1")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new ParadoxShaderMixer(mcm.Mixins["StreamChild"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Test]
        public void TestNonStageStream()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("NonStageStreamTest"),
                    new ShaderClassSource("StreamParent2"),
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();
            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            var keys = mcmFinal.Mixins["NonStageStreamTest"].VariableDependencies.Keys.ToList();
            extDictionary.Add(keys[0], new List<ModuleMixin> { mcm.Mixins["StreamParent2"].DeepClone() });
            extDictionary.Add(keys[1], new List<ModuleMixin> { mcm.Mixins["StreamParent2"].DeepClone() });
            var mixerFinal = new ParadoxShaderMixer(mcmFinal.Mixins["NonStageStreamTest"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Test]
        public void TestStreamSolverExtern()
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("StreamChild"),
                    new ShaderClassSource("StreamParent0"),
                    new ShaderClassSource("StreamParent1"),
                    new ShaderClassSource("StreamSolverExternTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();

            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            extDictionary.Add(mcmFinal.Mixins["StreamSolverExternTest"].VariableDependencies.First().Key, new List<ModuleMixin>{ mcm.Mixins["StreamChild"].DeepClone() });
            var mixerFinal = new ParadoxShaderMixer(mcmFinal.Mixins["StreamSolverExternTest"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Test]
        public void TestExternArray() // check behavior with a array of compositions
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ExternMixin"),
                    new ShaderClassSource("TestExternArray")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mcmFinal = mcm.DeepClone();

            var extDictionary = new Dictionary<Variable, List<ModuleMixin>>();
            var mixins = new List<ModuleMixin> { mcm.Mixins["ExternMixin"].DeepClone(), mcm.Mixins["ExternMixin"].DeepClone() };
            extDictionary.Add(mcmFinal.Mixins["TestExternArray"].VariableDependencies.First().Key, mixins);
            var mixerFinal = new ParadoxShaderMixer(mcmFinal.Mixins["TestExternArray"], mcmFinal.Mixins, extDictionary);
            mixerFinal.Mix();
        }

        [Test]
        public void TestConstantBuffer() // check behavior with a array of compositions
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("ConstantBufferTest")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new ParadoxShaderMixer(mcm.Mixins["ConstantBufferTest"], mcm.Mixins, null);
            mixerFinal.Mix();
        }

        [Test]
        public void TestComputeShader() // check behavior with a array of compositions
        {
            var shaderClassSourceList = new HashSet<ShaderClassSource>
                {
                    new ShaderClassSource("TestComputeShader")
                };
            var mcm = new ShaderCompilationContext(shaderClassSourceList, shaderLoader.LoadClassSource);
            mcm.Run();

            Assert.IsFalse(mcm.ErrorWarningLog.HasErrors);

            var mixerFinal = new ParadoxShaderMixer(mcm.Mixins["TestComputeShader"], mcm.Mixins, null);
            mixerFinal.Mix();
        }
    }
}
*/