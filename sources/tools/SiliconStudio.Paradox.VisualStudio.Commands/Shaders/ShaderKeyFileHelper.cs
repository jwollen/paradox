﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Linq;
using System.Text;

using SiliconStudio.Paradox.Shaders.Parser;
using SiliconStudio.Paradox.Shaders.Parser.Mixins;

namespace SiliconStudio.Paradox.VisualStudio.Commands.Shaders
{
    class ShaderKeyFileHelper
    {
        public static byte[] GenerateCode(string inputFileName, string inputFileContent)
        {
            // Always output a result into the file
            string result;
            try
            {
                var parsingResult = ParadoxShaderParser.TryPreProcessAndParse(inputFileContent, inputFileName);

                if (parsingResult.HasErrors)
                {
                    result = "// Failed to parse the shader:\n" + parsingResult;
                }
                else
                {
                    // Try to generate a mixin code.
                    var shaderKeyGenerator = new ShaderMixinCodeGen(parsingResult.Shader, parsingResult);

                    shaderKeyGenerator.Run();
                    result = shaderKeyGenerator.Text ?? string.Empty;
                }
            }
            catch (Exception ex)
            {
                result = "// Unexpected exceptions occurred while generating the file\n" + ex;
            }

            // We force the UTF8 to include the BOM to match VS default
            var data = Encoding.UTF8.GetBytes(result);
            return Encoding.UTF8.GetPreamble().Concat(data).ToArray();
        }
    }
}