using System.Text;
using System.Collections.Generic;
using System;
using System.Threading;

namespace LogJoint.UI.Presenters.FormatsWizard.TestDialog
{
	internal class Presenter : IPresenter, IDisposable, IViewEvents, ILogProviderHost
	{
		readonly IView view;
		readonly ITempFilesManager tempFilesManager;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly RegularExpressions.IRegexFactory regexFactory;

		readonly IModelThreadsInternal threads;
		readonly ILogSourceThreadsInternal logSourceThreads;
		readonly ISynchronizationContext synchronizationContext;
		readonly LogMedia.IFileSystem fileSystem;
		ILogProvider provider;
		LogViewer.DummyModel model;
		LogViewer.IPresenterInternal logPresenter;
		bool statusOk;

		public Presenter(
			IView view,
			ITempFilesManager tempFilesManager,
			ITraceSourceFactory traceSourceFactory,
			RegularExpressions.IRegexFactory regexFactory,
			LogViewer.IPresenterFactory logViewerPresenterFactory,
			ISynchronizationContext synchronizationContext,
			LogMedia.IFileSystem fileSystem
		)
		{
			this.view = view;
			this.view.SetEventsHandler(this);
			this.tempFilesManager = tempFilesManager;
			this.traceSourceFactory = traceSourceFactory;
			this.regexFactory = regexFactory;
			this.synchronizationContext = synchronizationContext;
			this.fileSystem = fileSystem;

			this.threads = new ModelThreads();
			this.logSourceThreads = new LogSourceThreads(
				LJTraceSource.EmptyTracer, threads, null);
			this.model = new Presenters.LogViewer.DummyModel();
			this.logPresenter = logViewerPresenterFactory.CreateIsolatedPresenter(model, view.LogViewer);
			logPresenter.ShowTime = true;
		}

		void IDisposable.Dispose ()
		{
			view.Dispose();
			logSourceThreads.Dispose();
			logPresenter.Dispose();
		}

		void IViewEvents.OnCloseButtonClicked ()
		{
			view.Close();
		}

		bool IPresenter.ShowDialog (ILogProviderFactory sampleLogFactory, IConnectionParams sampleLogConnectionParams)
		{
			ReadAll(sampleLogFactory, sampleLogConnectionParams);

			view.Show();
			return statusOk;
		}

		string ILogProviderHost.LoggingPrefix => "test";

		ITraceSourceFactory ILogProviderHost.TraceSourceFactory => traceSourceFactory;

		RegularExpressions.IRegexFactory ILogProviderHost.RegexFactory => regexFactory;

		ISynchronizationContext ILogProviderHost.ModelSynchronizationContext => synchronizationContext;

		LogMedia.IFileSystem ILogProviderHost.FileSystem => fileSystem;

		ITimeOffsets ILogProviderHost.TimeOffsets
		{
			get { return TimeOffsets.Empty; }
		}

		Settings.IGlobalSettingsAccessor ILogProviderHost.GlobalSettings
		{
			get { return Settings.DefaultSettingsAccessor.Instance; }
		}

		ITempFilesManager ILogProviderHost.TempFilesManager
		{
			get { return tempFilesManager; }
		}

		ILogSourceThreads ILogProviderHost.Threads
		{
			get { return logSourceThreads; }
		}

		void ILogProviderHost.OnStatisticsChanged(
			LogProviderStats value, LogProviderStats oldValue, LogProviderStatsFlag flags)
		{
		}

		private async void ReadAll(
			ILogProviderFactory factory,
			IConnectionParams connectParams)
		{
			try
			{
				provider = factory.CreateFromConnectionParams(this, connectParams);

				var messages = new List<IMessage>();
				await this.provider.EnumMessages(
					0, m =>
					{
						messages.Add(m);
						return true;
					}, 
					EnumMessagesFlag.Forward, 
					LogProviderCommandPriority.RealtimeUserAction, 
					CancellationToken.None
				);
				model.SetMessages(messages);
				await logPresenter.GoHome();
				UpdateStatusControls(messages.Count, null);
			}
			catch (Exception e)
			{
				UpdateStatusControls(0, e);
			}
		}

		private void UpdateStatusControls(int messagsCount, Exception e)
		{
			StringBuilder msg = new StringBuilder();
			bool? success = null;
			if (e != null)
			{
				while (e.InnerException != null)
					e = e.InnerException;
				msg.AppendFormat("Failed to parse sample log: {0}", e.Message);
				success = false;
			}
			else
			{
				LogProviderStats s = provider.Stats;
				switch (s.State)
				{
				case LogProviderState.Idle:
				case LogProviderState.DetectingAvailableTime:
				case LogProviderState.NoFile:
					if (messagsCount > 0)
					{
						success = true;
						msg.AppendFormat("Successfully parsed {0} message(s)", messagsCount);
					}
					else
					{
						if (s.State == LogProviderState.Idle)
						{
							msg.Append("No messages parsed");
							success = false;
						}
						else
						{
							msg.Append("Trying to parse...");
						}
					}
					break;
				case LogProviderState.LoadError:
					msg.AppendFormat("{0}", s.Error.Message);
					success = false;
					break;
				}
			}

			var testOutcome = TestOutcome.None;
			if (success.HasValue)
			if (success.Value)
				testOutcome = TestOutcome.Success;
			else
				testOutcome = TestOutcome.Failure;

			statusOk = success.GetValueOrDefault(false);

			view.SetData(msg.ToString(), testOutcome);
		}
	};
};