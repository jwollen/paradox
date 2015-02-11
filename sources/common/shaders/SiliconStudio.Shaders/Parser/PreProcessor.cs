﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using System.IO;
using System.Text;

using CppNet;

using SiliconStudio.Shaders.Parser;

namespace SiliconStudio.Shaders
{
    /// <summary>
    /// C++ preprocessor using D3DPreprocess method from d3dcompiler API.
    /// </summary>
    public partial class PreProcessor
    {
        /// <summary>
        /// Preprocesses the provided shader or effect source.
        /// </summary>
        /// <param name="shaderSource">An array of bytes containing the raw source of the shader or effect to preprocess.</param>
        /// <param name="sourceFileName">Name of the source file.</param>
        /// <param name="defines">A set of macros to define during preprocessing.</param>
        /// <param name="includeDirectories">The include directories used by the preprocessor.</param>
        /// <returns>
        /// The preprocessed shader source.
        /// </returns>
        public static string Run(string shaderSource, string sourceFileName, ShaderMacro[] defines = null, params string[] includeDirectories)
        {
            var cpp = new Preprocessor();
            cpp.addFeature(Feature.DIGRAPHS);
            cpp.addWarning(Warning.IMPORT);
            cpp.addFeature(Feature.INCLUDENEXT);
            cpp.addFeature(Feature.LINEMARKERS);
            
            // TODO: Handle warning and errors properly instead of relying only on exception
            // Don't setup a listener and get any errors via exceptions
            cpp.setListener(new ErrorListener());

            // Pass defines
            if (defines != null)
            {
                foreach (var define in defines)
                {
                    if (!string.IsNullOrWhiteSpace(define.Name))
                    {
                        cpp.addMacro(define.Name, define.Definition ?? string.Empty);
                    }
                }
            }

            // Setup input directories.
            var tempDirectories = new List<string>() { Path.GetDirectoryName(sourceFileName) };
            tempDirectories.AddRange(includeDirectories);
            cpp.setQuoteIncludePath(tempDirectories);

            var inputSource = new StringLexerSource(shaderSource, true, sourceFileName);

            cpp.addInput(inputSource);

            var textBuilder = new StringBuilder();

            var isEndOfStream = false;
            while (!isEndOfStream)
            {
                Token tok = cpp.token();
                switch (tok.getType())
                {
                    case Token.EOF:
                        isEndOfStream = true;
                        break;
                    case Token.CCOMMENT:
                        var strComment = tok.getText() ?? string.Empty;
                        foreach (var commentChar in strComment)
                        {
                            textBuilder.Append(commentChar == '\n' ? '\n' : ' ');
                        }
                        break;
                    case Token.CPPCOMMENT:
                        break;
                    default:
                        var tokenText = tok.getText();
                        if (tokenText != null)
                        {
                            textBuilder.Append(tokenText);
                        }
                        break;
                }
            }

            return textBuilder.ToString();
        }

        private class ErrorListener : CppNet.PreprocessorListenerBase
        {
            public override void handleError(Source source, int line, int column, string msg)
            {
                base.handleError(source, line, column, msg);
                throw new LexerException("Error at " + line + ":" + column + ": " + msg);
            }
        }
    }
}
