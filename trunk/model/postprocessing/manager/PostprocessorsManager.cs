using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using LogJoint.Analytics;

namespace LogJoint.Postprocessing
{
	public class PostprocessorsManager : IPostprocessorsManager
	{
		public PostprocessorsManager(
			ILogSourcesManager logSources,
			Telemetry.ITelemetryCollector telemetry,
			IInvokeSynchronization modelInvoke,
			IHeartBeatTimer heartbeat,
			Progress.IProgressAggregator progressAggregator,
			IPostprocessorsManagerUserInteractions userInteractions,
			Settings.IGlobalSettingsAccessor settingsAccessor
		)
		{
			this.userInteractions = userInteractions;
			this.logSources = logSources;
			this.telemetry = telemetry;
			this.progressAggregator = progressAggregator;
			this.settingsAccessor = settingsAccessor;

			logSources.OnLogSourceAdded += (sender, args) => { lazyUpdateTracker.Invalidate(); };
			logSources.OnLogSourceRemoved += (sender, args) => { lazyUpdateTracker.Invalidate(); };
			logSources.OnLogSourceAnnotationChanged += (sender, args) => { lazyUpdateTracker.Invalidate(); };
			logSources.OnLogSourceStatsChanged += (object sender, LogSourceStatsEventArgs e) => 
			{
				if ((e.Flags & LogProviderStatsFlag.ContentsEtag) != 0)
				{
					modelInvoke.Invoke(() => RefreshInternal(assumeSourceChanged: sender as ILogSource));
				}
			};
			heartbeat.OnTimer += (sender, args) =>
			{
				if (lazyUpdateTracker.Validate())
					RefreshInternal();
			};
			RefreshInternal();
		}

		public event EventHandler Changed;


		IEnumerable<LogSourcePostprocessorOutput> IPostprocessorsManager.LogSourcePostprocessorsOutputs
		{
			get
			{
				return knownLogSources.Values.SelectMany(rec =>
					rec.PostprocessorsOutputs.Select(postprocessorRec => postprocessorRec.GetData(rec.logSource, rec.metadata)));
			}
		}

		void IPostprocessorsManager.RegisterLogType(LogSourceMetadata meta)
		{
			this.knownLogTypes[meta.LogProviderFactory] = meta;
		}

		IEnumerable<LogSourceMetadata> IPostprocessorsManager.KnownLogTypes
		{
			get { return this.knownLogTypes.Values; }
		}

		void IPostprocessorsManager.RegisterCrossLogSourcePostprocessor(ILogSourcePostprocessor postprocessor)
		{
			this.crossLogSourcePostprocessorsTypes.Add(postprocessor);
		}

