﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using System.Collections.Generic;
using System.Linq;

using SiliconStudio.Core;
using SiliconStudio.Core.Serialization;

namespace SiliconStudio.Assets.Analysis
{
    /// <summary>
    /// Describes dependencies (in/out/broken) for a specific asset.
    /// </summary>
    /// <remarks>There are 3 types of dependencies:
    /// <ul>
    /// <li><c>in</c> dependencies: through the <see cref="LinksIn"/> property, contains assets                                 
    /// that are referencing this asset.</li>
    /// <li><c>out</c> dependencies: through the <see cref="LinksOut"/> property, contains assets 
    /// that are referenced by this asset.</li>
    /// <li><c>broken</c> dependencies: through the <see cref="BrokenLinksOut"/> property, 
    /// contains output links to assets that are missing.</li>
    /// </ul>
    /// </remarks>
    public class AssetDependencies
    {
        private readonly AssetItem item;
        private Dictionary<Guid, AssetLink> parents;
        private Dictionary<Guid, AssetLink> children;
        private Dictionary<Guid, AssetLink> missingChildren;

        public AssetDependencies(AssetItem assetItem)
        {
            if (assetItem == null) throw new ArgumentNullException("assetItem");
            item = assetItem;
        }

        public AssetDependencies(AssetDependencies set)
        {
            if (set == null) throw new ArgumentNullException("set");
            item = set.Item;

            // Copy Output refs
            foreach (var child in set.LinksOut)
                AddLinkOut(child, true);

            // Copy Input refs
            foreach (var child in set.LinksIn)
                AddLinkIn(child, true);

            // Copy missing refs
            foreach (var child in set.BrokenLinksOut)
                AddBrokenLinkOut(child.Element, child.Type);
        }

        public Guid Id
        {
            get
            {
                return item.Id;
            }
        }

        /// <summary>
        /// Gets the itemReferenced.
        /// </summary>
        /// <value>The itemReferenced.</value>
        public AssetItem Item
        {
            get
            {
                return item;
            }
        }

        /// <summary>
        /// Gets the links coming into the element.
        /// </summary>
        public IEnumerable<AssetLink> LinksIn
        {
            get
            {
                return parents != null? parents.Values: Enumerable.Empty<AssetLink>();
            }
        }

        /// <summary>
        /// Gets the links going out of the element.
        /// </summary>
        public IEnumerable<AssetLink> LinksOut
        {
            get
            {
                return children != null ? children.Values : Enumerable.Empty<AssetLink>();
            }
        }

        /// <summary>
        /// Gets the links out.
        /// </summary>
        /// <value>The missing references.</value>
        public IEnumerable<IContentLink> BrokenLinksOut
        {
            get
            {
                if (missingChildren == null)
                    yield break;

                foreach (var reference in missingChildren.Values)
                    yield return reference;
            }
        }

        /// <summary>
        /// Resets this instance and clear all dependencies (including missing)
        /// </summary>
        public void Reset(bool keepParents)
        {
            missingChildren = null;
            children = null;

            if (!keepParents) 
                parents = null;
        }

        /// <summary>
        /// Gets a value indicating whether this instance has missing references.
        /// </summary>
        /// <value><c>true</c> if this instance has missing references; otherwise, 
        /// <c>false</c>.</value>
        public bool HasMissingDependencies
        {
            get
            {
                return missingChildren != null && missingChildren.Count > 0;
            }
        }

        /// <summary>
        /// Gets the number of missing dependencies of the asset.
        /// </summary>
        public int MissingDependencyCount
        {
            get
            {
                return missingChildren != null ? missingChildren.Count : 0;
            }
        }

        /// <summary>
        /// Adds a link going into the element.
        /// </summary>
        /// <param name="fromItem">The element the link is coming from</param>
        /// <param name="contentLinkType">The type of link</param>
        /// <param name="cloneAssetItem">Indicate if the <see cref="AssetItem"/> should be cloned or not</param>
        /// <exception cref="ArgumentException">A link from this element already exists</exception>
        public void AddLinkIn(AssetItem fromItem, ContentLinkType contentLinkType, bool cloneAssetItem)
        {
            AddLink(ref parents, new AssetLink(fromItem, contentLinkType), cloneAssetItem);
        }

        /// <summary>
        /// Adds a link coming from the provided element.
        /// </summary>
        /// <param name="contentLink">The link in</param>
        /// <param name="cloneAssetItem">Indicate if the <see cref="AssetItem"/> should be cloned or not</param>
        /// <exception cref="ArgumentException">A link from this element already exists</exception>
        public void AddLinkIn(AssetLink contentLink, bool cloneAssetItem)
        {
            AddLink(ref parents, contentLink, cloneAssetItem);
        }

        /// <summary>
        /// Gets the link coming from the provided element.
        /// </summary>
        /// <param name="fromItem">The element the link is coming from</param>
        /// <returns>The link</returns>
        /// <exception cref="ArgumentException">There is not link to the provided element</exception>
        /// <exception cref="ArgumentNullException">fromItem</exception>
        public AssetLink GetLinkIn(AssetItem fromItem)
        {
            if (fromItem == null) throw new ArgumentNullException("fromItem");

            return GetLink(ref parents, fromItem.Id);
        }

