﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

using SiliconStudio.Core;
using SiliconStudio.Core.Collections;
using SiliconStudio.Paradox.Engine;

namespace SiliconStudio.Paradox.Rendering.Lights
{
    /// <summary>
    /// A list of <see cref="LightComponentCollection"/> for a particular type of light (direct light, direct light + shadows, environment lights).
    /// </summary>
    [DebuggerDisplay("LightCollections[{Count}]")]
    public sealed class LightComponentCollectionGroup : IEnumerable<LightComponentCollection>
    {
        // The reason of this class is to store lights according to their culling mask while minimizing the number of LightComponentCollection needed
        // if culling mask are similar.
        // For example, suppose we have a list of lights per cullin mask:
        // 111000  -> Light1, Light2, Light3, Light4, Light5
        // 101000  -> Light6
        // 100000  -> Light7, Light8
        // 011100  -> Light9
        //
        // We will generate 4 different LightComponentCollection (#number is the LightComponentCollectionInstance):
        //  Mask   -> Pool
        // -------    ----
        // 100000  -> #1   -> Light1, Light2, Light3, Light4, Light5, Light6, Light7, Light8
        // 011000  -> #2   -> Light1, Light2, Light3, Light4, Light5, Light9
        // 001000  -> #3   -> Light1, Light2, Light3, Light4, Light5, Light6, Light9
        // 000100  -> #4   -> Light9
        //
        // But if all lights belong to the same mask like 111111
        // 111000  -> Light1, Light2, Light3, Light4, Light5, Light6, Light7, Light8
        // We will generate a single group:
        //  Mask   -> Pool
        // -------    ----
        // 111000  -> #1   -> Light1, Light2, Light3, Light4, Light5, Light6, Light7, Light8
        //
        // We are also able to retreive efficiently a LightComponentCollection for a specific group mask

        private PoolListStruct<LightComponentCollection> lightCollectionPool;
        private readonly List<LightComponent> allLights;

        private readonly List<LightComponent> allLightsWithShadows;

        // a 32 bits * 2 entry per bits storing for each bit:
        // [0] The 'and' mask of all active light culling mask associated to this bit
        // [1] The index of the associated collection in the lightCollectionPool
        private readonly uint[] groupMasks;

        private readonly HashSet<EntityGroupMask> allMasks;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightComponentCollectionGroup"/> class.
        /// </summary>
        internal LightComponentCollectionGroup()
        {
            lightCollectionPool = new PoolListStruct<LightComponentCollection>(32, LightComponentCollectionFactory);
            groupMasks = new uint[32 * 2];
            allLights = new List<LightComponent>(128);
            allLightsWithShadows = new List<LightComponent>(128);
            allMasks = new HashSet<EntityGroupMask>();
        }

        /// <summary>
        /// Gets the <see cref="LightComponentCollection"/> at the specified index.
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>LightComponentCollection.</returns>
        /// <exception cref="System.ArgumentOutOfRangeException">index [{0}] out of range [0, {1}].ToFormat(index, lights.Count - 1)</exception>
        public LightComponentCollection this[int index]
        {
            get
            {
                return lightCollectionPool[index];
            }
        }

        /// <summary>
        /// Gets the light affecting a specific group.
        /// </summary>
        /// <param name="group">The group.</param>
        /// <returns>LightComponentCollection.</returns>
        public LightComponentCollection FindLightCollectionByGroup(EntityGroup group)
        {
            // If a mask is not zero, then we have a collection associated for this bit
            int groupBaseIndex = (int)group * 2;
            if (groupMasks[groupBaseIndex] != 0)
            {
                return lightCollectionPool[groupMasks[groupBaseIndex + 1]];
            }
            return null;
        }

        /// <summary>
        /// Gets all the lights stored in this group.
        /// </summary>
        /// <value>All lights.</value>
        public List<LightComponent> AllLights
        {
            get
            {
                return allLights;
            }
        }

        /// <summary>
        /// Gets the lights with shadows.
        /// </summary>
        /// <value>The lights with shadows.</value>
        public List<LightComponent> AllLightsWithShadows
        {
            get
            {
                return allLightsWithShadows;
            }
        }

