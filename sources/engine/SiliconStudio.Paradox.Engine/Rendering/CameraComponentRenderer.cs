// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;

using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.Engine;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Rendering
{
    /// <summary>
    /// Updates the parameters in the 
    /// </summary>
    public class CameraComponentRenderer : EntityComponentRendererBase
    {
        /// <summary>
        /// Property key to access the current collection of <see cref="CameraComponent"/> from <see cref="RenderContext.Tags"/>.
        /// </summary>
        public static readonly PropertyKey<CameraComponent> Current = new PropertyKey<CameraComponent>("CameraComponentRenderer.CurrentCamera", typeof(CameraComponent));

        public override bool SupportPicking { get { return true; } }

        protected override void PrepareCore(RenderContext context, RenderItemCollection opaqueList, RenderItemCollection transparentList)
        {
            var cameraState = context.GetCurrentCamera();

            if (cameraState == null)
                return;

            UpdateParameters(context, cameraState);
        }

        protected override void DrawCore(RenderContext context, RenderItemCollection renderItems, int fromIndex, int toIndex)
        {
            // Nothing to draw for this camera
        }

        public static void UpdateParameters(RenderContext context, CameraComponent camera)
        {
            if (camera == null) throw new ArgumentNullException("camera");

            // Store the current view/projection matrix in the context
            var viewParameters = context.Parameters;
            viewParameters.Set(TransformationKeys.View, camera.ViewMatrix);
            viewParameters.Set(TransformationKeys.Projection, camera.ProjectionMatrix);
            viewParameters.Set(TransformationKeys.ViewProjection, camera.ViewProjectionMatrix);
            viewParameters.Set(CameraKeys.NearClipPlane, camera.NearClipPlane);
            viewParameters.Set(CameraKeys.FarClipPlane, camera.FarClipPlane);
            viewParameters.Set(CameraKeys.VerticalFieldOfView, camera.VerticalFieldOfView);
            viewParameters.Set(CameraKeys.OrthoSize, camera.OrthographicSize);

            // Setup viewport size
            var currentViewport = context.GraphicsDevice.Viewport;
            viewParameters.Set(CameraKeys.ViewSize, new Vector2(currentViewport.Width, currentViewport.Height));

            // TODO: Review camera aspect ratio
            viewParameters.Set(CameraKeys.AspectRatio, currentViewport.AspectRatio);

            //viewParameters.Set(CameraKeys.FocusDistance, camera.FocusDistance);
        }
    }
}