﻿// Copyright (c) 2014 Silicon Studio Corp. (http://siliconstudio.co.jp)
// This file is distributed under GPL v3. See LICENSE.md for details.

using System.Globalization;

using SiliconStudio.Core;
using SiliconStudio.Core.Storage;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using SiliconStudio.Core.Diagnostics;
using SiliconStudio.Core.MicroThreading;
using SiliconStudio.Core.Serialization.Assets;
using SiliconStudio.Core.IO;

using System.Reflection;

namespace SiliconStudio.BuildEngine
{
    public class StepCounter
    {
        private readonly int[] stepResults;
        public int Total { get; private set; }

        public StepCounter()
        {
            stepResults = new int[Enum.GetValues(typeof(ResultStatus)).Length];
        }

        public void AddStepResult(ResultStatus result)
        {
            lock (stepResults)
            {
                ++Total;
                ++stepResults[(int)result];
            }
        }

        public int Get(ResultStatus result)
        {
            lock (stepResults)
            {
                return stepResults[(int)result];
            }
        }

        public void Clear()
        {
            lock (stepResults)
            {
                Total = 0;
                foreach (var value in Enum.GetValues(typeof(ResultStatus)))
                    stepResults[(int)value] = 0;
            }
        }
    }

    public class Builder
    {
        public const int ExpectedVersion = 3;
        public static readonly string DoNotPackTag = "DoNotPack";
        public static readonly string DoNotCompressTag = "DoNotCompress";

        #region Public Members

        /// <summary>
        /// Indicate which mode to use with this builder
        /// </summary>
        public enum Mode
        {
            /// <summary>
            /// Build the script
            /// </summary>
            Build,
            /// <summary>
            /// Clean the command cache used to determine wheither a command has already been triggered.
            /// </summary>
            Clean,
            /// <summary>
            /// Clean the command cache and delete every output objects
            /// </summary>
            CleanAndDelete,
        }

        /// <summary>
        /// Logger used by the builder and the commands
        /// </summary>
        public ILogger Logger { get; private set; }

        /// <summary>
        /// Builder name
        /// </summary>
        public string BuilderName { get; set; }

        /// <summary>
        /// The <see cref="Guid"/> assigned to the builder.
        /// </summary>
        public Guid BuilderId { get; private set; }

        /// <summary>
        /// The build path for spawned slave processes.
        /// </summary>
        public string SlaveBuilderPath { get; set; }

        /// <summary>
        /// Number of working threads to create
        /// </summary>
        public int ThreadCount {
            get { return threadCount; }
            set { threadCount = value; if (MaxParallelProcesses > value) MaxParallelProcesses = value; }
        }
        private int threadCount;

        /// <summary>
        /// Max number of processes that can be executed for remote commands
        /// </summary>
        public int MaxParallelProcesses {
            get { return maxParallelProcesses; }
            set { maxParallelProcesses = value; if (value > ThreadCount) throw new InvalidOperationException("MaxParallelProcesses can't be greater than ThreadCount."); }
        }
        private int maxParallelProcesses;

        /// <summary>
        /// The root build step of the builder defining the builds to perform.
        /// </summary>
        public ListBuildStep Root { get; private set; }

        /// <summary>
        /// Indicate whether this builder is currently running.
        /// </summary>
        public bool IsRunning { get; protected set; }

        /// <summary>
        /// Indicate whether the build has been canceled
        /// </summary>
        public bool Cancelled { get; protected set; }

        public List<string> MonitorPipeNames { get; private set; }
        
        public const string MonitorPipeName = "net.pipe://localhost/Paradox.BuildEngine.Monitor";

        public IDictionary<string, string> InitialVariables { get; private set; }

        public string MetadataDatabaseDirectory { get; set; }

        public readonly ISet<ObjectId> DisableCompressionIds = new HashSet<ObjectId>();

        #endregion Public Members
        #region Private Members
        
        /// <summary>
        /// The name on the disk of the index file name.
        /// </summary>
        private readonly string indexFilename;

        /// <summary>
        /// The name on the disk of the file caching the input file hashes
        /// </summary>
        private readonly string inputHashesFilename;

