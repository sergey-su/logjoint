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

namespace LogJoint.Preprocessing
{
	public class LogSourcesPreprocessingManager : ILogSourcesPreprocessingManager
	{
		#region Public interface

		public LogSourcesPreprocessingManager(
			IInvokeSynchronization invokeSynchronize,
			IFormatAutodetect formatAutodetect,
			IPreprocessingStepsFactory stepsFactory,
			IPreprocessingManagerExtensionsRegistry extensions,
			Telemetry.ITelemetryCollector telemetry)
		{
			this.trace = new LJTraceSource("PreprocessingManager", "prepr");
			this.invokeSynchronize = invokeSynchronize;
			this.formatAutodetect = formatAutodetect;
			this.providerYieldedCallback = prov =>
			{
				if (ProviderYielded != null)
					ProviderYielded(this, prov);
			};
			this.stepsFactory = stepsFactory;
			this.extensions = extensions;
			this.telemetry = telemetry;
		}


		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingAdded;
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingDisposed;
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingChangedAsync;
		public event EventHandler<YieldedProvider> ProviderYielded;

		Task<YieldedProvider[]> ILogSourcesPreprocessingManager.Preprocess(
			IEnumerable<IPreprocessingStep> steps,
			string preprocessingDisplayName,
			PreprocessingOptions options)
		{
			return ExecutePreprocessing(new LogSourcePreprocessing(this, userRequests, providerYieldedCallback, steps, preprocessingDisplayName, stepsFactory, options));
		}

		Task<YieldedProvider[]> ILogSourcesPreprocessingManager.Preprocess(
			RecentLogEntry recentLogEntry,
			bool makeHiddenLog)
		{
			return ExecutePreprocessing(new LogSourcePreprocessing(this, userRequests, stepsFactory, providerYieldedCallback, recentLogEntry, makeHiddenLog));
		}

		public IEnumerable<ILogSourcePreprocessing> Items
		{
			get { return items; }
		}

		void ILogSourcesPreprocessingManager.SetUserRequestsHandler(IPreprocessingUserRequests userRequests)
		{
			this.userRequests = userRequests;
		}

		bool ILogSourcesPreprocessingManager.ConnectionRequiresDownloadPreprocessing(IConnectionParams connectParams)
		{
			foreach (var step in LoadStepsFromConnectionParams(connectParams))
			{
				if (step.Action == DownloadingStep.name || extensions.Items.Any(e => e.IsDownloadingStep(step.Action)))
					return true;
			}
			return false;
		}

		#endregion

		class LogSourcePreprocessing : IPreprocessingStepCallback, ILogSourcePreprocessing
		{
			public LogSourcePreprocessing(
				LogSourcesPreprocessingManager owner, 
				IPreprocessingUserRequests userRequests,
				Action<YieldedProvider> providerYieldedCallback,
				IEnumerable<IPreprocessingStep> initialSteps,
				string preprocessingDisplayName,
				IPreprocessingStepsFactory stepsFactory,
				PreprocessingOptions options) :
				this(owner, userRequests, providerYieldedCallback)
			{
				this.displayName = preprocessingDisplayName;
				this.stepsFactory = stepsFactory;
				this.options = options;
				preprocLogic = async () =>
				{
					Queue<IPreprocessingStep> steps = new Queue<IPreprocessingStep>();
					foreach (var initialStep in initialSteps)
					{
						steps.Enqueue(initialStep);
					}
					for (; steps.Count > 0; )
					{
						if (cancellation.IsCancellationRequested)
							break;
						IPreprocessingStep currentStep = steps.Dequeue();
						nextSteps = steps;
						await currentStep.Execute(this).ConfigureAwait(continueOnCapturedContext: !isLongRunning);
						nextSteps = null;
						currentDescription = genericProcessingDescription;
					}
				};
			}