		async Task<bool> IPostprocessorsManager.RunPostprocessor(
			KeyValuePair<ILogSourcePostprocessor, ILogSource>[] typesAndSources, 
			bool forceSourcesSelection, object customData)
		{
			var sources = typesAndSources.Select(typesAndSource =>
			{
				var outputType = typesAndSource.Key;
				var forLogSource = typesAndSource.Value;

				LogSourceRecordInternal logSourceRecord;
				if (!knownLogSources.TryGetValue(forLogSource, out logSourceRecord))
					throw new ArgumentException("Log source is unknown");

				var postprocessorRecord = logSourceRecord.PostprocessorsOutputs.SingleOrDefault(parserRec => parserRec.Metadata == outputType);
				if (postprocessorRecord == null)
					throw new ArgumentException("Bad Postprocessor output type: " + outputType.TypeID);

				if (postprocessorRecord.status == LogSourcePostprocessorOutput.Status.InProgress)
					throw new InvalidOperationException("Postprocessor output for log source is already being generated");

				string outputFileName;
				using (var section = forLogSource.LogSourceSpecificStorageEntry.OpenXMLSection(
						MakePostprocessorOutputFileName(outputType), Persistence.StorageSectionOpenFlag.ReadOnly))
					outputFileName = section.AbsolutePath;

				bool needsProcessing = 
					logSourceRecord.logSource.Visible && (
						postprocessorRecord.status == LogSourcePostprocessorOutput.Status.NeverRun || 
						postprocessorRecord.status == LogSourcePostprocessorOutput.Status.Failed ||
						postprocessorRecord.status == LogSourcePostprocessorOutput.Status.Outdated);
				needsProcessing |= crossLogSourcePostprocessorsTypes.Contains(postprocessorRecord.Metadata);

				XAttribute contentsEtagAttr = null;
				var sourceContentsEtag = logSourceRecord.logSource.Provider.Stats.ContentsEtag;
				if (sourceContentsEtag != null)
				{
					contentsEtagAttr = new XAttribute(
						XName.Get("etag", xmlNs),
						logSourceRecord.logSource.Provider.Stats.ContentsEtag
					);
				}

				return new
				{
					OutputType = outputType,
					PostprocessorInput = logSourceRecord.ToPostprocessorInput(
						outputFileName, contentsEtagAttr, customData
					),
					PostprocessorRecord = postprocessorRecord,
					LogSourceMeta = logSourceRecord.metadata,
					LogSource = logSourceRecord.logSource,
					NeedsProcessing = needsProcessing,
					UILogSourceInfo = new LogsSourcesSelectorDialogParams.LogSourceInfo()
					{
						Description = forLogSource.GetShortDisplayNameWithAnnotation()
					},
					TemplatesTracker = new TemplatesTracker()
				};
			}).ToList();

			bool noLogNeedsProcessing = sources.All(s => !s.NeedsProcessing);
			if (noLogNeedsProcessing)
				sources.ForEach(s => s.UILogSourceInfo.IsSelected = true);
			else
				sources.ForEach(s => s.UILogSourceInfo.IsSelected = s.NeedsProcessing);

			if (forceSourcesSelection && userInteractions != null)
			{
				if (!await userInteractions.ShowLogsSourcesSelectorDialog(new LogsSourcesSelectorDialogParams()
				{
					LogSources = sources.Select(s => s.UILogSourceInfo).ToList()
				}, CancellationToken.None))
				{
					return false;
				}
			}

			var outerTasks = new List<Task>();

			foreach (var postprocessorTypeGroup in sources.Where(s => s.UILogSourceInfo.IsSelected).GroupBy(s => s.OutputType))
			{
				var postprocessorProgress = progressAggregator.CreateChildAggregator();
				postprocessorProgress.ProgressChanged += (s, e) => RefreshInternal(assumeSomethingChanging: true);

				var innerTask = RunPostprocessor(async () =>
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
				outerTasks.Add(AwaitOnPostprocessorTaskAndUpdate(innerTask));

				foreach (var postprocessorRecord in postprocessorTypeGroup.Select(s => s.PostprocessorRecord))
				{
					postprocessorRecord.postprocessorTask = innerTask;
					postprocessorRecord.postprocessorProgress = postprocessorProgress;
				}
			}

			RefreshInternal();

			await Task.WhenAll(outerTasks);

			return true;
		}

		IEnumerable<ILogSource> IPostprocessorsManager.KnownLogSources
		{
			get { return knownLogSources.Keys; }
		}


		static string MakePostprocessorOutputFileName(ILogSourcePostprocessor pp)
		{
			return string.Format("postproc-{0}.xml", pp.TypeID.ToLower());
		}


		void RefreshInternal(bool assumeSomethingChanging = false, ILogSource assumeSourceChanged = null)
		{
			bool somethingChanged = assumeSomethingChanging;
			foreach (LogSourceRecordInternal rec in knownLogSources.Values)
				rec.logSourceIsAlive = false;
			foreach (var src in EnumLogSourcesOfKnownTypes())
			{
				LogSourceRecordInternal rec;
				if (!knownLogSources.TryGetValue(src.Key, out rec))
				{
					rec = new LogSourceRecordInternal();
					rec.logSource = src.Key;
					rec.metadata = src.Value;
					rec.logFileName = src.Key.Provider.ConnectionParams[ConnectionParamsUtils.PathConnectionParam];
					rec.cancellation = new CancellationTokenSource();
					foreach (var postprocessorType in rec.metadata.SupportedPostprocessors)
						rec.PostprocessorsOutputs.Add(new PostprocessorOutputRecordInternal() { Metadata = postprocessorType });

					knownLogSources.Add(src.Key, rec);
					somethingChanged = true;
				}
				rec.logSourceIsAlive = true;

				foreach (var parserOutput in rec.PostprocessorsOutputs)
					RefreshPostprocessorOutput(rec, parserOutput, ref somethingChanged, rec.logSource == assumeSourceChanged);
			}
			foreach (LogSourceRecordInternal rec in new List<LogSourceRecordInternal>(knownLogSources.Values))
			{
				if (rec.logSource.IsDisposed)
				{
					rec.logSourceIsAlive = false;
				}
				if (!rec.logSourceIsAlive)
				{
					if (!rec.cancellation.IsCancellationRequested)
						rec.cancellation.Cancel();
					knownLogSources.Remove(rec.logSource);
					somethingChanged = true;
				}
			}
			if (somethingChanged)
				Changed?.Invoke(this, EventArgs.Empty);

			if (somethingChanged && settingsAccessor.EnableAutoPostprocessing)
			{
				var outputs = this.GetAutoPostprocessingCapableOutputs()
					.Where(x => x.PostprocessorMetadata.TypeID != PostprocessorIds.Correlator)
					.Select(output => new KeyValuePair<ILogSourcePostprocessor, ILogSource>(output.PostprocessorMetadata, output.LogSource))
					.ToArray();
				if (outputs.Length > 0)
					((IPostprocessorsManager)this).RunPostprocessor(outputs, forceSourcesSelection: false);
			}
		}

		private IEnumerable<KeyValuePair<ILogSource, LogSourceMetadata>> EnumLogSourcesOfKnownTypes()
		{
			foreach (ILogSource src in logSources.Items.ToArray())
			{
				if (src.IsDisposed)
					continue;
				LogSourceMetadata meta;
				if (knownLogTypes.TryGetValue(src.Provider.Factory, out meta))
					yield return new KeyValuePair<ILogSource, LogSourceMetadata>(src, meta);
			}
		}

		private void TryLoadParserOutputAndUpdateStatus(LogSourceRecordInternal logSourceRecord, PostprocessorOutputRecordInternal r)
		{
			if (string.IsNullOrEmpty(logSourceRecord.logFileName)
			|| (r.lastRunSummary != null && r.lastRunSummary.HasErrors))
			{
				r.status = LogSourcePostprocessorOutput.Status.Failed;
				return;
			}
			using (var existingSection = logSourceRecord.logSource.LogSourceSpecificStorageEntry.OpenXMLSection(
					MakePostprocessorOutputFileName(r.Metadata), Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				if (existingSection.Data.Root == null)
				{
					r.status = LogSourcePostprocessorOutput.Status.NeverRun;
					return;
				}
				try
				{
					object tmpOutput = r.Metadata.DeserializeOutputData(existingSection.Data, logSourceRecord.logSource);
					r.outputData = tmpOutput;
					if (IsOutputOutdated(logSourceRecord.logSource, existingSection))
						r.status = LogSourcePostprocessorOutput.Status.Outdated;
					else
						r.status = LogSourcePostprocessorOutput.Status.Finished;
				}
				catch (Exception)
				{
					// If reading a file throws exception assume that cached format is old and unsupported.
					r.status = LogSourcePostprocessorOutput.Status.NeverRun;
					return;
				}
			}
		}

		private bool IsOutputOutdated(ILogSource logSource, Persistence.IXMLStorageSection outputSection)
		{
			var logSourceEtag = logSource.Provider.Stats.ContentsEtag;
			if (logSourceEtag != null)
			{
				var etagAttr = outputSection.Data.Root.Attribute(XName.Get("etag", xmlNs));
				if (etagAttr != null)
				{
					if (logSourceEtag.Value.ToString() != etagAttr.Value)
					{
						return true;
					}
				}
			}
			return false;
		}

		private void RefreshPostprocessorOutput(
			LogSourceRecordInternal logSourceRecord, 
			PostprocessorOutputRecordInternal postprocessorOutputRecord, 
			ref bool somethingChanged,
			bool assumeChanged)
		{
			LogSourcePostprocessorOutput.Status oldStatus = postprocessorOutputRecord.status;
			IPostprocessorRunSummary oldSummary = postprocessorOutputRecord.lastRunSummary;

			bool postprocessorOutputNeedsLoading = 
				assumeChanged ||
				postprocessorOutputRecord.status != LogSourcePostprocessorOutput.Status.Finished;

			if (postprocessorOutputRecord.postprocessorTask != null)
			{
				if (postprocessorOutputRecord.postprocessorTask.IsCompleted)
				{
					if (postprocessorOutputRecord.postprocessorTask.GetTaskException() != null)
					{
						postprocessorOutputRecord.lastRunSummary = new FailedRunSummary(postprocessorOutputRecord.postprocessorTask.GetTaskException());
					}
					else
					{
						var runSummary = postprocessorOutputRecord.postprocessorTask.Result;
						var logSpecificRunSummary = runSummary?.GetLogSpecificSummary(logSourceRecord.logSource) ?? runSummary;
						postprocessorOutputRecord.lastRunSummary = logSpecificRunSummary;
					}
					postprocessorOutputRecord.ClearPostprocessorTask();
					postprocessorOutputNeedsLoading = true;
				}
				else
				{
					postprocessorOutputRecord.status = LogSourcePostprocessorOutput.Status.InProgress;
					postprocessorOutputNeedsLoading = false;
				}
			}

			if (postprocessorOutputNeedsLoading)
			{
				TryLoadParserOutputAndUpdateStatus(logSourceRecord, postprocessorOutputRecord);
			}

			somethingChanged = somethingChanged 
				|| (postprocessorOutputRecord.status != oldStatus)
				|| (postprocessorOutputRecord.lastRunSummary != oldSummary);
		}

		async Task<IPostprocessorRunSummary> RunPostprocessor(Func<Task<IPostprocessorRunSummary>> postprocessorBody)
		{
			await TaskUtils.SwitchToThreadpoolContext();
			var ret = await postprocessorBody();
			return ret;
		}

		async Task AwaitOnPostprocessorTaskAndUpdate(Task<IPostprocessorRunSummary> innerTask)
		{
			try
			{
				await innerTask;
			}
			catch (Exception e)
			{
				telemetry.ReportException(e, "postprocessor");
			}
			RefreshInternal();
		}

		static string MakeLogSourcePostprocessorFeatureId(LogSourceMetadata logSource, ILogSourcePostprocessor postproc)
		{
			return string.Format(@"postprocessor\{0}\{1}\{2}",
				logSource.LogProviderFactory.CompanyName, logSource.LogProviderFactory.FormatName, postproc.TypeID);
		}

		class PostprocessorOutputRecordInternal
		{
			public ILogSourcePostprocessor Metadata;
			public LogSourcePostprocessorOutput.Status status;
			public IPostprocessorRunSummary lastRunSummary;
			
			public Task<IPostprocessorRunSummary> postprocessorTask;
			public Progress.IProgressAggregator postprocessorProgress;
			public object outputData;

			public LogSourcePostprocessorOutput GetData(ILogSource logSource, LogSourceMetadata sourceType)
			{
				LogSourcePostprocessorOutput ret = new LogSourcePostprocessorOutput();
				ret.LogSource = logSource;
				ret.PostprocessorMetadata = Metadata;
				ret.LogSourceMeta = sourceType;
				ret.OutputStatus = status;
				if (postprocessorProgress != null)
					ret.Progress = postprocessorProgress.ProgressValue;
				ret.OutputData = outputData;
				ret.LastRunSummary = lastRunSummary;
				return ret;
			}

			public void ClearPostprocessorTask()
			{
				postprocessorTask = null;
				if (postprocessorProgress != null)
				{
					postprocessorProgress.Dispose();
					postprocessorProgress = null;
				}
			}
		};

		class LogSourceRecordInternal
		{
			public LogSourceMetadata metadata;
			public ILogSource logSource;
			public string logFileName;
			public bool logSourceIsAlive;
			public CancellationTokenSource cancellation;

			public List<PostprocessorOutputRecordInternal> PostprocessorsOutputs = new List<PostprocessorOutputRecordInternal>();

			public LogSourcePostprocessorInput ToPostprocessorInput(
				string outputFileName, XAttribute inputContentsEtagAttr, object customData)
			{
				return new LogSourcePostprocessorInput()
				{
					LogFileName = logFileName,
					LogSource = logSource,
					OutputFileName = outputFileName,
					CancellationToken = cancellation.Token,
					InputContentsEtagAttr = inputContentsEtagAttr,
					CustomData = customData
				};
			}
		};


		class ProgressSinksCollection: IDisposable
		{
			public readonly List<Progress.IProgressEventsSink> Sinks = new List<Progress.IProgressEventsSink>();

			public void Dispose()
			{
				Sinks.ForEach(s => s.Dispose());
			}
		};

		private readonly IPostprocessorsManagerUserInteractions userInteractions;
		private readonly ILogSourcesManager logSources;
		private readonly Telemetry.ITelemetryCollector telemetry;
		private readonly Progress.IProgressAggregator progressAggregator;
		private readonly Dictionary<ILogProviderFactory, LogSourceMetadata> knownLogTypes = new Dictionary<ILogProviderFactory, LogSourceMetadata>();
		private readonly Dictionary<ILogSource, LogSourceRecordInternal> knownLogSources = new Dictionary<ILogSource,LogSourceRecordInternal>();
		private readonly HashSet<ILogSourcePostprocessor> crossLogSourcePostprocessorsTypes = new HashSet<ILogSourcePostprocessor>();
		private readonly LazyUpdateFlag lazyUpdateTracker = new LazyUpdateFlag();
		private readonly Settings.IGlobalSettingsAccessor settingsAccessor;
		private readonly static string xmlNs = "https://logjoint.codeplex.com/postprocs";
	}
}