        /// <summary>
        /// The path on the disk where to perform the build
        /// </summary>
        private readonly string buildPath;

        /// <summary>
        /// The build profile
        /// </summary>
        private readonly string buildProfile;

        /// <summary>
        /// The path of the data base from the build path
        /// </summary>
        private const string DatabasePath = "/data/db/";

        /// <summary>
        /// Cancellation token source used for cancellation.
        /// </summary>
        private CancellationTokenSource cancellationTokenSource;

        private Scheduler scheduler;

        private readonly CommandIOMonitor ioMonitor;
        private readonly List<BuildThreadMonitor> threadMonitors = new List<BuildThreadMonitor>();

        /// <summary>
        /// A map containing results of each commands, indexed by command hashes. When the builder is running, this map if filled with the result of the commands of the current execution.
        /// </summary>
        private ObjectDatabase resultMap;
        
        private readonly DateTime startTime;

        private readonly StepCounter stepCounter = new StepCounter();

        /// <summary>
        /// The build mode of the current run execution
        /// </summary>
        private Mode runMode;

        #endregion Private Members

        /// <summary>
        /// The full path of the index file from the build directory.
        /// </summary>
        private string IndexFileFullPath
        {
            get { return DatabasePath + indexFilename; }
        }

        /// <summary>
        /// The full path of the input hashes file from the build directory.
        /// </summary>
        private string InputHashesFileFullPath
        {
            get { return DatabasePath + inputHashesFilename; }
        }

        public Builder(string buildPath, string buildProfile, string indexFilename, string inputHashesFilename, ILogger logger)
        {
            if (buildPath == null) throw new ArgumentNullException("buildPath");
            if (indexFilename == null) throw new ArgumentNullException("indexFilename");
            if (inputHashesFilename == null) throw new ArgumentNullException("inputHashesFilename");

            MonitorPipeNames = new List<string>();
            startTime = DateTime.Now;
            this.buildProfile = buildProfile;
            this.indexFilename = indexFilename;
            var entryAssembly = Assembly.GetEntryAssembly();
            SlaveBuilderPath = entryAssembly != null ? entryAssembly.Location : "";
            Logger = logger;
            this.inputHashesFilename = inputHashesFilename;
            this.buildPath = buildPath;
            Root = new ListBuildStep();
            ioMonitor = new CommandIOMonitor(Logger);
            ThreadCount = Environment.ProcessorCount;
            MaxParallelProcesses = ThreadCount;
            BuilderId = Guid.NewGuid();
            InitialVariables = new Dictionary<string, string>();

            SetupBuildPath(buildPath);

            var objectDatabase = IndexFileCommand.ObjectDatabase;

            // Check current database version, and erase it if too old
            int currentVersion = 0;
            var versionFile = Path.Combine(VirtualFileSystem.GetAbsolutePath(DatabasePath), "version");
            if (File.Exists(versionFile))
            {
                try
                {
                    var versionText = File.ReadAllText(versionFile);
                    currentVersion = int.Parse(versionText);
                }
                catch (Exception)
                {
                }
            }

            if (currentVersion != ExpectedVersion)
            {
                var looseObjects = objectDatabase.EnumerateLooseObjects().ToArray();

                if (looseObjects.Length > 0)
                {
                    Logger.Info("Database version number has been updated from {0} to {1}, erasing all objects...", currentVersion, ExpectedVersion);

                    // Database version has been updated, let's clean it
                    foreach (var objectId in looseObjects)
                    {
                        try
                        {
                            objectDatabase.Delete(objectId);
                        }
                        catch (IOException)
                        {
                        }
                    }
                }

                // Create directory
                File.WriteAllText(versionFile, ExpectedVersion.ToString(CultureInfo.InvariantCulture));
            }

            // Prepare data base directories
            AssetManager.GetFileProvider = () => IndexFileCommand.DatabaseFileProvider;
            var databasePathSplits = DatabasePath.Split('/');
            var accumulatorPath = "/";
            foreach (var pathPart in databasePathSplits.Where(x=>x!=""))
            {
                accumulatorPath += pathPart + "/";
                VirtualFileSystem.CreateDirectory(accumulatorPath);

                accumulatorPath += "";
            }
        }

