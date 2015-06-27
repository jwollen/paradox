// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Rendering.Lights;
using SiliconStudio.Paradox.Shaders;

namespace SiliconStudio.Paradox.Rendering.Shadows
{
    /// <summary>
    /// Renders a shadow map from a directional light.
    /// </summary>
    public class LightSpotShadowMapRenderer : LightShadowMapRendererBase
    {
        /// <summary>
        /// The various UP vectors to try.
        /// </summary>
        private static readonly Vector3[] VectorUps = { Vector3.UnitZ, Vector3.UnitY, Vector3.UnitX };

        private PoolListStruct<LightSpotShadowMapShaderData> shaderDataPool;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightSpotShadowMapRenderer"/> class.
        /// </summary>
        public LightSpotShadowMapRenderer()
        {
            shaderDataPool = new PoolListStruct<LightSpotShadowMapShaderData>(8, CreateLightSpotShadowMapShaderDAta);
        }
        
        public override void Reset()
        {
            shaderDataPool.Clear();
        }

        public override ILightShadowMapShaderGroupData CreateShaderGroupData(string compositionKey, LightShadowType shadowType, int maxLightCount)
        {
            return new LightSpotShadowMapGroupShaderData(compositionKey, shadowType, maxLightCount);
        }

        public override void Render(RenderContext context, ShadowMapRenderer shadowMapRenderer, LightShadowMapTexture lightShadowMap)
        {
            // TODO: Min and Max distance can be auto-computed from readback from Z buffer
            var shadow = (LightStandardShadowMap)lightShadowMap.Shadow;
            var shadowCamera = shadowMapRenderer.ShadowCamera;

            // Computes the cascade splits
            var lightComponent = lightShadowMap.LightComponent;
            var spotLight = (LightSpot)lightComponent.Type;
            var position = lightComponent.Position;
            var direction = lightComponent.Direction;
            var target = position + spotLight.Range * direction;
            var orthoSize = spotLight.LightRadiusAtTarget;

            // Fake value
            // It will be setup by next loop
            Vector3 side = Vector3.UnitX;
            Vector3 upDirection = Vector3.UnitX;

            // Select best Up vector
            // TODO: User preference?
            foreach (var vectorUp in VectorUps)
            {
                if (Vector3.Dot(direction, vectorUp) < (1.0 - 0.0001))
                {
                    side = Vector3.Normalize(Vector3.Cross(vectorUp, direction));
                    upDirection = Vector3.Normalize(Vector3.Cross(direction, side));
                    break;
                }
            }

            // Get new shader data from pool
            var shaderData = shaderDataPool.Add();
            lightShadowMap.ShaderData = shaderData;
            shaderData.Texture = lightShadowMap.Atlas.Texture;
            shaderData.DepthBias = shadow.BiasParameters.DepthBias;
            shaderData.OffsetScale = shadow.BiasParameters.NormalOffsetScale;

            var graphicsDevice = context.GraphicsDevice;

            graphicsDevice.PushState();

            // Update the shadow camera
            shadowCamera.ViewMatrix = Matrix.LookAtLH(position, target, upDirection); // View;;
            shadowCamera.ProjectionMatrix = Matrix.OrthoOffCenterLH(-orthoSize, orthoSize, -orthoSize, orthoSize, 0.0f, spotLight.Range); // Projection
            shadowCamera.Update();

            var shadowMapRectangle = lightShadowMap.GetRectangle(0);

            var cascadeTextureCoords = new Vector4((float)shadowMapRectangle.Left / lightShadowMap.Atlas.Width,
                (float)shadowMapRectangle.Top / lightShadowMap.Atlas.Height,
                (float)shadowMapRectangle.Right / lightShadowMap.Atlas.Width,
                (float)shadowMapRectangle.Bottom / lightShadowMap.Atlas.Height);

            //// Add border (avoid using edges due to bilinear filtering and blur)
            //var borderSizeU = VsmBlurSize / lightShadowMap.Atlas.Width;
            //var borderSizeV = VsmBlurSize / lightShadowMap.Atlas.Height;
            //cascadeTextureCoords.X += borderSizeU;
            //cascadeTextureCoords.Y += borderSizeV;
            //cascadeTextureCoords.Z -= borderSizeU;
            //cascadeTextureCoords.W -= borderSizeV;

            float leftX = (float)lightShadowMap.Size / lightShadowMap.Atlas.Width * 0.5f;
            float leftY = (float)lightShadowMap.Size / lightShadowMap.Atlas.Height * 0.5f;
            float centerX = 0.5f * (cascadeTextureCoords.X + cascadeTextureCoords.Z);
            float centerY = 0.5f * (cascadeTextureCoords.Y + cascadeTextureCoords.W);

            // Compute receiver view proj matrix
            Matrix adjustmentMatrix = Matrix.Scaling(leftX, -leftY, 1.0f) * Matrix.Translation(centerX, centerY, 0.0f);
            // Calculate View Proj matrix from World space to Cascade space
            Matrix.Multiply(ref shadowCamera.ViewProjectionMatrix, ref adjustmentMatrix, out shaderData.WorldToShadowCascadeUV);

            // Render to the atlas
            lightShadowMap.Atlas.RenderFrame.Activate(context);
            graphicsDevice.SetViewport(new Viewport(shadowMapRectangle.X, shadowMapRectangle.Y, shadowMapRectangle.Width, shadowMapRectangle.Height));

            // Render the scene for this cascade
            shadowMapRenderer.RenderCasters(context, lightShadowMap.LightComponent.CullingMask);
                //// Copy texture coords with border
                //cascades[cascadeLevel].CascadeLevels.CascadeTextureCoordsBorder = cascadeTextureCoords;
         
            graphicsDevice.PopState();
        }

