// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using System.Linq;

using SiliconStudio.Paradox.Shaders.Parser.Ast;
using SiliconStudio.Paradox.Shaders.Parser.Mixins;
using SiliconStudio.Paradox.Shaders.Parser.Utility;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Ast.Hlsl;
using SiliconStudio.Shaders.Parser;
using SiliconStudio.Shaders.Visitor;

using StorageQualifier = SiliconStudio.Shaders.Ast.Hlsl.StorageQualifier;

namespace SiliconStudio.Paradox.Shaders.Parser.Analysis
{
    internal class ParadoxSemanticAnalysis : ParadoxTypeAnalysis
    {
        #region Static members

        /// <summary>
        /// List of useful language keywords
        /// </summary>
        private static readonly string[] ParadoxKeywords = { "base", "streams", "this" };

        #endregion

        #region Private members

        /// <summary>
        /// The structure that will store all the information
        /// </summary>
        private ParadoxParsingInfo parsingInfo;
        
        /// <summary>
        /// List of all the mixins inside the module
        /// </summary>
        private readonly HashSet<ModuleMixin> moduleMixins = new HashSet<ModuleMixin>();

        /// <summary>
        /// The module that is the context of the analysis
        /// </summary>
        private readonly ModuleMixin analyzedModuleMixin = null;

        /// <summary>
        /// The method currently visited
        /// </summary>
        private MethodDeclaration currentVisitedMethod = null;

        /// <summary>
        /// a flag stating if the visitor visits a sampler
        /// </summary>
        private bool inSampler = false;

        /// <summary>
        /// Status of the assignment
        /// </summary>
        private AssignmentOperatorStatus currentAssignmentOperatorStatus = AssignmentOperatorStatus.Read;

        /// <summary>
        /// A flag for expanding foreach statements
        /// </summary>
        private bool expandForEachStatements;

        #endregion

        #region Constructor and helpers

        /// <summary>
        /// Initializes a new instance of the <see cref="ParadoxSemanticAnalysis"/> class.
        /// </summary>
        /// <param name="result">The result</param>
        /// <param name="analyzedMixin">the context in which the analysis is set</param>
        /// <param name="moduleMixinsInCompilationGroup">the list of all the modules that are not in the inheritance hierarchy of the context</param>
        public ParadoxSemanticAnalysis(ParsingResult result, ModuleMixin analyzedMixin, List<ModuleMixin> moduleMixinsInCompilationGroup)
            : base(result)
        {
            analyzedModuleMixin = analyzedMixin;
            
            ScopeStack.First().AddDeclaration(StreamsType.ThisStreams);

            var currentScope = new ScopeDeclaration(analyzedMixin.Shader);
            ScopeStack.Push(currentScope);

            currentScope.AddDeclarations(analyzedMixin.VirtualTable.Typedefs);
            currentScope.AddDeclarations(analyzedMixin.VirtualTable.StructureTypes);
            currentScope.AddDeclarations(analyzedMixin.VirtualTable.Variables.Select(x => x.Variable));
            currentScope.AddDeclarations(analyzedMixin.VirtualTable.Methods.Select(x => x.Method));
            currentScope.AddDeclarations(analyzedMixin.InheritanceList.Select(x => x.Shader));

            // add the mixins in the compilation group
            var sd = new ScopeDeclaration();
            ScopeStack.Push(sd);
            foreach (var mixin in moduleMixinsInCompilationGroup)
            {
                moduleMixins.Add(mixin);
                sd.AddDeclaration(mixin.Shader);
            }
        }

        #endregion

        #region Public static methods

        /// <summary>
        /// Run the analysis
        /// </summary>
        /// <param name="mixinToAnalyze">the current context (virtual table) from mixin inheritance</param>
        /// <param name="compilationContext">List of all the mixin in the compilation context</param>
        /// <returns>true if the shader is correct, false otherwise</returns>
        public static ParadoxParsingInfo RunAnalysis(ModuleMixin mixinToAnalyze, List<ModuleMixin> compilationContext, bool transformForEach = false)
        {
            var shader = new Shader();
            shader.Declarations.Add(mixinToAnalyze.Shader);
            var toParse = new ParsingResult { Shader = shader };
            var analysis = new ParadoxSemanticAnalysis(toParse, mixinToAnalyze, compilationContext) { parsingInfo = new ParadoxParsingInfo() };
            analysis.expandForEachStatements = transformForEach;
            analysis.Run();

            // look at the static classes
            analysis.parsingInfo.StaticClasses.UnionWith(analysis.parsingInfo.StaticReferences.VariablesReferences.Select(x => x.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin));
            analysis.parsingInfo.StaticClasses.UnionWith(analysis.parsingInfo.StaticReferences.MethodsReferences.Select(x => x.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin));
            analysis.parsingInfo.StaticClasses.Remove(mixinToAnalyze);
            analysis.parsingInfo.ErrorsWarnings = analysis.ParsingResult;

            return analysis.parsingInfo;
        }

        #endregion

        #region Private and protected methods
        
        /// <summary>
        /// Visits the specified shader type name.
        /// </summary>
        /// <param name="shaderTypeName">Name of the type.</param>
        [Visit]
        protected TypeBase Visit(ShaderTypeName shaderTypeName)
        {
            // just here to prevent a problem when a mixin class is called Texture (that creates an hlsl typename instead)
            // grammar was changed accordingly
            Visit((Node)shaderTypeName);
            return shaderTypeName;
        }

        /// <summary>
        /// Store the method in the correct list
        /// </summary>
        /// <param name="methodDeclaration"></param>
        private void StoreMethod(MethodDeclaration methodDeclaration)
        {
            if (!parsingInfo.ClassReferences.MethodsReferences.ContainsKey(methodDeclaration))
                parsingInfo.ClassReferences.MethodsReferences.Add(methodDeclaration, new HashSet<MethodInvocationExpression>());
        }

