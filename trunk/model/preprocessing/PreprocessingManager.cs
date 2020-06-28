using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.MRU;
using System.Runtime.CompilerServices;
using System.Collections.Immutable;

namespace LogJoint.Preprocessing
{
	public class LogSourcesPreprocessingManager : IManager
	{
		#region Public interface

		public LogSourcesPreprocessingManager(
			ISynchronizationContext invokeSynchronize,
			IFormatAutodetect formatAutodetect,
			IExtensionsRegistry extensions,
			IPreprocessingManagerExtension builtinStepsExtension,
			Telemetry.ITelemetryCollector telemetry,
			ITempFilesManager tempFilesManager,
			ILogSourcesManager logSourcesManager,
			IShutdown shutdown,
			ITraceSourceFactory traceSourceFactory,
			IChangeNotification changeNotification
		)
		{
			this.traceSourceFactory = traceSourceFactory;
			this.trace = traceSourceFactory.CreateTraceSource("PreprocessingManager", "prepr");
			this.invokeSynchronize = invokeSynchronize;
			this.formatAutodetect = formatAutodetect;
			this.providerYieldedCallback = prov => logSourcesManager.Create(prov.Factory, prov.ConnectionParams).Visible = !prov.IsHiddenLog;
			this.extensions = extensions;
			this.telemetry = telemetry;
			this.tempFilesManager = tempFilesManager;
			this.logSourcesManager = logSourcesManager;
			this.changeNotification = changeNotification;

			extensions.Register(builtinStepsExtension);

			shutdown.Cleanup += (sender, e) =>
			{
				shutdown.AddCleanupTask(this.DeleteAllPreprocessings());
			};
		}


		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingAdded;
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingWillDispose;
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingDisposed;
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingChangedAsync;
		public event EventHandler<LogSourcePreprocessingWillYieldEventArg> PreprocessingWillYieldProviders;
		public event EventHandler<LogSourcePreprocessingFailedEventArg> PreprocessingYieldFailed;

		Task<YieldedProvider[]> IManager.Preprocess(
			IEnumerable<IPreprocessingStep> steps,
			string preprocessingDisplayName,
			PreprocessingOptions options)
		{
			return ExecutePreprocessing(new LogSourcePreprocessing(this, providerYieldedCallback, steps, preprocessingDisplayName, options));
		}

		Task<YieldedProvider[]> IManager.Preprocess(
			IRecentlyUsedEntity recentLogEntry,
			PreprocessingOptions options)
		{
			return ExecutePreprocessing(new LogSourcePreprocessing(this, providerYieldedCallback, recentLogEntry, options));
		}

		IReadOnlyList<ILogSourcePreprocessing> IManager.Items => items;

		bool IManager.ConnectionRequiresDownloadPreprocessing(IConnectionParams connectParams)
		{
			return 
				LoadStepsFromConnectionParams(connectParams)
				.Any(s => CreateStepByName(s.StepName, null) is IDownloadPreprocessingStep);
		}

		string IManager.ExtractContentsContainerNameFromConnectionParams(IConnectionParams connectParams)
		{
			var steps = LoadStepsFromConnectionParams(connectParams).ToArray();
			var stepObjects = steps.Select(s => CreateStepByName(s.StepName, null));
			if (stepObjects.Any(s => s == null))
				return null;
			var getStep = stepObjects.FirstOrDefault() as IGetPreprocessingStep;
			if (getStep == null)
				return null;
			if (stepObjects.Skip(1).SkipWhile(s => s is IDownloadPreprocessingStep).Any(s => s is IUnpackPreprocessingStep))
				return getStep.GetContentsContainerName(steps[0].Argument);
			return null;
		}

		string IManager.ExtractCopyablePathFromConnectionParams(IConnectionParams connectParams)
		{
			var steps = LoadStepsFromConnectionParams(connectParams).ToArray();
			var stepObjects = steps.Select(s => CreateStepByName(s.StepName, null));
			var getStep = stepObjects.FirstOrDefault() as IGetPreprocessingStep;
			if (getStep != null)
				return getStep.GetContentsUrl(steps[0].Argument);
			var path = connectParams[ConnectionParamsKeys.PathConnectionParam];
			if (!tempFilesManager.IsTemporaryFile(path))
				return path;
			return null;
		}

