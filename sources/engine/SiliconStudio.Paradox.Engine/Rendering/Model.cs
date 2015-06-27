﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;

using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Core.Mathematics;
using SiliconStudio.Core.Serialization;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Paradox.Graphics;
using SiliconStudio.Paradox.Graphics.Data;

using Buffer = SiliconStudio.Paradox.Graphics.Buffer;

namespace SiliconStudio.Paradox.Rendering
{
    /// <summary>
    /// Collection of <see cref="Mesh"/>, each one usually being a different LOD of the same Model.
    /// The effect system will select the appropriate LOD depending on distance, current pass, and other effect-specific requirements.
    /// </summary>
    [DataSerializerGlobal(typeof(ReferenceSerializer<Model>), Profile = "Asset")]
    [ContentSerializer(typeof(DataContentSerializer<Model>))]
    [DataContract]
    public class Model : IEnumerable
    {
        private List<Mesh> meshes = new List<Mesh>();
        private readonly List<MaterialInstance> materials = new List<MaterialInstance>();
        private IList<Model> children;
        private Model parent;

        /// <summary>
        /// Gets the views.
        /// </summary>
        /// <value>
        /// The views.
        /// </value>
        public IList<Model> Children
        {
            get { return children; }
            set { children = value; }
        }

        /// <summary>
        /// Gets the materials.
        /// </summary>
        /// <value>
        /// The materials.
        /// </value>
        public List<MaterialInstance> Materials
        {
            get { return materials; }
        }

        /// <summary>
        /// Gets the meshes.
        /// </summary>
        /// <value>
        /// The meshes.
        /// </value>
        public List<Mesh> Meshes
        {
            get { return meshes; }
            set { meshes = value; }
        }

        /// <summary>
        /// Gets or sets the hierarchy definition, which describes nodes name, default transformation and hierarchical parent.
        /// </summary>
        /// <value>
        /// The hierarchy, which describes nodes name, default transformation and hierarchical parent.
        /// </value>
        public ModelViewHierarchyDefinition Hierarchy { get; set; }

        /// <summary>
        /// Gets or sets the bounding box encompassing all the <see cref="Meshes"/> (not including animation).
        /// </summary>
        /// <value>
        /// The bounding box.
        /// </value>
        public BoundingBox BoundingBox { get; set; }

        /// <summary>
        /// Gets or sets the bounding sphere encompassing all the <see cref="Meshes"/> (not including animation).
        /// </summary>
        /// <value>The bounding sphere.</value>
        public BoundingSphere BoundingSphere { get; set; }

        // Temporarily removed
        //[DataMemberConvert]
        //public ParameterCollection Parameters
        //{
        //    get { return parameters; }
        //}

        /// <summary>
        /// Adds the specified model view (for collection initializers).
        /// </summary>
        /// <param name="model">The model view.</param>
        public void Add(Model model)
        {
            children.Add(model);
        }

        /// <summary>
        /// Adds the specified mesh (for collection initializers).
        /// </summary>
        /// <param name="mesh">The mesh.</param>
        public void Add(Mesh mesh)
        {
            Meshes.Add(mesh);
        }

        /// <summary>
        /// Adds the specified material (for collection initializers).
        /// </summary>
        /// <param name="material">The mesh.</param>
        public void Add(MaterialInstance material)
        {
            Materials.Add(material);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return meshes.Cast<object>().Concat(materials).GetEnumerator();
        }

        /// <summary>
        /// Create a clone with its own ParameterCollection.
        /// It allows reuse of a single Model for multiple ModelComponent.
        /// </summary>
        public Model Instantiate()
        {
            var result = new Model();
            if (Children != null)
            {
                result.Children = new List<Model>();
                foreach (var child in Children)
                {
                    result.Children.Add(child.Instantiate());
                }
            }

            foreach (var mesh in Meshes)
            {
                var meshCopy = new Mesh(mesh);
                result.Meshes.Add(meshCopy);
            }

            result.Hierarchy = Hierarchy;
            result.BoundingBox = BoundingBox;

            return result;
        }

        private void children_CollectionChanged(object sender, TrackingCollectionChangedEventArgs e)
        {
            var child = (Model)e.Item;
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    if (child.parent != null)
                        throw new InvalidOperationException("Model already have a parent.");
                    child.parent = this;
                    break;
                case NotifyCollectionChangedAction.Remove:
                    if (child.parent != this)
                        throw new InvalidOperationException("Model doesn't have expected parent.");
                    child.parent = null;
                    break;
            }
        }
    }
}