        /// <summary>
        /// Checks that the method does not have mixin as parameter or return type
        /// </summary>
        /// <param name="methodDeclaration">the method.</param>
        private void CheckParamatersAndReturnType(MethodDeclaration methodDeclaration)
        {
            foreach (var parameter in methodDeclaration.Parameters)
            {
                if (parameter.Type.TypeInference.Declaration is ShaderClassType)
                    Error(ParadoxMessageCode.ErrorShaderClassTypeParameter, methodDeclaration.Span, methodDeclaration, parameter, analyzedModuleMixin.MixinName);
            }

            if (methodDeclaration.ReturnType.TypeInference.Declaration is ShaderClassType)
                Error(ParadoxMessageCode.ErrorShaderClassReturnType, methodDeclaration.Span, methodDeclaration, analyzedModuleMixin.MixinName);
        }

        /// <summary>
        /// Analyse the method declaration and store it in the correct list
        /// </summary>
        /// <param name="methodDeclaration">The MethodDeclaration</param>
        [Visit]
        protected void Visit(MethodDeclaration methodDeclaration)
        {
            currentVisitedMethod = methodDeclaration;
            
            if (!methodDeclaration.Qualifiers.Contains(ParadoxStorageQualifier.Abstract))
                Error(ParadoxMessageCode.ErrorMissingAbstract, methodDeclaration.Span, methodDeclaration, analyzedModuleMixin.MixinName);
            if (methodDeclaration.Qualifiers.Contains(ParadoxStorageQualifier.Override))
                Error(ParadoxMessageCode.ErrorUnnecessaryOverride, methodDeclaration.Span, methodDeclaration, analyzedModuleMixin.MixinName);

            Visit((Node)methodDeclaration);
            PostMethodDeclarationVisit(methodDeclaration);
        }

        /// <summary>
        /// Analyse the method definition and store it in the correct lists (based on storage and stream usage)
        /// </summary>
        /// <param name="methodDefinition">the MethodDefinition</param>
        /// <returns>the input method definition</returns>
        [Visit]
        protected override MethodDefinition Visit(MethodDefinition methodDefinition)
        {
            currentVisitedMethod = methodDefinition;
            
            if (methodDefinition.Qualifiers.Contains(ParadoxStorageQualifier.Abstract))
                Error(ParadoxMessageCode.ErrorUnnecessaryAbstract, methodDefinition.Span, methodDefinition, analyzedModuleMixin.MixinName);

            var ret = base.Visit(methodDefinition);

            PostMethodDeclarationVisit(methodDefinition);

            return ret;
        }

        /// <summary>
        /// Performs operations applicable for MethodDefinition & MethodDeclaration nodes
        /// </summary>
        /// <param name="methodDeclaration">the method declaration or definition</param>
        private void PostMethodDeclarationVisit(MethodDeclaration methodDeclaration)
        {
            currentVisitedMethod = null;
            StoreMethod(methodDeclaration);
            CheckParamatersAndReturnType(methodDeclaration);
        }

        /// <summary>
        /// Visits the specified variable
        /// </summary>
        /// <param name="variable">The variable</param>
        [Visit]
        protected void Visit(Variable variable)
        {
            if (inSampler)
                return;
            
            if (variable.Type is StateType)
                inSampler = true;

            // type inference for variable
            if (ParentNode is ForEachStatement)
            {
                var forEachStatement = ParentNode as ForEachStatement;
                if (variable == forEachStatement.Variable)
                {
                    var finalType = forEachStatement.Collection.TypeInference.TargetType;
                    if (finalType is ArrayType)
                        finalType = (finalType as ArrayType).Type;
                    variable.Type = finalType;
                    if ((forEachStatement.Collection.TypeInference.Declaration as Variable).Qualifiers.Contains(ParadoxStorageQualifier.Extern))
                        variable.Qualifiers |= ParadoxStorageQualifier.Extern;
                }
            }

            Visit((Node)variable);

            inSampler = false;

            if (currentVisitedMethod == null)
            {
                if (variable.InitialValue is VariableReferenceExpression)
                {
                    var vre = variable.InitialValue as VariableReferenceExpression;
                    if (vre.Name.Text == "stage")
                    {
                        if (variable.Qualifiers.Contains(StorageQualifier.Extern) && variable.Type.TypeInference.Declaration is ClassType)
                            parsingInfo.StageInitializedVariables.Add(variable);
                        else
                            Error(ParadoxMessageCode.ErrorStageInitNotClassType, variable.Span, variable, analyzedModuleMixin.MixinName);
                    }
                }

                if (variable.Qualifiers.Contains(StorageQualifier.Extern))
                {
                    var varType = variable.Type;
                    if (varType is ArrayType)
                        varType = (varType as ArrayType).Type;

                    if (!(varType.TypeInference.Declaration is ClassType))
                        Error(ParadoxMessageCode.ErrorExternNotClassType, variable.Span, variable, analyzedModuleMixin.MixinName);
                }

                // should not happen because extern keyword is set in the ShaderCompilationContext
                if (!variable.Qualifiers.Contains(StorageQualifier.Extern) && variable.Type.TypeInference.Declaration is ClassType)
                    Error(ParadoxMessageCode.ErrorMissingExtern, variable.Span, variable, analyzedModuleMixin.MixinName);
            }

            // check var type
            if (variable.Type is VarType)
            {
                if (variable.InitialValue == null)
                    Error(ParadoxMessageCode.ErrorVarNoInitialValue, variable.Span, variable, analyzedModuleMixin.MixinName);
                else if (variable.InitialValue.TypeInference.TargetType == null)
                    Error(ParadoxMessageCode.ErrorVarNoTypeFound, variable.Span, variable, analyzedModuleMixin.MixinName);
                else
                {
                    variable.Type = variable.InitialValue.TypeInference.TargetType.ResolveType();
                    // If we have a var type referencing a generic type, try to use the non-generic version of it
                    if (variable.Type is GenericType)
                    {
                        variable.Type = ((GenericType)variable.Type).ToNonGenericType();
                    }
                }
            }

            if (variable.ContainsTag(ParadoxTags.ShaderScope))
            {
                if (!parsingInfo.ClassReferences.VariablesReferences.ContainsKey(variable))
                    parsingInfo.ClassReferences.VariablesReferences.Add(variable, new HashSet<ExpressionNodeCouple>());
            }

            if (currentVisitedMethod != null && !(ParentNode is ForEachStatement))
            {
                if (FindFinalType(variable.Type) is ShaderClassType)
                    Error(ParadoxMessageCode.ErrorShaderVariable, variable.Span, variable, analyzedModuleMixin.MixinName);
            }
        }