		string IManager.ExtractUserBrowsableFileLocationFromConnectionParams(IConnectionParams connectParams)
		{
			var steps = LoadStepsFromConnectionParams(connectParams).ToArray();
			var stepObjects = steps.Select(s => CreateStepByName(s.StepName, null));
			var getStep = stepObjects.FirstOrDefault() as IGetPreprocessingStep;
			string fileName = null;
			if (getStep != null)
			{
				var secondStep = stepObjects.Skip(1).FirstOrDefault();
				if (secondStep == null || secondStep is IUnpackPreprocessingStep)
				{
					fileName = steps[0].Argument;
				}
			}
			else
			{
				fileName = connectParams[ConnectionParamsKeys.PathConnectionParam];
			}
			if (!string.IsNullOrEmpty(fileName) && !tempFilesManager.IsTemporaryFile(fileName))
			{
				return fileName;
			}
			return null;
		}

		IConnectionParams IManager.AppendStep(
			IConnectionParams connectParams, string stepName, string stepArgument)
		{
			var steps = LoadStepsFromConnectionParams(connectParams).ToList();
			if (steps.Count == 0)
			{
				var path = connectParams[ConnectionParamsKeys.PathConnectionParam];
				if (path == null)
					return null;
				steps.Add(new PreprocessingHistoryItem(GetPreprocessingStep.name, path));
			}
			steps.Add(new PreprocessingHistoryItem(stepName, stepArgument));
			var retVal = connectParams.Clone(makeWritebleCopyIfReadonly: true);
			int stepIdx = 0;
			foreach (var step in steps)
			{
				retVal[string.Format("{0}{1}", ConnectionParamsKeys.PreprocessingStepParamPrefix, stepIdx)] = 
					string.Format(string.IsNullOrEmpty(step.Argument) ? "{0}" : "{0} {1}", step.StepName, step.Argument);
				++stepIdx;
			}
			return retVal;
		}

		#endregion

		class LogSourcePreprocessing : IPreprocessingStepCallback, ILogSourcePreprocessing
		{
			public LogSourcePreprocessing(
				LogSourcesPreprocessingManager owner, 
				Action<YieldedProvider> providerYieldedCallback,
				IEnumerable<IPreprocessingStep> initialSteps,
				string preprocessingDisplayName,
				PreprocessingOptions options) :
				this(owner, providerYieldedCallback)
			{
				this.displayName = preprocessingDisplayName;
				this.options = options;
				preprocLogic = async () =>
				{
					using (var perfop = new Profiling.Operation(trace, displayName))
					{
						for (var steps = new Queue<IPreprocessingStep>(initialSteps); ;)
						{
							if (cancellation.IsCancellationRequested)
								break;
							nextSteps = steps;
							if (steps.Count > 0)
							{
								IPreprocessingStep currentStep = steps.Dequeue();
								await currentStep.Execute(this).ConfigureAwait(continueOnCapturedContext: !isLongRunning);
								perfop.Milestone("completed " + currentStep.ToString());
							}
							else
							{
								foreach (var e in owner.extensions.Items)
									await e.FinalizePreprocessing(this).ConfigureAwait(continueOnCapturedContext: !isLongRunning);
								perfop.Milestone("notified extensions about finalization");
								if (steps.Count == 0)
									break;
							}
							nextSteps = null;
							currentDescription = genericProcessingDescription;
						}
					}
				};
			}

