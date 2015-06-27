// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using SiliconStudio.Core.Collections;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Rendering.Shadows;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    public abstract class LightShaderGroup
    {
        protected LightShaderGroup(ShaderSource mixin, string compositionName, ILightShadowMapShaderGroupData shadowGroup)
        {
            if (mixin == null) throw new ArgumentNullException("mixin");
            if (compositionName == null) throw new ArgumentNullException("compositionName");
            CompositionName = compositionName;
            ShaderSource = mixin;
            ShadowGroup = shadowGroup;
        }

        public ShaderSource ShaderSource { get; private set; }

        public string CompositionName { get; private set; }

        public ILightShadowMapShaderGroupData ShadowGroup { get; private set; }

        public abstract void Reset();

        public abstract LightShaderGroupData CreateGroupData();
    }

    public abstract class LightShaderGroupAndDataPool<T> : LightShaderGroup where T : LightShaderGroupData
    {
        private PoolListStruct<T> dataPool;

        protected LightShaderGroupAndDataPool(ShaderSource mixin, string compositionName, ILightShadowMapShaderGroupData shadowGroupData)
            : base(mixin, compositionName, shadowGroupData)
        {
            dataPool = new PoolListStruct<T>(4, CreateData);
        }

        protected abstract T CreateData();

        public override void Reset()
        {
            foreach (var data in dataPool)
            {
                data.Reset();
            }

            dataPool.Clear();
        }

        public override LightShaderGroupData CreateGroupData()
        {
            var data = dataPool.Add();
            return data;
        }
    }

    public abstract class LightShaderGroupData
    {
        protected LightShaderGroupData(ILightShadowMapShaderGroupData shadowGroup)
        {
            ShadowGroup = shadowGroup;
        }

        public int Count { get; private set; }

        public ILightShadowMapShaderGroupData ShadowGroup { get; private set; }

        public void Reset()
        {
            Count = 0;
        }

        public void AddLight(LightComponent light, LightShadowMapTexture shadowMapTexture)
        {
            AddLightInternal(light);
            if (ShadowGroup != null)
            {
                ShadowGroup.SetShadowMapShaderData(Count, shadowMapTexture.ShaderData);
            }
            Count++;
        }

        public void ApplyParameters(ParameterCollection parameters)
        {
            ApplyParametersInternal(parameters);
            if (ShadowGroup != null)
            {
                ShadowGroup.ApplyParameters(parameters);
            }
        }

        protected abstract void AddLightInternal(LightComponent light);

        protected abstract void ApplyParametersInternal(ParameterCollection parameters);
    }
}