        public static void SetupBuildPath(string buildPath)
        {
            // Mount build path
            ((FileSystemProvider)VirtualFileSystem.ApplicationData).ChangeBasePath(buildPath);
            if (IndexFileCommand.ObjectDatabase == null)
                IndexFileCommand.ObjectDatabase = new ObjectDatabase(DatabasePath, loadDefaultBundle: false); // note: this has to be done after VFS.ChangeBasePath
        }

        private class ExecuteContext : IExecuteContext
        {
            private readonly BuilderContext builderContext;
            private readonly BuildStep buildStep;
            private readonly BuildTransaction buildTransaction;
            private readonly Logger logger;
            private readonly Builder builder;

            public ExecuteContext(Builder builder, BuilderContext builderContext, BuildStep buildStep)
            {
                logger = new BuildStepLogger(buildStep, builder.Logger, builder.startTime);
                this.builderContext = builderContext;
                this.builder = builder;
                this.buildStep = buildStep;
                buildTransaction = new BuildTransaction(null, buildStep.GetOutputObjectsGroups());
            }

            public Logger Logger { get { return logger; } }

            public ObjectDatabase ResultMap { get { return builder.resultMap; } }

            public CancellationTokenSource CancellationTokenSource { get { return builder.cancellationTokenSource; } }

            public Dictionary<string, string> Variables { get; set; }

            public IMetadataProvider MetadataProvider { get { return builderContext.MetadataProvider; } }

            public void ScheduleBuildStep(BuildStep step)
            {
                builder.ScheduleBuildStep(builderContext, buildStep, step, Variables);
            }

            public IEnumerable<IDictionary<ObjectUrl, OutputObject>> GetOutputObjectsGroups()
            {
                return buildStep.GetOutputObjectsGroups();
            }

            public ObjectId ComputeInputHash(UrlType type, string filePath)
            {
                var hash = ObjectId.Empty;

                switch (type)
                {
                    case UrlType.File:
                        hash = builderContext.InputHashes.ComputeFileHash(filePath);
                        break;
                    case UrlType.Internal:
                        if (!buildTransaction.TryGetValue(filePath, out hash))
                            Logger.Warning("Location " + filePath + " does not exist currently and is required to compute the current command hash. The build cache will not work for this command!");
                        break;
                    case UrlType.Virtual:
                        var providerResult = VirtualFileSystem.ResolveProvider(filePath, true);
                        var dbProvider = providerResult.Provider as DatabaseFileProvider;
                        var microProvider = providerResult.Provider as MicroThreadFileProvider;
                        if (microProvider != null)
                        {
                            dbProvider = microProvider.ThreadLocal.Value as DatabaseFileProvider;
                        }

                        if (dbProvider != null)
                        {
                            dbProvider.AssetIndexMap.TryGetValue(providerResult.Path, out hash);
                        }
                        break;
                }

                return hash;
            }

            public CommandBuildStep IsCommandCurrentlyRunning(ObjectId commandHash)
            {
                lock (builderContext.CommandsInProgress)
                {
                    CommandBuildStep step;
                    builderContext.CommandsInProgress.TryGetValue(commandHash, out step);
                    return step;
                }
            }

            public void NotifyCommandBuildStepStarted(CommandBuildStep commandBuildStep, ObjectId commandHash)
            {
                lock (builderContext.CommandsInProgress)
                {
                    if (!builderContext.CommandsInProgress.ContainsKey(commandHash))
                        builderContext.CommandsInProgress.Add(commandHash, commandBuildStep);

                    builder.ioMonitor.CommandStarted(commandBuildStep);
                }
            }

            public void NotifyCommandBuildStepFinished(CommandBuildStep commandBuildStep, ObjectId commandHash)
            {
                lock (builderContext.CommandsInProgress)
                {
                    builderContext.CommandsInProgress.Remove(commandHash);
                    builder.ioMonitor.CommandEnded(commandBuildStep);
                }
            }
        }

