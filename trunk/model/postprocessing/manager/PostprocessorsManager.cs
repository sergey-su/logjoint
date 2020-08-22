using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
	class PostprocessorsManager : IManagerInternal
	{
		public PostprocessorsManager(
			ILogSourcesManager logSources,
			Telemetry.ITelemetryCollector telemetry,
			ISynchronizationContext modelSyncContext,
			ISynchronizationContext threadPoolSyncContext,
			IHeartBeatTimer heartbeat,
			Progress.IProgressAggregator progressAggregator,
			Settings.IGlobalSettingsAccessor settingsAccessor,
			IOutputDataDeserializer outputDataDeserializer,
			ITraceSourceFactory traceSourceFactory,
			ILogPartTokenFactories logPartTokenFactories,
			Correlation.ISameNodeDetectionTokenFactories sameNodeDetectionTokenFactories,
			IChangeNotification changeNotification,
			LogMedia.IFileSystem logFileSystem
		)
		{
			this.logSources = logSources;
			this.telemetry = telemetry;
			this.progressAggregator = progressAggregator;
			this.settingsAccessor = settingsAccessor;
			this.modelSyncContext = modelSyncContext;
			this.threadPoolSyncContext = threadPoolSyncContext;
			this.heartbeat = heartbeat;
			this.outputDataDeserializer = outputDataDeserializer;
			this.logPartTokenFactories = logPartTokenFactories;
			this.sameNodeDetectionTokenFactories = sameNodeDetectionTokenFactories;
			this.changeNotification = changeNotification;
			this.logFileSystem = logFileSystem;
			this.tracer = traceSourceFactory.CreateTraceSource("App", "ppm");
			this.updater = new AsyncInvokeHelper(modelSyncContext, Refresh);

			logSources.OnLogSourceAdded += (sender, args) => updater.Invoke();
			logSources.OnLogSourceRemoved += (sender, args) => updater.Invoke();
			logSources.OnLogSourceAnnotationChanged += (sender, args) => updater.Invoke();
			logSources.OnLogSourceStatsChanged += (object sender, LogSourceStatsEventArgs e) => 
			{
				if ((e.Flags & LogProviderStatsFlag.ContentsEtag) != 0)
					updater.Invoke();
			};

			this.visiblePostprocessorsOutputs = Selectors.Create(
				() => postprocessorsOutputs,
				() => logSources.Items,
				(outputs, sources) => {
					var sourcesMap = sources.ToLookup(s => s);
					return ImmutableArray.CreateRange(outputs.Where(output => sourcesMap.Contains(output.LogSource)));
				}
			);

			Refresh();
		}

		public event EventHandler Changed;

		IReadOnlyList<LogSourcePostprocessorState> IManagerInternal.LogSourcePostprocessors => visiblePostprocessorsOutputs();

		void IManager.Register(LogSourceMetadata meta)
		{
			knownLogTypes = this.knownLogTypes.Add(meta.LogProviderFactory, meta);
		}

		void IManager.RegisterLogType(LogSourceMetadata meta)
		{
			((IManager)this).Register(meta);
		}

		void IManager.Register(ILogPartTokenFactory logPartFactory)
		{
			logPartTokenFactories.Register(logPartFactory);
		}

		void IManager.Register(Correlation.ISameNodeDetectionTokenFactory factory)
		{
			sameNodeDetectionTokenFactories.Register(factory);
		}

		async Task IManagerInternal.RunPostprocessors(
			IReadOnlyList<LogSourcePostprocessorState> typesAndSources, 
			object customData)
		{
			var sources = typesAndSources.Select(typesAndSource =>
			{
				var outputType = typesAndSource.Postprocessor;
				var forLogSource = typesAndSource.LogSource;

				if (!knownLogSources.TryGetValue(forLogSource, out LogSourceRecord logSourceRecord))
					throw new ArgumentException("Log source is unknown");

				var postprocessorRecord = logSourceRecord.PostprocessorsOutputs.SingleOrDefault(parserRec => parserRec.metadata == outputType);
				if (postprocessorRecord == null)
					throw new ArgumentException("Bad Postprocessor output type: " + outputType.Kind.ToString());

				if (postprocessorRecord.state.PostprocessorNeedsRunning == null)
					throw new InvalidOperationException($"Can not start postprocessor in this state {postprocessorRecord.state.GetType()}");

				Task<System.IO.Stream> openOutputStream()
				{
					var section = ((Persistence.Implementation.IStorageEntryInternal)forLogSource.LogSourceSpecificStorageEntry).OpenRawXMLSection(
							outputType.MakePostprocessorOutputFileName(), Persistence.StorageSectionOpenFlag.ReadWrite, 0);
					return Task.FromResult<System.IO.Stream>(new DelegatingStream(section.Data, ownStream: false, dispose: section.Dispose));
				};

				bool needsProcessing = logSourceRecord.logSource.Visible && postprocessorRecord.state.PostprocessorNeedsRunning == true;

				var sourceContentsEtag = logSourceRecord.logSource.Provider.Stats.ContentsEtag?.ToString();

				return new
				{
					OutputType = outputType,
					PostprocessorInput = logSourceRecord.ToPostprocessorInput(
						openOutputStream, sourceContentsEtag, customData
					),
					PostprocessorRecord = postprocessorRecord,
					LogSourceMeta = logSourceRecord.metadata,
					LogSource = logSourceRecord.logSource,
					NeedsProcessing = needsProcessing,
					TemplatesTracker = new TemplatesTracker(),
					IsSelected = new Ref<bool>()
				};
			}).ToList();

			bool noLogNeedsProcessing = sources.All(s => !s.NeedsProcessing);
			if (noLogNeedsProcessing)
				sources.ForEach(s => s.IsSelected.Value = true);
			else
				sources.ForEach(s => s.IsSelected.Value = s.NeedsProcessing);

			var outerTasks = new List<Task>();

			foreach (var postprocessorTypeGroup in sources.Where(s => s.IsSelected.Value).GroupBy(s => s.OutputType))
			{
				var postprocessorProgress = progressAggregator.CreateChildAggregator();
				postprocessorProgress.ProgressChanged += (s, e) => FireChangedEvent();

				var innerTask = RunPostprocessorBody(async () =>
				{
					IPostprocessorRunSummary summary;
					using (var progressSinks = new ProgressSinksCollection())
					{
						summary = await postprocessorTypeGroup.Key.Run(
							postprocessorTypeGroup
							.Select(s => s
								.PostprocessorInput
								.AttachProgressHandler(postprocessorProgress, progressSinks.Sinks)
								.SetTemplatesTracker(s.TemplatesTracker)
							)
							.ToArray()
						);
					}
					foreach (var p in postprocessorTypeGroup)
					{
						telemetry.ReportUsedFeature(
							MakeLogSourcePostprocessorFeatureId(p.LogSourceMeta, postprocessorTypeGroup.Key),
							p.TemplatesTracker.GetUsedTemplates()
						);
					}
					return summary;
				});

				foreach (var postprocessorRecord in postprocessorTypeGroup.Select(s => s.PostprocessorRecord))
				{
					var flowCompletion = new TaskCompletionSource<int>();
					postprocessorRecord.SetState(new RunningState(postprocessorRecord.state.ctx, innerTask, postprocessorProgress, flowCompletion));
					outerTasks.Add(flowCompletion.Task);
				}
			}

			Refresh();

			await Task.WhenAll(outerTasks);

			await Task.Yield();
		}


		void Refresh()
		{
			bool somethingChanged = false;
			foreach (LogSourceRecord rec in knownLogSources.Values)
				rec.logSourceIsAlive = false;
			foreach (var src in EnumLogSourcesOfKnownTypes())
			{
				if (!knownLogSources.TryGetValue(src.Key, out LogSourceRecord rec))
				{
					rec = new LogSourceRecord(src.Key, src.Value, logFileSystem);
					foreach (var postprocessorType in rec.metadata.SupportedPostprocessors)
						rec.PostprocessorsOutputs.Add(new PostprocessorOutputRecord(
							postprocessorType, rec, updater.Invoke,
							FireChangedEvent, tracer,
							heartbeat, modelSyncContext, threadPoolSyncContext, telemetry, outputDataDeserializer));

					knownLogSources = knownLogSources.Add(src.Key, rec);
					somethingChanged = true;
				}
				rec.logSourceIsAlive = true;

				foreach (var parserOutput in rec.PostprocessorsOutputs)
					if (parserOutput.SetState(parserOutput.state.Refresh()))
						somethingChanged = true;
			}
			foreach (LogSourceRecord rec in new List<LogSourceRecord>(knownLogSources.Values))
			{
				if (rec.logSource.IsDisposed)
				{
					rec.logSourceIsAlive = false;
				}
				if (!rec.logSourceIsAlive)
				{
					if (!rec.cancellation.IsCancellationRequested)
						rec.cancellation.Cancel();
					knownLogSources = knownLogSources.Remove(rec.logSource);
					rec.PostprocessorsOutputs.ForEach(ppo => ppo.Dispose());
					somethingChanged = true;
				}
			}
			if (somethingChanged)
				FireChangedEvent();

			if (somethingChanged && settingsAccessor.EnableAutoPostprocessing)
			{
				IManagerInternal intf = this;
				var outputs = intf.LogSourcePostprocessors.GetAutoPostprocessingCapableOutputs().ToArray();
				if (outputs.Length > 0)
					intf.RunPostprocessors(outputs);
			}
		}

		private void FireChangedEvent()
		{
			this.postprocessorsOutputs = ImmutableArray.CreateRange(
				knownLogSources.Values.SelectMany(rec =>
					rec.PostprocessorsOutputs.Select(postprocessorRec => postprocessorRec.state.GetData()))
			);
			changeNotification.Post();
			Changed?.Invoke(this, EventArgs.Empty);
		}

		private IEnumerable<KeyValuePair<ILogSource, LogSourceMetadata>> EnumLogSourcesOfKnownTypes()
		{
			foreach (ILogSource src in logSources.Items.ToArray())
			{
				LogSourceMetadata meta;
				if (knownLogTypes.TryGetValue(src.Provider.Factory, out meta))
					yield return new KeyValuePair<ILogSource, LogSourceMetadata>(src, meta);
			}
		}

		async Task<IPostprocessorRunSummary> RunPostprocessorBody(Func<Task<IPostprocessorRunSummary>> postprocessorBody)
		{
			try
			{
				var ret = await threadPoolSyncContext.InvokeAndAwait(postprocessorBody);
				return ret;
			}
			finally
			{
				updater.Invoke();
			}
		}

		static string MakeLogSourcePostprocessorFeatureId(LogSourceMetadata logSource, ILogSourcePostprocessor postproc)
		{
			return string.Format(@"postprocessor\{0}\{1}\{2}",
				logSource.LogProviderFactory.CompanyName, logSource.LogProviderFactory.FormatName, postproc.Kind.ToString());
		}

		class ProgressSinksCollection: IDisposable
		{
			public readonly List<Progress.IProgressEventsSink> Sinks = new List<Progress.IProgressEventsSink>();

			public void Dispose()
			{
				Sinks.ForEach(s => s.Dispose());
			}
		};

		private readonly ILogSourcesManager logSources;
		private readonly Telemetry.ITelemetryCollector telemetry;
		private readonly Progress.IProgressAggregator progressAggregator;
		private readonly ISynchronizationContext modelSyncContext;
		private readonly ISynchronizationContext threadPoolSyncContext;
		private readonly IHeartBeatTimer heartbeat;
		private ImmutableDictionary<ILogProviderFactory, LogSourceMetadata> knownLogTypes = ImmutableDictionary<ILogProviderFactory, LogSourceMetadata>.Empty;
		private ImmutableDictionary<ILogSource, LogSourceRecord> knownLogSources = ImmutableDictionary<ILogSource,LogSourceRecord>.Empty;
		private IReadOnlyList<LogSourcePostprocessorState> postprocessorsOutputs = ImmutableArray.Create<LogSourcePostprocessorState>();
		private readonly Func<ImmutableArray<LogSourcePostprocessorState>> visiblePostprocessorsOutputs;
		private readonly AsyncInvokeHelper updater;
		private readonly Settings.IGlobalSettingsAccessor settingsAccessor;
		private readonly LJTraceSource tracer;
		private readonly IOutputDataDeserializer outputDataDeserializer;
		private readonly ILogPartTokenFactories logPartTokenFactories;
		private readonly Correlation.ISameNodeDetectionTokenFactories sameNodeDetectionTokenFactories;
		private readonly IChangeNotification changeNotification;
		private readonly LogMedia.IFileSystem logFileSystem;
	}
}
