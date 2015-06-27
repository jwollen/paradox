﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Ast.Hlsl;

namespace SiliconStudio.Shaders.Visitor
{
    /// <summary>
    /// The strip visitor collects all function and declaration used by a set of entrypoints
    /// and remove any unreferenced/unused declaration.
    /// </summary>
    public class StripVisitor : ShaderVisitor
    {
        private Dictionary<Node, HashSet<Node>> indirectReferences;
        private readonly string[] entryPoints;

        /// <summary>
        /// Initializes a new instance of the <see cref="StripVisitor"/> class.
        /// </summary>
        /// <param name="entryPoints">The entry points to filter.</param>
        public StripVisitor(params string[] entryPoints) : base(true, true)
        {
            this.entryPoints = entryPoints;
            this.StripUniforms = true;
            this.KeepConstantBuffers = true;
        }

        public bool StripUniforms { get; set; }

        public bool KeepConstantBuffers { get; set; }

        [Visit]
        public void Visit(MethodInvocationExpression methodInvocationExpression)
        {
            Visit((Node)methodInvocationExpression);
            AddReference(GetDeclarationContainer(), (Node)methodInvocationExpression.TypeInference.Declaration);
        }

        [Visit]
        public void Visit(VariableReferenceExpression variableReferenceExpression)
        {
            Visit((Node)variableReferenceExpression);
            AddReference(GetDeclarationContainer(), (Node)variableReferenceExpression.TypeInference.Declaration);
        }

        private ConstantBuffer currentConstantBuffer = null;

        [Visit]
        public void Visit(ConstantBuffer constantBuffer)
        {
            currentConstantBuffer = constantBuffer;
            Visit((Node)constantBuffer);
            currentConstantBuffer = null;
        }

        protected override bool PreVisitNode(Node node)
        {
            // Sometimes it is desirable that constant buffer are not modified so that
            // they can be shared between different stages, even if some variables are unused.
            // In this case, use symetric reference so that using a constant buffer will include all its variables.
            if (KeepConstantBuffers && currentConstantBuffer != null && node is IDeclaration)
            {
                AddReference(node, currentConstantBuffer);
                AddReference(currentConstantBuffer, node);
            }

            return base.PreVisitNode(node);

        }

        [Visit]
        public void Visit(Parameter parameter)
        {
            Visit((Node)parameter);
            var containers = GetDeclarationContainers();
            var container = containers[containers.Count - 2];
            AddReference((Node)container, parameter);
        }

        [Visit]
        public void Visit(TypeBase typeReference)
        {
            Visit((Node)typeReference);
            AddReference(GetDeclarationContainer(), (Node)typeReference.TypeInference.Declaration);
        }

        [Visit]
        public void Visit(MethodDefinition methodDefinition)
        {
            Visit((Node)methodDefinition);

            // If a method definition has a method declaration, we must link them together
            if (!ReferenceEquals(methodDefinition.Declaration, methodDefinition))
            {
                AddReference(methodDefinition.Declaration, methodDefinition);
            }
        }

        [Visit]
        public void Visit(Variable variable)
        {
            Visit((Node)variable);
            var containers = GetDeclarationContainers();
            if (containers.Count > 1)
            {
                var container = containers[containers.Count - 2];
                AddReference((Node)container, variable);
            }
        }
        
        [Visit]
        public void Visit(Shader shader)
        {
            indirectReferences = new Dictionary<Node, HashSet<Node>>();

            // Visit AST.
            Visit((Node) shader);

            // Get list of function referenced (directly or indirectly) by entry point.
            // Using hashset and recursion to avoid cycle.
            var collectedReferences = new List<Node>();
            foreach (var entryPointName in entryPoints)
            {
                // Find entry point
                var entryPoint = shader.Declarations.OfType<MethodDefinition>().FirstOrDefault(x => x.Name == entryPointName);

                if (entryPoint == null)
                    throw new ArgumentException(string.Format("Could not find entry point named {0}", entryPointName));

                CollectReferences(collectedReferences, entryPoint);
            }

            if (KeepConstantBuffers)
            {
                // Include dependencies of cbuffer (i.e. dependent types)
                foreach (var variable in shader.Declarations.OfType<ConstantBuffer>())
                {
                    CollectReferences(collectedReferences, variable);
                }
            }

            StripDeclarations(shader.Declarations, collectedReferences, StripUniforms);
        }