        /// <summary>
        /// Gets the number of <see cref="LightComponentCollection"/> stored in this group.
        /// </summary>
        /// <value>The number of <see cref="LightComponentCollection"/> stored in this group.</value>
        public int Count
        {
            get
            {
                return lightCollectionPool.Count;
            }
        }

        internal unsafe void Clear()
        {
            allLights.Clear();
            allLightsWithShadows.Clear();
            allMasks.Clear();

            fixed (void* ptr = groupMasks)
                Interop.memset(ptr, 0, groupMasks.Length * sizeof(uint));
            
            // Only clear collections that were previously allocated (no need to iterate on all collections from the pool)
            foreach (var collection in lightCollectionPool)
            {
                collection.Clear();
            }
        }

        internal void PrepareLight(LightComponent lightComponent)
        {
            var cullingMask = lightComponent.CullingMask;

            // Don't procses a mask have we have already processed. We don't expect a huge combination of culling mask here
            if (!allMasks.Add(cullingMask))
            {
                return;
            }

            // Fit individual bits and prepare collection group based on mask
            // We iterate on each possible bits and we `And` all active masks for this particular bit
            var groupMask = (uint)cullingMask;
            for (int groupIndex = 0; groupMask != 0; groupMask = groupMask >> 1, groupIndex++)
            {
                if ((groupMask & 1) == 0)
                {
                    continue;
                }

                var previousMask = groupMasks[groupIndex * 2];
                previousMask = previousMask == 0 ? (uint)cullingMask : previousMask & (uint)cullingMask;
                groupMasks[groupIndex * 2] = previousMask;
            }
        }

        internal void AllocateCollectionsPerGroupOfCullingMask()
        {
            // Clear the pool (but keep instances already created)
            lightCollectionPool.Clear();

            // At worst, we have 32 collections (one per bit active in the culling mask)
            for (int i = 0; i < groupMasks.Length; i++)
            {
                var mask = groupMasks[i++];
                if (mask == 0)
                {
                    continue;
                }

                // Check if there is a previous collection for the current mask
                int collectionIndex = -1;
                for (int j = 0; j < lightCollectionPool.Count; j++)
                {
                    if ((uint)lightCollectionPool[j].CullingMask == mask)
                    {
                        collectionIndex = j;
                        break;
                    }
                }

                // If no collection found for this mask, create a new one
                if (collectionIndex < 0)
                {
                    collectionIndex = lightCollectionPool.Count;

                    // Use a pool to avoid recreating collections
                    var collection = lightCollectionPool.Add();

                    // The selected collection is associated with the specified mask
                    collection.CullingMask = (EntityGroupMask)mask;
                }

                // Store the index of the collection for the current bit
                groupMasks[i] = (uint)collectionIndex;
            }
        }

        internal void AddLight(LightComponent lightComponent)
        {
            var cullingMask = lightComponent.CullingMask;

            // Iterate only on allocated collections
            foreach (var lightCollectionGroup in lightCollectionPool)
            {
                // Check if a light culling mask belong to the current group
                if ((lightCollectionGroup.CullingMask & cullingMask) == 0)
                {
                    continue;
                }
                lightCollectionGroup.Add(lightComponent);
            }

            // Keep a list of all lights for this group
            allLights.Add(lightComponent);

            // If the light has shadows, populate a separate list
            var directLight = lightComponent.Type as IDirectLight;
            if (directLight != null && directLight.Shadow.Enabled)
            {
                allLightsWithShadows.Add(lightComponent);
            }
        }

        public FastListStruct<LightComponentCollection>.Enumerator GetEnumerator()
        {
            return lightCollectionPool.GetEnumerator();
        }

        IEnumerator<LightComponentCollection> IEnumerable<LightComponentCollection>.GetEnumerator()
        {
            return lightCollectionPool.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return lightCollectionPool.GetEnumerator();
        }

        private static LightComponentCollection LightComponentCollectionFactory()
        {
            return new LightComponentCollection();
        }
    }
}