			public LogSourcePreprocessing(
				LogSourcesPreprocessingManager owner,
				IPreprocessingUserRequests userRequests,
				IPreprocessingStepsFactory stepsFactory,
				Action<YieldedProvider> providerYieldedCallback,
				RecentLogEntry recentLogEntry,
				bool makeHiddenLog
			):
				this(owner, userRequests, providerYieldedCallback)
			{
				this.stepsFactory = stepsFactory;
				preprocLogic = async () =>
				{
					IConnectionParams preprocessedConnectParams = null;
					IFileBasedLogProviderFactory fileBasedFactory = recentLogEntry.Factory as IFileBasedLogProviderFactory;
					if (fileBasedFactory != null)
					{
						PreprocessingStepParams currentParams = null;
						foreach (var loadedStep in LoadStepsFromConnectionParams(recentLogEntry.ConnectionParams))
						{
							currentParams = await ProcessLoadedStep(loadedStep, currentParams);
							if (currentParams == null)
								throw new Exception(string.Format("Preprocessing failed on step '{0} {1}'", loadedStep.Action, loadedStep.Param));
							currentDescription = genericProcessingDescription;
						}
						if (currentParams != null)
						{
							preprocessedConnectParams = fileBasedFactory.CreateParams(currentParams.Uri);
							currentParams.DumpToConnectionParams(preprocessedConnectParams);
						}
					}
					var provider = new YieldedProvider(recentLogEntry.Factory, preprocessedConnectParams ?? recentLogEntry.ConnectionParams, "", makeHiddenLog);
					((IPreprocessingStepCallback)this).YieldLogProvider(provider);
				};
			}

			LogSourcePreprocessing(
				LogSourcesPreprocessingManager owner,
				IPreprocessingUserRequests userRequests,
				Action<YieldedProvider> providerYieldedCallback)
			{
				this.owner = owner;
				this.providerYieldedCallback = providerYieldedCallback;
				// clone formatAutodetect to avoid multithreaded access to the same object from concurrent LogSourcePreprocessing objects
				this.formatAutodetect = owner.formatAutodetect.Clone();
				this.tempFiles = LogJoint.TempFilesManager.GetInstance();
				this.trace = owner.trace;
				this.userRequests = userRequests;
			}

			public Task<YieldedProvider[]> Execute()
			{
				Func<Task<YieldedProvider[]>> helper = async () =>
				{
					await preprocLogic();
					LoadChildPreprocessings();
					return await owner.invokeSynchronize.Invoke<YieldedProvider[]>(LoadYieldedProviders);
				};
				var innerTask = helper();
				this.task = innerTask.ContinueWith(t =>
				{
					var e = t.Exception;
					if (t.Exception != null)
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
					ScheduleFinishPreprocessing(keepTaskAlive: t.Exception != null && !cancellation.IsCancellationRequested);
				});
				return innerTask;
			}

			private void LoadChildPreprocessings()
			{
				childPreprocessings.ForEach(
					logEntry => ((ILogSourcesPreprocessingManager)owner).Preprocess(logEntry.Param, logEntry.MakeHiddenLog));
			}


			async Task<PreprocessingStepParams> ProcessLoadedStep(LoadedPreprocessingStep loadedStep, PreprocessingStepParams currentParams)
			{
				switch (loadedStep.Action)
				{
					case PreprocessingStepParams.DefaultStepName:
						return new PreprocessingStepParams(loadedStep.Param);
					case DownloadingStep.name:
						return await stepsFactory.CreateDownloadingStep(currentParams).ExecuteLoadedStep(this, loadedStep.Param);
					case UnpackingStep.name:
						return await stepsFactory.CreateUnpackingStep(currentParams).ExecuteLoadedStep(this, loadedStep.Param);
					case GunzippingStep.name:
						return await stepsFactory.CreateGunzippingStep(currentParams).ExecuteLoadedStep(this, loadedStep.Param);
					default:
						var step = 
							owner
							.extensions
							.Items
							.Select(e => e.CreateStepByName(loadedStep.Action, currentParams))
							.FirstOrDefault(s => s != null);
						if (step != null)
							return await step.ExecuteLoadedStep(this, loadedStep.Param);
						return null;
				}
			}

			void ScheduleFinishPreprocessing(bool keepTaskAlive)
			{
				owner.invokeSynchronize.BeginInvoke((Action<bool>)FinishPreprocessing,
					new object[] { keepTaskAlive });
			}

			void FinishPreprocessing(bool keepTaskAlive)
			{
				if (!keepTaskAlive)
				{
					trace.Info("Disposing");
					Dispose();
				}
				else
				{
					trace.Info("Preprocessing failed and user didn't cancel it. Leaving it to allow user see the problem");
					FirePreprocessingChanged();
				}
			}