			public LogSourcePreprocessing(
				LogSourcesPreprocessingManager owner,
				Action<YieldedProvider> providerYieldedCallback,
				IRecentlyUsedEntity recentLogEntry,
				PreprocessingOptions options 
			) :
				this(owner, providerYieldedCallback)
			{
				this.options = options;
				preprocLogic = async () =>
				{
					IConnectionParams preprocessedConnectParams = null;
					IFileBasedLogProviderFactory fileBasedFactory = recentLogEntry.Factory as IFileBasedLogProviderFactory;
					bool interrupted = false;
					if (fileBasedFactory != null)
					{
						using (var perfop = new Profiling.Operation(trace, recentLogEntry.Factory.GetUserFriendlyConnectionName(recentLogEntry.ConnectionParams)))
						{
							PreprocessingStepParams currentParams = null;
							foreach (var loadedStep in LoadStepsFromConnectionParams(recentLogEntry.ConnectionParams))
							{
								currentParams = await ProcessLoadedStep(loadedStep, currentParams).ConfigureAwait(continueOnCapturedContext: !isLongRunning);
								perfop.Milestone(string.Format("completed {0}", loadedStep));
								if (currentParams == null)
								{
									interrupted = true;
									break;
								}
								currentDescription = genericProcessingDescription;
							}
							if (currentParams != null)
							{
								preprocessedConnectParams = fileBasedFactory.CreateParams(currentParams.Location);
								currentParams.DumpToConnectionParams(preprocessedConnectParams);
							}
						}
					}
					if (!interrupted)
					{
						var provider = new YieldedProvider(recentLogEntry.Factory, preprocessedConnectParams ?? recentLogEntry.ConnectionParams, "",
							(this.options & PreprocessingOptions.MakeLogHidden) != 0);
						((IPreprocessingStepCallback)this).YieldLogProvider(provider);
					}
				};
			}

			LogSourcePreprocessing(
				LogSourcesPreprocessingManager owner,
				Action<YieldedProvider> providerYieldedCallback)
			{
				this.owner = owner;
				this.id = string.Format("{0}.{1}", owner.trace.Prefix, Interlocked.Increment(ref owner.lastPreprocId));
				this.providerYieldedCallback = providerYieldedCallback;
				// clone formatAutodetect to avoid multithreaded access to the same object from concurrent LogSourcePreprocessing objects
				this.formatAutodetect = owner.formatAutodetect.Clone();
				this.tempFiles = owner.tempFilesManager;
				this.scopedTempFiles = new TempFilesCleanupList(tempFiles);
				this.trace = owner.traceSourceFactory.CreateTraceSource("PreprocessingManager", id);
			}

			public Task<YieldedProvider[]> Execute()
			{
				async Task<YieldedProvider[]> helper()
				{
					await preprocLogic();
					LoadChildPreprocessings();
					return await owner.invokeSynchronize.InvokeAndAwait(LoadYieldedProviders);
				}
				var innerTask = helper();
				this.task = innerTask.ContinueWith(t =>
				{
					var e = t.GetTaskException();
					if (e != null)
					{
						trace.Error(e, "Preprocessing failed");
						failure = e;
						bool isExpected = false;
						for (; ; )
						{
							var agg = failure as AggregateException;
							if (agg == null || agg.InnerException == null)
								break;
							isExpected = isExpected || agg is ExpectedErrorException;
							failure = agg.InnerException;
						}
						if (!isExpected)
							owner.telemetry.ReportException(e, "preprocessing failed");
					}
					ScheduleFinishPreprocessing(keepTaskAlive: e != null && !cancellation.IsCancellationRequested);
				});
				return innerTask;
			}

			private void LoadChildPreprocessings()
			{
				childPreprocessings.ForEach(
					logEntry => ((IManager)owner).Preprocess(
						logEntry.Param, 
						this.options | (logEntry.MakeHiddenLog ? PreprocessingOptions.MakeLogHidden : PreprocessingOptions.None)
					)
				);
			}

			static PreprocessingStepParams SetArgument(PreprocessingStepParams p, string argument)
			{
				return new PreprocessingStepParams(
					p?.Location,
					p?.FullPath,
					p?.PreprocessingHistory,
					p?.DisplayName,
					argument
				);
			}

			async Task<PreprocessingStepParams> ProcessLoadedStep(PreprocessingHistoryItem loadedStep, PreprocessingStepParams currentParams)
			{
				var step = owner.CreateStepByName(loadedStep.StepName, SetArgument(currentParams, loadedStep.Argument));
				if (step != null)
					return await step.ExecuteLoadedStep(this);
				return null;
			}

