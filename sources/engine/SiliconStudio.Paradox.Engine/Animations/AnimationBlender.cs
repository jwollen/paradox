// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Animations
{
    /// <summary>
    /// Performs animation blending.
    /// For now, all AnimationClip must target the same skeleton.
    /// </summary>
    public sealed class AnimationBlender
    {
        private static Stack<AnimationClipEvaluator> evaluatorPool = new Stack<AnimationClipEvaluator>(); 

        // Pool of available objects for intermediate results.
        private static Stack<AnimationClipResult> availableResultsPool = new Stack<AnimationClipResult>();

        private Stack<AnimationClipResult> animationStack = new Stack<AnimationClipResult>();

        private HashSet<AnimationClipEvaluator> evaluators = new HashSet<AnimationClipEvaluator>();
        private HashSet<AnimationClip> clips = new HashSet<AnimationClip>();
        private Dictionary<string, Channel> channelsByName = new Dictionary<string, Channel>();
        private List<Channel> channels = new List<Channel>();
        private int structureSize;

        public AnimationClipEvaluator CreateEvaluator(AnimationClip clip)
        {
            // Check if this clip has already been used
            if (clips.Add(clip))
            {
                // If new clip, let's scan its channel to add new ones.
                foreach (var curve in clip.Channels)
                {
                    Channel channel;
                    if (channelsByName.TryGetValue(curve.Key, out channel))
                    {
                        // TODO: Check if channel matches
                    }
                    else
                    {
                        // New channel, add it to every evaluator

                        // Find blend type
                        var blendType = BlendType.None;
                        var elementType = curve.Value.ElementType;

                        if (elementType == typeof(Quaternion))
                        {
                            blendType = BlendType.Quaternion;
                        }
                        else if (elementType == typeof(float))
                        {
                            blendType = BlendType.Float1;
                        }
                        else if (elementType == typeof(Vector2))
                        {
                            blendType = BlendType.Float2;
                        }
                        else if (elementType == typeof(Vector3))
                        {
                            blendType = BlendType.Float3;
                        }
                        else if (elementType == typeof(Vector4))
                        {
                            blendType = BlendType.Float4;
                        }

                        // Create channel structure
                        channel.BlendType = blendType;
                        channel.Offset = structureSize;
                        channel.PropertyName = curve.Key;

                        // TODO: Remove this totally hardcoded property name parsing!
                        channel.NodeName = curve.Value.NodeName;
                        channel.Type = curve.Value.Type;

                        channel.Size = curve.Value.ElementSize;

                        // Add channel
                        channelsByName.Add(channel.PropertyName, channel);
                        channels.Add(channel);

                        // Update new structure size
                        // We also reserve space for a float that will specify channel existence and factor in case of subtree blending
                        structureSize += sizeof(float) + channel.Size;

                        // Add new channel update info to every evaluator
                        // TODO: Maybe it's better lazily done? (avoid need to store list of all evaluators)
                        foreach (var currentEvaluator in evaluators)
                        {
                            currentEvaluator.AddChannel(ref channel);
                        }
                    }
                }
            }

            // Update results to fit the new data size
            lock (availableResultsPool)
            {
                foreach (var result in availableResultsPool)
                {
                    if (result.DataSize < structureSize)
                    {
                        result.DataSize = structureSize;
                        result.Data = new byte[structureSize];
                    }
                }
            }

            // Create evaluator and store it in list of instantiated evaluators
            AnimationClipEvaluator evaluator;
            lock (evaluatorPool)
            {
                if (evaluatorPool.Count > 0)
                {
                    evaluator = evaluatorPool.Pop();
                }
                else
                {
                    evaluator = new AnimationClipEvaluator();
                }
            }
            
            evaluator.Initialize(clip, channels);
            evaluators.Add(evaluator);

            return evaluator;
        }

        public void ReleaseEvaluator(AnimationClipEvaluator evaluator)
        {
            lock (evaluatorPool)
            {
                evaluators.Remove(evaluator);
                evaluator.Cleanup();
                evaluatorPool.Push(evaluator);
            }
        }

        public static unsafe void Blend(AnimationBlendOperation blendOperation, float blendFactor, AnimationClipResult sourceLeft, AnimationClipResult sourceRight, AnimationClipResult result)
        {
            fixed (byte* sourceLeftDataStart = sourceLeft.Data)
            fixed (byte* sourceRightDataStart = sourceRight.Data)
            fixed (byte* resultDataStart = result.Data)
            {
                foreach (var channel in sourceLeft.Channels)
                {
                    int offset = channel.Offset;
                    var sourceLeftData = (float*)(sourceLeftDataStart + offset);
                    var sourceRightData = (float*)(sourceRightDataStart + offset);
                    var resultData = (float*)(resultDataStart + offset);

                    float factorLeft = *sourceLeftData++;
                    float factorRight = *sourceRightData++;

                    // Ignore this channel
                    if (factorLeft == 0.0f && factorRight == 0.0f)
                    {
                        *resultData++ = 0.0f;
                        continue;
                    }

                    // Use left value
                    if (factorLeft > 0.0f && factorRight == 0.0f)
                    {
                        *resultData++ = 1.0f;
                        Utilities.CopyMemory((IntPtr)resultData, (IntPtr)sourceLeftData, channel.Size);
                        continue;
                    }

                    // Use right value
                    if (factorRight > 0.0f && factorLeft == 0.0f)
                    {
                        *resultData++ = 1.0f;
                        Utilities.CopyMemory((IntPtr)resultData, (IntPtr)sourceRightData, channel.Size);
                        continue;
                    }

                    // Blending
                    *resultData++ = 1.0f;

                    switch (blendOperation)
                    {
                        case AnimationBlendOperation.LinearBlend:
                            // Perform linear blending
                            // It will blend between left (0.0) and right (1.0)
                            switch (channel.BlendType)
                            {
                                case BlendType.None:
                                    Utilities.CopyMemory((IntPtr)resultData, (IntPtr)sourceLeftData, channel.Size);
                                    break;
                                case BlendType.Float2:
                                    Vector2.Lerp(ref *(Vector2*)sourceLeftData, ref *(Vector2*)sourceRightData, blendFactor, out *(Vector2*)resultData);
                                    break;
                                case BlendType.Float3:
                                    Vector3.Lerp(ref *(Vector3*)sourceLeftData, ref *(Vector3*)sourceRightData, blendFactor, out *(Vector3*)resultData);
                                    break;
                                case BlendType.Quaternion:
                                    Quaternion.Slerp(ref *(Quaternion*)sourceLeftData, ref *(Quaternion*)sourceRightData, blendFactor, out *(Quaternion*)resultData);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case AnimationBlendOperation.Add:
                            // Perform additive blending
                            // It will blend between left (0.0) and left + right (1.0).
                            switch (channel.BlendType)
                            {
                                case BlendType.None:
                                    Utilities.CopyMemory((IntPtr)resultData, (IntPtr)sourceLeftData, channel.Size);
                                    break;
                                case BlendType.Float2:
                                    Vector2 rightValue2;
                                    Vector2.Add(ref *(Vector2*)sourceLeftData, ref *(Vector2*)sourceRightData, out rightValue2);
                                    Vector2.Lerp(ref *(Vector2*)sourceLeftData, ref rightValue2, blendFactor, out *(Vector2*)resultData);
                                    break;
                                case BlendType.Float3:
                                    Vector3 rightValue3;
                                    Vector3.Add(ref *(Vector3*)sourceLeftData, ref *(Vector3*)sourceRightData, out rightValue3);
                                    Vector3.Lerp(ref *(Vector3*)sourceLeftData, ref rightValue3, blendFactor, out *(Vector3*)resultData);
                                    break;
                                case BlendType.Quaternion:
                                    Quaternion rightValueQ;
                                    Quaternion.Multiply(ref *(Quaternion*)sourceLeftData, ref *(Quaternion*)sourceRightData, out rightValueQ);
                                    Quaternion.Normalize(ref rightValueQ, out rightValueQ);
                                    Quaternion.Slerp(ref *(Quaternion*)sourceLeftData, ref rightValueQ, blendFactor, out *(Quaternion*)resultData);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        case AnimationBlendOperation.Subtract:
                            // Perform subtractive blending
                            // It will blend between left (0.0) and left - right (1.0).
                            switch (channel.BlendType)
                            {
                                case BlendType.None:
                                    Utilities.CopyMemory((IntPtr)resultData, (IntPtr)sourceLeftData, channel.Size);
                                    break;
                                case BlendType.Float2:
                                    Vector2 rightValue2;
                                    Vector2.Subtract(ref *(Vector2*)sourceLeftData, ref *(Vector2*)sourceRightData, out rightValue2);
                                    Vector2.Lerp(ref *(Vector2*)sourceLeftData, ref rightValue2, blendFactor, out *(Vector2*)resultData);
                                    break;
                                case BlendType.Float3:
                                    Vector3 rightValue3;
                                    Vector3.Subtract(ref *(Vector3*)sourceLeftData, ref *(Vector3*)sourceRightData, out rightValue3);
                                    Vector3.Lerp(ref *(Vector3*)sourceLeftData, ref rightValue3, blendFactor, out *(Vector3*)resultData);
                                    break;
                                case BlendType.Quaternion:
                                    Quaternion rightValueQ;
                                    // blend between left (0.0) and left * conjugate(right) (1.0)
                                    Quaternion.Invert(ref *(Quaternion*)sourceRightData, out rightValueQ);
                                    Quaternion.Multiply(ref rightValueQ, ref *(Quaternion*)sourceLeftData, out rightValueQ);
                                    Quaternion.Normalize(ref rightValueQ, out rightValueQ);
                                    //throw new NotImplementedException();
                                    Quaternion.Slerp(ref *(Quaternion*)sourceLeftData, ref rightValueQ, blendFactor, out *(Quaternion*)resultData);
                                    break;
                                default:
                                    throw new ArgumentOutOfRangeException();
                            }
                            break;
                        default:
                            throw new ArgumentOutOfRangeException("blendOperation");
                    }
                }
            }
        }

        /// <summary>
        /// Computes the specified animation operations.
        /// </summary>
        /// <param name="animationOperations">The animation operations to perform.</param>
        /// <param name="result">The optional result (if not null, it expects the final stack to end up with this element).</param>
        public void Compute(FastList<AnimationOperation> animationOperations, ref AnimationClipResult result)
        {
            // Clear animation stack
            animationStack.Clear();

            // Apply first operation (should be a push), directly into result (considered first item in the stack)
            var animationOperation0 = animationOperations.Items[0];

            if (animationOperation0.Type != AnimationOperationType.Push)
                throw new InvalidOperationException("First operation should be a push");

            // TODO: Either result is null (should have a Pop operation) or result is non null (stack end up being size 1)
            var hasResult = result != null;
            if (hasResult)
            {
                // Ensure there is enough size
                // TODO: Force result allocation to happen on user side?
                if (result.DataSize < structureSize)
                {
                    result.DataSize = structureSize;
                    result.Data = new byte[structureSize];
                }

                result.Channels = channels;
            }
            else
            {
                result = AllocateIntermediateResult();
            }

            animationOperation0.Evaluator.Compute((CompressedTimeSpan)animationOperation0.Time, result);

            animationStack.Push(result);

            for (int index = 1; index < animationOperations.Count; index++)
            {
                var animationOperation = animationOperations.Items[index];

                ApplyAnimationOperation(ref animationOperation);
            }

            if (hasResult && (animationStack.Count != 1 || animationStack.Pop() != result))
            {
                throw new InvalidOperationException("Stack should end up with result.");
            }
        }

        private void ApplyAnimationOperation(ref AnimationOperation animationOperation)
        {
            switch (animationOperation.Type)
            {
                case AnimationOperationType.Blend:
                    {
                        // Blend stack[last - 1] and stack[last] into stack[last - 1], then pop stack[last]
                        var op2 = animationStack.Pop();
                        var op1 = animationStack.Peek();
                        Blend(animationOperation.BlendOperation, animationOperation.BlendFactor, op1, op2, op1);
                        FreeIntermediateResult(op2);
                    }
                    break;
                case AnimationOperationType.Push:
                    {
                        var op = AllocateIntermediateResult();
                        animationOperation.Evaluator.Compute((CompressedTimeSpan)animationOperation.Time, op);
                        animationStack.Push(op);
                    }
                    break;
                case AnimationOperationType.Pop:
                    {
                        var op = animationStack.Pop();
                        animationOperation.Evaluator.AddCurveValues((CompressedTimeSpan)animationOperation.Time, op);
                    }
                    break;
            }
        }

        private AnimationClipResult AllocateIntermediateResult()
        {
            lock (availableResultsPool)
            {
                if (availableResultsPool.Count > 0)
                {
                    var result = availableResultsPool.Pop();

                    if (result.DataSize < structureSize)
                    {
                        result.DataSize = structureSize;
                        result.Data = new byte[structureSize];
                    }

                    result.Channels = channels;

                    return result;
                }
            }

            // Nothing available, create new one.
            return new AnimationClipResult
                {
                    DataSize = structureSize,
                    Data = new byte[structureSize],
                    Channels = channels,
                };
        }

        internal void FreeIntermediateResult(AnimationClipResult result)
        {
            // Returns it to pool of available intermediate results
            lock (availableResultsPool)
            {
                result.Channels = null;
                availableResultsPool.Push(result);
            }
        }

        [DataContract]
        public enum BlendType
        {
            None,
            Float1,
            Float2,
            Float3,
            Float4,
            Quaternion,
        }

        [DataContract]
        public struct Channel
        {
            public string PropertyName;
            public string NodeName;
            public MeshAnimationUpdater.ChannelType Type;
            public int Offset;
            public int Size;
            public BlendType BlendType;

            public override string ToString()
            {
                return PropertyName;
            }
        }
    }
}