        private void ScheduleBuildStep(BuilderContext builderContext, BuildStep instigator, BuildStep buildStep, IDictionary<string, string> variables)
        {
            if (buildStep.ExecutionId == 0)
            {
                if (buildStep.Parent != null && buildStep.Parent != instigator)
                    throw new InvalidOperationException("Scheduling a BuildStep with a different instigator that its parent");
                if (buildStep.Parent == null)
                {
                    buildStep.Parent = instigator;
                }

                var executeContext = new ExecuteContext(this, builderContext, buildStep) { Variables = new Dictionary<string, string>(variables) };
                //buildStep.ExpandStrings(executeContext);

                if (runMode == Mode.Build)
                {
                    MicroThread microThread = scheduler.Create();

                    // Find priority from this build step, or one of its parent.
                    var buildStepPriority = buildStep;
                    while (buildStepPriority != null)
                    {
                        if (buildStepPriority.Priority.HasValue)
                        {
                            microThread.Priority = buildStepPriority.Priority.Value;
                            break;
                        }

                        buildStepPriority = buildStepPriority.Parent;
                    }

                    buildStep.ExecutionId = microThread.Id;

                    foreach (var threadMonitor in threadMonitors)
                    {
                        threadMonitor.RegisterBuildStep(buildStep, ((BuildStepLogger)executeContext.Logger).StepLogger);
                    }

                    microThread.Name = buildStep.ToString();

                    // Default:
                    // Schedule continuations as early as possible to help EnumerableBuildStep finish when all its task are finished.
                    // Otherwise, it would wait for all leaf to finish first before finishing parent EnumerableBuildStep.
                    // This should also reduce memory usage, and might improve cache coherency as well.
                    microThread.ScheduleMode = ScheduleMode.First;

                    microThread.Start(async () =>
                    {
                        // Wait for prerequisites
                        await Task.WhenAll(buildStep.PrerequisiteSteps.Select(x => x.ExecutedAsync()).ToArray());

                        // Check for failed prerequisites
                        var status = ResultStatus.NotProcessed;

                        if (buildStep.ArePrerequisitesSuccessful)
                        {
                            try
                            {
                                IndexFileCommand.MountDatabase(executeContext.GetOutputObjectsGroups());

                                // Execute
                                status = await buildStep.Execute(executeContext, builderContext);
                            }
                            catch (TaskCanceledException e)
                            {
                                // Benlitz: I'm NOT SURE this is the correct explanation, it might be a more subtle race condition, but I can't manage to reproduce it again
                                executeContext.Logger.Warning("A child task of build step " + buildStep + " triggered a TaskCanceledException that was not caught by the parent task. The command has not handled cancellation gracefully.");
                                executeContext.Logger.Warning(e.Message);
                                status = ResultStatus.Cancelled;
                            }
                            catch (Exception e)
                            {
                                executeContext.Logger.Error("Exception in command " + buildStep + ": " + e);
                                status = ResultStatus.Failed;
                            }
                            finally
                            {
                                IndexFileCommand.UnmountDatabase();
                                
                                // Ensure the command set at least the result status
                                if (status == ResultStatus.NotProcessed)
                                    throw new InvalidDataException("The build step " + buildStep + " returned ResultStatus.NotProcessed after completion.");
                            }
                            if (microThread.Exception != null)
                            {
                                executeContext.Logger.Error("Exception in command " + buildStep + ": " + microThread.Exception);
                                status = ResultStatus.Failed;
                            }
                        }
                        else
                        {
                            status = ResultStatus.NotTriggeredPrerequisiteFailed;
                        }

                        //if (completedTask.IsCanceled)
                        //{
                        //    completedStep.Status = ResultStatus.Cancelled;
                        //}
                        var logType = LogMessageType.Info;
                        string logText = null;
                        
                        switch (status)
                        {
                            case ResultStatus.Successful:
                                logType = LogMessageType.Info;
                                logText = "BuildStep {0} was successful.".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.Failed:
                                logType = LogMessageType.Error;
                                logText = "BuildStep {0} failed.".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.Cancelled:
                                logType = LogMessageType.Warning;
                                logText = "BuildStep {0} cancelled.".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.NotTriggeredWasSuccessful:
                                logType = LogMessageType.Verbose;
                                logText = "BuildStep {0} is up-to-date and has been skipped".ToFormat(buildStep.ToString());
                                break;
                            case ResultStatus.NotProcessed:
                                throw new InvalidDataException("BuildStep has neither succeeded, failed, nor been cancelled");
                        }
                        if (logText != null)
                        {
                            var logMessage = new LogMessage(buildStep.Module, logType, logText);
                            Logger.Log(logMessage);
                        }

                        buildStep.RegisterResult(executeContext, status);
                        stepCounter.AddStepResult(status);
                    });
                }
                else
                {
                    buildStep.Clean(executeContext, builderContext, runMode == Mode.CleanAndDelete);
                }
            }
        }