			void ScheduleFinishPreprocessing(bool keepTaskAlive)
			{
				if (keepTaskAlive)
					owner.invokeSynchronize.Invoke(FinishFailedPreprocessing);
				else
					owner.invokeSynchronize.Invoke(() => ((ILogSourcePreprocessing)this).Dispose());
			}

			void FinishFailedPreprocessing()
			{
				trace.Info("Preprocessing failed and user didn't cancel it. Leaving it to allow user see the problem");
				FirePreprocessingChanged();
			}

			async Task<YieldedProvider[]> LoadYieldedProviders() // this method is run in model thread
			{
				trace.Info("Loading yielded providers");
				YieldedProvider[] providersToYield;
				if (cancellation.IsCancellationRequested)
				{
					providersToYield = new YieldedProvider[0];
				}
				else
				{
					var postponeTasks = new List<Task>();
					var eventArg = new LogSourcePreprocessingWillYieldEventArg(
						this, yieldedProviders.AsReadOnly(), postponeTasks);
					owner?.PreprocessingWillYieldProviders?.Invoke(owner, eventArg);
					if (postponeTasks.Count > 0)
					{
						((IPreprocessingStepCallback)this).SetStepDescription("Waiting");
						await Task.WhenAll(postponeTasks);
					}
					var selection = Enumerable.Range(0, yieldedProviders.Count).Select(eventArg.IsAllowed).ToArray();
					providersToYield = yieldedProviders.Zip(Enumerable.Range(0, yieldedProviders.Count),
						(p, i) => selection[i] ? p : new YieldedProvider()).Where(p => p.Factory != null).ToArray();
				}
				var failedProviders = new List<YieldedProvider>();
				foreach (var provider in providersToYield)
				{
					try
					{
						providerYieldedCallback(provider);
					}
					catch (Exception e)
					{
						failedProviders.Add(provider);
						trace.Error(e, "Failed to load from {0} from {1}", provider.Factory.FormatName, provider.ConnectionParams);
					}
				}
				if (failedProviders.Count > 0)
					owner.PreprocessingYieldFailed?.Invoke(owner,
						new LogSourcePreprocessingFailedEventArg(this, failedProviders.AsReadOnly()));
				return providersToYield;
			}

			void FirePreprocessingChanged()
			{
				owner.PreprocessingChangedAsync?.Invoke(owner, new LogSourcePreprocessingEventArg(this));
			}

			void IPreprocessingStepCallback.YieldLogProvider(YieldedProvider provider)
			{
				provider.ConnectionParams = SanitizePreprocessingSteps(provider.ConnectionParams);
				yieldedProviders.Add(provider);
			}

			void IPreprocessingStepCallback.YieldChildPreprocessing(IRecentlyUsedEntity recentLogEntry, bool isHiddenLog)
			{
				childPreprocessings.Add(new ChildPreprocessingParams() { Param = recentLogEntry, MakeHiddenLog = isHiddenLog } );
			}

			void IPreprocessingStepCallback.YieldNextStep(IPreprocessingStep step)
			{
				if (nextSteps != null)
					nextSteps.Enqueue(step);
			}

			async Task<PreprocessingStepParams> IPreprocessingStepCallback.ReplayHistory(ImmutableArray<PreprocessingHistoryItem> history)
			{
				PreprocessingStepParams currentParams = null;
				foreach (var loadedStep in history)
				{
					currentParams = await ProcessLoadedStep(loadedStep, currentParams).ConfigureAwait(continueOnCapturedContext: !isLongRunning);
					if (currentParams == null)
						return null;
				}
				return currentParams;
			}

			ILogSourcePreprocessing IPreprocessingStepCallback.Owner => this;


			void IPreprocessingStepCallback.SetOption(PreprocessingOptions opt, bool value)
			{
				// allow only few specific modifications

				if (opt == PreprocessingOptions.Silent && value)
				{
					options |= PreprocessingOptions.Silent;
				}
			}

