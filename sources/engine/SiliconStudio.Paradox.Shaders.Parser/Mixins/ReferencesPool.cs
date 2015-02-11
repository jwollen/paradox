// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SiliconStudio.Paradox.Shaders.Parser.Analysis;
using SiliconStudio.Shaders.Ast;

namespace SiliconStudio.Paradox.Shaders.Parser.Mixins
{
    [DebuggerDisplay("Variables[{VariablesReferences.Count}] Methods[{MethodsReferences.Count}]")]
    internal class ReferencesPool
    {
        /// <summary>
        /// List of all the variable references
        /// </summary>
        public Dictionary<Variable, HashSet<ExpressionNodeCouple>> VariablesReferences { get; private set; }

        /// <summary>
        /// List of all the variable references
        /// </summary>
        public Dictionary<MethodDeclaration, HashSet<MethodInvocationExpression>> MethodsReferences { get; private set; }

        public ReferencesPool()
        {
            VariablesReferences = new Dictionary<Variable, HashSet<ExpressionNodeCouple>>();
            MethodsReferences = new Dictionary<MethodDeclaration, HashSet<MethodInvocationExpression>>();
        }

        /// <summary>
        /// Merge the argument references into this one
        /// </summary>
        /// <param name="pool">the ReferencePool</param>
        public void Merge(ReferencesPool pool)
        {
            // merge the VariablesReferences
            foreach (var variableReference in pool.VariablesReferences)
            {
                if (!VariablesReferences.ContainsKey(variableReference.Key))
                    VariablesReferences.Add(variableReference.Key, new HashSet<ExpressionNodeCouple>());

                VariablesReferences[variableReference.Key].UnionWith(variableReference.Value);
            }
            // merge the MethodsReferences
            foreach (var methodReference in pool.MethodsReferences)
            {
                if (!MethodsReferences.ContainsKey(methodReference.Key))
                    MethodsReferences.Add(methodReference.Key, new HashSet<MethodInvocationExpression>());

                MethodsReferences[methodReference.Key].UnionWith(methodReference.Value);
            }
        }

        /// <summary>
        /// Regen the keys bacause they could have been modified
        /// </summary>
        public void RegenKeys()
        {
            VariablesReferences = VariablesReferences.ToDictionary(variable => variable.Key, variable => variable.Value);
            MethodsReferences = MethodsReferences.ToDictionary(method => method.Key, variable => variable.Value);
        }

        /// <summary>
        /// Insert a variable reference
        /// </summary>
        /// <param name="variable">the variable</param>
        /// <param name="expression">the reference</param>
        public void InsertVariable(Variable variable, ExpressionNodeCouple expression)
        {
            if (!VariablesReferences.ContainsKey(variable))
                VariablesReferences.Add(variable, new HashSet<ExpressionNodeCouple>());
            VariablesReferences[variable].Add(expression);
        }

        /// <summary>
        /// Insert a method reference
        /// </summary>
        /// <param name="methodDeclaration">the method</param>
        /// <param name="expression">the reference</param>
        public void InsertMethod(MethodDeclaration methodDeclaration, MethodInvocationExpression expression)
        {
            if (!MethodsReferences.ContainsKey(methodDeclaration))
                MethodsReferences.Add(methodDeclaration, new HashSet<MethodInvocationExpression>());
            MethodsReferences[methodDeclaration].Add(expression);
        }
    }
}
