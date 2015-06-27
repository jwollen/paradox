﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System;

namespace SiliconStudio.Paradox.Assets.Entities
{
    public interface IEntityScriptReference
    {
        EntityReference Entity { get; }

        Guid Id { get; }
    }
}