        /// <summary>
        /// Find the base type in case of array
        /// </summary>
        /// <param name="typeBase">the type to explore</param>
        /// <returns>the base type</returns>
        private static TypeBase FindFinalType(TypeBase typeBase)
        {
            if (typeBase is ArrayType)
                return FindFinalType((typeBase as ArrayType).Type);
            return typeBase.ResolveType();
        }

        /// <summary>
        /// store the Typedef
        /// </summary>
        /// <param name="typedef">the Typedef</param>
        [Visit]
        private void Visit(Typedef typedef)
        {
            Visit((Node)typedef);

            if (currentVisitedMethod != null)
                Error(ParadoxMessageCode.ErrorTypedefInMethod, typedef.Span, typedef, currentVisitedMethod, analyzedModuleMixin.MixinName);

            parsingInfo.Typedefs.Add(typedef);
        }

        /// <summary>
        /// Visit a technique, store an error
        /// </summary>
        /// <param name="technique">the technique</param>
        [Visit]
        public override void Visit(Technique technique)
        {
            Error(ParadoxMessageCode.ErrorTechniqueFound, technique.Span, technique, analyzedModuleMixin.MixinName); // TODO: remove because parsing may fail before
        }

        /// <summary>
        /// Visits the specified member reference.
        /// </summary>
        /// <param name="memberReference">The member reference.</param>
        protected override void CommonVisit(MemberReferenceExpression memberReference)
        {
            var targetDecl = memberReference.Target.TypeInference.Declaration;
            var variableTargetDecl = targetDecl as Variable;

            if (memberReference.Target is IndexerExpression)
                variableTargetDecl = (memberReference.Target as IndexerExpression).Target.TypeInference.Declaration as Variable;

            if (variableTargetDecl != null && memberReference.TypeInference.Declaration == null && variableTargetDecl.Qualifiers.Contains(StorageQualifier.Extern)) // from composition
            {
                var varType = variableTargetDecl.Type;
                if (varType is ArrayType)
                    varType = (varType as ArrayType).Type;

                var matchingDecls = FindDeclarationsFromObject(varType, memberReference.Member.Text).ToList();
                var varDecl = matchingDecls.OfType<Variable>().FirstOrDefault();
                var methodDecl = matchingDecls.OfType<MethodDeclaration>().FirstOrDefault();
                var shaderDecl = matchingDecls.OfType<ShaderClassType>().FirstOrDefault();

                if (varDecl != null)
                {
                    memberReference.TypeInference.Declaration = varDecl;
                    memberReference.TypeInference.TargetType = varDecl.Type.ResolveType();

                    if (!(ParentNode is MemberReferenceExpression) || varType is VectorType) // do not store the intermediate references, only the last one - except for vector types
                    {
                        if (IsStageInitMember(memberReference))
                            memberReference.SetTag(ParadoxTags.StageInitRef, null);
                        else
                            memberReference.SetTag(ParadoxTags.ExternRef, null);
                    }
                }
                else if (shaderDecl != null)
                {
                    memberReference.TypeInference.Declaration = shaderDecl;
                    memberReference.TypeInference.TargetType = shaderDecl.ResolveType();
                }
                else if (methodDecl == null)
                {
                    Error(ParadoxMessageCode.ErrorExternMemberNotFound, memberReference.Span, memberReference, variableTargetDecl.Type, analyzedModuleMixin.MixinName);
                }
            }
            else if (targetDecl is ShaderClassType)
                FindMemberTypeReference(targetDecl as ShaderClassType, memberReference);
            else
                base.CommonVisit(memberReference);

            if (IsStreamMember(memberReference))
            {
                if (!(memberReference.Target.TypeInference.TargetType is VectorType
                    || memberReference.Target.TypeInference.TargetType != null && memberReference.Target.TypeInference.TargetType.TypeInference.TargetType is VectorType)) // do not look deeper in vector types
                    CheckStreamMemberReference(memberReference);

                if (memberReference.TypeInference.Declaration is Variable)
                {
                    var refAsVariable = memberReference.TypeInference.Declaration as Variable;
                    if (!(refAsVariable.Type is MemberName) && !refAsVariable.Qualifiers.Contains(ParadoxStorageQualifier.Stream) && !refAsVariable.Qualifiers.Contains(ParadoxStorageQualifier.PatchStream))
                        Error(ParadoxMessageCode.ErrorExtraStreamsPrefix, memberReference.Span, memberReference, refAsVariable, analyzedModuleMixin.MixinName);
                }
            }
            else if (IsInputOutputMember(memberReference))
            {
                CheckStreamMemberReference(memberReference);
            }
            else if (memberReference.TypeInference.Declaration is Variable)
            {
                var variableDecl = (Variable)memberReference.TypeInference.Declaration;
                if (variableDecl.Qualifiers.Contains(ParadoxStorageQualifier.Stream) || variableDecl.Qualifiers.Contains(ParadoxStorageQualifier.PatchStream))
                    Error(ParadoxMessageCode.ErrorMissingStreamsStruct, memberReference.Span, memberReference, analyzedModuleMixin.MixinName);
            }

            if (memberReference.TypeInference.Declaration is Variable) // TODO: check if it is a variable whose scope is inside the hierarchy
            {
                var isExtern = HasExternQualifier(memberReference);
                var shouldStoreExpression = !(ParentNode is MemberReferenceExpression) ^ (memberReference.TypeInference.TargetType is VectorType || memberReference.TypeInference.TargetType is MatrixType);
                if (shouldStoreExpression && isExtern)
                    memberReference.SetTag(ParadoxTags.ExternRef, null);

                if (!isExtern && memberReference.Target.TypeInference.Declaration is ShaderClassType && !ReferenceEquals(analyzedModuleMixin.Shader, memberReference.Target.TypeInference.Declaration) && analyzedModuleMixin.InheritanceList.All(x => !ReferenceEquals(x.Shader, memberReference.Target.TypeInference.Declaration)))
                    memberReference.SetTag(ParadoxTags.StaticRef, null);

                var varDecl = (Variable)memberReference.TypeInference.Declaration;
                if (currentVisitedMethod != null && currentVisitedMethod.Qualifiers.Contains(StorageQualifier.Static) && varDecl != null && varDecl.GetTag(ParadoxTags.BaseDeclarationMixin) != null)
                    Error(ParadoxMessageCode.ErrorNonStaticReferenceInStaticMethod, memberReference.Span, currentVisitedMethod, varDecl, analyzedModuleMixin.MixinName);
            }

            // Add to variable references list
            AddToVariablesReference(memberReference);
        }
        