			YieldedProvider[] LoadYieldedProviders() // this method is run in model thread
			{
				trace.Info("Loading yielded providers");
				YieldedProvider[] providersToYield;
				if (yieldedProviders.Count > 1)
				{
					bool[] selection;
					if ((options & PreprocessingOptions.SkipLogsSelectionDialog) != 0)
						selection = Enumerable.Repeat(true, yieldedProviders.Count).ToArray();
					else
						selection = userRequests.SelectItems("Select logs to load",
							yieldedProviders.Select(p => string.Format(
								"{1}\\{2}: {0}", p.DisplayName, p.Factory.CompanyName, p.Factory.FormatName)).ToArray());
					providersToYield = yieldedProviders.Zip(Enumerable.Range(0, yieldedProviders.Count),
						(p, i) => selection[i] ? p : new YieldedProvider()).Where(p => p.Factory != null).ToArray();
				}
				else
				{
					providersToYield = yieldedProviders.ToArray();
					if (yieldedProviders.Count == 0 && failure == null && childPreprocessings.Count == 0)
						userRequests.NotifyUserAboutIneffectivePreprocessing(displayName);
				}
				var failedProviders = new List<string>();
				foreach (var provider in providersToYield)
				{
					try
					{
						providerYieldedCallback(provider);
					}
					catch (Exception e)
					{
						failedProviders.Add(provider.Factory.GetUserFriendlyConnectionName(provider.ConnectionParams));
						trace.Error(e, "Failed to load from {0} from {1}", provider.Factory.FormatName, provider.ConnectionParams);
					}
				}
				if (failedProviders.Count > 0)
					userRequests.NotifyUserAboutPreprocessingFailure(displayName,
						"Failed to handle " + string.Join(", ", failedProviders));
				return providersToYield;
			}

			void FirePreprocessingChanged()
			{
				if (owner.PreprocessingChangedAsync != null)
					owner.PreprocessingChangedAsync(owner, new LogSourcePreprocessingEventArg(this));
			}

			void IPreprocessingStepCallback.YieldLogProvider(YieldedProvider provider)
			{
				provider.ConnectionParams = RemoveTheOnlyGetPreprocessingStep(provider.ConnectionParams);
				yieldedProviders.Add(provider);
			}

			void IPreprocessingStepCallback.YieldChildPreprocessing(RecentLogEntry recentLogEntry, bool isHiddenLog)
			{
				childPreprocessings.Add(new ChildPreprocessingParams() { Param = recentLogEntry, MakeHiddenLog = isHiddenLog } );
			}

			void IPreprocessingStepCallback.YieldNextStep(IPreprocessingStep step)
			{
				if (nextSteps != null)
					nextSteps.Enqueue(step);
				else
					; // todo: handle it somehow
			}


			IPreprocessingStepsFactory IPreprocessingStepCallback.PreprocessingStepsFactory
			{
				get { return owner.stepsFactory; }
			}

			ConfiguredTaskAwaitable IPreprocessingStepCallback.BecomeLongRunning()
			{
				trace.Info("Preprocessing is now long running");
				isLongRunning = true;
				return TaskUtils.SwitchToThreadpoolContext();
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

			public void SetStepDescription(string desc)
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
				get { return owner.trace; }
			}

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

			public void Dispose()
			{
				if (disposed)
					return;

				using (trace.NewFrame)
				{
					disposed = true;
					cancellation.Cancel();
					trace.Info("Waiting task");
					task.Wait();
					trace.Info("Task finished");

					owner.Remove(this);

					cancellation.Dispose();
				}
			}

			static IConnectionParams RemoveTheOnlyGetPreprocessingStep(IConnectionParams providerConnectionParams)
			{
				var steps = LoadStepsFromConnectionParams(providerConnectionParams).ToArray();
				if (steps.Length == 1 && steps[0].Action == PreprocessingStepParams.DefaultStepName)
				{
					providerConnectionParams = providerConnectionParams.Clone();
					providerConnectionParams[ConnectionParamsUtils.PreprocessingStepParamPrefix + "0"] = null;
				}
				return providerConnectionParams;
			}

