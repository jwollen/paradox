// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;
using SiliconStudio.Core;
using SiliconStudio.Core.Serialization.Contents;
using SiliconStudio.Paradox.Graphics;

namespace SiliconStudio.Paradox.Engine.Design
{
    /// <summary>
    /// Stores some default parameters for the game.
    /// </summary>
    [DataContract("GameSettings")]
    [ContentSerializer(typeof(DataContentSerializer<GameSettings>))]
    public sealed class GameSettings
    {
        public const string AssetUrl = "__GameSettings__";

        public GameSettings()
        {
            EffectCompilation = EffectCompilationMode.Local;
        }

        public Guid PackageId { get; set; }

        public string DefaultSceneUrl { get; set; }

        public int DefaultBackBufferWidth { get; set; }

        public int DefaultBackBufferHeight { get; set; }

        public GraphicsProfile DefaultGraphicsProfileUsed { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether effect compile should be allowed, and if yes, should it be done locally (if possible) or remotely?
        /// </summary>
        public EffectCompilationMode EffectCompilation { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether effect compile (local or remote) should be recorded and sent to effect compile server for GameStudio notification.
        /// </summary>
        public bool RecordUsedEffects { get; set; }
    }
}
