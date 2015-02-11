// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections.Generic;
using System.Linq;

using SiliconStudio.Paradox.Shaders.Parser.Analysis;
using SiliconStudio.Paradox.Shaders.Parser.Ast;
using SiliconStudio.Paradox.Shaders.Parser.Utility;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Ast.Hlsl;
using SiliconStudio.Shaders.Utility;

using StorageQualifier = SiliconStudio.Shaders.Ast.StorageQualifier;

namespace SiliconStudio.Paradox.Shaders.Parser.Mixins
{
    internal class ParadoxShaderMixer
    {
        private readonly static string FlipRendertargetVariableName = "ParadoxFlipRendertarget";
        
        #region Public members

        /// <summary>
        /// The final shader
        /// </summary>
        public ShaderClassType MixedShader = null;

        /// <summary>
        /// Log of all the warnings and errors
        /// </summary>
        private readonly LoggerResult log;

        #endregion

        #region Private members

        /// <summary>
        /// the module to generate
        /// </summary>
        private ModuleMixin mainModuleMixin = null;

        /// <summary>
        /// the extern modules
        /// </summary>
        private Dictionary<Variable, List<ModuleMixin>> CompositionsPerVariable;

        /// <summary>
        /// List of all the method Declaration
        /// </summary>
        private HashSet<List<MethodDeclaration>> StageMethodInheritance = new HashSet<List<MethodDeclaration>>();

        /// <summary>
        /// Ordered list of all the mixin in their appearance order
        /// </summary>
        private List<ModuleMixin> MixinInheritance = new List<ModuleMixin>();

        /// <summary>
        /// Dictionary of all the mixins used for this compilation
        /// </summary>
        private Dictionary<string, ModuleMixin> mixContext;

        /// <summary>
        /// The default clone context
        /// </summary>
        private CloneContext defaultCloneContext;

        #endregion

        #region Constructor

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="moduleMixin">the final shader information</param>
        /// <param name="log">The log.</param>
        /// <param name="context">all the mixins in the context</param>
        /// <param name="compositionsPerVariable">The compositions per variable.</param>
        /// <param name="cloneContext">The clone context.</param>
        /// <exception cref="System.ArgumentNullException">
        /// moduleMixin
        /// or
        /// log
        /// or
        /// context
        /// </exception>
        public ParadoxShaderMixer(ModuleMixin moduleMixin, LoggerResult log, Dictionary<string, ModuleMixin> context, Dictionary<Variable, List<ModuleMixin>> compositionsPerVariable, CloneContext cloneContext = null)
        {
            if (moduleMixin == null)
                throw new ArgumentNullException("moduleMixin");

            if (log == null) 
                throw new ArgumentNullException("log");

            if (context == null)
                throw new ArgumentNullException("context");

            this.log = log;

            mixContext = context;
            mainModuleMixin = moduleMixin;
            defaultCloneContext = cloneContext;

            if (compositionsPerVariable != null)
                CompositionsPerVariable = compositionsPerVariable;
            else
                CompositionsPerVariable = new Dictionary<Variable, List<ModuleMixin>>();

            var mixinsToAnalyze = new Stack<ModuleMixin>(CompositionsPerVariable.SelectMany(x => x.Value));
            mixinsToAnalyze.Push(mainModuleMixin);

            while (mixinsToAnalyze.Count > 0)
                AddDefaultCompositions(mixinsToAnalyze);
        }

        #endregion

        #region Public methods

/// <summary>
        /// Performs the mix
        /// </summary>
        public void Mix()
        {
            CreateReferencesStructures();
            
            mainModuleMixin.ClassReferences.RegenKeys();
            mainModuleMixin.ExternReferences.RegenKeys();
            mainModuleMixin.StaticReferences.RegenKeys();
            mainModuleMixin.StageInitReferences.RegenKeys();

            foreach (var externMix in CompositionsPerVariable.SelectMany(externMixes => externMixes.Value))
            {
                externMix.ClassReferences.RegenKeys();
                externMix.ExternReferences.RegenKeys();
                externMix.StaticReferences.RegenKeys();
                externMix.StageInitReferences.RegenKeys();
            }
            
            BuildMixinInheritance(mainModuleMixin);
            MixinInheritance = MixinInheritance.Distinct().ToList();
            
            ComputeMixinOccurence();
            BuildStageInheritance();
            
            LinkVariables(mainModuleMixin, "", new List<ModuleMixin>());
            ProcessExterns();
            
            // patch the base/this functions
            PatchAllMethodInferences(mainModuleMixin);
            
            MergeReferences();
            
            // then everything in the inheritance
            RenameAllVariables();
            RenameAllMethods();
            
            // group into one AST
            GenerateShader();
        }

        public Shader GetMixedShader()
        {
            var shader = new Shader();
            shader.Declarations.AddRange(MixedShader.Members);

            return shader;
        }

        #endregion

        #region Private methods
        
        /// <summary>
        /// Add default compositions if no already present
        /// </summary>
        /// <param name="mixinsToAnalyze">the remaining mixins to analyzez</param>
        private void AddDefaultCompositions(Stack<ModuleMixin> mixinsToAnalyze)
        {
            var nextMixin = mixinsToAnalyze.Pop();

            foreach (var externVar in nextMixin.VariableDependencies)
            {
                if (!CompositionsPerVariable.ContainsKey(externVar.Key))
                {
                    var newComp = externVar.Value.DeepClone(defaultCloneContext);
                    mixinsToAnalyze.Push(newComp);
                    CompositionsPerVariable.Add(externVar.Key, new List<ModuleMixin> { newComp });
                }
            }
            foreach (var dep in nextMixin.InheritanceList)
                mixinsToAnalyze.Push(dep);
        }

        /// <summary>
        /// performs semantic analysis on mixin that have composition arrays
        /// </summary>
        private void RedoSematicAnalysis()
        {
            // first: assign the size of the array
            foreach (var composition in CompositionsPerVariable.Where(x => x.Key.Type is ArrayType))
            {
                var arrayType = composition.Key.Type as ArrayType;
                if (arrayType.Dimensions.Count > 1)
                {
                    log.Error(ParadoxMessageCode.ErrorMultidimensionalCompositionArray, arrayType.Span, arrayType, composition.Value.First().MixinName);
                    return;
                }
                arrayType.Dimensions[0] = new LiteralExpression(composition.Value.Count);
            }

            // then rerun the semantic analysis
            foreach (var composition in CompositionsPerVariable.Where(x => x.Key.Type is ArrayType))
            {
                var moduleMixin = GetTopMixin(composition.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin);
                var compilationContext = moduleMixin.MinimalContext.Where(x => !(moduleMixin.InheritanceList.Contains(x) || x == moduleMixin)).ToList();

                // rerun the semantic analysis in all the shader that inherits from the one where the composition was declared.
                foreach (var inheritedMixin in moduleMixin.InheritanceList)
                    inheritedMixin.ParsingInfo = ParadoxSemanticAnalysis.RunAnalysis(inheritedMixin, compilationContext, true);
                moduleMixin.ParsingInfo = ParadoxSemanticAnalysis.RunAnalysis(moduleMixin, compilationContext, true);

                if (moduleMixin.ParsingInfo.ErrorsWarnings.HasErrors)
                    return;
                    //throw new Exception("Semantic analysis failed in ParadoxShaderMixer");
            }
        }

        /// <summary>
        /// Create the references for each top mixin
        /// </summary>
        private void CreateReferencesStructures()
        {
            RedoSematicAnalysis();
            CreateReferencesStructures(mainModuleMixin);
            foreach (var compositions in CompositionsPerVariable)
            {
                foreach (var comp in compositions.Value)
                    CreateReferencesStructures(comp);
            }
        }

