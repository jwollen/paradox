﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using System.IO;
using System.Linq;

using NUnit.Framework;

using SiliconStudio.Core.IO;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.Storage;
using SiliconStudio.Paradox.Shaders.Parser;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Ast.Hlsl;

namespace SiliconStudio.Paradox.Shaders.Tests
{
    public class TestMixinMacros
    {
        private ShaderMixinParser shaderMixinParser;

        [SetUp]
        public void Init()
        {
            // Create and mount database file system
            var objDatabase = new ObjectDatabase("/data/db", "index", "/local/db");
            var databaseFileProvider = new DatabaseFileProvider(objDatabase);
            AssetManager.GetFileProvider = () => databaseFileProvider;

            shaderMixinParser = new ShaderMixinParser();
            shaderMixinParser.SourceManager.LookupDirectoryList.Add("/shaders"); 
        }

        [Test]
        public void TestMacros()
        {
            // test that macros are correctly used
            var baseMixin = new ShaderMixinSource();
            baseMixin.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D", 1);
            baseMixin.Macros.Add(new ShaderMacro("MACRO_TEST", "int"));
            baseMixin.Mixins.Add(new ShaderClassSource("TestMacros"));
            
            var macros0 = new ShaderMixinSource();
            macros0.Mixins.Add(new ShaderClassSource("MacroTest"));
            baseMixin.Compositions.Add("macros0", macros0);

            var macros1 = new ShaderMixinSource();
            macros1.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros1.Macros.Add(new ShaderMacro("MACRO_TEST", "float"));
            baseMixin.Compositions.Add("macros1", macros1);

            var macros2 = new ShaderMixinSource();
            macros2.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros2.Macros.Add(new ShaderMacro("MACRO_TEST", "float4"));
            baseMixin.Compositions.Add("macros2", macros2);

            var parsingResult = shaderMixinParser.Parse(baseMixin, baseMixin.Macros.ToArray());
            
            Assert.IsFalse(parsingResult.HasErrors);
            var cBufferVar = parsingResult.Shader.Declarations.OfType<ConstantBuffer>().First(x => x.Name == "Globals").Members.OfType<Variable>().ToList();
            Assert.AreEqual(1, cBufferVar.Count(x => x.Type.Name.Text == "int"));
            Assert.AreEqual(1, cBufferVar.Count(x => x.Type.Name.Text == "float"));
            Assert.AreEqual(1, cBufferVar.Count(x => x.Type.Name.Text == "float4"));

            // test clash when reloading
            var baseMixin2 = new ShaderMixinSource();
            baseMixin2.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D", 1);
            baseMixin2.Macros.Add(new ShaderMacro("MACRO_TEST", "int"));
            baseMixin2.Mixins.Add(new ShaderClassSource("TestMacros"));

            var macros3 = new ShaderMixinSource();
            macros3.Mixins.Add(new ShaderClassSource("MacroTest"));
            baseMixin2.Compositions.Add("macros0", macros3);

            var macros4 = new ShaderMixinSource();
            macros4.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros4.Macros.Add(new ShaderMacro("MACRO_TEST", "uint4"));
            baseMixin2.Compositions.Add("macros1", macros4);

            var macros5 = new ShaderMixinSource();
            macros5.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros5.Macros.Add(new ShaderMacro("MACRO_TEST", "float4"));
            baseMixin2.Compositions.Add("macros2", macros5);

            var parsingResult2 = shaderMixinParser.Parse(baseMixin2, baseMixin2.Macros.ToArray());

            Assert.IsFalse(parsingResult.HasErrors);
            var cBufferVar2 = parsingResult2.Shader.Declarations.OfType<ConstantBuffer>().First(x => x.Name == "Globals").Members.OfType<Variable>().ToList();
            Assert.AreEqual(1, cBufferVar2.Count(x => x.Type.Name.Text == "int"));
            Assert.AreEqual(1, cBufferVar2.Count(x => x.Type.Name.Text == "uint4"));
            Assert.AreEqual(1, cBufferVar2.Count(x => x.Type.Name.Text == "float4"));
        }

        [Test]
        public void TestMacrosArray()
        {
            // test that macros are correctly used through an array
            var baseMixin = new ShaderMixinSource();
            baseMixin.AddMacro("SILICONSTUDIO_PARADOX_GRAPHICS_API_DIRECT3D", 1);
            baseMixin.Macros.Add(new ShaderMacro("MACRO_TEST", "int"));
            baseMixin.Mixins.Add(new ShaderClassSource("TestMacrosArray"));

            var compositionArray = new ShaderArraySource();

            var macros0 = new ShaderMixinSource();
            macros0.Mixins.Add(new ShaderClassSource("MacroTest"));
            compositionArray.Add(macros0);

            var macros1 = new ShaderMixinSource();
            macros1.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros1.Macros.Add(new ShaderMacro("MACRO_TEST", "float"));
            compositionArray.Add(macros1);

            var macros2 = new ShaderMixinSource();
            macros2.Mixins.Add(new ShaderClassSource("MacroTest"));
            macros2.Macros.Add(new ShaderMacro("MACRO_TEST", "float4"));
            compositionArray.Add(macros2);
            
            baseMixin.Compositions.Add("macrosArray", compositionArray);

            var parsingResult = shaderMixinParser.Parse(baseMixin, baseMixin.Macros.ToArray());

            Assert.IsFalse(parsingResult.HasErrors);
            var cBufferVar = parsingResult.Shader.Declarations.OfType<ConstantBuffer>().First(x => x.Name == "Globals").Members.OfType<Variable>().ToList();
            Assert.AreEqual(1, cBufferVar.Count(x => x.Type.Name.Text == "int"));
            Assert.AreEqual(1, cBufferVar.Count(x => x.Type.Name.Text == "float"));
            Assert.AreEqual(1, cBufferVar.Count(x => x.Type.Name.Text == "float4"));
        }


        public void Run()
        {
            Init();
            //TestMacros();
            TestMacrosArray();
        }

        public static void Main()
        {
            var test = new TestMixinMacros();
            test.Run();
        }
    }
}
