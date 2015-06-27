﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.Serialization.Assets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SiliconStudio.BuildEngine
{
    public abstract class BuildStep
    {
        private readonly LoggerResult logger = new LoggerResult();

        protected BuildStep(ResultStatus status = ResultStatus.NotProcessed)
        {
            Status = status;
        }

        /// <summary>
        /// Gets or sets the module associated with this build step, used when logging error/information.
        /// </summary>
        /// <value>The module.</value>
        public string Module { get; set; }

        /// <summary>
        /// Gets or sets the priority amongst other build steps.
        /// </summary>
        /// <value>
        /// The priority.
        /// </value>
        public int? Priority { get; set; }

        /// <summary>
        /// Title of the build step. Intended to be short
        /// </summary>
        public abstract string Title { get; }

        /// <summary>
        /// Description of the build step. Intended to be longer and more descriptive than the <see cref="Title"/>
        /// </summary>
        public string Description { get { return ToString(); } }

        /// <summary>
        /// The status of the result.
        /// </summary>
        public ResultStatus Status { get; private set; }

        /// <summary>
        /// Indicate whether this command has already been processed (ie. executed or skipped) by the Builder
        /// </summary>
        public bool Processed { get { return Status != ResultStatus.NotProcessed; } }

        /// <summary>
        /// Indicate whether the result corresponds to a successful execution (even if the command has not been triggered)
        /// </summary>
        public bool Succeeded { get { return Status == ResultStatus.Successful || Status == ResultStatus.NotTriggeredWasSuccessful; } }

        /// <summary>
        /// Indicate whether the result corresponds to a failed execution (even if the command has not been triggered)
        /// </summary>
        public bool Failed { get { return Status == ResultStatus.Failed || Status == ResultStatus.NotTriggeredPrerequisiteFailed; } }

        /// <summary>
        /// A tag property that can contain anything useful for tools based on this build Engine.
        /// </summary>
        public object Tag { get; set; }

        /// <summary>
        /// List of commands that must be executed prior this one (direct dependence only).
        /// </summary>
        // TODO: this is probably obsolete now
        public IEnumerable<BuildStep> PrerequisiteSteps { get { return prerequisiteSteps; } }
        private readonly List<BuildStep> prerequisiteSteps = new List<BuildStep>();

        /// <summary>
        /// List of commands that needs this command to be successfully executed before being processed
        /// </summary>
        public IEnumerable<CommandBuildStep> SpawnedSteps { get { return SpawnedStepsList; } }
        protected readonly List<CommandBuildStep> SpawnedStepsList = new List<CommandBuildStep>();

        /// <summary>
        /// The parent build step, which will be the instigator of the step
        /// </summary>
        public BuildStep Parent { get { return parent; } protected internal set { if (parent != null && value != null) throw new InvalidOperationException("BuildStep already has a parent"); parent = value; } }
        private BuildStep parent;

        /// <summary>
        /// An unique id during a build execution, assigned once the build step is scheduled.
        /// </summary>
        public long ExecutionId { get; internal set; }

        /// <summary>
        /// Indicate whether all prerequisite commands have been processed
        /// </summary>
        public bool ArePrerequisitesCompleted { get { return PrerequisiteSteps.All(x => x.Processed); } }

        /// <summary>
        /// Indicate whether all prerequisite commands have been processed and are in a successful state
        /// </summary>
        public bool ArePrerequisitesSuccessful { get { return PrerequisiteSteps.All(x => x.Succeeded); } }

        /// <summary>
        /// Gets the logger for the current build step.
        /// </summary>
        public LoggerResult Logger { get { return logger; } }

        /// <summary>
        /// Event raised when the command is processed (even if it has been skipped or if it failed)
        /// </summary>
        public event EventHandler<BuildStepEventArgs> StepProcessed;

        /// <summary>
        /// Execute the BuildStep, usually resulting in scheduling tasks in the scheduler 
        /// </summary>
        /// <param name="executeContext">The execute context</param>
        /// <param name="builderContext">The builder context</param>
        /// <returns>A task returning <see cref="ResultStatus"/> indicating weither the execution has successed or failed.</returns>
        public abstract Task<ResultStatus> Execute(IExecuteContext executeContext, BuilderContext builderContext);

        /// <summary>
        /// Clean the build, deleting the command cache which is used to determine wheither a command has already been executed, and deleting the output objects if asked.
        /// </summary>
        /// <param name="executeContext">The execute context</param>
        /// <param name="builderContext">The builder context</param>
        /// <param name="deleteOutput">if true, every output object is also deleted, in addition of the command cache.</param>
        public virtual void Clean(IExecuteContext executeContext, BuilderContext builderContext, bool deleteOutput)
        {
            // By default, do the same as Execute. This will apply for flow control steps (lists, enumerations...)
            // Specific implementation exists for CommandBuildStep
            Execute(executeContext, builderContext);
        }

        /// <summary>
        /// Clone this Build Step.
        /// </summary>
        /// <returns></returns>
        public abstract BuildStep Clone();

        public abstract override string ToString();

        public static void LinkBuildSteps(BuildStep parent, BuildStep child)
        {
            lock (child.prerequisiteSteps)
            {
                child.prerequisiteSteps.Add(parent);
            }
        }

        public Task<BuildStep> ExecutedAsync()
        {
            var tcs = new TaskCompletionSource<BuildStep>();
            StepProcessed += (sender, e) => tcs.TrySetResult(e.Step);
            return Processed ? Task.FromResult(this) : tcs.Task;
        }

        /// <summary>
        /// Associate the given <see cref="ResultStatus" /> object as the result of the current step and execute the <see cref="StepProcessed"/> event.
        /// </summary>
        /// <param name="executeContext">The execute context.</param>
        /// <param name="status">The result status.</param>
        internal void RegisterResult(IExecuteContext executeContext, ResultStatus status)
        {
            Status = status;

            //executeContext.Logger.Debug("Step timer for {0}: callbacks: {1}ms, total: {2}ms", this, CallbackWatch.ElapsedMilliseconds, MicroThreadWatch.ElapsedMilliseconds);

            if (StepProcessed != null)
            {
                try
                {
                    IndexFileCommand.MountDatabase(executeContext.GetOutputObjectsGroups());
                    StepProcessed(this, new BuildStepEventArgs(this, executeContext.Logger));
                }
                catch (Exception ex)
                {
                    executeContext.Logger.Error("Exception in command " + this + ": " + ex);
                }
                finally
                {
                    IndexFileCommand.UnmountDatabase();                    
                }
            }
        }

        public IEnumerable<IDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups()
        {
            var currentBuildStep = this;
            while (currentBuildStep != null)
            {
                var enumBuildStep = currentBuildStep as EnumerableBuildStep;
                if (enumBuildStep != null)
                    yield return enumBuildStep.OutputObjects;
                currentBuildStep = currentBuildStep.Parent;
            }
        }
    }
}
