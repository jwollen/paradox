﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;

using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Graphics.Internals;

namespace SiliconStudio.Paradox.Graphics
{
    /// <summary>
    /// Extensions for the <see cref="GraphicsDevice"/>
    /// </summary>
    public static class GraphicsDeviceExtensions
    {
        /// <summary>
        /// Draws a fullscreen quad with the specified effect and parameters.
        /// </summary>
        /// <param name="device">The device.</param>
        /// <param name="effect">The effect.</param>
        /// <param name="effectParameterCollectionGroup">The shader parameter updater.</param>
        /// <exception cref="System.ArgumentNullException">effect</exception>
        public static void DrawQuad(this GraphicsDevice device, Effect effect, EffectParameterCollectionGroup effectParameterCollectionGroup)
        {
            if (effect == null) throw new ArgumentNullException("effect");

            // Apply the effect
            effect.Apply(device, effectParameterCollectionGroup, false);

            // Draw a full screen quad
            device.DrawQuad();

            // Unapply
            effect.UnbindResources(device);
        }

        /// <summary>
        /// Resets the <see cref="BlendState"/>, <see cref="DepthStencilState"/> and <see cref="RasterizerState"/> to their 
        /// default values.
        /// </summary>
        /// <param name="device">The device.</param>
        public static void ResetStates(this GraphicsDevice device)
        {
            device.SetBlendState(device.BlendStates.Default);
            device.SetDepthStencilState(device.DepthStencilStates.Default);
            device.SetRasterizerState(device.RasterizerStates.CullBack);
        }

        public static Texture GetSharedWhiteTexture(this GraphicsDevice device)
        {
            return device.GetOrCreateSharedData(GraphicsDeviceSharedDataType.PerDevice, "WhiteTexture", CreateWhiteTexture);
        }

        private static Texture CreateWhiteTexture(GraphicsDevice device)
        {
            const int Size = 2;
            var whiteData = new Color[Size * Size];
            for (int i = 0; i < Size*Size; i++)
                whiteData[i] = Color.White;

            return Texture.New2D(device, Size, Size, PixelFormat.R8G8B8A8_UNorm, whiteData);
        }
    }
}