        /// <summary>
        /// Strips the declarations.
        /// </summary>
        /// <param name="nodes">The nodes.</param>
        /// <param name="collectedReferences">The collected references.</param>
        private static void StripDeclarations(IList<Node> nodes, ICollection<Node> collectedReferences, bool stripUniforms)
        {
            // Remove all the unreferenced function amd types declaration from the shader.
            for (int i = 0; i < nodes.Count; i++)
            {
                var declaration = nodes[i];
                if (declaration is Variable)
                {
                    var variableDeclaration = (Variable)declaration;
                    if ((!stripUniforms && variableDeclaration.Qualifiers.Contains(Ast.StorageQualifier.Uniform)) || variableDeclaration.Name.Text == "ParadoxFlipRendertarget")
                        continue;

                    if (variableDeclaration.IsGroup)
                    {
                        variableDeclaration.SubVariables.RemoveAll(x => !collectedReferences.Contains(x));
                        if (variableDeclaration.SubVariables.Count == 0)
                        {
                            nodes.RemoveAt(i);
                            i--;
                        }
                    }
                    else if (!collectedReferences.Contains(declaration))
                    {
                        nodes.RemoveAt(i);
                        i--;                        
                    }
                }
                else if (declaration is IDeclaration && !collectedReferences.Contains(declaration))
                {
                    nodes.RemoveAt(i);
                    i--;
                } 
                else if (declaration is ConstantBuffer)
                {
                    // Do not stript constant buffer anymore, they should be kept as is
                    if (stripUniforms)
                    {
                        var constantBuffer = (ConstantBuffer)declaration;
                        StripDeclarations(constantBuffer.Members, collectedReferences, stripUniforms);
                    }
                }
            }            
        }

        /// <summary>
        /// Helper to collects the referenced declarations recursively.
        /// </summary>
        /// <param name="collectedReferences">The collected declarations.</param>
        /// <param name="reference">The reference to collect.</param>
        private void CollectReferences(List<Node> collectedReferences, Node reference)
        {
            if (!collectedReferences.Contains(reference))
            {
                // Collect reference (if not already added)
                collectedReferences.Add(reference);

                // Collect recursively
                HashSet<Node> referencedFunctions;
                if (indirectReferences.TryGetValue((Node)reference, out referencedFunctions))
                {
                    foreach (var referencedFunction in referencedFunctions)
                        CollectReferences(collectedReferences, referencedFunction);
                }
            }
        }

        private void AddReference(Node parent, Node declaration)
        {
            if (parent != null && declaration != null)
            {
                HashSet<Node> childReferences;
                if (!indirectReferences.TryGetValue(parent, out childReferences))
                {
                    childReferences = new HashSet<Node>();
                    indirectReferences[parent] = childReferences;
                }
                if (!childReferences.Contains(declaration))
                    childReferences.Add(declaration);
            }
        }


        private Node GetDeclarationContainer()
        {
            // By default use the method definition as the main declarator container
            var methodDefinition = (Node)NodeStack.OfType<MethodDefinition>().LastOrDefault();
            if (methodDefinition != null)
                return methodDefinition;

            // Else use the IDeclaration
            return (Node)NodeStack.OfType<IDeclaration>().LastOrDefault();
        }

        private List<IDeclaration> GetDeclarationContainers()
        {
            return NodeStack.OfType<IDeclaration>().ToList();
        }
    }
}

