﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
//
// Copyright (c) 2010-2013 SharpDX - Alexandre Mutel
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.Games
{
    /// <summary>
    /// Contains context used to render the game (Control for WinForm, a DrawingSurface for WP8...etc.).
    /// </summary>
    public partial class GameContext
    {
        /// <summary>
        /// The context type of this instance.
        /// </summary>
        public readonly AppContextType ContextType;

        // TODO: remove these requested values.

        /// <summary>
        /// The requested width.
        /// </summary>
        public int RequestedWidth;

        /// <summary>
        /// The requested height.
        /// </summary>
        public int RequestedHeight;

        /// <summary>
        /// The requested back buffer format.
        /// </summary>
        public PixelFormat RequestedBackBufferFormat;

        /// <summary>
        /// The requested depth stencil format.
        /// </summary>
        public PixelFormat RequestedDepthStencilFormat;

        /// <summary>
        /// THe requested graphics profiles.
        /// </summary>
        public GraphicsProfile[] RequestedGraphicsProfile;

        /// <summary>
        /// Indicate whether the game must initialize the default database when it starts running.
        /// </summary>
        public bool InitializeDatabase = true;
    }
}