        /// <summary>
        /// Analyze the stream and store the datas
        /// </summary>
        /// <param name="memberReference">the MemberReferenceExpression</param>
        private void CheckStreamMemberReference(MemberReferenceExpression memberReference)
        {
            // search the reference variable that should be stream
            var decl = memberReference.TypeInference.Declaration ?? FindDeclarations(memberReference.Member.Text).FirstOrDefault();
            var variableDecl = decl as Variable;
            var mixinDecl = decl as ShaderClassType;

            if (variableDecl != null)
            {
                memberReference.TypeInference.Declaration = variableDecl;
                memberReference.TypeInference.TargetType = variableDecl.Type.ResolveType();
            }
            else if (mixinDecl != null)
            {
                memberReference.TypeInference.Declaration = mixinDecl;
                memberReference.TypeInference.TargetType = mixinDecl.ResolveType();
            }
            else
            {
                Error(ParadoxMessageCode.ErrorStreamNotFound, memberReference.Span, memberReference.Member.Text, analyzedModuleMixin.MixinName);
            }
        }

        /// <summary>
        /// Finds the member type reference.
        /// </summary>
        /// <param name="shaderDecl">Type of the shader</param>
        /// <param name="memberReference">The member reference.</param>
        protected void FindMemberTypeReference(ShaderClassType shaderDecl, MemberReferenceExpression memberReference)
        {
            var mixin = moduleMixins.FirstOrDefault(x => x.Shader == shaderDecl);
            if (mixin != null)
            {
                var shader = mixin.InheritanceList.FirstOrDefault(x => x.MixinName == memberReference.Member.Text);
                if (shader != null)
                {
                    memberReference.TypeInference.Declaration = shader.Shader;
                    memberReference.TypeInference.TargetType = shader.Shader;
                    return;
                }

                var variableDecl = mixin.VirtualTable.Variables.FirstOrDefault(x => x.Variable.Name.Text == memberReference.Member.Text);
                if (variableDecl != null)
                {
                    var isStream = IsStreamMember(memberReference);
                    if (!isStream || variableDecl.Variable.Qualifiers.Contains(ParadoxStorageQualifier.Stream))
                    {
                        memberReference.TypeInference.Declaration = variableDecl.Variable;
                        memberReference.TypeInference.TargetType = variableDecl.Variable.Type.ResolveType();
                        return;
                    }
                }

                if (ParentNode is MethodInvocationExpression)
                {
                    var invocationExpression = ParentNode as MethodInvocationExpression;
                    if (ReferenceEquals(invocationExpression.Target, memberReference))
                    {
                        var methodDecl = mixin.VirtualTable.Methods.Select(x => x.Method).FirstOrDefault(x => x.IsSameSignature(invocationExpression));
                        if (methodDecl != null)
                        {
                            memberReference.TypeInference.Declaration = methodDecl;
                            invocationExpression.TypeInference.TargetType = methodDecl.ReturnType.ResolveType();
                            return;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Finds the declaration inside the compositions and calls the base method too
        /// </summary>
        /// <param name="typeBase">the type of the object</param>
        /// <param name="memberName">the name of its member</param>
        /// <returns>a collection of all possible members</returns>
        protected override IEnumerable<IDeclaration> FindDeclarationsFromObject(TypeBase typeBase, string memberName)
        {
            if (typeBase == null)
            {
                yield break;
            }

            // look if it is a composition
            // the typebase is unique for each extern so there is no need to look for the right class
            //var mixin = compositionsVirtualTables.FirstOrDefault(x => ReferenceEquals(x.Key.ResolveType(), typeBase.ResolveType())).Value;

            var mixin = moduleMixins.FirstOrDefault(x => x.MixinName == typeBase.Name.Text);

            if (mixin != null)
            {
                foreach (var member in mixin.VirtualTable.Variables.Where(x => x.Variable.Name.Text == memberName))
                    yield return member.Variable;

                foreach (var member in mixin.VirtualTable.Methods.Where(x => x.Method.Name.Text == memberName))
                    yield return member.Method;

                if (mixin.MixinName == memberName)
                    yield return mixin.Shader;

                foreach (var dep in mixin.InheritanceList.Where(x => x.MixinName == memberName).Select(x => x.Shader))
                    yield return dep;
            }
            else
            {
                foreach (var item in base.FindDeclarationsFromObject(typeBase.ResolveType(), memberName))
                    yield return item;
            }
        }


        /// <summary>
        /// Finds a list of declaration by its name.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns>A list of declaration</returns>
        protected override IEnumerable<IDeclaration> FindDeclarations(string name)
        {
            var res = base.FindDeclarations(name);

            if (res.OfType<Variable>().Any(x => analyzedModuleMixin.PotentialConflictingVariables.Contains(x)))
                Error(ParadoxMessageCode.ErrorVariableNameAmbiguity, new SourceSpan(), name, analyzedModuleMixin.MixinName);
            if (res.OfType<MethodDeclaration>().Any(x => analyzedModuleMixin.PotentialConflictingMethods.Contains(x)))
                Error(ParadoxMessageCode.ErrorMethodNameAmbiguity, new SourceSpan(), name, analyzedModuleMixin.MixinName);
            
            return res;
        }

        /// <summary>
        /// Calls the base method but modify the stream usage beforehand
        /// </summary>
        /// <param name="expression">the method expression</param>
        [Visit]
        protected override void Visit(MethodInvocationExpression expression)
        {
            expression.SetTag(ParadoxTags.CurrentShader, analyzedModuleMixin);

            base.Visit(expression);

            if (expression.TypeInference.TargetType == null && expression.Target.TypeInference.Declaration == null)
                Error(ParadoxMessageCode.ErrorMissingMethod, expression.Span, expression, analyzedModuleMixin.MixinName);
            else if (!Equals(expression.TypeInference.Declaration, expression.Target.TypeInference.Declaration))
                expression.TypeInference.Declaration = expression.Target.TypeInference.Declaration;

            var methodDecl = expression.Target.TypeInference.Declaration as MethodDeclaration;

            if (methodDecl != null)
            {
                expression.Target.TypeInference.TargetType = (expression.Target.TypeInference.Declaration as MethodDeclaration).ReturnType.ResolveType();
                expression.TypeInference.TargetType = expression.Target.TypeInference.TargetType.ResolveType();

                if (currentVisitedMethod.Qualifiers.Contains(StorageQualifier.Static)
                    && !methodDecl.Qualifiers.Contains(StorageQualifier.Static)
                    && methodDecl.GetTag(ParadoxTags.BaseDeclarationMixin) != null)
                {
                        Error(ParadoxMessageCode.ErrorNonStaticCallInStaticMethod, expression.Span, currentVisitedMethod, methodDecl, analyzedModuleMixin.MixinName);
                }
            }

            // add to the reference list
            AddToMethodsReferences(expression);

            if (methodDecl != null)
                expression.Target.SetTag(ParadoxTags.VirtualTableReference, methodDecl.GetTag(ParadoxTags.VirtualTableReference));
        }

        /// <summary>
        /// Analyse the MethodInvocationExpression, link to the base calls, remove "this" from virtual calls, store in the correct list for later analysis
        /// </summary>
        /// <param name="expression">the method expression</param>
        /// <param name="methodName">the method name</param>
        /// <param name="declarations">the special declarations</param>
        protected override void ProcessMethodInvocation(MethodInvocationExpression expression, string methodName, List<IDeclaration> declarations)
        {
            bool callBaseProcessMethodInvocation = true;
            bool isNotBaseCall = true;

            // check if it is a base/this invocation
            var memberReferenceExpression = expression.Target as MemberReferenceExpression;
            if (memberReferenceExpression != null)
            {
                var variableReferenceExpression = memberReferenceExpression.Target as VariableReferenceExpression;
                if (variableReferenceExpression != null)
                {
                    switch (variableReferenceExpression.Name.Text)
                    {
                        case "base":
                            {
                                parsingInfo.BaseMethodCalls.Add(expression);
                                isNotBaseCall = false;
                                callBaseProcessMethodInvocation = false;

                                // get a base method declaration
                                MethodDeclaration baseMethod = null;
                                foreach (var mixin in analyzedModuleMixin.InheritanceList)
                                {
                                    baseMethod = mixin.LocalVirtualTable.Methods.Select(x => x.Method).FirstOrDefault(x => x.IsSameSignature(expression));
                                    if (baseMethod != null)
                                        break;
                                }
                                if (baseMethod == null)
                                    baseMethod = analyzedModuleMixin.LocalVirtualTable.Methods.Select(x => x.Method).FirstOrDefault(x => x.IsSameSignature(expression));
                                
                                if (baseMethod != null)
                                {
                                    expression.TypeInference.TargetType = baseMethod.ReturnType.ResolveType();
                                    expression.Target.TypeInference.Declaration = baseMethod;
                                }
                                else
                                    Error(ParadoxMessageCode.ErrorImpossibleBaseCall, memberReferenceExpression.Span, expression, analyzedModuleMixin.MixinName);
                                break;
                            }
                        case "this":
                            {
                                // remove "this" keyword
                                var vre = new VariableReferenceExpression(memberReferenceExpression.Member);
                                expression.Target = vre;
                                
                                callBaseProcessMethodInvocation = false;
                                
                                // get top method declaration
                                var topMethod = analyzedModuleMixin.VirtualTable.Methods.Select(x => x.Method).FirstOrDefault(x => x.IsSameSignature(expression));
                                if (topMethod != null)
                                {
                                    expression.TypeInference.TargetType = topMethod.ReturnType.ResolveType();
                                    expression.Target.TypeInference.Declaration = topMethod;
                                }
                                else
                                    Error(ParadoxMessageCode.ErrorImpossibleVirtualCall, memberReferenceExpression.Span, expression, analyzedModuleMixin.MixinName, analyzedModuleMixin.MixinName);

                                memberReferenceExpression = null;

                                break;
                            }
                    }
                    
                }

                if (expression.Target is MemberReferenceExpression)
                {
                    var typeCall = (expression.Target as MemberReferenceExpression).Target.TypeInference.TargetType;
                    if (typeCall is ShaderClassType)
                        declarations.AddRange(FindDeclarationsFromObject(typeCall, memberReferenceExpression.Member));
                }
            }

            // call base
            if (callBaseProcessMethodInvocation)
                base.ProcessMethodInvocation(expression, methodName, declarations);

            var methodDecl = expression.Target.TypeInference.Declaration as MethodDeclaration;
            var isBuiltIn = true;

            if (methodDecl != null)
            {
                // check if it is a recursive call
                if (ReferenceEquals(currentVisitedMethod, expression.Target.TypeInference.Declaration)) // How to handle "this" keyword?
                    Error(ParadoxMessageCode.ErrorCyclicMethod, currentVisitedMethod.Span, currentVisitedMethod, analyzedModuleMixin.MixinName);

                // check if it is a build-in method
                isBuiltIn = !methodDecl.ContainsTag(ParadoxTags.ShaderScope);

                if (memberReferenceExpression != null)
                {
                    var varDecl = memberReferenceExpression.Target.TypeInference.Declaration as Variable;
                    if (memberReferenceExpression.Target is IndexerExpression)
                        varDecl = (memberReferenceExpression.Target as IndexerExpression).Target.TypeInference.Declaration as Variable;

                    if (varDecl != null && varDecl.Qualifiers.Contains(StorageQualifier.Extern))
                    {
                        if (IsStageInitMember(memberReferenceExpression))
                            expression.SetTag(ParadoxTags.StageInitRef, null);
                        else
                            expression.SetTag(ParadoxTags.ExternRef, null);
                    }

                    var shaderDecl = memberReferenceExpression.Target.TypeInference.Declaration as ShaderClassType;
                    if (shaderDecl != null && shaderDecl != analyzedModuleMixin.Shader && analyzedModuleMixin.InheritanceList.All(x => x.Shader != shaderDecl))
                        expression.SetTag(ParadoxTags.StaticRef, null);
                }

                if (!isBuiltIn)
                {
                    // store if not a base call
                    if (isNotBaseCall && !expression.ContainsTag(ParadoxTags.ExternRef) && !expression.ContainsTag(ParadoxTags.StageInitRef) && !expression.ContainsTag(ParadoxTags.StaticRef))
                        parsingInfo.ThisMethodCalls.Add(expression);

                    if (methodDecl.Qualifiers.Contains(ParadoxStorageQualifier.Stage))
                        parsingInfo.StageMethodCalls.Add(expression);
                }
            }
        }

        /// <summary>
        /// Tests the arguments of the method - check streams type here
        /// </summary>
        /// <param name="argTypeBase">the argument typebase</param>
        /// <param name="expectedTypeBase">the expected typebase</param>
        /// <param name="argType">the argument type</param>
        /// <param name="expectedType">the expected type</param>
        /// <param name="score">the score of the overload</param>
        /// <returns>true if the overload is correct, false otherwise</returns>
        protected override bool TestMethodInvocationArgument(TypeBase argTypeBase, TypeBase expectedTypeBase, TypeBase argType, TypeBase expectedType, ref int score)
        {
            if (argTypeBase == StreamsType.Streams && expectedTypeBase == StreamsType.Output) // streams and output are the same
                return true;
            if (argTypeBase is StreamsType && expectedType is StreamsType)
                return argTypeBase == expectedType;

            return base.TestMethodInvocationArgument(argTypeBase, expectedTypeBase, argType, expectedType, ref score);
        }

        /// <summary>
        /// Analyse the AssignmentExpression to correctly infer the potential stream usage
        /// </summary>
        /// <param name="assignmentExpression">the AssignmentExpression</param>
        [Visit]
        protected override void Visit(AssignmentExpression assignmentExpression)
        {
            if (currentAssignmentOperatorStatus != AssignmentOperatorStatus.Read)
                Error(ParadoxMessageCode.ErrorNestedAssignment, assignmentExpression.Span, assignmentExpression, analyzedModuleMixin.MixinName);

            assignmentExpression.Value = (Expression)VisitDynamic(assignmentExpression.Value);
            currentAssignmentOperatorStatus = (assignmentExpression.Operator != AssignmentOperator.Default) ? AssignmentOperatorStatus.ReadWrite : AssignmentOperatorStatus.Write;

            assignmentExpression.Target = (Expression)VisitDynamic(assignmentExpression.Target);

            currentAssignmentOperatorStatus = AssignmentOperatorStatus.Read;
        }

        /// <summary>
        /// Checks that the name does not bear many meanings
        /// </summary>
        /// <param name="variableReferenceExpression">the variable reference expression to check to check</param>
        private void CheckNameConflict(VariableReferenceExpression variableReferenceExpression)
        {
            var name = variableReferenceExpression.Name.Text;
            // NOTE: a VariableReferenceExpression means that we are in the context of the currently analyzed mixin
            // we need to check that a variable does not appear twice
            var varCount = analyzedModuleMixin.VirtualTable.Variables.Count(x => x.Variable.Name.Text == name);

            if (varCount > 1)
                Error(ParadoxMessageCode.ErrorVariableNameAmbiguity, variableReferenceExpression.Span, variableReferenceExpression, analyzedModuleMixin.MixinName);
        }

        /// <summary>
        /// Analyse the VariableReferenceExpression, detects streams, propagate type inference, get stored in the correct list for later analysis
        /// </summary>
        /// <param name="variableReferenceExpression">the VariableReferenceExpression</param>
        [Visit]
        protected override void Visit(VariableReferenceExpression variableReferenceExpression)
        {
            // HACK: force types on base, this and stream keyword to eliminate errors in the log and use the standard type inference
            var name = variableReferenceExpression.Name.Text;
            if (name == "base")
            {
                variableReferenceExpression.TypeInference.Declaration = analyzedModuleMixin.Shader;
                variableReferenceExpression.TypeInference.TargetType = analyzedModuleMixin.Shader;
                return;
            }
            if (name == "this")
            {
                variableReferenceExpression.TypeInference.Declaration = analyzedModuleMixin.Shader;
                variableReferenceExpression.TypeInference.TargetType = analyzedModuleMixin.Shader;
                return;
            }
            if (name == "stage")
            {
                if (!(ParentNode is Variable && (ParentNode as Variable).InitialValue == variableReferenceExpression))
                    Error(ParadoxMessageCode.ErrorStageOutsideVariable, ParentNode.Span, ParentNode, analyzedModuleMixin.MixinName);

                return;
            }
            if (name == StreamsType.ThisStreams.Name.Text)
            {
                variableReferenceExpression.TypeInference.Declaration = StreamsType.ThisStreams;
                variableReferenceExpression.TypeInference.TargetType = StreamsType.ThisStreams.Type;
            }
            
            // check if the variable is double-defined
            if (!ParadoxKeywords.Contains(variableReferenceExpression.Name.Text))
                CheckNameConflict(variableReferenceExpression);

            base.Visit(variableReferenceExpression);

            var varDecl = variableReferenceExpression.TypeInference.Declaration as Variable;

            if (varDecl != null)
            {
                // because the parent classes do not do this all the time
                if (variableReferenceExpression.TypeInference.TargetType == null)
                    variableReferenceExpression.TypeInference.TargetType = (variableReferenceExpression.TypeInference.Declaration as Variable).Type.ResolveType();

                if (varDecl.ContainsTag(ParadoxTags.ShaderScope))
                {
                    // stream variable should be called within the streams scope
                    if (varDecl.Qualifiers.Contains(ParadoxStorageQualifier.Stream) || varDecl.Qualifiers.Contains(ParadoxStorageQualifier.PatchStream))
                        Error(ParadoxMessageCode.ErrorMissingStreamsStruct, variableReferenceExpression.Span, variableReferenceExpression, analyzedModuleMixin.MixinName);
                }
            }

            var isMethodName = defaultDeclarations.Any(x => x.Name.Text == variableReferenceExpression.Name.Text);

            if (!ParadoxKeywords.Contains(variableReferenceExpression.Name.Text) && variableReferenceExpression.TypeInference.Declaration == null && !inSampler && !isMethodName)
                Error(ParadoxMessageCode.ErrorMissingVariable, variableReferenceExpression.Span, variableReferenceExpression, analyzedModuleMixin.MixinName);

            // update function static status
            if (!inSampler && !isMethodName && variableReferenceExpression.TypeInference.Declaration == null)
                Error(ParadoxMessageCode.ErrorNoTypeInference, variableReferenceExpression.Span, variableReferenceExpression, analyzedModuleMixin.MixinName);

            if (currentVisitedMethod != null && currentVisitedMethod.Qualifiers.Contains(StorageQualifier.Static) && varDecl != null && varDecl.GetTag(ParadoxTags.BaseDeclarationMixin) != null)
                Error(ParadoxMessageCode.ErrorNonStaticReferenceInStaticMethod, variableReferenceExpression.Span, currentVisitedMethod, varDecl, analyzedModuleMixin.MixinName);

            // Add to the variables references list
            AddToVariablesReference(variableReferenceExpression);
        }

        /// <summary>
        /// Find the type of the expression
        /// </summary>
        /// <param name="indexerExpression">the indexer expression</param>
        protected override void ProcessIndexerExpression(IndexerExpression indexerExpression)
        {
            var targetType = indexerExpression.Target.TypeInference.TargetType;

            if (targetType is ClassType && (targetType.Name.Text == "InputPatch" || targetType.Name.Text == "OutputPatch" || targetType.Name.Text == "PointStream" || targetType.Name.Text == "StructuredBuffer" || targetType.Name.Text == "RWStructuredBuffer"))
                indexerExpression.TypeInference.TargetType = (targetType as ClassType).GenericArguments[0].ResolveType();
            else
                base.ProcessIndexerExpression(indexerExpression);

            if (!(indexerExpression.Index is LiteralExpression) && indexerExpression.Target.TypeInference.Declaration is Variable)
            {
                var varDecl = indexerExpression.Target.TypeInference.Declaration as Variable;
                if (varDecl.Qualifiers.Contains(ParadoxStorageQualifier.Extern))
                    Error(ParadoxMessageCode.ErrorIndexerNotLiteral, indexerExpression.Span, indexerExpression, analyzedModuleMixin.MixinName);
            }
        }

        /// <summary>
        /// Visit an interface to send an error
        /// </summary>
        /// <param name="interfaceType">the interface.</param>
        [Visit]
        private void Visit(InterfaceType interfaceType)
        {
            Error(ParadoxMessageCode.ErrorInterfaceFound, interfaceType.Span, interfaceType, analyzedModuleMixin.MixinName);
        }

        /// <summary>
        /// Visit a structure and store its definition
        /// </summary>
        /// <param name="structType">the structure definition</param>
        [Visit]
        private void Visit(StructType structType)
        {
            Visit((Node)structType);

            if (structType.ContainsTag(ParadoxTags.ShaderScope))
                parsingInfo.StructureDefinitions.Add(structType);
        }

        /// <summary>
        /// Visit a generic type and test that it has no shader class type
        /// </summary>
        /// <param name="genericType">the generic type</param>
        [Visit]
        protected override void Visit(GenericType genericType)
        {
            base.Visit(genericType);

            foreach (var param in genericType.Parameters.OfType<TypeName>())
            {
                if (param.TypeInference.TargetType is ShaderClassType)
                    Error(ParadoxMessageCode.ErrorMixinAsGeneric, param.Span, param, genericType, analyzedModuleMixin.MixinName);
            }
        }

        /// <summary>
        /// Adds the expression to the reference list of the variable
        /// </summary>
        /// <param name="expression">the Expression</param>
        private void AddToVariablesReference(Expression expression)
        {
            var variable = expression.TypeInference.Declaration as Variable;
            if (variable != null && variable.ContainsTag(ParadoxTags.ShaderScope))
            {
                if (expression.ContainsTag(ParadoxTags.StaticRef) || variable.Qualifiers.Contains(StorageQualifier.Static))
                    parsingInfo.StaticReferences.InsertVariable(variable, new ExpressionNodeCouple(expression, ParentNode));
                else if (expression.ContainsTag(ParadoxTags.ExternRef))
                    parsingInfo.ExternReferences.InsertVariable(variable, new ExpressionNodeCouple(expression, ParentNode));
                else if (expression.ContainsTag(ParadoxTags.StageInitRef))
                    parsingInfo.StageInitReferences.InsertVariable(variable, new ExpressionNodeCouple(expression, ParentNode));
                else
                    parsingInfo.ClassReferences.InsertVariable(variable, new ExpressionNodeCouple(expression, ParentNode));
            }
            else
            {
                parsingInfo.NavigableNodes.Add(expression);
            }
        }

        /// <summary>
        /// Adds the method reference to the list of methods references
        /// </summary>
        /// <param name="expression">the method reference expression</param>
        private void AddToMethodsReferences(MethodInvocationExpression expression)
        {
            var methodDecl = expression.Target.TypeInference.Declaration as MethodDeclaration;
            if (methodDecl != null && methodDecl.ContainsTag(ParadoxTags.ShaderScope))
            {
                if (expression.ContainsTag(ParadoxTags.StaticRef) || methodDecl.Qualifiers.Contains(StorageQualifier.Static))
                    parsingInfo.StaticReferences.InsertMethod(methodDecl, expression);
                else if (expression.ContainsTag(ParadoxTags.ExternRef))
                    parsingInfo.ExternReferences.InsertMethod(methodDecl, expression);
                else if (expression.ContainsTag(ParadoxTags.StageInitRef))
                    parsingInfo.StageInitReferences.InsertMethod(methodDecl, expression);
                else
                    parsingInfo.ClassReferences.InsertMethod(methodDecl, expression);
            }
            else
            {
                parsingInfo.NavigableNodes.Add(expression);
            }
        }

        [Visit]
        private void Visit(ShaderClassType shaderClassType)
        {
            Visit((Node)shaderClassType);

            // Allow to navigate to base classes
            foreach (var baseClass in shaderClassType.BaseClasses)
            {
                var firstOrDefault = analyzedModuleMixin.InheritanceList.FirstOrDefault(moduleMixin => moduleMixin.Shader.Name.Text == baseClass.Name.Text);
                if (firstOrDefault != null)
                {
                    var declaration = firstOrDefault.Shader;
                    baseClass.TypeInference.Declaration = declaration;
                }

                parsingInfo.NavigableNodes.Add(baseClass);
            }
        }

        [Visit]
        protected override TypeBase Visit(TypeName typeName)
        {
            var newTypeName = base.Visit(typeName);

            if (newTypeName.TypeInference.Declaration != null)
            {
                parsingInfo.NavigableNodes.Add(typeName);
            }
            return newTypeName;
        }

        /// <summary>
        /// Visits the ForEachStatement Node and collects information from it.
        /// </summary>
        /// <param name="forEachStatement">The ForEachStatement</param>
        [Visit]
        private Node Visit(ForEachStatement forEachStatement)
        {
            if (expandForEachStatements)
            {
                // run analysis on collection
                Visit(forEachStatement.Collection);

                var inference = forEachStatement.Collection.TypeInference.Declaration as Variable;
                if (!(inference != null && inference.Type is ArrayType))
                    return forEachStatement;

                if ((inference.Type as ArrayType).Dimensions.Count > 1)
                {
                    Error(ParadoxMessageCode.ErrorMultiDimArray, forEachStatement.Span, inference, forEachStatement, analyzedModuleMixin.MixinName);
                    return forEachStatement;
                }

                var dim = (int)((inference.Type as ArrayType).Dimensions.FirstOrDefault() as LiteralExpression).Value;

                var result = new StatementList();
                for (int i = 0; i < dim; ++i)
                {
                    var cloned = forEachStatement.DeepClone();
                    var replace = new ParadoxReplaceExtern(cloned.Variable, new IndexerExpression(cloned.Collection, new LiteralExpression(i)));
                    replace.Run(cloned.Body);
                    result.Add(cloned.Body);
                }

                Visit((Node)result);
                return result;
            }
            else
            {
                Visit((Node)forEachStatement);
                parsingInfo.ForEachStatements.Add(new StatementNodeCouple(forEachStatement, ParentNode));
                return forEachStatement;
            }
        }

        /// <summary>
        /// Gets the type of the binary implicit conversion.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <param name="isBinaryOperator">if set to <c>true</c> [is binary operator].</param>
        /// <returns>
        /// The implicit conversion between between to two types
        /// </returns>
        protected override TypeBase GetBinaryImplicitConversionType(SourceSpan span, TypeBase left, TypeBase right, bool isBinaryOperator)
        {
            if (left.ResolveType() is StreamsType || right.ResolveType() is StreamsType)
                return StreamsType.Streams;

            return base.GetBinaryImplicitConversionType(span, left, right, isBinaryOperator);
        }

        /// <summary>
        /// Gets the type of the binary implicit conversion.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The implicit conversion between between to two types</returns>
        protected override TypeBase GetMultiplyImplicitConversionType(SourceSpan span, TypeBase left, TypeBase right)
        {
            if (left.ResolveType() is StreamsType || right.ResolveType() is StreamsType)
                return StreamsType.Streams;

            return base.GetMultiplyImplicitConversionType(span, left, right);
        }

        /// <summary>
        /// Gets the type of the binary implicit conversion.
        /// </summary>
        /// <param name="span">The span.</param>
        /// <param name="left">The left.</param>
        /// <param name="right">The right.</param>
        /// <returns>The implicit conversion between between to two types</returns>
        protected override TypeBase GetDivideImplicitConversionType(SourceSpan span, TypeBase left, TypeBase right)
        {
            if (left.ResolveType() is StreamsType || right.ResolveType() is StreamsType)
                return StreamsType.Streams;

            return base.GetDivideImplicitConversionType(span, left, right);
        }

        /// <summary>
        /// Test if the expression is from a stage nitialized one
        /// </summary>
        /// <param name="expression">the expression</param>
        /// <returns>true if it is the case, false otherwise</returns>
        private bool IsStageInitMember(Expression expression)
        {
            if (expression != null)
                return parsingInfo.StageInitializedVariables.Contains(expression.TypeInference.Declaration) || (expression is MemberReferenceExpression && IsStageInitMember((expression as MemberReferenceExpression).Target));
            
            return false;
        }

        #endregion

        #region Private static helper functions


        /// <summary>
        /// Look for extern qualifier in expression typeinference
        /// </summary>
        /// <param name="expression">the expression</param>
        /// <returns>true if there is a reference to an extern variable</returns>
        private static bool HasExternQualifier(Expression expression)
        {
            var varDecl = expression.TypeInference.Declaration as Variable;
            if (varDecl != null && varDecl.Qualifiers.Contains(StorageQualifier.Extern))
                return !(varDecl.InitialValue is VariableReferenceExpression) || (varDecl.InitialValue as VariableReferenceExpression).Name.Text != "stage";
            
            if (expression is MemberReferenceExpression)
                return HasExternQualifier((expression as MemberReferenceExpression).Target);
            return false;
        }

        /// <summary>
        /// Checks if expression is a stream
        /// </summary>
        /// <param name="expression">the Expression</param>
        /// <returns>true if it is a stream, false otherwise</returns>
        private static bool IsStreamMember(MemberReferenceExpression expression)
        {
            if (expression != null)
            {
                if (expression.Target.TypeInference.TargetType is StreamsType)
                    return true;

                // iterate until the base "class" is found and compare it to "streams"
                var target = expression.Target;
                while (target is MemberReferenceExpression)
                    target = (target as MemberReferenceExpression).Target;
                
                var variableReferenceExpression = target as VariableReferenceExpression;
                return variableReferenceExpression != null && (variableReferenceExpression.Name == StreamsType.ThisStreams.Name || variableReferenceExpression.TypeInference.TargetType is StreamsType);
            }
            return false;
        }

        /// <summary>
        /// Tests if a MemberReferenceExpression is a reference to a stream from an Input/Output type
        /// </summary>
        /// <param name="expression">the expression to analyze</param>
        /// <returns>true if it is a member of an Input/Output type</returns>
        private static bool IsInputOutputMember(MemberReferenceExpression expression)
        {
            var targetType = expression.Target.TypeInference.TargetType as StreamsType;
            return targetType != null && targetType.IsInputOutput;
        }

        #endregion
    }
}