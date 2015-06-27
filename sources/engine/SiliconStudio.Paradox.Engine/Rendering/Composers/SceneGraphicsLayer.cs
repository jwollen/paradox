﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections;
using System.ComponentModel;

using SiliconStudio.Core;
using SiliconStudio.Core.Annotations;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Rendering.Composers
{
    /// <summary>
    /// A graphics layer.
    /// </summary>
    [DataContract("SceneGraphicsLayer")]
    public class SceneGraphicsLayer : RendererBase, IEnumerable
    {
        private IGraphicsLayerOutput output;

        /// <summary>
        /// Property key to access the Master <see cref="RenderFrame"/> from <see cref="RenderContext.Tags"/>.
        /// </summary>
        public static readonly PropertyKey<RenderFrame> Master = new PropertyKey<RenderFrame>("RenderFrame.Master", typeof(RenderFrame));

        /// <summary>
        /// Property key to access the Input <see cref="RenderFrame"/> from the current SceneGraphicsLayer
        /// </summary>
        public static readonly PropertyKey<RenderFrame> CurrentInput = new PropertyKey<RenderFrame>("SceneGraphicsLayer.CurrentInput", typeof(SceneGraphicsLayer));

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneGraphicsLayer"/> class.
        /// </summary>
        public SceneGraphicsLayer()
        {
            Output = CurrentRenderFrameProvider.Instance;
            Renderers = new SceneRendererCollection();
        }

        [DataMember(10)]
        public override string Name
        {
            get
            {
                return base.Name;
            }
            set
            {
                base.Name = value;
            }
        }

        /// <summary>
        /// Gets or sets the output of this layer.
        /// </summary>
        /// <value>The output.</value>
        /// <userdoc>
        /// Defines the output of a layer. This can be a local or shared render target.
        /// (This can be the previous layer or a specific layer or a render target...etc.)
        /// </userdoc>
        [DataMember(40)]
        [NotNull]
        public IGraphicsLayerOutput Output
        {
            get
            {
                return output;
            }
            set
            {
                // master layer is always a master output and cannot be changed
                output = IsMaster ? MasterRenderFrameProvider.Instance : value;
            }
        }

        /// <summary>
        /// Gets the renderers that will be used to render this layer.
        /// </summary>
        /// <value>The renderers.</value>
        /// <userdoc>
        /// The renderers that will be used to render this layer.
        /// </userdoc>
        [DataMember(50)]
        [Category]
        [NotNullItems]
        public SceneRendererCollection Renderers { get; private set; }

        internal bool IsMaster { get; set; }

        /// <summary>
        /// Adds the specified scene renderer.
        /// </summary>
        /// <param name="sceneRenderer">The scene renderer.</param>
        public void Add(ISceneRenderer sceneRenderer)
        {
            Renderers.Add(sceneRenderer);
        }

        protected override void Unload()
        {
            // Dispose the output
            if (Output != null)
            {
                Output.Dispose();
            }

            foreach (var renderer in Renderers)
            {
                renderer.Dispose();
            }

            base.Unload();
        }

        protected override void DrawCore(RenderContext context)
        {
            if (!Enabled || Output == null)
            {
                return;
            }

            // Sets the input of the layer (== last Current)
            var currentRenderFrame = context.Tags.Get(RenderFrame.Current);
            
            // Sets the output of the layer 
            // Master is always going to use the Master frame for the current frame.
            var renderFrame = Output.GetRenderFrame(context);

            using (var t1 = context.PushTagAndRestore(CurrentInput, currentRenderFrame))
            {
                context.Tags.Set(RenderFrame.Current, renderFrame);
                Renderers.Draw(context);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Renderers.GetEnumerator();
        }
    }
}