        /// <summary>
        /// Merge reference from mixin dependencies
        /// </summary>
        /// <param name="mixin"></param>
        private void CreateReferencesStructures(ModuleMixin mixin)
        {
            GetStaticReferences(mixin, mixin);

            // merge class reference
            mixin.ClassReferences.Merge(mixin.ParsingInfo.ClassReferences);
            foreach (var dep in mixin.InheritanceList)
                mixin.ClassReferences.Merge(dep.ParsingInfo.ClassReferences);
            // merge static references
            mixin.StaticReferences.Merge(mixin.ParsingInfo.StaticReferences);
            foreach (var dep in mixin.InheritanceList)
                mixin.StaticReferences.Merge(dep.ParsingInfo.StaticReferences);
            // merge extern references
            mixin.ExternReferences.Merge(mixin.ParsingInfo.ExternReferences);
            foreach (var dep in mixin.InheritanceList)
                mixin.ExternReferences.Merge(dep.ParsingInfo.ExternReferences);
            // merge stage init references
            mixin.StageInitReferences.Merge(mixin.ParsingInfo.StageInitReferences);
            foreach (var dep in mixin.InheritanceList)
                mixin.StageInitReferences.Merge(dep.ParsingInfo.StageInitReferences);
        }

        /// <summary>
        /// bubble up the static references in the mixin dependency tree
        /// </summary>
        /// <param name="topMixin">the top mixin</param>
        /// <param name="staticMixin">the mixin to look into</param>
        private void GetStaticReferences(ModuleMixin topMixin, ModuleMixin staticMixin)
        {
            foreach (var staticDep in staticMixin.ParsingInfo.StaticClasses)
                GetStaticReferences(topMixin, staticDep);
            foreach (var staticDep in staticMixin.InheritanceList)
                GetStaticReferences(topMixin, staticDep);
            
            topMixin.StaticReferences.Merge(staticMixin.ParsingInfo.StaticReferences);
        }

