﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.
using System.Collections.Generic;
using System.Threading.Tasks;
using SiliconStudio.Core.Storage;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Serialization.Assets;

namespace SiliconStudio.BuildEngine
{
    public class LocalCommandContext : CommandContextBase
    {
        private readonly IExecuteContext executeContext;

        private readonly LoggerResult logger;

        public CommandBuildStep Step { get; protected set; }

        public override LoggerResult Logger { get { return logger; } }

        public LocalCommandContext(IExecuteContext executeContext, CommandBuildStep step, BuilderContext builderContext) : base(step.Command, builderContext)
        {
            this.executeContext = executeContext;
            logger = new ForwardingLoggerResult(executeContext.Logger);
            Step = step;
        }

        public override IEnumerable<IDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups()
        {
            return Step.GetOutputObjectsGroups();
        }

        protected override async Task<ResultStatus> ScheduleAndExecuteCommandInternal(Command command)
        {
            var resultStatus = await Step.SpawnCommand(command, executeContext);
            return resultStatus;
        }

        internal protected override ObjectId ComputeInputHash(UrlType type, string filePath)
        {
            return executeContext.ComputeInputHash(type, filePath);
        }
    }
}