        /// <summary>
        /// Removes the link coming from the provided element.
        /// </summary>
        /// <param name="fromItem">The element the link is coming from</param>
        /// <exception cref="ArgumentNullException">fromItem</exception>
        /// <returns>The removed link</returns>
        public AssetLink RemoveLinkIn(AssetItem fromItem)
        {
            if (fromItem == null) throw new ArgumentNullException("fromItem");

            return RemoveLink(ref parents, fromItem.Id, ContentLinkType.All);
        }

        /// <summary>
        /// Adds a link going to the provided element.
        /// </summary>
        /// <param name="toItem">The element the link is going to</param>
        /// <param name="contentLinkType">The type of link</param>
        /// <param name="cloneAssetItem">Indicate if the <see cref="AssetItem"/> should be cloned or not</param>
        /// <exception cref="ArgumentException">A link to this element already exists</exception>
        public void AddLinkOut(AssetItem toItem, ContentLinkType contentLinkType, bool cloneAssetItem)
        {
            AddLink(ref children, new AssetLink(toItem, contentLinkType), cloneAssetItem);
        }

        /// <summary>
        /// Adds a link going to the provided element.
        /// </summary>
        /// <param name="contentLink">The link out</param>
        /// <param name="cloneAssetItem">Indicate if the <see cref="AssetItem"/> should be cloned or not</param>
        /// <exception cref="ArgumentException">A link to this element already exists</exception>
        public void AddLinkOut(AssetLink contentLink, bool cloneAssetItem)
        {
            AddLink(ref children, contentLink, cloneAssetItem);
        }

        /// <summary>
        /// Gets the link going to the provided element.
        /// </summary>
        /// <param name="toItem">The element the link is going to</param>
        /// <returns>The link</returns>
        /// <exception cref="ArgumentException">There is not link to the provided element</exception>
        /// <exception cref="ArgumentNullException">toItem</exception>
        public AssetLink GetLinkOut(AssetItem toItem)
        {
            if (toItem == null) throw new ArgumentNullException("toItem");

            return GetLink(ref children, toItem.Id);
        }

        /// <summary>
        /// Removes the link going to the provided element.
        /// </summary>
        /// <param name="toItem">The element the link is going to</param>
        /// <exception cref="ArgumentNullException">toItem</exception>
        /// <returns>The removed link</returns>
        public AssetLink RemoveLinkOut(AssetItem toItem)
        {
            if (toItem  == null) throw new ArgumentNullException("toItem");

            return RemoveLink(ref children, toItem.Id, ContentLinkType.All);
        }

        /// <summary>
        /// Adds a broken link out.
        /// </summary>
        /// <param name="reference">the reference to the missing element</param>
        /// <param name="contentLinkType">The type of link</param>
        /// <exception cref="ArgumentException">A broken link to this element already exists</exception>
        public void AddBrokenLinkOut(IContentReference reference, ContentLinkType contentLinkType)
        {
            AddLink(ref missingChildren, new AssetLink(reference, contentLinkType), false);
        }

        /// <summary>
        /// Adds a broken link out.
        /// </summary>
        /// <param name="contentLink">The broken link</param>
        /// <exception cref="ArgumentException">A broken link to this element already exists</exception>
        public void AddBrokenLinkOut(IContentLink contentLink)
        {
            AddLink(ref missingChildren, new AssetLink(contentLink.Element, contentLink.Type), false);
        }

        /// <summary>
        /// Gets the broken link out to the provided element.
        /// </summary>
        /// <param name="id">The id of the element the link is going to</param>
        /// <returns>The link</returns>
        /// <exception cref="ArgumentException">There is not link to the provided element</exception>
        /// <exception cref="ArgumentNullException">toItem</exception>
        public IContentLink GetBrokenLinkOut(Guid id)
        {
            return GetLink(ref missingChildren, id);
        }

        /// <summary>
        /// Removes the broken link to the provided element.
        /// </summary>
        /// <param name="id">The id to the missing element</param>
        /// <exception cref="ArgumentNullException">toItem</exception>
        /// <returns>The removed link</returns>
        public IContentLink RemoveBrokenLinkOut(Guid id)
        {
            return RemoveLink(ref missingChildren, id, ContentLinkType.All);
        }

        private void AddLink(ref Dictionary<Guid, AssetLink> dictionary, AssetLink contentLink, bool cloneAssetItem)
        {
            if(dictionary == null)
                dictionary = new Dictionary<Guid, AssetLink>();

            var id = contentLink.Element.Id;
            if (dictionary.ContainsKey(id))
                contentLink.Type |= dictionary[id].Type;

            dictionary[id] = cloneAssetItem? contentLink.Clone(): contentLink;
        }

        private AssetLink GetLink(ref Dictionary<Guid, AssetLink> dictionary, Guid id)
        {
            if(dictionary == null || !dictionary.ContainsKey(id))
                throw new ArgumentException("There is currently no link between elements '{0}' and '{1}'".ToFormat(item.Id, id));

            return dictionary[id];
        }

        private AssetLink RemoveLink(ref Dictionary<Guid, AssetLink> dictionary, Guid id, ContentLinkType type)
        {
            if (dictionary == null || !dictionary.ContainsKey(id))
                throw new ArgumentException("There is currently no link between elements '{0}' and '{1}'".ToFormat(item.Id, id));

            var oldLink = dictionary[id];
            var newLink = oldLink;

            newLink.Type &= ~type;
            oldLink.Type &= type;

            if(newLink.Type == 0)
                dictionary.Remove(id);

            if (dictionary.Count == 0)
                dictionary = null;

            return oldLink;
        }
    }
}