			ConfiguredTaskAwaitable IPreprocessingStepCallback.BecomeLongRunning()
			{
				if (!isLongRunning)
				{
					trace.Info("Preprocessing is now long running");
					isLongRunning = true;
					return TaskUtils.SwitchToThreadpoolContext();
				}
				else
				{
					return ((Task)Task.FromResult(0)).ConfigureAwait(continueOnCapturedContext: true);
				}
			}

			CancellationToken IPreprocessingStepCallback.Cancellation
			{
				get { return cancellation.Token; }
			}

			Exception ILogSourcePreprocessing.Failure
			{
				get { return failure; }
			}

			public IFormatAutodetect FormatAutodetect
			{
				get { return formatAutodetect; }
			}

			public ITempFilesManager TempFilesManager
			{
				get { return tempFiles; }
			}

			ITempFilesCleanupList IPreprocessingStepCallback.TempFilesCleanupList
			{
				get { return scopedTempFiles; }
			}

			void IPreprocessingStepCallback.SetStepDescription(string desc)
			{
				trace.Info("description -> {0}", desc);
				currentDescription = desc;
				FirePreprocessingChanged();
			}

			ISharedValueLease<T> IPreprocessingStepCallback.GetOrAddSharedValue<T>(string key, Func<T> valueFactory)
			{
				return new SharedValueLease<T>(owner.sharedValues, owner.sharedValues, key, valueFactory);
			}

			public LJTraceSource Trace
			{
				get { return trace; }
			}

			string ILogSourcePreprocessing.DisplayName => displayName ?? "Log preprocessor";

			string ILogSourcePreprocessing.CurrentStepDescription
			{
				get { return currentDescription; }
			}

			PreprocessingOptions ILogSourcePreprocessing.Flags
			{
				get { return options; }
			}

			bool ILogSourcePreprocessing.IsDisposed
			{
				get { return disposed; }
			}

			async Task ILogSourcePreprocessing.Dispose()
			{
				if (disposed)
					return;

				using (trace.NewFrame)
				{
					disposed = true;
					owner.FireWillDispose(this);
					cancellation.Cancel();
					trace.Info("Waiting task");
					await task;
					trace.Info("Task finished");

					owner.Remove(this);

					cancellation.Dispose();

					scopedTempFiles.Dispose();
				}
			}

			IConnectionParams SanitizePreprocessingSteps(IConnectionParams providerConnectionParams)
			{
				var steps = LoadStepsFromConnectionParams(providerConnectionParams).ToArray();

				// Remove the only "get" preprocessing step
				if (steps.Length == 1 && steps[0].StepName == GetPreprocessingStep.name)
				{
					providerConnectionParams = providerConnectionParams.Clone();
					providerConnectionParams[ConnectionParamsKeys.PreprocessingStepParamPrefix + "0"] = null;
					return providerConnectionParams;
				}

				return providerConnectionParams;
			}

			bool disposed;
			readonly LogSourcesPreprocessingManager owner;
			readonly string id;
			public Action<YieldedProvider> providerYieldedCallback;
			readonly LJTraceSource trace;
			readonly IFormatAutodetect formatAutodetect;
			readonly ITempFilesManager tempFiles;
			readonly ITempFilesCleanupList scopedTempFiles;
			readonly CancellationTokenSource cancellation = new CancellationTokenSource();
			readonly List<YieldedProvider> yieldedProviders = new List<YieldedProvider>();
			readonly List<ChildPreprocessingParams> childPreprocessings = new List<ChildPreprocessingParams>();
			readonly string displayName;
			PreprocessingOptions options;
			string currentDescription = "";
			bool isLongRunning;
			Exception failure;
			Func<Task> preprocLogic;
			Task task; // this task never fails
			Queue<IPreprocessingStep> nextSteps;

			static readonly string genericProcessingDescription = "Processing...";
		};

		struct ChildPreprocessingParams
		{
			public IRecentlyUsedEntity Param;
			public bool MakeHiddenLog;
		};

		#region Implementation

		Task<YieldedProvider[]> ExecutePreprocessing(LogSourcePreprocessing prep)
		{
			var ret = prep.Execute();
			if (ret.IsCompleted)
				return ret;
			items = items.Add(prep);
			changeNotification.Post();
			PreprocessingAdded?.Invoke(this, new LogSourcePreprocessingEventArg(prep));
			return ret;
		}

