﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Shaders.Compiler
{
    /// <summary>
    /// Result of a compilation.
    /// </summary>
    public class CompilerResults : LoggerResult
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="LoggerResult" /> class.
        /// </summary>
        public CompilerResults() : base(null)
        {
        }

        /// <summary>
        /// Gets or sets the main bytecode.
        /// </summary>
        /// <value>
        /// The main bytecode.
        /// </value>
        public TaskOrResult<EffectBytecodeCompilerResult> Bytecode { get; set; }

        /// <summary>
        /// Parameters used to create this shader.
        /// </summary>
        /// <value>The ParameterCollection.</value>
        public ShaderMixinParameters UsedParameters { get; set; }
    }
}