// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core;
using SiliconStudio.Paradox.Engine.Processors;
using SiliconStudio.Paradox.Rendering;
using SiliconStudio.Paradox.Rendering.Composers;

namespace SiliconStudio.Paradox.Engine
{
    /// <summary>
    /// A renderer for a child scene defined by a <see cref="ChildSceneComponent"/>.
    /// </summary>
    [DataContract("SceneChildRenderer")]
    [Display("Render Child Scene")]
    public sealed class SceneChildRenderer : SceneRendererBase
    {
        private SceneInstance currentSceneInstance;
        private ChildSceneProcessor childSceneProcessor;

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneChildRenderer"/> class.
        /// </summary>
        public SceneChildRenderer() : this(null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="SceneChildRenderer"/> class.
        /// </summary>
        /// <param name="childScene">The scene child.</param>
        public SceneChildRenderer(ChildSceneComponent childScene)
        {
            ChildScene = childScene;
        }

        /// <summary>
        /// Gets or sets the scene.
        /// </summary>
        /// <value>The scene.</value>
        [DataMember(10)]
        public ChildSceneComponent ChildScene { get; set; }

        /// <summary>
        /// Gets or sets the graphics compositor override, allowing to override the composition of the scene.
        /// </summary>
        /// <value>The graphics compositor override.</value>
        [DataMemberIgnore]
        public ISceneGraphicsCompositor GraphicsCompositorOverride { get; set; } // Overrides are accessible only at runtime

        protected override void Destroy()
        {
            if (GraphicsCompositorOverride != null)
            {
                GraphicsCompositorOverride.Dispose();
                GraphicsCompositorOverride = null;
            }

            base.Destroy();
        }

        protected override void DrawCore(RenderContext context, RenderFrame output)
        {
            if (ChildScene == null || !ChildScene.Enabled)
            {
                return;
            }

            currentSceneInstance = SceneInstance.GetCurrent(Context);

            childSceneProcessor = childSceneProcessor ?? currentSceneInstance.GetProcessor<ChildSceneProcessor>();

            if (childSceneProcessor == null)
            {
                return;
            }

            SceneInstance sceneInstance = childSceneProcessor.GetSceneInstance(ChildScene);
            if (sceneInstance != null)
            {
                sceneInstance.Draw(context, output, GraphicsCompositorOverride);
            }
        }
    }
}