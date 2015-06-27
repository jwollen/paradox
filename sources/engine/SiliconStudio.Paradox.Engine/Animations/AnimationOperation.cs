// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;

namespace SiliconStudio.Paradox.Animations
{
    /// <summary>
    /// A single animation operation (push or blend).
    /// </summary>
    public struct AnimationOperation
    {
        public AnimationOperationType Type;

        // Blend parameters
        public AnimationBlendOperation BlendOperation;
        public float BlendFactor;

        // Push parameters
        public AnimationClipEvaluator Evaluator;
        public TimeSpan Time;

        /// <summary>
        /// Creates a new animation push operation.
        /// </summary>
        /// <param name="evaluator">The evaluator.</param>
        /// <returns></returns>
        public static AnimationOperation NewPush(AnimationClipEvaluator evaluator)
        {
            return new AnimationOperation { Type = AnimationOperationType.Push, Evaluator = evaluator, Time = TimeSpan.Zero };
        }

        /// <summary>
        /// Creates a new animation push operation.
        /// </summary>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public static AnimationOperation NewPush(AnimationClipEvaluator evaluator, TimeSpan time)
        {
            return new AnimationOperation { Type = AnimationOperationType.Push, Evaluator = evaluator, Time = time };
        }

        /// <summary>
        /// Creates a new animation pop operation.
        /// </summary>
        /// <param name="evaluator">The evaluator.</param>
        /// <param name="time">The time.</param>
        /// <returns></returns>
        public static AnimationOperation NewPop(AnimationClipEvaluator evaluator, TimeSpan time)
        {
            return new AnimationOperation { Type = AnimationOperationType.Pop, Evaluator = evaluator, Time = time };
        }

        /// <summary>
        /// Creates a new animation blend operation.
        /// </summary>
        /// <param name="operation">The blend operation.</param>
        /// <param name="blendFactor">The blend factor.</param>
        /// <returns></returns>
        public static AnimationOperation NewBlend(AnimationBlendOperation operation, float blendFactor)
        {
            return new AnimationOperation { Type = AnimationOperationType.Blend, BlendOperation = operation, BlendFactor = blendFactor };
        }
    }
}