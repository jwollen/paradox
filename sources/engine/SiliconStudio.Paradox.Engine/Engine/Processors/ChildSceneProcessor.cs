// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Paradox.Games;
using SiliconStudio.Paradox.Rendering;

namespace SiliconStudio.Paradox.Engine.Processors
{
    /// <summary>
    /// The scene child processor to handle a child scene. See remarks.
    /// </summary>
    /// <remarks>
    /// This processor is handling specially an entity with a <see cref="ChildSceneComponent"/>. If an scene component is found, it will
    /// create a sub-<see cref="EntityManager"/> dedicated to handle the entities inside the child scene.
    /// </remarks>
    public sealed class ChildSceneProcessor : EntityProcessor<ChildSceneComponent>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ChildSceneProcessor"/> class.
        /// </summary>
        public ChildSceneProcessor()
            : base(ChildSceneComponent.Key)
        {
        }

        /// <summary>
        /// Returns the scene containing the processor.
        /// </summary>
        private Scene ContainingScene
        {
            get
            {
                var sceneInstance = EntityManager as SceneInstance;
                return sceneInstance != null ? sceneInstance.Scene : null;
            }
        }

        public SceneInstance GetSceneInstance(ChildSceneComponent component)
        {
            return component.SceneInstance;
        }

        protected override ChildSceneComponent GenerateAssociatedData(Entity entity)
        {
            return entity.Get<ChildSceneComponent>();
        }

        protected override void OnEntityAdding(Entity entity, ChildSceneComponent component)
        {
            // safe guard for infinite recursion when setting component child scene on the scene that contains it.
            var scene = ContainingScene != component.Scene ? component.Scene : null;
            component.SceneInstance = new SceneInstance(EntityManager.Services, scene, EntityManager.GetProcessor<ScriptProcessor>() != null);
        }

        protected override void OnEntityRemoved(Entity entity, ChildSceneComponent component)
        {
            if (component != null)
            {
                component.SceneInstance.Dispose();
                component.SceneInstance = null;
            }
        }

        public override void Update(GameTime time)
        {
            foreach (var childComponent in enabledEntities.Values)
            {
                if (childComponent.Enabled)
                {
                    UpdateSceneInstance(childComponent);
                    childComponent.SceneInstance.Update(time);
                }
            }
        }

        public override void Draw(RenderContext context)
        {
            foreach (ChildSceneComponent childComponent in enabledEntities.Values)
            {
                if (childComponent.Enabled)
                {
                    UpdateSceneInstance(childComponent);
                    childComponent.SceneInstance.Draw(context);
                }
            }
        }

        private void UpdateSceneInstance(ChildSceneComponent childComponent)
        {
            if (childComponent.Enabled)
            {
                // safe guard against infinite recursion
                var currentScene = ContainingScene != childComponent.Scene ? childComponent.Scene : null;

                // Copy back the scene from the component to the instance
                childComponent.SceneInstance.Scene = currentScene;
            }
        }
    }
}