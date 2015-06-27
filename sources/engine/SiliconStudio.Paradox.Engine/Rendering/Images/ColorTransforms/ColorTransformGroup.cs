﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;

namespace SiliconStudio.Paradox.Rendering.Images
{
    /// <summary>
    /// An effect combining a list of <see cref="ColorTransform"/> sub-effects.
    /// </summary>
    /// <remarks>
    /// This effect and all <see cref="Transforms"/> are collected and compiled into a single shader.
    /// </remarks>
    [DataContract("ColorTransformGroup")]
    [Display("Color Transforms")]
    public class ColorTransformGroup : ImageEffect
    {
        private readonly ParameterCollection transformsParameters;
        private ImageEffectShader transformGroupEffect;
        private readonly Dictionary<ParameterCompositeKey, ParameterKey> compositeKeys;
        private readonly ColorTransformCollection transforms;
        private readonly List<ColorTransform> collectTransforms;
        private readonly List<ColorTransform> enabledTransforms;
        private readonly GammaTransform gammaTransform;
        private ColorTransformContext transformContext;
        private readonly string colorTransformGroupEffectName;

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorTransformGroup"/> class.
        /// </summary>
        public ColorTransformGroup() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorTransformGroup"/> class.
        /// </summary>
        /// <param name="colorTransformGroupEffect">The color transform group effect.</param>
        public ColorTransformGroup(string colorTransformGroupEffect)
            : base(colorTransformGroupEffect)
        {
            compositeKeys = new Dictionary<ParameterCompositeKey, ParameterKey>();
            transforms = new ColorTransformCollection();
            enabledTransforms = new List<ColorTransform>();
            collectTransforms = new List<ColorTransform>();
            transformsParameters = new ParameterCollection();
            gammaTransform = new GammaTransform();
            colorTransformGroupEffectName = colorTransformGroupEffect ?? "ColorTransformGroupEffect";
        }

        /// <inheritdoc/>
        protected override void InitializeCore()
        {
            base.InitializeCore();

            transformGroupEffect = new ImageEffectShader(colorTransformGroupEffectName);
            transformGroupEffect.SharedParameterCollections.Add(Parameters);
            transformGroupEffect.Initialize(Context);

            // we are adding parameter collections after as transform parameters should override previous parameters
            transformGroupEffect.ParameterCollections.Add(transformsParameters);

            this.transformContext = new ColorTransformContext(this);
        }

        /// <summary>
        /// Gets the color transforms.
        /// </summary>
        /// <value>The transforms.</value>
        [DataMember(10)]
        [Display("Transforms", AlwaysExpand = true)]
        [NotNullItems]
        public ColorTransformCollection Transforms
        {
            get
            {
                return transforms;
            }
        }

        /// <summary>
        /// Gets the gamma transform that is applied after all <see cref="Transforms"/>
        /// </summary>
        /// <value>The gamma transform.</value>
        [DataMember(20)]
        public GammaTransform GammaTransform
        {
            get
            {
                return gammaTransform;
            }
        }

        protected override void DrawCore(RenderContext context1)
        {
            var output = GetOutput(0);
            if (output == null)
            {
                return;
            }

            // Collect all transform parameters
            CollectTransformsParameters();

            for (int i = 0; i < transformContext.Inputs.Count; i++)
            {
                transformGroupEffect.SetInput(i, transformContext.Inputs[i]);
            }
            transformGroupEffect.SetOutput(output);
            transformGroupEffect.Draw(context1, name: Name);
        }

        protected virtual void CollectPreTransforms()
        {
        }


        protected virtual void CollectPostTransforms()
        {
            AddTemporaryTransform(gammaTransform);
        }

        protected void AddTemporaryTransform(ColorTransform transform)
        {
            if (transform == null) throw new ArgumentNullException("transform");
            if (transform.Shader == null) throw new ArgumentOutOfRangeException("transform", "Transform parameter must have a Shader not null");
            collectTransforms.Add(transform);
        }

        private void CollectTransforms()
        {
            collectTransforms.Clear();
            CollectPreTransforms();
            collectTransforms.AddRange(transforms);
            CollectPostTransforms();

            // Copy all parameters from ColorTransform to effect parameters
            enabledTransforms.Clear();
            enabledTransforms.AddRange(collectTransforms);
        }

        private void CollectTransformsParameters()
        {
            transformContext.Inputs.Clear();
            for (int i = 0; i < InputCount; i++)
            {
                transformContext.Inputs.Add(GetInput(i));
            }

            // Grab all color transforms
            CollectTransforms();

            transformsParameters.Clear();
            for (int i = 0; i < enabledTransforms.Count; i++)
            {
                var transform = enabledTransforms[i];
                // Always update parameters
                transform.UpdateParameters(transformContext);

                // Copy transform parameters back to the composition with the current index
                var sourceParameters = transform.Parameters;
                foreach (var parameterValue in sourceParameters.Keys)
                {
                    var key = GetComposedKey(parameterValue, i);
                    sourceParameters.CopySharedTo(parameterValue, key, transformsParameters);
                }
            }

            // NOTE: This is very important to reset the transforms here, as pre-caching by DynamicEffectCompiler is done on parameters changes
            // and as we have a list here, modifying a list doesn't trigger a change for the specified key
            // TODO: if the list was the same than previous one, we could optimize this and not setup the value
            Parameters.Set(ColorTransformGroupKeys.Transforms, enabledTransforms);
        }

        private ParameterKey GetComposedKey(ParameterKey key, int transformIndex)
        {
            var compositeKey = new ParameterCompositeKey(key, transformIndex);

            ParameterKey rawCompositeKey;
            if (!compositeKeys.TryGetValue(compositeKey, out rawCompositeKey))
            {
                rawCompositeKey = ParameterKeys.FindByName(string.Format("{0}.Transforms[{1}]", key.Name, transformIndex));
                compositeKeys.Add(compositeKey, rawCompositeKey);
            }
            return rawCompositeKey;
        }

        /// <summary>
        /// An internal key to cache {Key,TransformIndex} => CompositeKey
        /// </summary>
        private struct ParameterCompositeKey : IEquatable<ParameterCompositeKey>
        {
            private readonly ParameterKey key;

            private readonly int index;

            /// <summary>
            /// Initializes a new instance of the <see cref="ParameterCompositeKey"/> struct.
            /// </summary>
            /// <param name="key">The key.</param>
            /// <param name="transformIndex">Index of the transform.</param>
            public ParameterCompositeKey(ParameterKey key, int transformIndex)
            {
                if (key == null) throw new ArgumentNullException("key");
                this.key = key;
                index = transformIndex;
            }

            public bool Equals(ParameterCompositeKey other)
            {
                return key.Equals(other.key) && index == other.index;
            }

            public override bool Equals(object obj)
            {
                if (ReferenceEquals(null, obj)) return false;
                return obj is ParameterCompositeKey && Equals((ParameterCompositeKey)obj);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return (key.GetHashCode() * 397) ^ index;
                }
            }
        }
    }
}