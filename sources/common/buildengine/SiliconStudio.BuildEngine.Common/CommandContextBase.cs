﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using SiliconStudio.Core.Storage;
using System.Threading.Tasks;

using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Serialization.Assets;

namespace SiliconStudio.BuildEngine
{
    public abstract class CommandContextBase : ICommandContext
    {
        public Command CurrentCommand { get; private set; }

        public abstract LoggerResult Logger { get; }

        public BuildParameterCollection BuildParameters { get; private set; }

        public IMetadataProvider MetadataProvider { get; private set; }

        protected internal readonly CommandResultEntry ResultEntry;

        protected abstract Task<ResultStatus> ScheduleAndExecuteCommandInternal(Command command);

        public abstract IEnumerable<IDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups();

        internal protected abstract ObjectId ComputeInputHash(UrlType type, string filePath);

        protected CommandContextBase(Command command, BuilderContext builderContext)
        {
            CurrentCommand = command;
            BuildParameters = builderContext.Parameters;
            ResultEntry = new CommandResultEntry();
            MetadataProvider = builderContext.MetadataProvider;
        }

        public Task<ResultStatus> ScheduleAndExecuteCommand(Command command)
        {
            ResultEntry.SpawnedCommands.Add(command);
            return ScheduleAndExecuteCommandInternal(command);
        }

        public void RegisterInputDependency(ObjectUrl url)
        {
            ResultEntry.InputDependencyVersions.Add(url, ComputeInputHash(url.Type, url.Path));
        }

        public void RegisterOutput(ObjectUrl url, ObjectId hash)
        {
            ResultEntry.OutputObjects.Add(url, hash);
        }

        public void RegisterCommandLog(IEnumerable<ILogMessage> logMessages)
        {
            foreach (var message in logMessages)
            {
                ResultEntry.LogMessages.Add(message as SerializableLogMessage ?? new SerializableLogMessage((LogMessage)message));
            }
        }

        public void AddTag(ObjectUrl url, TagSymbol tagSymbol)
        {
            ResultEntry.TagSymbols.Add(new KeyValuePair<ObjectUrl, string>(url, tagSymbol.Name));
        }

        public void RegisterSpawnedCommandWithoutScheduling(Command command)
        {
            ResultEntry.SpawnedCommands.Add(command);
        }
    }
}