		internal void Remove(ILogSourcePreprocessing prep)
		{
			items = items.Remove(prep);
			changeNotification.Post();
			PreprocessingDisposed?.Invoke(this, new LogSourcePreprocessingEventArg(prep));
		}

		internal void FireWillDispose(ILogSourcePreprocessing prep)
		{
			PreprocessingWillDispose?.Invoke(this, new LogSourcePreprocessingEventArg(prep));
		}

		static IEnumerable<PreprocessingHistoryItem> LoadStepsFromConnectionParams(IConnectionParams connectParams)
		{
			for (int stepIdx = 0; ; ++stepIdx)
			{
				string stepStr = connectParams[string.Format("{0}{1}", ConnectionParamsKeys.PreprocessingStepParamPrefix, stepIdx)];
				if (stepStr == null)
					break;
				if (PreprocessingHistoryItem.TryParse(stepStr, out var step))
					yield return step;
			}
		}

		IPreprocessingStep CreateStepByName(string name, PreprocessingStepParams param)
		{
			return
				extensions
				.Items
				.Select(e => e.CreateStepByName(name, param))
				.FirstOrDefault(s => s != null);
		}

		class SharedValueRecord
		{
			public string key;
			public IDisposable value;
			public int useCounter;
			public TimeSpan ttl;
			public Task cleanupTask;
			public int cleanupId;
		};

		class SharedValueLease<T> : ISharedValueLease<T> where T : IDisposable
		{
			readonly Dictionary<string, SharedValueRecord> sharedValues;
			readonly object syncRoot;
			readonly SharedValueRecord record;
			readonly bool isValueCreator;
			bool isDisposed;

			public SharedValueLease(Dictionary<string, SharedValueRecord> sharedValues, object syncRoot, string key, Func<T> valueFactory)
			{
				this.sharedValues = sharedValues;
				this.syncRoot = syncRoot;
				lock (syncRoot)
				{
					if (!sharedValues.TryGetValue(key, out record))
					{
						sharedValues.Add(key, record = new SharedValueRecord() { key = key, value = valueFactory() });
						isValueCreator = true;
					}
					record.useCounter++;
					record.cleanupTask = null; // cancel cleanup if it happends to be scheduled
				}
			}

			T ISharedValueLease<T>.Value
			{
				get { return (T)record.value; }
			}

			bool ISharedValueLease<T>.IsValueCreator
			{
				get { return isValueCreator; }
			}

			void ISharedValueLease<T>.KeepAlive(TimeSpan ttl)
			{
				lock (syncRoot)
				{
					if (isDisposed)
						throw new ObjectDisposedException("SharedValueLease");
					record.ttl = ttl;
				}
			}

			void IDisposable.Dispose()
			{
				lock (syncRoot)
				{
					if (isDisposed)
						return;
					isDisposed = true;
					if (--record.useCounter == 0)
					{
						var cleanupId = ++record.cleanupId;
						record.cleanupTask = Task.Run(async () =>
						{
							await Task.Delay(record.ttl);
							lock (syncRoot)
							{
								if (record.cleanupTask != null && record.cleanupId == cleanupId)
								{
									sharedValues.Remove(record.key);
									record.value.Dispose();
								}
							}
						});
					}
				}
			}
		};

		readonly ISynchronizationContext invokeSynchronize;
		readonly IChangeNotification changeNotification;
		readonly IFormatAutodetect formatAutodetect;
		readonly Action<YieldedProvider> providerYieldedCallback;
		readonly IExtensionsRegistry extensions;
		ImmutableList<ILogSourcePreprocessing> items = ImmutableList.Create<ILogSourcePreprocessing>();
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly LJTraceSource trace;
		readonly ITempFilesManager tempFilesManager;
		readonly ILogSourcesManager logSourcesManager;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly Dictionary<string, SharedValueRecord> sharedValues = new Dictionary<string, SharedValueRecord>(); // todo: move to separate class
		int lastPreprocId;

		#endregion
	};
}
