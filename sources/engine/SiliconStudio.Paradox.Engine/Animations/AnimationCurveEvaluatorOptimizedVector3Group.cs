// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using SiliconStudio.Core.Mathematics;

namespace SiliconStudio.Paradox.Animations
{
    public class AnimationCurveEvaluatorOptimizedVector3Group : AnimationCurveEvaluatorOptimizedGroup<Vector3>
    {
        protected unsafe override void ProcessChannel(ref Channel channel, CompressedTimeSpan currentTime, IntPtr location, float factor)
        {
            if (channel.InterpolationType == AnimationCurveInterpolationType.Cubic)
            {
                Interpolator.Vector3.Cubic(
                    ref channel.ValuePrev.Value,
                    ref channel.ValueStart.Value,
                    ref channel.ValueEnd.Value,
                    ref channel.ValueNext.Value,
                    factor,
                    out *(Vector3*)(location + channel.Offset));
            }
            else if (channel.InterpolationType == AnimationCurveInterpolationType.Linear)
            {
                Interpolator.Vector3.Linear(
                    ref channel.ValueStart.Value,
                    ref channel.ValueEnd.Value,
                    factor,
                    out *(Vector3*)(location + channel.Offset));
            }
            else
            {
                throw new NotImplementedException();
            }
        }
    }
}