        /// <summary>
        /// Cancel the currently executing build.
        /// </summary>
        public void CancelBuild()
        {
            if (IsRunning)
            {
                Cancelled = true;
                cancellationTokenSource.Cancel();
            }
        }

        public void RunUntilEnd()
        {
            foreach (var threadMonitor in threadMonitors)
                threadMonitor.RegisterThread(Thread.CurrentThread.ManagedThreadId);

            while (true)
            {
                scheduler.Run();
                
                // Exit loop if no more micro threads
                lock (scheduler.MicroThreads)
                {
                    if (scheduler.MicroThreads.Count == 0)
                        break;
                }

                // TODO: improve how we wait for work. Thread.Sleep(0) uses too much CPU.
                Thread.Sleep(1);
            }
        }

        /// <summary>
        /// Discard the current <see cref="Root"/> build step and initialize a new empty one.
        /// </summary>
        public void Reset()
        {
            Root = new ListBuildStep();
            stepCounter.Clear();
        }

        /// <summary>
        /// Write the generated objects into the index map file.
        /// </summary>
        /// <param name="mergeWithCurrentIndexFile">Indicate if old values must be deleted or merged</param>
        public void WriteIndexFile(bool mergeWithCurrentIndexFile)
        {
            if (!mergeWithCurrentIndexFile)
            {
                try
                {
                    VirtualFileSystem.FileDelete(IndexFileFullPath);
                }
                catch (IOException)
                {
                }
            }

            using (var indexFile = AssetIndexMap.NewTool(indexFilename))
            {
                // Filter database Location
                indexFile.AddValues(
                    Root.OutputObjects.Where(x => x.Key.Type == UrlType.Internal)
                        .Select(x => new KeyValuePair<string, ObjectId>(x.Key.Path, x.Value.ObjectId)));

                foreach (var x in Root.OutputObjects)
                {
                    if(x.Key.Type != UrlType.Internal)
                        continue;

                    if (x.Value.Tags.Contains(DoNotCompressTag))
                        DisableCompressionIds.Add(x.Value.ObjectId);
                }
            }
        }

