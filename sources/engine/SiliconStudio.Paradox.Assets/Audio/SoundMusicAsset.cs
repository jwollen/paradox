﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;

using SiliconStudio.Assets;
using SiliconStudio.Assets.Compiler;
using SiliconStudio.Core;
using SiliconStudio.Core.Reflection;

namespace SiliconStudio.Paradox.Assets.Audio
{
    [DataContract("SoundMusic")]
    [AssetDescription(FileExtension)]
    [ObjectFactory(typeof(SoundMusicFactory))]
    [AssetCompiler(typeof(SoundAssetCompiler))]
    [ThumbnailCompiler(PreviewerCompilerNames.SoundThumbnailCompilerQualifiedName)]
    [Display(125, "Sound Music", "A music track")]
    public class SoundMusicAsset : SoundAsset
    {
        private class SoundMusicFactory : IObjectFactory
        {
            public object New(Type type)
            {
                return new SoundMusicAsset();
            }
        }
    }
}