        private class LightSpotShadowMapShaderData : ILightShadowMapShaderData
        {
            public Texture Texture;

            public float DepthBias;

            public float OffsetScale;

            public Matrix WorldToShadowCascadeUV;
        }

        private class LightSpotShadowMapGroupShaderData : ILightShadowMapShaderGroupData
        {
            private const string ShaderName = "ShadowMapReceiverSpot";

            private readonly LightShadowType shadowType;

            private readonly Matrix[] worldToShadowCascadeUV;

            private readonly float[] depthBiases;

            private readonly float[] offsetScales;

            private Texture shadowMapTexture;

            private Vector2 shadowMapTextureSize;

            private Vector2 shadowMapTextureTexelSize;

            private readonly ShaderMixinSource shadowShader;

            private readonly ParameterKey<Texture> shadowMapTextureKey;

            private readonly ParameterKey<Matrix[]> worldToShadowCascadeUVsKey;

            private readonly ParameterKey<float[]> depthBiasesKey;

            private readonly ParameterKey<float[]> offsetScalesKey;

            private readonly ParameterKey<Vector2> shadowMapTextureSizeKey;

            private readonly ParameterKey<Vector2> shadowMapTextureTexelSizeKey;

            /// <summary>
            /// Initializes a new instance of the <see cref="LightSpotShadowMapGroupShaderData" /> class.
            /// </summary>
            /// <param name="compositionKey">The composition key.</param>
            /// <param name="shadowType">Type of the shadow.</param>
            /// <param name="lightCountMax">The light count maximum.</param>
            public LightSpotShadowMapGroupShaderData(string compositionKey, LightShadowType shadowType, int lightCountMax)
            {
                this.shadowType = shadowType;
                worldToShadowCascadeUV = new Matrix[lightCountMax];
                depthBiases = new float[lightCountMax];
                offsetScales = new float[lightCountMax];

                var mixin = new ShaderMixinSource();
                mixin.Mixins.Add(new ShaderClassSource(ShaderName,lightCountMax, (this.shadowType & LightShadowType.Debug) != 0));
                // TODO: Temporary passing filter here

                switch (shadowType & LightShadowType.FilterMask)
                {
                    case LightShadowType.PCF3x3:
                        mixin.Mixins.Add(new ShaderClassSource("ShadowMapFilterPcf", 3));
                        break;
                    case LightShadowType.PCF5x5:
                        mixin.Mixins.Add(new ShaderClassSource("ShadowMapFilterPcf", 5));
                        break;
                    case LightShadowType.PCF7x7:
                        mixin.Mixins.Add(new ShaderClassSource("ShadowMapFilterPcf", 7));
                        break;
                    default:
                        mixin.Mixins.Add(new ShaderClassSource("ShadowMapFilterDefault"));
                        break;
                }

                shadowShader = mixin;
                shadowMapTextureKey = ShadowMapKeys.Texture.ComposeWith(compositionKey);
                shadowMapTextureSizeKey = ShadowMapKeys.TextureSize.ComposeWith(compositionKey);
                shadowMapTextureTexelSizeKey = ShadowMapKeys.TextureTexelSize.ComposeWith(compositionKey);
                worldToShadowCascadeUVsKey = ShadowMapReceiverBaseKeys.WorldToShadowCascadeUV.ComposeWith(compositionKey);
                depthBiasesKey = ShadowMapReceiverBaseKeys.DepthBiases.ComposeWith(compositionKey);
                offsetScalesKey = ShadowMapReceiverBaseKeys.OffsetScales.ComposeWith(compositionKey);
            }

            public void ApplyShader(ShaderMixinSource mixin)
            {
                mixin.CloneFrom(shadowShader);
            }

            public void SetShadowMapShaderData(int index, ILightShadowMapShaderData shaderData)
            {
                var singleLightData = (LightSpotShadowMapShaderData)shaderData;
                worldToShadowCascadeUV[index] = singleLightData.WorldToShadowCascadeUV;

                depthBiases[index] = singleLightData.DepthBias;
                offsetScales[index] = singleLightData.OffsetScale;

                // TODO: should be setup just once at creation time
                if (index == 0)
                {
                    shadowMapTexture = singleLightData.Texture;
                    if (shadowMapTexture != null)
                    {
                        shadowMapTextureSize = new Vector2(shadowMapTexture.Width, shadowMapTexture.Height);
                        shadowMapTextureTexelSize = 1.0f / shadowMapTextureSize;
                    }
                }
            }

            public void ApplyParameters(ParameterCollection parameters)
            {
                parameters.Set(shadowMapTextureKey, shadowMapTexture);
                parameters.Set(shadowMapTextureSizeKey, shadowMapTextureSize);
                parameters.Set(shadowMapTextureTexelSizeKey, shadowMapTextureTexelSize);
                parameters.Set(worldToShadowCascadeUVsKey, worldToShadowCascadeUV);
                parameters.Set(depthBiasesKey, depthBiases);
                parameters.Set(offsetScalesKey, offsetScales);
            }
        }

        private static LightSpotShadowMapShaderData CreateLightSpotShadowMapShaderDAta()
        {
            return new LightSpotShadowMapShaderData();
        }
    }
}