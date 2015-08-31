using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.MRU;

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
			Trace = LJTraceSource.EmptyTracer;
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


		public LJTraceSource Trace { get; set; }

		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingAdded;
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingDisposed;
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingChangedAsync;
		public event EventHandler<YieldedProvider> ProviderYielded;

		Task ILogSourcesPreprocessingManager.Preprocess(
			IEnumerable<IPreprocessingStep> steps,
			string preprocessingDisplayName,
			PreprocessingOptions options)
		{
			return ExecutePreprocessing(new LogSourcePreprocessing(this, userRequests, providerYieldedCallback, steps, preprocessingDisplayName, stepsFactory, options));
		}

		Task ILogSourcesPreprocessingManager.Preprocess(
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

		class LogSourcePreprocessing : IPreprocessingStepCallback, ILogSourcePreprocessing, IPreprocessingUserRequests
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
				threadLogic = () =>
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
						foreach (var nextStep in currentStep.Execute(this))
							steps.Enqueue(nextStep);
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
				threadLogic = () =>
				{
					IConnectionParams preprocessedConnectParams = null;
					IFileBasedLogProviderFactory fileBasedFactory = recentLogEntry.Factory as IFileBasedLogProviderFactory;
					if (fileBasedFactory != null)
					{
						PreprocessingStepParams currentParams = null;
						foreach (var loadedStep in LoadStepsFromConnectionParams(recentLogEntry.ConnectionParams))
						{
							currentParams = ProcessLoadedStep(loadedStep, currentParams);
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
					((IPreprocessingStepCallback)this).
						YieldLogProvider(recentLogEntry.Factory, preprocessedConnectParams ?? recentLogEntry.ConnectionParams, "", makeHiddenLog);
				};
			}

			public Task Task
			{
				get { return taskSource.Task; }
			}

			public bool Execute()
			{
				thread.Start();
				WaitHandle[] handles = new WaitHandle[] { finishedEvt, becomeLongRunningEvt };
				int idx = WaitHandle.WaitAny(handles);
				if (idx == 0)
					return true;
				return false;
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
				this.trace = owner.Trace;
				this.userRequests = userRequests;
				this.thread = new Thread(ThreadProc);
			}

			void ThreadProc()
			{
				bool preprocessingFailed = false;
				try
				{
					threadLogic();
					taskSource.SetResult(1);
				}
				catch (Exception e)
				{
					preprocessingFailed = true;
					trace.Error(e, "Preprocessing failed");
					failure = e;
					taskSource.SetException(e);

					// this "observes" task exception so that user code does not have to care
					var observedTaskException = taskSource.Task.Exception;

					owner.telemetry.ReportException(observedTaskException, "preprocessing failed");
				}

				bool loadYieldedProviders = !cancellation.IsCancellationRequested;
				bool keepTaskAlive = preprocessingFailed && !cancellation.IsCancellationRequested;
				owner.invokeSynchronize.BeginInvoke((Action<bool, bool>)FinishPreprocessing, 
					new object[] { loadYieldedProviders, keepTaskAlive });
				
				finishedEvt.Set();
			}


			PreprocessingStepParams ProcessLoadedStep(LoadedPreprocessingStep loadedStep, PreprocessingStepParams currentParams)
			{
				switch (loadedStep.Action)
				{
					case PreprocessingStepParams.DefaultStepName:
						return new PreprocessingStepParams(loadedStep.Param);
					case DownloadingStep.name:
						return stepsFactory.CreateDownloadingStep(currentParams).ExecuteLoadedStep(this, loadedStep.Param);
					case UnpackingStep.name:
						return stepsFactory.CreateUnpackingStep(currentParams).ExecuteLoadedStep(this, loadedStep.Param);
					case GunzippingStep.name:
						return stepsFactory.CreateGunzippingStep(currentParams).ExecuteLoadedStep(this, loadedStep.Param);
					default:
						var step = 
							owner
							.extensions
							.Items
							.Select(e => e.CreateStepByName(loadedStep.Action, currentParams))
							.FirstOrDefault(s => s != null);
						if (step != null)
							return step.ExecuteLoadedStep(this, loadedStep.Param);
						return null;
				}
			}

			void FinishPreprocessing(bool loadYieldedProviders, bool keepTaskAlive)
			{
				if (loadYieldedProviders)
				{
					trace.Info("Loading yielded providers");
					LoadYieldedProviders();
				}

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

			void LoadYieldedProviders()
			{
				childPreprocessings.ForEach(
					logEntry => ((ILogSourcesPreprocessingManager)owner).Preprocess(logEntry.Param, logEntry.MakeHiddenLog));

				IEnumerable<YieldedProvider> providersToYield;
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
						(p, i) => selection[i] ? p : new YieldedProvider()).Where(p => p.Factory != null);
				}
				else
				{
					providersToYield = yieldedProviders;
					if (yieldedProviders.Count == 0 && failure == null && childPreprocessings.Count == 0)
					{
						userRequests.NotifyUserAboutIneffectivePreprocessing(displayName);
					}
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
			}

			void FirePreprocessingChanged()
			{
				if (owner.PreprocessingChangedAsync != null)
					owner.PreprocessingChangedAsync(owner, new LogSourcePreprocessingEventArg(this));
			}

			void IPreprocessingStepCallback.YieldLogProvider(ILogProviderFactory providerFactory, IConnectionParams providerConnectionParams, string displayName, bool makeHiddenLog)
			{
				providerConnectionParams = RemoveTheOnlyGetPreprocessingStep(providerConnectionParams);
				yieldedProviders.Add(new YieldedProvider() { Factory = providerFactory, ConnectionParams = providerConnectionParams, DisplayName = displayName, IsHiddenLog = makeHiddenLog });
			}

			void IPreprocessingStepCallback.YieldChildPreprocessing(RecentLogEntry recentLogEntry, bool isHiddenLog)
			{
				childPreprocessings.Add(new ChildPreprocessingParams() { Param = recentLogEntry, MakeHiddenLog = isHiddenLog } );
			}

			IPreprocessingStepsFactory IPreprocessingStepCallback.PreprocessingStepsFactory
			{
				get { return owner.stepsFactory; }
			}

			public void BecomeLongRunning()
			{
				becomeLongRunningEvt.Set();
				trace.Info("Preprocessing is now long running");
			}

			CancellationToken IPreprocessingStepCallback.Cancellation
			{
				get { return cancellation.Token; }
			}


			public Exception Failure
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
				get { return owner.Trace; }
			}

			public IPreprocessingUserRequests UserRequests
			{
				get { return this; }
			}

			public string CurrentStepDescription
			{
				get { return currentDescription; }
			}

			public bool IsDisposed
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
					trace.Info("Waiting thread");
					thread.Join();
					trace.Info("Thread finished");

					owner.Remove(this);

					cancellation.Dispose();
					finishedEvt.Dispose();
					becomeLongRunningEvt.Dispose();
				}
			}

			bool IsLongRunning
			{
				get { return becomeLongRunningEvt.WaitOne(0); }
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

			void CheckIsLongRunning()
			{
				if (!IsLongRunning)
					throw new InvalidOperationException("Preprocessing must be long-running to perform this operation");
			}

			NetworkCredential IPreprocessingUserRequests.QueryCredentials(Uri site, string authType)
			{
				CheckIsLongRunning();
				return owner.invokeSynchronize.Invoke(
					(Func<NetworkCredential>)(() => userRequests.QueryCredentials(site, authType)), new object[] { }) as NetworkCredential;
			}

			void IPreprocessingUserRequests.NotifyUserAboutIneffectivePreprocessing(string notificationSource)
			{
				owner.invokeSynchronize.Invoke(
					(Action)(() => userRequests.NotifyUserAboutIneffectivePreprocessing(notificationSource)), new object[] { });
			}

			void IPreprocessingUserRequests.NotifyUserAboutPreprocessingFailure(string notificationSource, string message)
			{
				owner.invokeSynchronize.Invoke(
					(Action)(() => userRequests.NotifyUserAboutPreprocessingFailure(notificationSource, message)), new object[] { });
			}

			void IPreprocessingUserRequests.InvalidateCredentialsCache(Uri site, string authType)
			{
				CheckIsLongRunning();
				owner.invokeSynchronize.Invoke(
					(Action)(() => userRequests.InvalidateCredentialsCache(site, authType)), new object[] { });
			}

			bool[] IPreprocessingUserRequests.SelectItems(string prompt, string[] items)
			{
				CheckIsLongRunning();
				return owner.invokeSynchronize.Invoke(
					(Func<bool[]>)(() => userRequests.SelectItems(prompt, items)), new object[] { }) as bool[];
			}

			bool disposed;
			readonly LogSourcesPreprocessingManager owner;
			readonly IPreprocessingStepsFactory stepsFactory;
			public Action<YieldedProvider> providerYieldedCallback;
			readonly LJTraceSource trace;
			readonly Thread thread;
			readonly IPreprocessingUserRequests userRequests;
			readonly IFormatAutodetect formatAutodetect;
			readonly ITempFilesManager tempFiles;
			readonly ManualResetEvent finishedEvt = new ManualResetEvent(false);
			readonly ManualResetEvent becomeLongRunningEvt = new ManualResetEvent(false);
			readonly CancellationTokenSource cancellation = new CancellationTokenSource();
			readonly List<YieldedProvider> yieldedProviders = new List<YieldedProvider>();
			readonly List<ChildPreprocessingParams> childPreprocessings = new List<ChildPreprocessingParams>();
			readonly string displayName;
			readonly PreprocessingOptions options;
			readonly TaskCompletionSource<int> taskSource = new TaskCompletionSource<int>();
			string currentDescription = "";
			Exception failure;
			Action threadLogic;

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

		Task ExecutePreprocessing(LogSourcePreprocessing prep)
		{
			if (prep.Execute())
				return prep.Task;
			items.Add(prep);
			if (PreprocessingAdded != null)
				PreprocessingAdded(this, new LogSourcePreprocessingEventArg(prep));
			return prep.Task;
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
