using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{
	public class LogSourcesPreprocessingManager
	{
		#region Public interface

		public LogSourcesPreprocessingManager(
			IInvokeSynchronization invokeSynchronize,
			IFormatAutodetect formatAutodetect,
			Action<YieldedProvider> providerYielded)
		{
			Trace = LJTraceSource.EmptyTracer;
			this.invokeSynchronize = invokeSynchronize;
			this.formatAutodetect = formatAutodetect;
			this.providerYieldedCallback = providerYielded;
		}

		public struct YieldedProvider
		{
			public ILogProviderFactory Factory;
			public IConnectionParams ConnectionParams;
			public string DisplayName;
		};

		public LJTraceSource Trace { get; set; }

		/// <summary>
		/// Raised when new preprocessing object added to LogSourcesPreprocessingManager.
		/// That usually happens when one calls Preprocess().
		/// </summary>
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingAdded;
		/// <summary>
		/// Raised when preprocessing object gets disposed and deleted from LogSourcesPreprocessingManager.
		/// Preprocessing object deletes itself automatically when it finishes. 
		/// This event is called throught IInvokeSynchronization passed to 
		/// LogSourcesPreprocessingManager's constructor.
		/// </summary>
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingDisposed;
		/// <summary>
		/// Raised when properties of one of ILogSourcePreprocessing objects changed. 
		/// Note: This event is raised in worker thread.
		/// That's for optimization purposes: PreprocessingChangedAsync can be raised very often and we we din't 
		/// want invocation queue to be spammed.
		/// </summary>
		public event EventHandler<LogSourcePreprocessingEventArg> PreprocessingChangedAsync;

		public void Preprocess(
			IEnumerable<IPreprocessingStep> steps, 
			IPreprocessingUserRequests userRequests)
		{
			ExecutePreprocessing(new LogSourcePreprocessing(this, userRequests, providerYieldedCallback, steps));
		}

		public void Preprocess(
			RecentLogEntry recentLogEntry,
			IPreprocessingUserRequests userRequests)
		{
			ExecutePreprocessing(new LogSourcePreprocessing(this, userRequests, providerYieldedCallback, recentLogEntry));
		}

		public IEnumerable<ILogSourcePreprocessing> Items
		{
			get { return items; }
		}

		#endregion

		class LogSourcePreprocessing : IPreprocessingStepCallback, ILogSourcePreprocessing, IPreprocessingUserRequests
		{
			public LogSourcePreprocessing(
				LogSourcesPreprocessingManager owner, 
				IPreprocessingUserRequests userRequests,
				Action<YieldedProvider> providerYieldedCallback,
				IEnumerable<IPreprocessingStep> initialSteps):
				this(owner, userRequests, providerYieldedCallback)
			{
				threadLogic = () =>
				{
					Queue<IPreprocessingStep> steps = new Queue<IPreprocessingStep>();
					foreach (var initialStep in initialSteps)
					{
						steps.Enqueue(initialStep);
					}
					for (; steps.Count > 0; )
					{
						if (IsCancellationRequested)
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
				Action<YieldedProvider> providerYieldedCallback,
				RecentLogEntry recentLogEntry):
				this(owner, userRequests, providerYieldedCallback)
			{
				threadLogic = () =>
				{
					IConnectionParams preprocessedConnectParams = null;
					IFileBasedLogProviderFactory fileBasedFactory = recentLogEntry.Factory as IFileBasedLogProviderFactory;
					if (fileBasedFactory != null)
					{
						PreprocessingStepParams currentParams = null;
						foreach (var loadedStep in LoadSteps(recentLogEntry.ConnectionParams))
						{
							currentParams = ProcessLoadedStep(loadedStep, currentParams);
							if (currentParams == null)
								throw new Exception(string.Format("Preprocessing failed on step '{0} {1}'", loadedStep.Action, loadedStep.Param));
							currentDescription = genericProcessingDescription;
						}
						if (currentParams != null)
						{
							preprocessedConnectParams = fileBasedFactory.CreateParams(currentParams.Uri);
							Utils.DumpPreprocessingParamsToConnectionParams(currentParams, preprocessedConnectParams);
						}
					}
					YieldLogProvider(recentLogEntry.Factory, preprocessedConnectParams ?? recentLogEntry.ConnectionParams, "");
				};
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
				}
				catch (Exception e)
				{
					preprocessingFailed = true;
					trace.Error(e, "Preprocessing failed");
					failure = e;
				}

				bool loadYieldedProviders = !IsCancellationRequested;
				bool keepTaskAlive = preprocessingFailed && !IsCancellationRequested;
				owner.invokeSynchronize.BeginInvoke((Action<bool, bool>)FinishPreprocessing, 
					new object[] { loadYieldedProviders, keepTaskAlive });
				
				finishedEvt.Set();
			}


			PreprocessingStepParams ProcessLoadedStep(LoadedPreprocessingStep loadedStep, PreprocessingStepParams currentParams)
			{
				switch (loadedStep.Action)
				{
					case "get":
						return new PreprocessingStepParams(loadedStep.Param);
					case "download":
						return (new DownloadingStep(currentParams)).ExecuteLoadedStep(this, loadedStep.Param);
					case "unzip":
						return (new UnpackingStep(currentParams)).ExecuteLoadedStep(this, loadedStep.Param);
					default:
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
				IEnumerable<YieldedProvider> providersToYield;
				if (yieldedProviders.Count > 1)
				{
					var userSelection = userRequests.SelectItems("Select logs to load",
						yieldedProviders.Select(p => string.Format(
							"{1}\\{2}: {0}", p.DisplayName, p.Factory.CompanyName, p.Factory.FormatName)).ToArray());
					providersToYield = yieldedProviders.Zip(Enumerable.Range(0, yieldedProviders.Count),
						(p, i) => userSelection[i] ? p : new YieldedProvider()).Where(p => p.Factory != null);
				}
				else
				{
					providersToYield = yieldedProviders;
				}
				foreach (var provider in providersToYield)
				{
					try
					{
						providerYieldedCallback(provider);
					}
					catch (Exception e)
					{
						trace.Error(e, "Failed to load from {0} from {1}", provider.Factory.FormatName, provider.ConnectionParams);
					}
				}
			}

			void FirePreprocessingChanged()
			{
				if (owner.PreprocessingChangedAsync != null)
					owner.PreprocessingChangedAsync(owner, new LogSourcePreprocessingEventArg(this));
			}

			public void YieldLogProvider(ILogProviderFactory providerFactory, IConnectionParams providerConnectionParams, string displayName)
			{
				providerConnectionParams = RemoveTheOnlyGetPreprocessingStep(providerConnectionParams);
				yieldedProviders.Add(new YieldedProvider() { Factory = providerFactory, ConnectionParams = providerConnectionParams, DisplayName = displayName });
			}

			public void BecomeLongRunning()
			{
				becomeLongRunningEvt.Set();
				trace.Info("Preprocessing is now long running");
			}

			public bool IsCancellationRequested
			{
				get { return disposed; }
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

			public WaitHandle CancellationEvent
			{
				get { return cancelledEvt; }
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
					cancelledEvt.Set();
					trace.Info("Waiting thread");
					thread.Join();
					trace.Info("Thread finished");

					owner.Remove(this);

					cancelledEvt.Dispose();
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
				var steps = LoadSteps(providerConnectionParams).ToArray();
				if (steps.Length == 1 && steps[0].Action == "get")
				{
					providerConnectionParams = providerConnectionParams.Clone();
					providerConnectionParams["prep-step0"] = null;
				}
				return providerConnectionParams;
			}

			void CheckIsLongRunning()
			{
				if (!IsLongRunning)
					throw new InvalidOperationException("Preprocessing must be long-running to perform this operation");
			}

			public NetworkCredential QueryCredentials(Uri site, string authType)
			{
				CheckIsLongRunning();
				return owner.invokeSynchronize.Invoke(
					(Func<NetworkCredential>)(() => userRequests.QueryCredentials(site, authType)), new object[] { }) as NetworkCredential;
			}

			public void InvalidCredentials(Uri site, string authType)
			{
				CheckIsLongRunning();
				owner.invokeSynchronize.Invoke(
					(Action)(() => userRequests.InvalidCredentials(site, authType)), new object[] { });
			}

			public bool[] SelectItems(string prompt, string[] items)
			{
				CheckIsLongRunning();
				return owner.invokeSynchronize.Invoke(
					(Func<bool[]>)(() => userRequests.SelectItems(prompt, items)), new object[] { }) as bool[];
			}

			bool disposed;
			readonly LogSourcesPreprocessingManager owner;
			public Action<YieldedProvider> providerYieldedCallback;
			readonly LJTraceSource trace;
			readonly Thread thread;
			readonly IPreprocessingUserRequests userRequests;
			readonly IFormatAutodetect formatAutodetect;
			readonly ITempFilesManager tempFiles;
			readonly ManualResetEvent finishedEvt = new ManualResetEvent(false);
			readonly ManualResetEvent becomeLongRunningEvt = new ManualResetEvent(false);
			readonly ManualResetEvent cancelledEvt = new ManualResetEvent(false);
			readonly List<YieldedProvider> yieldedProviders = new List<YieldedProvider>();
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

		#region Implementation

		void ExecutePreprocessing(LogSourcePreprocessing prep)
		{
			if (prep.Execute())
				return;
			items.Add(prep);
			if (PreprocessingAdded != null)
				PreprocessingAdded(this, new LogSourcePreprocessingEventArg(prep));
		}

		internal void Remove(ILogSourcePreprocessing prep)
		{
			items.Remove(prep);
			if (PreprocessingDisposed != null)
				PreprocessingDisposed(this, new LogSourcePreprocessingEventArg(prep));
		}

		static IEnumerable<LoadedPreprocessingStep> LoadSteps(IConnectionParams connectParams)
		{
			for (int stepIdx = 0; ; ++stepIdx)
			{
				string stepStr = connectParams[string.Format("prep-step{0}", stepIdx)];
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

		readonly IInvokeSynchronization invokeSynchronize;
		readonly IFormatAutodetect formatAutodetect;
		readonly Action<YieldedProvider> providerYieldedCallback;
		readonly List<ILogSourcePreprocessing> items = new List<ILogSourcePreprocessing>();

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