			bool disposed;
			readonly LogSourcesPreprocessingManager owner;
			readonly IPreprocessingStepsFactory stepsFactory;
			public Action<YieldedProvider> providerYieldedCallback;
			readonly LJTraceSource trace;
			readonly IPreprocessingUserRequests userRequests;
			readonly IFormatAutodetect formatAutodetect;
			readonly ITempFilesManager tempFiles;
			readonly CancellationTokenSource cancellation = new CancellationTokenSource();
			readonly List<YieldedProvider> yieldedProviders = new List<YieldedProvider>();
			readonly List<ChildPreprocessingParams> childPreprocessings = new List<ChildPreprocessingParams>();
			readonly string displayName;
			readonly PreprocessingOptions options;
			string currentDescription = "";
			bool isLongRunning;
			Exception failure;
			Func<Task> preprocLogic;
			Task task; // this task never fails
			Queue<IPreprocessingStep> nextSteps;

			static readonly string genericProcessingDescription = "Processing...";
		};

		struct LoadedPreprocessingStep
		{
			public readonly string Action;
			public readonly string Param;
			public LoadedPreprocessingStep(string action, string param)
			{
				Action = action;
				Param = param;
			}
		};

		struct ChildPreprocessingParams
		{
			public RecentLogEntry Param;
			public bool MakeHiddenLog;
		};

		#region Implementation

		Task<YieldedProvider[]> ExecutePreprocessing(LogSourcePreprocessing prep)
		{
			var ret = prep.Execute();
			if (ret.IsCompleted)
				return ret;
			items.Add(prep);
			if (PreprocessingAdded != null)
				PreprocessingAdded(this, new LogSourcePreprocessingEventArg(prep));
			return ret;
		}

		internal void Remove(ILogSourcePreprocessing prep)
		{
			items.Remove(prep);
			if (PreprocessingDisposed != null)
				PreprocessingDisposed(this, new LogSourcePreprocessingEventArg(prep));
		}

		static IEnumerable<LoadedPreprocessingStep> LoadStepsFromConnectionParams(IConnectionParams connectParams)
		{
			for (int stepIdx = 0; ; ++stepIdx)
			{
				string stepStr = connectParams[string.Format("{0}{1}", ConnectionParamsUtils.PreprocessingStepParamPrefix, stepIdx)];
				if (stepStr == null)
					break;
				stepStr = stepStr.Trim();
				if (stepStr.Length == 0)
					break;
				int idx = stepStr.IndexOf(' ');
				if (idx == -1)
					yield return new LoadedPreprocessingStep(stepStr, "");
				else
					yield return new LoadedPreprocessingStep(stepStr.Substring(0, idx), stepStr.Substring(idx + 1));
			}
		}

		class SharedValueRecord
		{
			public string key;
			public IDisposable value;
			public int useCounter;
		};

		class SharedValueLease<T> : ISharedValueLease<T> where T : IDisposable
		{
			readonly Dictionary<string, SharedValueRecord> sharedValues;
			readonly object syncRoot;
			readonly SharedValueRecord record;
			readonly bool isValueCreator;

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

			void IDisposable.Dispose()
			{
				lock (syncRoot)
				{
					if (--record.useCounter == 0)
					{
						sharedValues.Remove(record.key);
						record.value.Dispose();
					}
				}
			}
		};

		readonly IInvokeSynchronization invokeSynchronize;
		readonly IFormatAutodetect formatAutodetect;
		readonly Action<YieldedProvider> providerYieldedCallback;
		readonly IPreprocessingStepsFactory stepsFactory;
		readonly IPreprocessingManagerExtensionsRegistry extensions;
		readonly List<ILogSourcePreprocessing> items = new List<ILogSourcePreprocessing>();
		readonly Telemetry.ITelemetryCollector telemetry;
		readonly LJTraceSource trace;
		IPreprocessingUserRequests userRequests;
		readonly Dictionary<string, SharedValueRecord> sharedValues = new Dictionary<string, SharedValueRecord>();

		#endregion
	};

	public class LogSourcePreprocessingEventArg : EventArgs
	{
		public ILogSourcePreprocessing LogSourcePreprocessing { get { return lsp; } }

		public LogSourcePreprocessingEventArg(ILogSourcePreprocessing lsp)
		{
			this.lsp = lsp;
		}

		ILogSourcePreprocessing lsp;
	};
}
