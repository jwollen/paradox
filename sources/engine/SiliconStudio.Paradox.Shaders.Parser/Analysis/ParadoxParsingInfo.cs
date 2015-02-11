// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;

using SiliconStudio.Paradox.Shaders.Parser.Mixins;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Ast.Hlsl;
using SiliconStudio.Shaders.Parser;

namespace SiliconStudio.Paradox.Shaders.Parser.Analysis
{
    internal class ParadoxParsingInfo
    {
        #region Public properties

        /// <summary>
        /// Variables that referenced the stage class ( "= stage" )
        /// </summary>
        public HashSet<Variable> StageInitializedVariables { get; private set; }

        /// <summary>
        /// All typedefs
        /// </summary>
        public List<Typedef> Typedefs { get; private set; }

        /// <summary>
        /// All structure definitions
        /// </summary>
        public List<StructType> StructureDefinitions { get; private set; }

        /// <summary>
        /// All the base method calls (base.xxx)
        /// </summary>
        public HashSet<MethodInvocationExpression> BaseMethodCalls { get; private set; }

        /// <summary>
        /// All the method calls that are not base
        /// </summary>
        public HashSet<MethodInvocationExpression> ThisMethodCalls { get; private set; }

        /// <summary>
        /// All the method calls to stage methods
        /// </summary>
        public HashSet<MethodInvocationExpression> StageMethodCalls { get; private set; }

        /// <summary>
        /// All foreach statements
        /// </summary>
        public HashSet<StatementNodeCouple> ForEachStatements { get; private set; }

        /// <summary>
        /// References to members of the current shader
        /// </summary>
        public ReferencesPool ClassReferences { get; private set; }

        /// <summary>
        /// Static references to class members
        /// </summary>
        public ReferencesPool StaticReferences { get; private set; }

        /// <summary>
        /// References to extern members
        /// </summary>
        public ReferencesPool ExternReferences { get; private set; }

        /// <summary>
        /// References to stage initialized variables and methods
        /// </summary>
        public ReferencesPool StageInitReferences { get; private set; }

        /// <summary>
        /// Gets navigable nodes (local variables, base class...etc.)
        /// </summary>
        /// <value>The navigable nodes.</value>
        public List<Node> NavigableNodes { get; private set; }

        /// <summary>
        /// List of the static classes
        /// </summary>
        public HashSet<ModuleMixin> StaticClasses { get; private set; }

        #endregion

        #region Public members

        /// <summary>
        /// Error logger
        /// </summary>
        public ParsingResult ErrorsWarnings = null;

        #endregion

        #region Constructor

        public ParadoxParsingInfo()
        {
            StageInitializedVariables = new HashSet<Variable>();
            Typedefs = new List<Typedef>();
            StructureDefinitions = new List<StructType>();
            BaseMethodCalls = new HashSet<MethodInvocationExpression>();
            ThisMethodCalls = new HashSet<MethodInvocationExpression>();
            StageMethodCalls = new HashSet<MethodInvocationExpression>();
            ForEachStatements = new HashSet<StatementNodeCouple>();
            ClassReferences = new ReferencesPool();
            StaticReferences = new ReferencesPool();
            ExternReferences = new ReferencesPool();
            StageInitReferences = new ReferencesPool();
            StaticClasses = new HashSet<ModuleMixin>();
            NavigableNodes = new List<Node>();
        }

        #endregion
    }
}