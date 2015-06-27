﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;

using SiliconStudio.Assets;
using SiliconStudio.Core;
using SiliconStudio.Core.IO;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Paradox.Assets.Model
{
    [DataContract("AdditiveAnimation")]
    [ObjectFactory(typeof(AdditiveAnimationFactory))]
    [Display(175, "Additive Animation", "An additive skeletal animation")]
    public class AdditiveAnimationAsset : AnimationAsset
    {
        /// <summary>
        /// Gets or sets the path to the base source animation model when using additive animation.
        /// </summary>
        /// <value>The source.</value>
        [DataMember(30)]
        public UFile BaseSource { get; set; }

        [DataMember(40)]
        public AdditiveAnimationBaseMode Mode { get; set; }

        private class AdditiveAnimationFactory : IObjectFactory
        {
            public object New(Type type)
            {
                return new AdditiveAnimationAsset();
            }
        }
    }

    [DataContract]
    public enum AdditiveAnimationBaseMode
    {
        // TODO: Add support for reference pose (need to add the concept to AnimationClip?)
        //ReferencePose = 0,

        /// <summary>
        /// Uses first frame of animation.
        /// </summary>
        FirstFrame = 1,

        /// <summary>
        /// Uses animation.
        /// </summary>
        Animation = 2,
    }
}