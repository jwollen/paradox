// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Rendering.Shadows;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    public class LightPointGroupRenderer : LightGroupRendererBase
    {
        private const int StaticLightMaxCount = 8;

        private static readonly ShaderClassSource DynamicDirectionalGroupShaderSource = new ShaderClassSource("LightPointGroup", StaticLightMaxCount);

        public LightPointGroupRenderer()
        {
            LightMaxCount = StaticLightMaxCount;
            CanHaveShadows = true;
        }

        public override void Initialize(RenderContext context)
        {
            var isLowProfile = context.GraphicsDevice.Features.Profile < GraphicsProfile.Level_10_0;
            LightMaxCount = isLowProfile ? 2 : StaticLightMaxCount;
            AllocateLightMaxCount = !isLowProfile;
        }

        public override LightShaderGroup CreateLightShaderGroup(string compositionName, int lightMaxCount, ILightShadowMapShaderGroupData shadowGroup)
        {
            var mixin = new ShaderMixinSource();
            if (AllocateLightMaxCount)
            {
                mixin.Mixins.Add(DynamicDirectionalGroupShaderSource);
            }
            else
            {
                mixin.Mixins.Add(new ShaderClassSource("LightPointGroup", lightMaxCount));
                mixin.Mixins.Add(new ShaderClassSource("DirectLightGroupFixed", lightMaxCount));
            }

            if (shadowGroup != null)
            {
                shadowGroup.ApplyShader(mixin);
            }

            return new SpotLightShaderGroup(mixin, compositionName, shadowGroup);
        }

        class SpotLightShaderGroup : LightShaderGroupAndDataPool<SpotLightShaderGroupData>
        {
            internal readonly ParameterKey<int> CountKey;
            internal readonly ParameterKey<Vector3[]> PositionsKey;
            internal readonly ParameterKey<float[]> InvSquareRadiusKey;
            internal readonly ParameterKey<Color3[]> ColorsKey;

            public SpotLightShaderGroup(ShaderMixinSource mixin, string compositionName, ILightShadowMapShaderGroupData shadowGroupData)
                : base(mixin, compositionName, shadowGroupData)
            {
                CountKey = DirectLightGroupKeys.LightCount.ComposeWith(compositionName);
                PositionsKey = LightPointGroupKeys.LightPositionWS.ComposeWith(compositionName);
                InvSquareRadiusKey = LightPointGroupKeys.LightInvSquareRadius.ComposeWith(compositionName); 
                ColorsKey = LightPointGroupKeys.LightColor.ComposeWith(compositionName);
            }

            protected override SpotLightShaderGroupData CreateData()
            {
                return new SpotLightShaderGroupData(this, ShadowGroup);
            }
        }

        class SpotLightShaderGroupData : LightShaderGroupData
        {
            private readonly ParameterKey<int> countKey;
            private readonly ParameterKey<Color3[]> colorsKey;
            private readonly ParameterKey<Vector3[]> positionsKey;
            private readonly ParameterKey<float[]> invSquareRadiusKey;
            private readonly Vector3[] lightDirections;
            private readonly Vector3[] lightPositions;
            private readonly float[] invSquareRadius;
            private readonly Color3[] lightColors;

            public SpotLightShaderGroupData(SpotLightShaderGroup group, ILightShadowMapShaderGroupData shadowGroupData)
                : base(shadowGroupData)
            {
                countKey = group.CountKey;
                colorsKey = group.ColorsKey;
                positionsKey = group.PositionsKey;
                invSquareRadiusKey = group.InvSquareRadiusKey;

                lightDirections = new Vector3[StaticLightMaxCount];
                lightColors = new Color3[StaticLightMaxCount];
                lightPositions = new Vector3[StaticLightMaxCount];
                invSquareRadius = new float[StaticLightMaxCount];
            }

            protected override void AddLightInternal(LightComponent light)
            {
                var pointLight = (LightPoint)light.Type;
                lightDirections[Count] = light.Direction;
                lightColors[Count] = light.Color;
                lightPositions[Count] = light.Position;
                invSquareRadius[Count] = pointLight.InvSquareRadius;
            }

            protected override void ApplyParametersInternal(ParameterCollection parameters)
            {
                parameters.Set(countKey, Count);
                parameters.Set(colorsKey, lightColors);
                parameters.Set(positionsKey, lightPositions);
                parameters.Set(invSquareRadiusKey, invSquareRadius);
            }
        }
    }
}