        /// <summary>
        /// Runs this instance.
        /// </summary>
        public BuildResultCode Run(Mode mode, bool writeIndexFile = true, bool enableMonitor = true)
        {
            runMode = mode;

            if (IsRunning)
                throw new InvalidOperationException("An instance of this Builder is already running.");

            // reset build cache from previous build run
            var parameters = new BuildParameterCollection();
            cancellationTokenSource = new CancellationTokenSource();
            Cancelled = false;
            IsRunning = true;
            DisableCompressionIds.Clear();
            
            // Reseting result map
            var inputHashes = FileVersionTracker.GetDefault();
            {
                var builderContext = new BuilderContext(buildPath, buildProfile, inputHashes, parameters, MaxParallelProcesses, SlaveBuilderPath);
                if (!string.IsNullOrWhiteSpace(MetadataDatabaseDirectory))
                {
                    var metadataProvider = new QueryMetadataProvider();
                    if (metadataProvider.Open(Path.Combine(MetadataDatabaseDirectory, QueryMetadataProvider.DefaultDatabaseFilename), false))
                    {
                        builderContext.MetadataProvider = metadataProvider;
                    }
                }

                resultMap = IndexFileCommand.ObjectDatabase;

                scheduler = new Scheduler();
                if (enableMonitor)
                {
                    threadMonitors.Add(new BuildThreadMonitor(scheduler, BuilderId));
                    foreach (var monitorPipeName in MonitorPipeNames)
                        threadMonitors.Add(new BuildThreadMonitor(scheduler, BuilderId, monitorPipeName));

                    foreach (var threadMonitor in threadMonitors)
                        threadMonitor.Start();
                }

                ScheduleBuildStep(builderContext, null, Root, InitialVariables);

                // Create threads
                var threads = Enumerable.Range(0, ThreadCount).Select(x => new Thread(SafeAction.Wrap(RunUntilEnd)) { IsBackground = true }).ToArray();

                // Start threads
                int threadId = 0;
                foreach (var thread in threads)
                {
                    thread.Name = (BuilderName ?? "Builder") + " worker thread " + (++threadId);
                    thread.Start();
                }

                // Wait for all threads to finish
                foreach (var thread in threads)
                {
                    thread.Join();
                }

                foreach (var threadMonitor in threadMonitors)
                    threadMonitor.Finish();

                foreach (var threadMonitor in threadMonitors)
                    threadMonitor.Join();
            }

            threadMonitors.Clear();
            BuildResultCode result;

            if (runMode == Mode.Build)
            {
                Logger.Info("");
                if (cancellationTokenSource.IsCancellationRequested)
                {
                    Logger.Error("Build cancelled.");
                    result = BuildResultCode.Cancelled;

                }
                else if (stepCounter.Get(ResultStatus.Failed) > 0 || stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed) > 0)
                {
                    Logger.Error("Build finished in {0} steps. Command results: {1} succeeded, {2} up-to-date, {3} failed, {4} not triggered due to previous failure.",
                       stepCounter.Total, stepCounter.Get(ResultStatus.Successful), stepCounter.Get(ResultStatus.NotTriggeredWasSuccessful),
                       stepCounter.Get(ResultStatus.Failed), stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed));

                    Logger.Error("Build failed.");
                    result = BuildResultCode.BuildError;
                }
                else
                {
                    Logger.Info("Build finished in {0} steps. Command results: {1} succeeded, {2} up-to-date, {3} failed, {4} not triggered due to previous failure.",
                        stepCounter.Total, stepCounter.Get(ResultStatus.Successful), stepCounter.Get(ResultStatus.NotTriggeredWasSuccessful),
                        stepCounter.Get(ResultStatus.Failed), stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed));

                    Logger.Info("Build is successful.");
                    result = BuildResultCode.Successful;
                }
            }
            else
            {
                // Clean input hashes file
                if (VirtualFileSystem.FileExists(InputHashesFileFullPath))
                {
                    try
                    {
                        VirtualFileSystem.FileDelete(InputHashesFileFullPath);
                    }
                    catch (IOException)
                    {
                        return BuildResultCode.BuildError;
                    }
                }
                string modeName;
                switch (runMode)
                {
                    case Mode.Clean:
                        modeName = "Clean";
                        break;
                    case Mode.CleanAndDelete:
                        modeName = "Clean-and-delete";
                        break;
                    default:
                        throw new InvalidOperationException("Builder executed in unknown mode.");
                }

                if (cancellationTokenSource.IsCancellationRequested)
                {
                    Logger.Error(modeName + " has been cancelled.");
                    result = BuildResultCode.Cancelled;

                }
                else if (stepCounter.Get(ResultStatus.Failed) > 0 || stepCounter.Get(ResultStatus.NotTriggeredPrerequisiteFailed) > 0)
                {
                    Logger.Error(modeName + " has failed.");
                    result = BuildResultCode.BuildError;
                }
                else
                {
                    Logger.Error(modeName + " has been successfully completed.");
                    result = BuildResultCode.Successful;
                }
            }
            scheduler = null;
            resultMap = null;
            IsRunning = false;

            return result;
        }
    }
}