        /// <summary>
        /// Rename the links of the variables
        /// </summary>
        /// <param name="mixin">the current mixin</param>
        /// <param name="context">the string to append</param>
        /// <param name="visitedMixins">list of already visited mixin</param>
        private void LinkVariables(ModuleMixin mixin, string context, List<ModuleMixin> visitedMixins)
        {
            if (visitedMixins.Contains(mixin))
                return;
            
            visitedMixins.Add(mixin);

            foreach (var variable in mixin.LocalVirtualTable.Variables.Select(x => x.Variable))
            {
                if (variable.Qualifiers.Contains(ParadoxStorageQualifier.Extern))
                {
                    List<ModuleMixin> mixins;
                    if (CompositionsPerVariable.TryGetValue(variable, out mixins))
                    {
                        if (variable.Type is ArrayType)
                        {
                            for (var i = 0; i < mixins.Count; ++i)
                            {
                                var baselink = "." + variable.Name.Text + "[" + i + "]" + context;
                                LinkVariables(mixins[i], baselink, visitedMixins);
                            }
                        }
                        else
                        {
                            var baselink = "." + variable.Name.Text + context;
                            LinkVariables(mixins[0], baselink, visitedMixins);
                        }
                    }
                }

                if (!(variable.Qualifiers.Values.Contains(ParadoxStorageQualifier.Stream)
                      || variable.Qualifiers.Values.Contains(ParadoxStorageQualifier.PatchStream)
                      || variable.Qualifiers.Values.Contains(ParadoxStorageQualifier.Extern)))
                {
                    var attribute = variable.Attributes.OfType<AttributeDeclaration>().FirstOrDefault(x => x.Name == "Link");
                    if (attribute == null)
                    {
                        // Try to get class name before generics
                        //string baseClassName;
                        //if (!genericTypeDefinitions.TryGetValue(baseClass, out baseClassName))
                        //    baseClassName = baseClass.Name;

                        // TODO: class name before renaming if generics
                        string linkName;

                        // Use Map attribute (if it exists)
                        var mapAttribute = variable.Attributes.OfType<AttributeDeclaration>().FirstOrDefault(x => x.Name == "Map");
                        if (mapAttribute != null)
                        {
                            linkName = (string)mapAttribute.Parameters[0].Value;
                            // Remove "Keys" from class name (or maybe we should just include it in key name to avoid issues?)
                            linkName = linkName.Replace("Keys.", ".");
                        }
                        else
                        {
                            linkName = mixin.MixinGenericName + "." + variable.Name.Text;
                        }

                        attribute = new AttributeDeclaration { Name = new Identifier("Link"), Parameters = new List<Literal> { new Literal(linkName) } };
                        variable.Attributes.Add(attribute);
                    }

                    // Append location to key in case it is a local variable
                    if (!variable.Qualifiers.Values.Contains(ParadoxStorageQualifier.Stage))
                    {
                        attribute.Parameters[0].SubLiterals = null; // set to null to avoid conflict with the member Value
                        attribute.Parameters[0].Value = (string)attribute.Parameters[0].Value + context;
                    }
                }
            }

            foreach (var variable in mixin.StaticReferences.VariablesReferences.Select(x => x.Key))
            {
                var attribute = variable.Attributes.OfType<AttributeDeclaration>().FirstOrDefault(x => x.Name == "Link");
                if (attribute == null)
                {
                    var baseClassName = (variable.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinGenericName;

                    attribute = new AttributeDeclaration { Name = new Identifier("Link"), Parameters = new List<Literal> { new Literal(baseClassName + "." + variable.Name.Text) } };
                    variable.Attributes.Add(attribute);
                }
            }

            mixin.InheritanceList.ForEach(x => LinkVariables(x, context, visitedMixins));
        }

        /// <summary>
        /// Merge the class references of the externs to the main class, and the static calls too
        /// </summary>
        private void MergeReferences()
        {
            foreach (var externMixes in CompositionsPerVariable)
            {
                foreach (var externMix in externMixes.Value)
                    mainModuleMixin.ClassReferences.Merge(externMix.ClassReferences);
            }

            mainModuleMixin.ClassReferences.Merge(mainModuleMixin.StaticReferences);
        }

        /// <summary>
        /// Add the stage variables from the mixin to the main one
        /// </summary>
        /// <param name="mixin">the ModuleMixin</param>
        private void AddStageVariables(ModuleMixin mixin)
        {
            mixin.InheritanceList.ForEach(AddStageVariables);
            CompositionsPerVariable.Where(x => mixin.LocalVirtualTable.Variables.Any(y => y.Variable == x.Key)).ToList().ForEach(externMixes => externMixes.Value.ForEach(AddStageVariables));
            
            foreach (var variable in mixin.LocalVirtualTable.Variables)
            {
                if (variable.Variable.Qualifiers.Contains(ParadoxStorageQualifier.Stage))
                {
                    var shaderName = variable.Shader.Name.Text;
                    var sameVar = mainModuleMixin.ClassReferences.VariablesReferences.FirstOrDefault(x => x.Key.Name.Text == variable.Variable.Name.Text && (x.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName == shaderName).Key;
                    if (sameVar != null)
                        continue;
                }
                if (!mainModuleMixin.ClassReferences.VariablesReferences.ContainsKey(variable.Variable))
                    mainModuleMixin.ClassReferences.VariablesReferences.Add(variable.Variable, new HashSet<ExpressionNodeCouple>());
            }
        }

        /// <summary>
        /// Build an ordered list of mixin defining the inheritance for stage values
        /// </summary>
        /// <param name="mixin">the mixin to add</param>
        private void BuildMixinInheritance(ModuleMixin mixin)
        {
            mixin.InheritanceList.ForEach(BuildMixinInheritance);
            MixinInheritance.Add(mixin);
            CompositionsPerVariable.Where(x => mixin.LocalVirtualTable.Variables.Any(y => y.Variable == x.Key)).ToList().ForEach(externMixes => externMixes.Value.ForEach(BuildMixinInheritance));
        }

        /// <summary>
        /// Compute the occurence Id of each mixin
        /// </summary>
        private void ComputeMixinOccurence()
        {
            foreach (var mixin in MixinInheritance)
            {
                foreach (var mixin2 in MixinInheritance)
                {
                    if (mixin.MixinName == mixin2.MixinName)
                        ++(mixin.OccurenceId);
                    if (mixin == mixin2)
                        break;
                }
            }
        }

        /// <summary>
        /// Find the correct variable inference
        /// </summary>
        /// <param name="expression"></param>
        /// <param name="mixin"></param>
        /// <returns></returns>
        private Variable FindVariable(Expression expression, ref ModuleMixin mixin)
        {
            Variable result = null;
            var index = 0;
            if (expression is VariableReferenceExpression)
            {
                result = FindVariableInMixin((expression as VariableReferenceExpression).Name.Text, mixin);
            }
            else if (expression is MemberReferenceExpression)
            {
                var memberExpression = expression as MemberReferenceExpression;
                var target = memberExpression.Target;

                if (target.TypeInference.Declaration is Variable)
                    FindVariable(target, ref mixin);
                else if (target.TypeInference.Declaration is ShaderClassType || target.TypeInference.TargetType is ShaderClassType)
                    FindShader(target, ref mixin);

                result = FindVariableInMixin(memberExpression.Member.Text, mixin);
            }
            else if (expression is IndexerExpression)
            {
                var indexerExpression = expression as IndexerExpression;
                var target = indexerExpression.Target;

                if (target.TypeInference.Declaration is Variable)
                    result = FindVariable(target, ref mixin);

                index = (int)(indexerExpression.Index as LiteralExpression).Value;
            }

            if (result is Variable && (result as Variable).Qualifiers.Contains(ParadoxStorageQualifier.Extern) && !((result as Variable).Type is ArrayType))
                mixin = CompositionsPerVariable[result as Variable][index];

            return result;
        }

        private Variable FindVariableInMixin(string varName, ModuleMixin mixin)
        {
            if (varName == "streams")
                return null;

            var foundVar = mixin.VirtualTable.Variables.FirstOrDefault(x => x.Variable.Name.Text == varName);
            if (foundVar != null)
                return foundVar.Variable;

            log.Error(ParadoxMessageCode.ErrorVariableNotFound, new SourceSpan(), varName, mixin.MixinName);
            return null;
        }

        private MethodDeclaration FindMethod(Expression expression, ref ModuleMixin mixin)
        {
            if (expression is MemberReferenceExpression)
            {
                var memberExpression = expression as MemberReferenceExpression;
                var target = memberExpression.Target;

                if (target.TypeInference.Declaration is Variable)
                    FindVariable(target, ref mixin);
                else if (target.TypeInference.Declaration is ShaderClassType || target.TypeInference.TargetType is ShaderClassType)
                    FindShader(target, ref mixin);
            }

            var topMixin = GetTopMixin(mixin);
            if (topMixin == null)
            {
                log.Error(ParadoxMessageCode.ErrorTopMixinNotFound, expression.Span, expression);
                return null;
            }
            var foundMethod = topMixin.GetMethodFromExpression(expression);
            if (foundMethod == null)
            {
                log.Error(ParadoxMessageCode.ErrorCallNotFound, expression.Span, expression);
                return null;
            }
            if (foundMethod.Qualifiers.Contains(ParadoxStorageQualifier.Abstract))
            {
                log.Error(ParadoxMessageCode.ErrorCallToAbstractMethod, expression.Span, expression, foundMethod);
                return null;
            }
            return foundMethod;
        }

        private void FindShader(Expression expression, ref ModuleMixin mixin)
        {
            if (expression is MemberReferenceExpression)
            {
                var memberExpression = expression as MemberReferenceExpression;
                var target = memberExpression.Target;

                if (target.TypeInference.Declaration is Variable)
                    FindVariable(target, ref mixin);

                var mixinName = (expression.TypeInference.Declaration as ShaderClassType).Name.Text;
                mixin = mixin.MixinName == mixinName ? mixin : mixin.InheritanceList.FirstOrDefault(x => x.MixinName == mixinName);
            }
            else if (expression is IndexerExpression)
            {
                var indexerExpression = expression as IndexerExpression;
                var target = indexerExpression.Target;

                Variable result = null;

                if (target.TypeInference.Declaration is Variable)
                    result = FindVariable(target, ref mixin);

                var index = (int)(indexerExpression.Index as LiteralExpression).Value;
                if (result is Variable && (result as Variable).Qualifiers.Contains(ParadoxStorageQualifier.Extern))
                    mixin = CompositionsPerVariable[result as Variable][index];
            }
        }

        /// <summary>
        /// Build inheritance list for methods
        /// </summary>
        private void BuildStageInheritance()
        {
            foreach (var mixin in MixinInheritance)
                InsertStageMethods(mixin.LocalVirtualTable.Methods.Select(x => x.Method).Where(x => x.Qualifiers.Values.Contains(ParadoxStorageQualifier.Stage)).ToList(), GetTopMixin(mixin));
        }

        /// <summary>
        /// Adds le methods in the list to the inheritance list
        /// </summary>
        /// <param name="extMethodList">the list of methods</param>
        /// <param name="mixin">the mixin in which the methods are defined</param>
        public void InsertStageMethods(List<MethodDeclaration> extMethodList, ModuleMixin mixin)
        {
            foreach (var extMethod in extMethodList)
            {
                if (extMethod is MethodDefinition)
                {
                    var isClone = extMethod.Qualifiers.Values.Contains(ParadoxStorageQualifier.Clone);
                    var newEntry = true;

                    // find a corresponding method
                    var vtReference = mixin.VirtualTable.GetBaseDeclaration(extMethod);
                    foreach (var stageMethodList in StageMethodInheritance)
                    {
                        if (!newEntry)
                            break;

                        if (stageMethodList == null || stageMethodList.Count == 0)
                            continue;

                        var firstOccurence = stageMethodList.First();
                        var occurenceMixin = firstOccurence.GetTag(ParadoxTags.ShaderScope) as ModuleMixin;
                        var listVTReference = occurenceMixin.VirtualTable.GetBaseDeclaration(firstOccurence);

                        if (vtReference.Slot != listVTReference.Slot || vtReference.Shader != listVTReference.Shader)
                            continue;

                        newEntry = false;
                        var extMixin = extMethod.GetTag(ParadoxTags.ShaderScope) as ModuleMixin;
                        if (isClone || extMixin.OccurenceId == 1)
                            stageMethodList.Add(extMethod);
                    }
                    
                    if (newEntry)
                    {
                        var list = new List<MethodDeclaration>();
                        list.Add(extMethod);
                        StageMethodInheritance.Add(list);
                    }
                    
                    var externClassRef = GetTopMixin(mixin).ClassReferences;
                    if (externClassRef != null && !mainModuleMixin.ClassReferences.MethodsReferences.ContainsKey(extMethod))
                    {
                        externClassRef.RegenKeys();
                        mainModuleMixin.ClassReferences.MethodsReferences.Add(extMethod, externClassRef.MethodsReferences[extMethod]);
                        externClassRef.MethodsReferences.Remove(extMethod);
                    }
                }
            }
        }

        /// <summary>
        /// Add the method to its correct dictionary
        /// </summary>
        /// <param name="expression"></param>
        private void AddToMethodsReferences(MethodInvocationExpression expression)
        {
            var decl = expression.Target.TypeInference.Declaration as MethodDeclaration;
            if (decl != null)
            {
                if (!mainModuleMixin.ClassReferences.MethodsReferences.ContainsKey(decl))
                    mainModuleMixin.ClassReferences.MethodsReferences.Add(decl, new HashSet<MethodInvocationExpression>());
                mainModuleMixin.ClassReferences.MethodsReferences[decl].Add(expression);
            }
        }

        /// <summary>
        /// Remove the method from the correctdictionary
        /// </summary>
        /// <param name="expression"></param>
        private void RemoveFromMethodsReferences(MethodInvocationExpression expression, ModuleMixin mixin)
        {
            foreach (var refList in mixin.ClassReferences.MethodsReferences)
                refList.Value.RemoveWhere(x => x == expression);

            foreach (var refList in mainModuleMixin.ClassReferences.MethodsReferences)
                refList.Value.RemoveWhere(x => x == expression);
        }

        /// <summary>
        /// Find the mixin in which the parameter is a dependency
        /// </summary>
        /// <param name="mixin">the mixin</param>
        /// <returns>the mixin that depends on the parameter</returns>
        private ModuleMixin GetTopMixin(ModuleMixin mixin)
        {
            var topMixin = mainModuleMixin == mixin || mainModuleMixin.InheritanceList.Any(x => x == mixin) ? mainModuleMixin : null;
            if (topMixin == null)
            {
                foreach (var externMixes in CompositionsPerVariable)
                {
                    foreach (var externMix in externMixes.Value)
                    {
                        topMixin = externMix == mixin || externMix.InheritanceList.Any(x => x == mixin) ? externMix : null;
                        if (topMixin != null)
                            break;
                    }
                    if (topMixin != null)
                        break;
                }
            }
            return topMixin;
        }

        /// <summary>
        /// Find a static method
        /// </summary>
        /// <param name="expression">the calling expression</param>
        /// <returns>the correct called method</returns>
        private MethodDeclaration FindStaticMethod(MethodInvocationExpression expression)
        {
            var defMixin = (expression.Target.TypeInference.Declaration as MethodDeclaration).GetTag(ParadoxTags.ShaderScope) as ModuleMixin;
            defMixin = mixContext[defMixin.MixinName];
            return defMixin.GetMethodFromExpression(expression.Target);
        }

        /// <summary>
        /// Gets the base stage method
        /// </summary>
        /// <param name="methodCall">the reference expression</param>
        /// <returns>the base declaration</returns>
        private MethodDeclaration GetBaseStageMethod(MethodInvocationExpression methodCall)
        {
            var mixin = methodCall.GetTag(ParadoxTags.CurrentShader) as ModuleMixin;
            var vtReference = mixin.VirtualTable.GetBaseDeclaration(methodCall.Target.TypeInference.Declaration as MethodDeclaration);
            foreach (var stageMethodList in StageMethodInheritance)
            {
                if (stageMethodList == null || stageMethodList.Count == 0)
                    continue;

                var firstOccurence = stageMethodList.First();
                var occurenceMixin = firstOccurence.GetTag(ParadoxTags.ShaderScope) as ModuleMixin;
                var listVTReference = occurenceMixin.VirtualTable.GetBaseDeclaration(firstOccurence);

                if (vtReference.Slot != listVTReference.Slot || vtReference.Shader != listVTReference.Shader)
                    continue;

                //TODO: can we call a base without overriding ?
                for (int j = stageMethodList.Count - 1; j > 0; --j)
                {
                    var decl = stageMethodList[j];
                    if (decl.GetTag(ParadoxTags.ShaderScope) as ModuleMixin == mixin)
                        return stageMethodList[j - 1];
                }
                //for (int j = stageMethodList.Count - 1; j >= 0; --j)
                //{
                //    var decl = stageMethodList[j];
                //    if (decl.GetTag(ParadoxTags.ShaderScope) as ModuleMixin == mixin)
                //    {
                //        if (j == 0)
                //            return stageMethodList[0];
                //        return stageMethodList[j - 1];
                //    }
                //}
            }
            return null;
        }

        /// <summary>
        /// Gets the last override of the method
        /// </summary>
        /// <param name="methodCall">the method call</param>
        /// <returns>the declaration</returns>
        private MethodDeclaration GetThisStageMethod(MethodInvocationExpression methodCall)
        {
            var mixin = methodCall.GetTag(ParadoxTags.CurrentShader) as ModuleMixin;
            var vtReference = mixin.VirtualTable.GetBaseDeclaration(methodCall.Target.TypeInference.Declaration as MethodDeclaration);
            foreach (var stageMethodList in StageMethodInheritance)
            {
                if (stageMethodList == null || stageMethodList.Count == 0)
                    continue;

                var firstOccurence = stageMethodList.First();
                var occurenceMixin = firstOccurence.GetTag(ParadoxTags.ShaderScope) as ModuleMixin;
                var listVTReference = occurenceMixin.VirtualTable.GetBaseDeclaration(firstOccurence);

                if (vtReference.Slot != listVTReference.Slot || vtReference.Shader != listVTReference.Shader)
                    continue;

                return stageMethodList.Last();
            }
            return null;
        }

        /// <summary>
        /// Solves both base and direct method calls
        /// </summary>
        /// <param name="mixin"></param>
        private void PatchAllMethodInferences(ModuleMixin mixin)
        {
            mixin.InheritanceList.ForEach(PatchAllMethodInferences);
            CompositionsPerVariable.Where(x => mixin.LocalVirtualTable.Variables.Any(y => y.Variable == x.Key)).ToList().ForEach(externMixes => externMixes.Value.ForEach(PatchAllMethodInferences));

            var topMixin = GetTopMixin(mixin);

            foreach (var baseCall in mixin.ParsingInfo.BaseMethodCalls)
            {
                MethodDeclaration decl = null;
                if ((baseCall.Target.TypeInference.Declaration as MethodDeclaration).Qualifiers.Contains(ParadoxStorageQualifier.Stage))
                    decl = GetBaseStageMethod(baseCall);
                else
                    decl = topMixin.GetBaseMethodFromExpression(baseCall.Target, mixin);

                if (decl != null)
                {
                    RemoveFromMethodsReferences(baseCall, topMixin);

                    baseCall.TypeInference.TargetType = decl.ReturnType;
                    baseCall.Target.TypeInference.Declaration = decl;

                    AddToMethodsReferences(baseCall);
                }
                else
                    log.Error(ParadoxMessageCode.ErrorImpossibleBaseCall, baseCall.Span, baseCall, mixin.MixinName);
            }

            // resolve this calls
            foreach (var thisCall in mixin.ParsingInfo.ThisMethodCalls)
            {
                MethodDeclaration decl = null;
                if ((thisCall.Target.TypeInference.Declaration as MethodDeclaration).Qualifiers.Contains(ParadoxStorageQualifier.Stage))
                    decl = GetThisStageMethod(thisCall);
                else if (thisCall.ContainsTag(ParadoxTags.StaticRef))
                    decl = FindStaticMethod(thisCall);
                else
                    decl = topMixin.GetMethodFromExpression(thisCall.Target);
                
                if (decl != null)
                {
                    RemoveFromMethodsReferences(thisCall, topMixin);

                    thisCall.TypeInference.TargetType = decl.ReturnType;
                    thisCall.Target.TypeInference.Declaration = decl;

                    if (!thisCall.ContainsTag(ParadoxTags.StaticRef))
                        AddToMethodsReferences(thisCall);
                }
                else
                    log.Error(ParadoxMessageCode.ErrorImpossibleVirtualCall, thisCall.Span, thisCall, mixin.MixinName, mainModuleMixin.MixinName);
            }
        }

        /// <summary>
        /// Rebranch the type inference for the stage variable reference in the extern
        /// </summary>
        /// <param name="externMix"></param>
        private void InferStageVariables(ModuleMixin externMix)
        {
            var stageDict = externMix.ClassReferences.VariablesReferences.Where(x => x.Key.Qualifiers.Contains(ParadoxStorageQualifier.Stage)).ToDictionary(x => x.Key, x => x.Value);
            foreach (var variable in stageDict)
            {
                var shaderName = (variable.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName;
                var foundDeclaration = mainModuleMixin.ClassReferences.VariablesReferences.FirstOrDefault(x => x.Key.Name.Text == variable.Key.Name.Text && (x.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName == shaderName).Key;
                if (foundDeclaration == null)// get by semantics if necessary
                {
                    var semantic = variable.Key.Qualifiers.Values.OfType<Semantic>().FirstOrDefault();
                    if (semantic != null)
                    {
                        foundDeclaration = mainModuleMixin.ClassReferences.VariablesReferences.FirstOrDefault(
                            x =>
                                {
                                    var varSemantic = x.Key.Qualifiers.Values.OfType<Semantic>().FirstOrDefault();
                                    if (varSemantic != null && semantic.Name.Text == varSemantic.Name.Text)
                                        return true;
                                    return false;
                                }).Key;
                    }
                }

                if (foundDeclaration != null)
                {
                    mainModuleMixin.ClassReferences.VariablesReferences[foundDeclaration].UnionWith(variable.Value);
                    foreach (var varRef in variable.Value)
                    {
                        varRef.Expression.TypeInference.Declaration = foundDeclaration;
                        varRef.Expression.TypeInference.TargetType = foundDeclaration.Type;
                    }
                }
                else
                {
                    log.Error(ParadoxMessageCode.ErrorMissingStageVariable, variable.Key.Span, variable, externMix.MixinName);
                    return;
                }
            }

            foreach (var key in stageDict.Keys)
                externMix.ClassReferences.VariablesReferences.Remove(key);
        }

        /// <summary>
        /// Inference for extern calls
        /// </summary>
        /// <param name="mixin"></param>
        private void ProcessExternReferences(ModuleMixin mixin)
        {
            mixin.InheritanceList.ForEach(ProcessExternReferences);
            CompositionsPerVariable.Where(x => mixin.LocalVirtualTable.Variables.Any(y => y.Variable == x.Key)).ToList().ForEach(externMixes => externMixes.Value.ForEach(ProcessExternReferences));
            
            foreach (var externReferences in mixin.ExternReferences.VariablesReferences)
            {
                foreach (var expression in externReferences.Value)
                {
                    var searchMixin = mixin;
                    var foundDefinition = FindVariable(expression.Expression, ref searchMixin);
                    if (foundDefinition != null) // should be always true
                    {
                        if (foundDefinition.Qualifiers.Contains(ParadoxStorageQualifier.Stage))
                        {

                            var sameVar =
                                mixin.ClassReferences.VariablesReferences.FirstOrDefault(
                                    x => x.Key.Name.Text == foundDefinition.Name.Text && (x.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName == (foundDefinition.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName).Key;
                            if (sameVar == null)
                            {
                                mixin.ClassReferences.VariablesReferences.Add(foundDefinition, new HashSet<ExpressionNodeCouple>());
                                sameVar = foundDefinition;
                            }
                            mixin.ClassReferences.VariablesReferences[sameVar].Add(expression);
                            expression.Expression.TypeInference.Declaration = sameVar;
                            expression.Expression.TypeInference.TargetType = sameVar.Type.ResolveType();
                        }
                        else
                        {
                            if (!mixin.ClassReferences.VariablesReferences.ContainsKey(foundDefinition))
                                mixin.ClassReferences.VariablesReferences.Add(foundDefinition, new HashSet<ExpressionNodeCouple>());
                            mixin.ClassReferences.VariablesReferences[foundDefinition].Add(expression);
                            expression.Expression.TypeInference.Declaration = foundDefinition;
                            expression.Expression.TypeInference.TargetType = foundDefinition.Type.ResolveType();
                        }
                    }
                    else
                        log.Error(ParadoxMessageCode.ErrorExternReferenceNotFound, expression.Expression.Span, expression, mixin.MixinName);
                }
            }
            mixin.ExternReferences.VariablesReferences.Clear();
            
            foreach (var externReferences in mixin.ExternReferences.MethodsReferences)
            {
                foreach (var methodInvoc in externReferences.Value)
                {
                    var searchMixin = mixin;
                    var foundDefinition = FindMethod(methodInvoc.Target, ref searchMixin);
                    if (foundDefinition != null) // should be always true
                    {
                        if (!mixin.ClassReferences.MethodsReferences.ContainsKey(foundDefinition))
                            mixin.ClassReferences.MethodsReferences.Add(foundDefinition, new HashSet<MethodInvocationExpression>());
                        mixin.ClassReferences.MethodsReferences[foundDefinition].Add(methodInvoc);
                        methodInvoc.Target.TypeInference.Declaration = foundDefinition;
                    }
                    else
                        log.Error(ParadoxMessageCode.ErrorExternReferenceNotFound, methodInvoc.Span, methodInvoc, mixin.MixinName);
                }
            }
            mixin.ExternReferences.MethodsReferences.Clear();
        }

        /// <summary>
        /// Redo type inference for stage init variables
        /// </summary>
        /// <param name="moduleMixin">the module mixin to analyze</param>
        private void ProcessStageInitReferences(ModuleMixin moduleMixin)
        {
            foreach (var variable in moduleMixin.StageInitReferences.VariablesReferences)
            {
                var varMixinName = ((ModuleMixin)variable.Key.GetTag(ParadoxTags.ShaderScope)).MixinName;
                var mixin = MixinInheritance.FirstOrDefault(x => x.MixinName == varMixinName);
                if (mixin == null)
                {
                    log.Error(ParadoxMessageCode.ErrorStageMixinNotFound, new SourceSpan(), varMixinName, moduleMixin.MixinName);
                    return;
                } 
                
                var trueVar = mixin.ClassReferences.VariablesReferences.FirstOrDefault(x => x.Key.Name.Text == variable.Key.Name.Text).Key;
                if (trueVar == null)
                {
                    var sourceShader = ((ModuleMixin)variable.Key.GetTag(ParadoxTags.ShaderScope)).MixinName;
                    log.Error(ParadoxMessageCode.ErrorStageMixinVariableNotFound, new SourceSpan(), varMixinName, sourceShader, moduleMixin.MixinName);
                    return;
                }

                foreach (var varRef in variable.Value)
                {
                    varRef.Expression.TypeInference.Declaration = trueVar;
                    varRef.Expression.TypeInference.TargetType = trueVar.Type.ResolveType();
                }

                mainModuleMixin.ClassReferences.VariablesReferences[trueVar].UnionWith(variable.Value);
            }
            foreach (var method in moduleMixin.StageInitReferences.MethodsReferences)
            {
                var varMixinName = ((ModuleMixin)method.Key.GetTag(ParadoxTags.ShaderScope)).MixinName;
                var mixin = MixinInheritance.FirstOrDefault(x => x.MixinName == varMixinName);
                if (mixin == null)
                {
                    log.Error(ParadoxMessageCode.ErrorStageMixinNotFound, new SourceSpan(), varMixinName, moduleMixin.MixinName);
                    return;
                }

                var trueVar = GetTopMixin(mixin).GetMethodFromDeclaration(method.Key);
                if (trueVar == null)
                {
                    log.Error(ParadoxMessageCode.ErrorStageMixinMethodNotFound, new SourceSpan(), varMixinName, method, moduleMixin.MixinName);
                    return;
                }

                foreach (var varRef in method.Value)
                {
                    varRef.Target.TypeInference.Declaration = trueVar;
                    varRef.Target.SetTag(ParadoxTags.VirtualTableReference, trueVar.GetTag(ParadoxTags.VirtualTableReference));
                }

                mainModuleMixin.ClassReferences.MethodsReferences[trueVar].UnionWith(method.Value);
            }
        }

        /// <summary>
        /// Relink stage references, extern references, merge static references from externs
        /// </summary>
        private void ProcessExterns()
        {
            ProcessExternReferences(mainModuleMixin);

            AddStageVariables(mainModuleMixin);
            foreach (var externMix in CompositionsPerVariable.SelectMany(externMixes => externMixes.Value))
                InferStageVariables(externMix);

            ProcessStageInitReferences(mainModuleMixin);
            CompositionsPerVariable.SelectMany(externMixes => externMixes.Value).ToList().ForEach(ProcessStageInitReferences);
            
            foreach (var externMix in CompositionsPerVariable.SelectMany(externMixes => externMixes.Value))
            {
                foreach (var variable in externMix.StaticReferences.VariablesReferences)
                {
                    var varMixinName = (variable.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName;
                    var staticVars = mainModuleMixin.StaticReferences.VariablesReferences.Where(x => (x.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName == varMixinName && x.Key.Name.Text == variable.Key.Name.Text).ToDictionary(x => x.Key, x => x.Value);

                    // if the entry already exists, append to it
                    if (staticVars.Count > 0)
                    {
                        var staticVar = staticVars.FirstOrDefault();
                        foreach (var varRef in variable.Value)
                        {
                            varRef.Expression.TypeInference.Declaration = staticVar.Key;
                            varRef.Expression.TypeInference.TargetType = staticVar.Key.Type.ResolveType();
                        }
                        staticVar.Value.UnionWith(variable.Value);
                    }
                    else // create the entry
                        mainModuleMixin.StaticReferences.VariablesReferences.Add(variable.Key, variable.Value);
                }
                foreach (var method in externMix.StaticReferences.MethodsReferences)
                {
                    var methodMixinName = (method.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName;
                    var staticMethods = mainModuleMixin.StaticReferences.MethodsReferences.Where(x => (x.Key.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName == methodMixinName && x.Key.IsSameSignature(method.Key)).ToDictionary(x => x.Key, x => x.Value);

                    // if the entry already exists, append to it
                    if (staticMethods.Count > 0)
                    {
                        var staticMethod = staticMethods.FirstOrDefault();
                        foreach (var methodRef in method.Value)
                            methodRef.Target.TypeInference.Declaration = staticMethod.Key;

                        staticMethod.Value.UnionWith(method.Value);
                    }
                    else // create the entry
                        mainModuleMixin.StaticReferences.MethodsReferences.Add(method.Key, method.Value);
                }
            }
        }

        /// <summary>
        /// Rename all the variables
        /// </summary>
        private void RenameAllVariables()
        {
            int id = 0;
            RenameAllVariables(mainModuleMixin.ClassReferences, ref id);
        }

        /// <summary>
        /// Rename all the variables and their references based on the id
        /// </summary>
        /// <param name="references">the pool to rename</param>
        /// <param name="id">the id used to build the new name</param>
        private void RenameAllVariables(ReferencesPool references, ref int id)
        {
            foreach (var variable in references.VariablesReferences)
            {
                if (variable.Key.Name.Text == FlipRendertargetVariableName) // DO NOT RENAME THIS SPECIFIC VARIABLE
                    continue;

                foreach (var varRef in variable.Value)
                {
                    if (varRef.Expression is MemberReferenceExpression)
                    {
                        if (variable.Key.Qualifiers.Contains(ParadoxStorageQualifier.Stream)) // TODO: change test
                        {
                            (varRef.Expression as MemberReferenceExpression).Member = variable.Key.Name;

                            var type = (varRef.Expression as MemberReferenceExpression).Target.TypeInference.TargetType as StreamsType;
                            if (type == null || !type.IsInputOutput)
                                (varRef.Expression as MemberReferenceExpression).Target = new VariableReferenceExpression(StreamsType.ThisStreams);
                        }
                        else if (variable.Key.Qualifiers.Contains(ParadoxStorageQualifier.PatchStream))
                        {
                            (varRef.Expression as MemberReferenceExpression).Member = variable.Key.Name;
                        }
                        else
                        {
                            var vre = new VariableReferenceExpression(variable.Key.Name);
                            vre.TypeInference.Declaration = variable.Key;
                            vre.TypeInference.TargetType = variable.Key.Type.ResolveType();
                            ReplaceMemberReferenceExpressionByVariableReferenceExpression(varRef.Expression as MemberReferenceExpression, vre, varRef.Node);
                            varRef.Expression = vre;
                        }
                    }
                    else
                        (varRef.Expression as VariableReferenceExpression).Name = variable.Key.Name;
                }

                variable.Key.Name.Text += "_id" + id;
                ++id;
            }
        }

        /// <summary>
        /// Rename the methods and their references
        /// </summary>
        /// <param name="references">the pool to rename</param>
        /// <param name="id">the id used to build the new name</param>
        private void RenameAllMethods(ReferencesPool references, HashSet<MethodDefinition> renameFreeMethods, ref int id)
        {
            foreach (var method in references.MethodsReferences)
            {
                if (renameFreeMethods.Contains(method.Key) || !(method.Key is MethodDefinition))
                    continue;

                foreach (var methodRef in method.Value)
                {
                    var targetMre = methodRef.Target as MemberReferenceExpression;
                    if (targetMre != null)
                    {
                        var vre = new VariableReferenceExpression();
                        methodRef.Target = vre;
                        vre.TypeInference.Declaration = targetMre.TypeInference.Declaration;
                        vre.TypeInference.TargetType = targetMre.TypeInference.TargetType;
                    }

                    var targetVre = methodRef.Target as VariableReferenceExpression;
                    targetVre.Name = method.Key.Name;
                }
                
                method.Key.Name.Text += "_id" + id;
                ++id;
            }
            references.RegenKeys();
        }

        /// <summary>
        /// Rename all the methods
        /// </summary>
        private void RenameAllMethods()
        {
            // Find entry points
            var vertexShaderMethod = FindEntryPoint("VSMain");
            var hullShaderMethod = FindEntryPoint("HSMain");
            var hullConstantShaderMethod = FindEntryPoint("HSConstantMain");
            var domainShaderMethod = FindEntryPoint("DSMain");
            var geometryShaderMethod = FindEntryPoint("GSMain");
            var pixelShaderMethod = FindEntryPoint("PSMain");
            var computeShaderMethod = FindEntryPoint("CSMain");

            if (pixelShaderMethod != null && pixelShaderMethod.Body.Count == 0)
                pixelShaderMethod = null;

            var renameFreeMethods = new HashSet<MethodDefinition>();

            // store these methods to prevent their renaming
            if (vertexShaderMethod != null)
                renameFreeMethods.Add(vertexShaderMethod);
            if (hullShaderMethod != null)
                renameFreeMethods.Add(hullShaderMethod);
            if (hullConstantShaderMethod != null)
                renameFreeMethods.Add(hullConstantShaderMethod);
            if (domainShaderMethod != null)
                renameFreeMethods.Add(domainShaderMethod);
            if (geometryShaderMethod != null)
                renameFreeMethods.Add(geometryShaderMethod);
            if (pixelShaderMethod != null)
                renameFreeMethods.Add(pixelShaderMethod);
            if (computeShaderMethod != null)
                renameFreeMethods.Add(computeShaderMethod);

            int id = 0;

            RenameAllMethods(mainModuleMixin.ClassReferences, renameFreeMethods, ref id);
        }

        /// <summary>
        /// Finds all the function with the name
        /// </summary>
        /// <param name="name">the name of the function</param>
        /// <returns>a collection of all the functions with that name, correctly ordered</returns>
        private MethodDefinition FindEntryPoint(string name)
        {
            for (int i = MixinInheritance.Count - 1; i >= 0; --i)
            {
                var mixin = MixinInheritance[i];
                var count = 0;
                for (int j = 0; j < i; ++j)
                {
                    count += mixin.MixinName == MixinInheritance[j].MixinName ? 1 : 0;
                }
                
                var method = mixin.LocalVirtualTable.Methods.FirstOrDefault(x => x.Method.Name.Text == name && x.Method is MethodDefinition);
                if (method != null && (count == 0 || method.Method.Qualifiers.Contains(ParadoxStorageQualifier.Clone)))
                    return method.Method as MethodDefinition;
            }
            return null;
        }

        /// <summary>
        /// Creates a new AST with all the definitions
        /// </summary>
        private void GenerateShader()
        {
            MixedShader = new ShaderClassType(mainModuleMixin.MixinName);

            // Add structures, typedefs
            foreach (var mixin in MixinInheritance.Where(x => x.OccurenceId == 1))
                MixedShader.Members.AddRange(mixin.ParsingInfo.Typedefs);
            foreach (var mixin in MixinInheritance.Where(x => x.OccurenceId == 1))
                MixedShader.Members.AddRange(mixin.ParsingInfo.StructureDefinitions);
            
            // Create constant buffer
            GroupByConstantBuffer();
            
            // add the methods
            MixedShader.Members.AddRange(mainModuleMixin.ClassReferences.MethodsReferences.Select(x => x.Key).Where(x => x is MethodDefinition));

            // remove duplicates
            MixedShader.Members = MixedShader.Members.Distinct().ToList();

            // Create streams
            ParadoxStreamCreator.Run(MixedShader, mainModuleMixin, MixinInheritance, log);
            
            if (log.HasErrors)
                return;

            // deal with foreach statements
            ExpandForEachStatements(mainModuleMixin);
            foreach (var externMix in CompositionsPerVariable.SelectMany(x => x.Value))
                ExpandForEachStatements(externMix);
            
            // remove useless variables
            RemoveUselessVariables();
        }

        /// <summary>
        /// Remove useless variables
        /// </summary>
        private void RemoveUselessVariables()
        {
            var variablesUsages = MixedShader.Members.OfType<Variable>().ToDictionary(variable => variable, variable => false);
            
            foreach (var constantBuffer in MixedShader.Members.OfType<ConstantBuffer>())
            {
                foreach (var variable in constantBuffer.Members.OfType<Variable>().ToList())
                    variablesUsages.Add(variable, false);
            }

            MixedShader.Members.RemoveAll(x => x is Variable && (x as Variable).Qualifiers.Contains(ParadoxStorageQualifier.Extern));
            
            var variableUsageVisitor = new ParadoxVariableUsageVisitor(variablesUsages);
            variableUsageVisitor.Run(MixedShader);

            foreach (var variable in MixedShader.Members.OfType<Variable>().ToList())
            {
                bool used;
                if (variablesUsages.TryGetValue(variable, out used))
                {
                    if (!used && variable.Name.Text != FlipRendertargetVariableName)
                        MixedShader.Members.Remove(variable);
                }
            }

            foreach (var constantBuffer in MixedShader.Members.OfType<ConstantBuffer>())
            {
                foreach (var variable in constantBuffer.Members.OfType<Variable>().ToList())
                {
                    bool used;
                    if (variablesUsages.TryGetValue(variable, out used))
                    {
                        if (!used && variable.Name.Text != FlipRendertargetVariableName)
                            constantBuffer.Members.Remove(variable);
                    }
                }
            }
        }

        /// <summary>
        /// Test if the variable should be in a constant buffer
        /// </summary>
        /// <param name="variable">the variable</param>
        /// <returns>true/false</returns>
        private bool IsOutOfCBufferVariable(Variable variable)
        {
            return variable.Type is SamplerType || variable.Type is SamplerStateType || variable.Type is TextureType || variable.Type is StateType || variable.Type.ResolveType() is ObjectType || variable.Qualifiers.Contains(StorageQualifier.Const);
        }

        /// <summary>
        /// Test if the variable should be in a constant buffer
        /// </summary>
        /// <param name="variable">the variable</param>
        /// <returns>true/false</returns>
        private bool KeepVariableInCBuffer(Variable variable)
        {
            return !(variable.Qualifiers.Contains(ParadoxStorageQualifier.Extern) || variable.Qualifiers.Contains(ParadoxStorageQualifier.Stream) || variable.Qualifiers.Contains(ParadoxStorageQualifier.PatchStream) || IsOutOfCBufferVariable(variable));
        }

        // Group everything by constant buffers
        private void GroupByConstantBuffer()
        {
            MergeSameSemanticVariables(mainModuleMixin.ClassReferences.VariablesReferences.Select(x => x.Key).ToList());
            MergeReferenceVariables(mainModuleMixin.ClassReferences.VariablesReferences.Select(x => x.Key).ToList());
            var usefulVars = mainModuleMixin.ClassReferences.VariablesReferences.Select(x => x.Key).Where(KeepVariableInCBuffer);
            var varList = usefulVars.Where(x => x.ContainsTag(ParadoxTags.ConstantBuffer)).ToList();
            var groupedVarList = varList.GroupBy(x => x.GetTag(ParadoxTags.ConstantBuffer) as string).Select(x => x.ToList()).ToList();

            foreach (var group in groupedVarList)
            {
                var cbufferName = group.FirstOrDefault().GetTag(ParadoxTags.ConstantBuffer) as string;
                var cbuffer = new ConstantBuffer { Type = SiliconStudio.Shaders.Ast.Hlsl.ConstantBufferType.Constant, Name = cbufferName };
                cbuffer.Members.AddRange(group);

                MixedShader.Members.Add(cbuffer);
            }

            var remainingVars = usefulVars.Where(x => !x.ContainsTag(ParadoxTags.ConstantBuffer)).ToList();
            var globalBuffer = new ConstantBuffer { Type = SiliconStudio.Shaders.Ast.Hlsl.ConstantBufferType.Constant, Name = "Globals" };
            if (remainingVars.Count > 0)
            {
                globalBuffer.Members.AddRange(remainingVars);
                MixedShader.Members.Add(globalBuffer);
            }

            MixedShader.Members.AddRange(mainModuleMixin.ClassReferences.VariablesReferences.Select(x => x.Key).Where(IsOutOfCBufferVariable));
        }


        /// <summary>
        /// Merge all the variables with the same semantic and rename them (but typeinference is not correct)
        /// </summary>
        private void MergeSameSemanticVariables(List<Variable> variables)
        {
            var duplicateVariables = new List<Variable>(); // list of variables that will be removed
            var allVariablesWithSemantic = variables.Where(x => x.Qualifiers.Values.Any(y => y is Semantic)).ToList();
            foreach (var variable in allVariablesWithSemantic)
            {
                if (!duplicateVariables.Contains(variable))
                {
                    var semantic = variable.Qualifiers.OfType<Semantic>().First();

                    var sameSemanticVariables = allVariablesWithSemantic.Where(x => x != variable && x.Qualifiers.Values.OfType<Semantic>().Any(y => AreSameSemantics(y.Name.Text, semantic.Name.Text))).ToList();
                    
                    foreach (var sameSemVar in sameSemanticVariables)
                    {
                        var cbufferName = variable.ContainsTag(ParadoxTags.ConstantBuffer) ? variable.GetTag(ParadoxTags.ConstantBuffer) as string : null;
                        var newcbufferName = sameSemVar.ContainsTag(ParadoxTags.ConstantBuffer) ? sameSemVar.GetTag(ParadoxTags.ConstantBuffer) as string : null;
                        if (cbufferName != null ^ newcbufferName != null)
                            variable.SetTag(ParadoxTags.ConstantBuffer, cbufferName ?? newcbufferName);
                        else if (cbufferName != null && cbufferName != newcbufferName)
                        {
                            var sourceMixinName = (variable.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName;
                            var newMixinName = (sameSemVar.GetTag(ParadoxTags.ShaderScope) as ModuleMixin).MixinName;
                            log.Error(ParadoxMessageCode.ErrorSemanticCbufferConflict, variable.Span, variable, sourceMixinName, sameSemVar, newMixinName, semantic, cbufferName, newcbufferName);
                        }

                        foreach (var exp in mainModuleMixin.ClassReferences.VariablesReferences[sameSemVar])
                        {
                            if (exp.Expression is VariableReferenceExpression)
                                (exp.Expression as VariableReferenceExpression).Name = variable.Name;
                            else if (exp.Expression is MemberReferenceExpression)
                                (exp.Expression as MemberReferenceExpression).Member = variable.Name;

                            exp.Expression.TypeInference.Declaration = variable;
                        }
                        mainModuleMixin.ClassReferences.VariablesReferences[variable].UnionWith(mainModuleMixin.ClassReferences.VariablesReferences[sameSemVar]);
                        sameSemVar.Name = variable.Name;
                    }

                    duplicateVariables.AddRange(sameSemanticVariables);
                }
            }
            
            mainModuleMixin.ClassReferences.RegenKeys();
            duplicateVariables.ForEach(variable => mainModuleMixin.ClassReferences.VariablesReferences.Remove(variable));
        }

        /// <summary>
        /// Merge variables that are references of another one
        /// </summary>
        /// <param name="variables"></param>
        private void MergeReferenceVariables(List<Variable> variables)
        {
            var duplicateVariables = new List<Variable>();
            foreach (var variable in variables.Where(x => x.InitialValue is MemberReferenceExpression || x.InitialValue is VariableReferenceExpression))
            {
                //find reference
                var target = variable.InitialValue.TypeInference.Declaration as Variable;
                if (target != null)
                {
                    foreach (var exp in mainModuleMixin.ClassReferences.VariablesReferences[variable])
                    {
                        if (exp.Expression is VariableReferenceExpression)
                            (exp.Expression as VariableReferenceExpression).Name = target.Name;
                        else if (exp.Expression is MemberReferenceExpression)
                            (exp.Expression as MemberReferenceExpression).Member = target.Name;

                        exp.Expression.TypeInference.Declaration = target;
                    }
                    mainModuleMixin.ClassReferences.VariablesReferences[target].UnionWith(mainModuleMixin.ClassReferences.VariablesReferences[variable]);
                    variable.Name = target.Name;
                    duplicateVariables.Add(variable);
                }
            }

            mainModuleMixin.ClassReferences.RegenKeys();
            duplicateVariables.ForEach(variable => mainModuleMixin.ClassReferences.VariablesReferences.Remove(variable));
        }

        #endregion

        #region Static helpers

        /// <summary>
        /// Replaces the ForEachStatements in the mixin by ForStatements
        /// </summary>
        /// <param name="mixin">the mixin</param>
        private static void ExpandForEachStatements(ModuleMixin mixin)
        {
            foreach (var statementNodeCouple in mixin.ParsingInfo.ForEachStatements.Where(x => !(x.Statement as ForEachStatement).Variable.Qualifiers.Contains(ParadoxStorageQualifier.Extern)))
            {
                var newStatement = ExpandForEachStatement(statementNodeCouple.Statement as ForEachStatement);
                if (newStatement != null)
                {
                    var replace = new ParadoxReplaceVisitor(statementNodeCouple.Statement, newStatement);
                    replace.Run(statementNodeCouple.Node);
                }
            }

            mixin.InheritanceList.ForEach(ExpandForEachStatements);
        }

        /// <summary>
        /// Creates a ForStatement with the same behavior
        /// </summary>
        /// <param name="forEachStatement">the ForEachStatement</param>
        /// <returns>the ForStatement</returns>
        private static ForStatement ExpandForEachStatement(ForEachStatement forEachStatement)
        {
            if (forEachStatement != null)
            {
                var collec = forEachStatement.Collection.TypeInference.Declaration as Variable;
                LiteralExpression dimLit = null;
                if (collec.Type is ArrayType)
                {
                    if ((collec.Type as ArrayType).Dimensions.Count == 1)
                    {
                        dimLit = (collec.Type as ArrayType).Dimensions[0] as LiteralExpression;
                    }
                }

                if (dimLit != null)
                {
                    var initializer = new Variable(ScalarType.Int, forEachStatement.Variable.Name.Text + "Iter", new LiteralExpression(0));
                    var vre = new VariableReferenceExpression(initializer.Name);
                    var condition = new BinaryExpression(BinaryOperator.Less, vre, dimLit);
                    var next = new UnaryExpression(UnaryOperator.PreIncrement, vre);
                    ForStatement forStatement = new ForStatement(new DeclarationStatement(initializer), condition, next);
                    var body = new BlockStatement();

                    var variable = forEachStatement.Variable;
                    variable.InitialValue = new IndexerExpression(forEachStatement.Collection, new VariableReferenceExpression(initializer));
                    body.Statements.Add(new DeclarationStatement(variable));

                    if (forEachStatement.Body is BlockStatement)
                        body.Statements.AddRange((forEachStatement.Body as BlockStatement).Statements);
                    else
                        body.Statements.Add(forEachStatement.Body);

                    forStatement.Body = body;

                    return forStatement;
                }

                // TODO: multidimension-array?
                // TODO: unroll?
                // TODO: multiple foreach?
            }
            return null;
        }

        /// <summary>
        /// Replace a MemberReferenceExpression by a VariableReferenceExpression in the AST
        /// </summary>
        /// <param name="memberReferenceExpression">the member reference expression.</param>
        /// <param name="variableReferenceExpression">the variable reference expression.</param>
        /// <param name="parentNode">the parent node.</param>
        private static void ReplaceMemberReferenceExpressionByVariableReferenceExpression(MemberReferenceExpression memberReferenceExpression, VariableReferenceExpression variableReferenceExpression, Node parentNode)
        {
            var replacor = new ParadoxReplaceVisitor(memberReferenceExpression, variableReferenceExpression);
            replacor.Run(parentNode);
        }

        /// <summary>
        /// Compare the semantics
        /// </summary>
        /// <param name="sem0"></param>
        /// <param name="sem1"></param>
        /// <returns></returns>
        private static bool AreSameSemantics(string sem0, string sem1)
        {
            var upperSem0 = sem0.ToUpperInvariant();
            var upperSem1 = sem1.ToUpperInvariant();

            if (upperSem0 == upperSem1)
                return true;

            var i = upperSem0.Length - 1;
            while (i > 0 && char.IsDigit(upperSem0[i]))
                --i;
            string trimSem0 = upperSem0.Substring(0, i + 1);
            int sem0Index = i == upperSem0.Length - 1 ? 0 : Int32.Parse(upperSem0.Substring(i + 1, upperSem0.Length - i - 1));

            i = upperSem1.Length - 1;
            while (i > 0 && char.IsDigit(upperSem1[i]))
                --i;
            string trimSem1 = upperSem1.Substring(0, i + 1);
            int sem1Index = i == upperSem1.Length - 1 ? 0 : Int32.Parse(upperSem1.Substring(i + 1, upperSem1.Length - i - 1));

            return trimSem0 == trimSem1 && sem0Index == sem1Index;
        }

        #endregion
    }
}
