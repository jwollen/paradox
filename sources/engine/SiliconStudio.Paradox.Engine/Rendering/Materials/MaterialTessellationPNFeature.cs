﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Rendering.Materials;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Materials
{
    /// <summary>
    /// Material for Point-Normal tessellation.
    /// </summary>
    [DataContract("MaterialTessellationPNFeature")]
    [Display("Point Normal Tessellation")]
    public class MaterialTessellationPNFeature : MaterialTessellationBaseFeature
    {
        public override void Visit(MaterialGeneratorContext context)
        {
            base.Visit(context);

            if (HasAlreadyTessellationFeature) 
                return;

            // set the tessellation method used enumeration
            context.Material.TessellationMethod |= ParadoxTessellationMethod.PointNormal;

            // create and affect the shader source
            var tessellationShader = new ShaderMixinSource();
            tessellationShader.Mixins.Add(new ShaderClassSource("TessellationPN"));
            if (AdjacentEdgeAverage)
                tessellationShader.Mixins.Add(new ShaderClassSource("TessellationAE4", "PositionWS"));

            context.Parameters.Set(MaterialKeys.TessellationShader, tessellationShader);
        }
    }
}