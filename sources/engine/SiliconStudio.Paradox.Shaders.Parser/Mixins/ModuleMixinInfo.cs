// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

using SiliconStudio.Core.Storage;
using SiliconStudio.Paradox.Shaders.Parser.Ast;
using SiliconStudio.Shaders.Ast;
using SiliconStudio.Shaders.Utility;

namespace SiliconStudio.Paradox.Shaders.Parser.Mixins
{
    [DebuggerDisplay("Mixin: {mixinName}")]
    internal class ModuleMixinInfo
    {
        #region Private members

        /// <summary>
        /// The name of the mixin
        /// </summary>
        private string mixinName = "";

        /// <summary>
        /// The ShaderClassType
        /// </summary>
        private ShaderClassType mixinAst;

        #endregion

        #region Public members and properties

        /// <summary>
        /// The ShaderClassSource to load
        /// </summary>
        public ShaderSource ShaderSource { get; set; }

        /// <summary>
        /// The name of the mixin (property)
        /// </summary>
        public string MixinName { get { return mixinName; } }

        /// <summary>
        /// The name of the mixin with its hashed code (property)
        /// </summary>
        public string MixinGenericName;

        /// <summary>
        /// The log stored by this mixin info.
        /// </summary>
        public readonly LoggerResult Log;

        /// <summary>
        /// The ShaderClassType (property)
        /// </summary>
        public ShaderClassType MixinAst
        {
            get
            {
                return mixinAst;
            }
            set
            {
                mixinAst = value;
                mixinName = mixinAst.Name.Text;
            }
        }

        /// <summary>
        /// The ModuleMixin
        /// </summary>
        public ModuleMixin Mixin = new ModuleMixin();

        /// <summary>
        /// A flag stating that the mixin is instanciated
        /// </summary>
        public bool Instanciated { get; set; }

        /// <summary>
        /// a flag checking that the check for replacement has be done
        /// </summary>
        public bool ReplacementChecked = false;

        /// <summary>
        /// the source hash
        /// </summary>
        public ObjectId SourceHash;

        /// <summary>
        /// the SHA1 hash of the source
        /// </summary>
        public ObjectId HashPreprocessSource;

        /// <summary>
        /// The macros used for this mixin
        /// </summary>
        public SiliconStudio.Shaders.Parser.ShaderMacro[] Macros = new SiliconStudio.Shaders.Parser.ShaderMacro[0];

        /// <summary>
        /// the list of all the necessary MixinInfos to compile the shader
        /// </summary>
        public HashSet<ModuleMixinInfo> MinimalContext = new HashSet<ModuleMixinInfo>();
        
        #endregion

        public ModuleMixinInfo()
        {
            Log = new LoggerResult();
            Instanciated = true;
        }

        #region Public methods

        public ModuleMixinInfo Copy(SiliconStudio.Shaders.Parser.ShaderMacro[] macros)
        {
            var mixinInfo = new ModuleMixinInfo();
            mixinInfo.ShaderSource = ShaderSource;
            mixinInfo.MixinAst = MixinAst;
            mixinInfo.MixinGenericName = MixinGenericName;
            mixinInfo.Mixin = Mixin;
            mixinInfo.Instanciated = Instanciated;
            mixinInfo.HashPreprocessSource = HashPreprocessSource;
            mixinInfo.Macros = macros;

            return mixinInfo;
        }

        public bool AreEqual(ShaderSource shaderSource, SiliconStudio.Shaders.Parser.ShaderMacro[] macros)
        {
            return ShaderSource.Equals(shaderSource) && macros.All(macro => Macros.Any(x => x.Name == macro.Name && x.Definition == macro.Definition)) && Macros.All(macro => macros.Any(x => x.Name == macro.Name && x.Definition == macro.Definition));
        }

        #endregion

        #region Static members

        /// <summary>
        /// Cleans the identifiers (i.e. make them use the minimal string)
        /// </summary>
        /// <param name="genList">The list of identifier</param>
        public static void CleanIdentifiers(List<Identifier> genList)
        {
            foreach (var gen in genList.OfType<LiteralIdentifier>())
            {
                gen.Text = gen.Value.Value.ToString();
            }
        }

        #endregion
    }
}