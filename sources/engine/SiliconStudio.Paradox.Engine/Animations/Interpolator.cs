// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Runtime.CompilerServices;

namespace SiliconStudio.Paradox.Animations
{
    /// <summary>
    /// Various helper functions for float, Vector3 and Quaternion interpolations.
    /// </summary>
    public static class Interpolator
    {
        public static class Vector3
        {
            public static void Cubic(ref Core.Mathematics.Vector3 value1, ref Core.Mathematics.Vector3 value2, ref Core.Mathematics.Vector3 value3, ref Core.Mathematics.Vector3 value4, float t, out Core.Mathematics.Vector3 result)
            {
                // http://en.wikipedia.org/wiki/Cubic_Hermite_spline#Interpolation_on_the_unit_interval_without_exact_derivatives
                float t2 = t * t;
                float t3 = t2 * t;

                float factor0 = -t3 + 2.0f * t2 - t;
                float factor1 = 3.0f * t3 - 5.0f * t2 + 2.0f;
                float factor2 = -3.0f * t3 + 4.0f * t2 + t;
                float factor3 = t3 - t2;

                // TODO: Use Vector3(ref,out) functions
                result.X = 0.5f * (value1.X * factor0 + value2.X * factor1 + value3.X * factor2 + value4.X * factor3);
                result.Y = 0.5f * (value1.Y * factor0 + value2.Y * factor1 + value3.Y * factor2 + value4.Y * factor3);
                result.Z = 0.5f * (value1.Z * factor0 + value2.Z * factor1 + value3.Z * factor2 + value4.Z * factor3);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void Linear(ref Core.Mathematics.Vector3 value1, ref Core.Mathematics.Vector3 value2, float t, out Core.Mathematics.Vector3 result)
            {
                Core.Mathematics.Vector3.Lerp(ref value1, ref value2, t, out result);
            }
        }

        public static class Quaternion
        {
            public static void Cubic(ref Core.Mathematics.Quaternion value1, ref Core.Mathematics.Quaternion value2, ref Core.Mathematics.Quaternion value3, ref Core.Mathematics.Quaternion value4, float t, out Core.Mathematics.Quaternion result)
            {
                // TODO Investigate: Squad doesn't seem to do the same thing as implicit derivatives
                throw new NotImplementedException();
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public static void SphericalLinear(ref Core.Mathematics.Quaternion value1, ref Core.Mathematics.Quaternion value2, float t, out Core.Mathematics.Quaternion result)
            {
                Core.Mathematics.Quaternion.Slerp(ref value1, ref value2, t, out result);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Linear(float value1, float value2, float t)
        {
            return (1.0f - t) * value1 + t * value2;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Cubic(float value1, float value2, float value3, float value4, float t)
        {
            // http://en.wikipedia.org/wiki/Cubic_Hermite_spline#Interpolation_on_the_unit_interval_without_exact_derivatives
            float t2 = t * t;
            float t3 = t2 * t;

            float factor0 = -t3 + 2.0f * t2 - t;
            float factor1 = 3.0f * t3 - 5.0f * t2 + 2.0f;
            float factor2 = -3.0f * t3 + 4.0f * t2 + t;
            float factor3 = t3 - t2;

            return 0.5f * (value1 * factor0
                         + value2 * factor1
                         + value3 * factor2
                         + value4 * factor3);
        }
    }
}