﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

using SiliconStudio.Paradox.Shaders.Parser.Ast;
using SiliconStudio.Paradox.Shaders.Parser.Utility;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Ast.Hlsl;
using SiliconStudio.Shaders.Utility;
using SiliconStudio.Shaders.Visitor;

using StorageQualifier = SiliconStudio.Shaders.Ast.StorageQualifier;

namespace SiliconStudio.Paradox.Shaders.Parser.Mixins
{
    internal class ParadoxClassInstantiator : ShaderVisitor
    {
        private ShaderClassType shaderClassType;

        private LoggerResult logger;

        private bool autoGenericInstances;

        private Dictionary<string, Variable> variableGenerics;

        private Dictionary<string, Expression> expressionGenerics;
        
        private Dictionary<string, Identifier> identifiersGenerics;

        private Dictionary<string, string> stringGenerics;

        private ParadoxClassInstantiator(ShaderClassType classType, Dictionary<string, Expression> expressions, Dictionary<string, Identifier> identifiers, bool autoGenericInstances, LoggerResult log)
            : base(false, false)
        {
            shaderClassType = classType;
            expressionGenerics = expressions;
            identifiersGenerics = identifiers;
            this.autoGenericInstances = autoGenericInstances;
            logger = log;
            variableGenerics = shaderClassType.ShaderGenerics.ToDictionary(x => x.Name.Text, x => x);
        }

        public static void Instantiate(ShaderClassType classType, Dictionary<string, Expression> expressions, Dictionary<string, Identifier> identifiers, bool autoGenericInstances, LoggerResult log)
        {
            var instantiator = new ParadoxClassInstantiator(classType, expressions, identifiers, autoGenericInstances, log);
            instantiator.Run();
        }

        private void Run()
        {
            stringGenerics = identifiersGenerics.ToDictionary(x => x.Key, x => x.Value.ToString());

            foreach (var baseClass in shaderClassType.BaseClasses)
                VisitDynamic(baseClass); // look for IdentifierGeneric

            foreach (var member in shaderClassType.Members)
                VisitDynamic(member); // look for IdentifierGeneric and Variable

            int insertIndex = 0;
            foreach (var variable in shaderClassType.ShaderGenerics)
            {
                // For all string generic argument, don't try to assign an initial value as they are replaced directly at visit time. 
                if (variable.Type is IGenericStringArgument)
                    continue;

                variable.InitialValue = expressionGenerics[variable.Name.Text];
                
                // TODO: be more precise

                if (!(variable.InitialValue is VariableReferenceExpression || variable.InitialValue is MemberReferenceExpression))
                {
                    variable.Qualifiers |= StorageQualifier.Const;
                    variable.Qualifiers |= SiliconStudio.Shaders.Ast.Hlsl.StorageQualifier.Static;
                }
                // Because FindDeclaration is broken for variable declared at the scope of the class, make sure  to
                // put const at the beginning of the class to allow further usage of the variable to work
                shaderClassType.Members.Insert(insertIndex++, variable);
            }
        }

        [Visit]
        protected void Visit(MemberReferenceExpression memberReferenceExpression)
        {
            Visit((Node)memberReferenceExpression);

            // Try to find usage of all MemberName 'yyy' in reference expressions like "xxx.yyy" and replace by their
            // generic instantiation
            var memberVariableName = memberReferenceExpression.Member.Text;
            if (variableGenerics.ContainsKey(memberVariableName) && variableGenerics[memberVariableName].Type is MemberName)
            {
                string memberName;
                if (stringGenerics.TryGetValue(memberVariableName, out memberName) && !autoGenericInstances)
                {
                    memberReferenceExpression.Member = new Identifier(memberName);
                }
                else
                {
                    memberReferenceExpression.TypeInference.Declaration = variableGenerics[memberVariableName];
                }
            }
        }

        [Visit]
        protected void Visit(Variable variable)
        {
            Visit((Node)variable);
            //TODO: check types

            // Don't perform any replacement if we are just auto instancing shaders
            if (autoGenericInstances)
            {
                return;
            }

            // no call on base
            foreach (var sem in variable.Qualifiers.Values.OfType<Semantic>())
            {
                string replacementSemantic;
                if (stringGenerics.TryGetValue(sem.Name, out replacementSemantic))
                {
                    if (logger != null && !(variableGenerics[sem.Name].Type is SemanticType))
                        logger.Warning(ParadoxMessageCode.WarningUseSemanticType, variable.Span, variableGenerics[sem.Name]);
                    sem.Name = replacementSemantic;
                }
            }

            foreach (var annotation in variable.Attributes.OfType<AttributeDeclaration>().Where(x => x.Name == "Link" && x.Parameters.Count > 0))
            {
                var linkName = (string)annotation.Parameters[0].Value;

                if (String.IsNullOrEmpty(linkName))
                    continue;

                var replacements = new List<Tuple<string, int>>();

                foreach (var generic in variableGenerics.Where(x => x.Value.Type is LinkType))
                {
                    var index = linkName.IndexOf(generic.Key, 0);
                    if (index >= 0)
                        replacements.Add(Tuple.Create(generic.Key, index));
                }

                if (replacements.Count > 0)
                {
                    var finalString = "";
                    var currentIndex = 0;
                    foreach (var replacement in replacements.OrderBy(x => x.Item2))
                    {
                        var replacementIndex = replacement.Item2;
                        var stringToReplace = replacement.Item1;

                        if (replacementIndex - currentIndex > 0)
                            finalString += linkName.Substring(currentIndex, replacementIndex - currentIndex);
                        finalString += stringGenerics[stringToReplace];
                        currentIndex = replacementIndex + stringToReplace.Length;
                    }

                    if (currentIndex < linkName.Length)
                        finalString += linkName.Substring(currentIndex);

                    annotation.Parameters[0] = new Literal(finalString);
                }
            }
        }

        [Visit]
        protected void Visit(IdentifierGeneric identifierGeneric)
        {
            Visit((Node)identifierGeneric);

            for (var i = 0; i < identifierGeneric.Identifiers.Count; ++i)
            {
                Identifier replacement;
                if (identifiersGenerics.TryGetValue(identifierGeneric.Identifiers[i].ToString(), out replacement))
                    identifierGeneric.Identifiers[i] = replacement;
            }
        }
    }
}
