﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.


using SiliconStudio.Core;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Paradox.UI.Controls;

namespace SiliconStudio.Paradox.UI.Renderers
{
    /// <summary>
    /// The default renderer for <see cref="Border"/>.
    /// </summary>
    public class DefaultBorderRenderer : ElementRenderer
    {
        public DefaultBorderRenderer(IServiceRegistry services)
            : base(services)
        {
        }

        public override void RenderColor(UIElement element, UIRenderingContext context)
        {
            base.RenderColor(element, context);

            Vector3 offsets;
            Vector3 borderSize;

            var border = (Border)element;

            var borderThickness = border.BorderThickness;
            var elementHalfBorders = borderThickness / 2;
            var elementSize = element.RenderSizeInternal;
            var elementHalfSize = elementSize / 2;

            // left/front
            offsets = new Vector3(-elementHalfSize.X + elementHalfBorders.Left, 0, -elementHalfSize.Z + elementHalfBorders.Front);
            borderSize = new Vector3(borderThickness.Left, elementSize.Y, borderThickness.Front);
            DrawBorder(border, ref offsets, ref borderSize, context);
            
            // right/front
            offsets = new Vector3(elementHalfSize.X - elementHalfBorders.Right, 0, -elementHalfSize.Z + elementHalfBorders.Front);
            borderSize = new Vector3(borderThickness.Right, elementSize.Y, borderThickness.Front);
            DrawBorder(border, ref offsets, ref borderSize, context);
            
            // top/front
            offsets = new Vector3(0, -elementHalfSize.Y + elementHalfBorders.Top, -elementHalfSize.Z + elementHalfBorders.Front);
            borderSize = new Vector3(elementSize.X, borderThickness.Top, borderThickness.Front);
            DrawBorder(border, ref offsets, ref borderSize, context);
            
            // bottom/front
            offsets = new Vector3(0, elementHalfSize.Y - elementHalfBorders.Bottom, -elementHalfSize.Z + elementHalfBorders.Front);
            borderSize = new Vector3(elementSize.X, borderThickness.Bottom, borderThickness.Back);
            DrawBorder(border, ref offsets, ref borderSize, context);

            // if the element is 3D draw the extra borders
            if (element.ActualDepth > MathUtil.ZeroTolerance)
            {
                // left/back
                offsets = new Vector3(-elementHalfSize.X + elementHalfBorders.Left, 0, elementHalfSize.Z - elementHalfBorders.Back);
                borderSize = new Vector3(borderThickness.Left, elementSize.Y, borderThickness.Back);
                DrawBorder(border, ref offsets, ref borderSize, context);
                
                // right/back
                offsets = new Vector3(elementHalfSize.X - elementHalfBorders.Right, 0, elementHalfSize.Z - elementHalfBorders.Back);
                borderSize = new Vector3(borderThickness.Right, elementSize.Y, borderThickness.Back);
                DrawBorder(border, ref offsets, ref borderSize, context);
                
                // top/back
                offsets = new Vector3(0, -elementHalfSize.Y + elementHalfBorders.Top, elementHalfSize.Z - elementHalfBorders.Back);
                borderSize = new Vector3(elementSize.X, borderThickness.Top, borderThickness.Back);
                DrawBorder(border, ref offsets, ref borderSize, context);
                
                // bottom/back
                offsets = new Vector3(0, elementHalfSize.Y - elementHalfBorders.Bottom, elementHalfSize.Z - elementHalfBorders.Back);
                borderSize = new Vector3(elementSize.X, borderThickness.Bottom, borderThickness.Back);
                DrawBorder(border, ref offsets, ref borderSize, context);
                
                // left/top
                offsets = new Vector3(-elementHalfSize.X + elementHalfBorders.Left, -elementHalfSize.Y + elementHalfBorders.Top, 0);
                borderSize = new Vector3(borderThickness.Left, borderThickness.Top, elementSize.Z);
                DrawBorder(border, ref offsets, ref borderSize, context);
                
                // right/top
                offsets = new Vector3(elementHalfSize.X - elementHalfBorders.Right, -elementHalfSize.Y + elementHalfBorders.Top, 0);
                borderSize = new Vector3(borderThickness.Right, borderThickness.Top, elementSize.Z);
                DrawBorder(border, ref offsets, ref borderSize, context);
                
                // left/bottom
                offsets = new Vector3(-elementHalfSize.X + elementHalfBorders.Left, elementHalfSize.Y - elementHalfBorders.Bottom, 0);
                borderSize = new Vector3(borderThickness.Left, borderThickness.Bottom, elementSize.Z);
                DrawBorder(border, ref offsets, ref borderSize, context);
                
                // right/bottom
                offsets = new Vector3(elementHalfSize.X - elementHalfBorders.Right, elementHalfSize.Y - elementHalfBorders.Bottom, 0);
                borderSize = new Vector3(borderThickness.Right, borderThickness.Bottom, elementSize.Z);
                DrawBorder(border, ref offsets, ref borderSize, context);
            }
        }

        private void DrawBorder(Border border, ref Vector3 offsets, ref Vector3 borderSize, UIRenderingContext context)
        {
            var worldMatrix = border.WorldMatrixInternal;
            worldMatrix.M41 += worldMatrix.M11 * offsets.X + worldMatrix.M21 * offsets.Y + worldMatrix.M31 * offsets.Z;
            worldMatrix.M42 += worldMatrix.M12 * offsets.X + worldMatrix.M22 * offsets.Y + worldMatrix.M32 * offsets.Z;
            worldMatrix.M43 += worldMatrix.M13 * offsets.X + worldMatrix.M23 * offsets.Y + worldMatrix.M33 * offsets.Z;
            Batch.DrawCube(ref worldMatrix, ref borderSize, ref border.BorderColorInternal, context.DepthBias);
        }
    }
}