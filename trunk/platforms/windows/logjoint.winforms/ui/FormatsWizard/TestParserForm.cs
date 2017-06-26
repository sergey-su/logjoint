using System;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public partial class TestParserForm : Form, ILogProviderHost
	{
		readonly IModelThreads threads;
		readonly ILogSourceThreads logSourceThreads;
		ILogProvider provider;
		Presenters.LogViewer.DummyModel model;
		UI.Presenters.LogViewer.IPresenter presenter;
		ITempFilesManager tempFilesManager;
		bool statusOk;

		private TestParserForm(
			ILogProviderFactory factory, 
			IConnectionParams connectParams,
			ITempFilesManager tempFilesManager, 
			Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory
		)
		{
			this.tempFilesManager = tempFilesManager;
			this.threads = new ModelThreads();
			this.logSourceThreads = new LogSourceThreads(LJTraceSource.EmptyTracer, threads, null);

			InitializeComponent();

			this.model = new Presenters.LogViewer.DummyModel(threads);
			this.presenter = logViewerPresenterFactory.Create(model, viewerControl, createIsolatedPresenter: true);
			presenter.ShowTime = true;

			ReadAll(factory, connectParams);
		}

		private async void ReadAll(
			ILogProviderFactory factory,
			IConnectionParams connectParams)
		{
			try
			{
				provider = factory.CreateFromConnectionParams(this, connectParams);

				var messages = new List<IMessage>();
				await this.provider.EnumMessages(0, m =>
				{
					messages.Add(m);
					return true;
				}, EnumMessagesFlag.Forward, LogProviderCommandPriority.RealtimeUserAction, CancellationToken.None);
				model.SetMessages(messages);
				await presenter.GoHome();
				UpdateStatusControls(messages.Count, null);
			}
			catch (Exception e)
			{
				UpdateStatusControls(0, e);
			}
		}

		public static bool Execute(ILogProviderFactory factory, IConnectionParams connectParams,
			ITempFilesManager tempFilesManager, Presenters.LogViewer.IPresenterFactory logViewerPresenterFactory)
		{
			using (TestParserForm f = new TestParserForm(factory, connectParams, tempFilesManager, logViewerPresenterFactory))
			{
				f.ShowDialog();
				return f.statusOk;
			}
		}

		#region ILogProviderHost Members

		LJTraceSource ILogProviderHost.Trace
		{
			get { return LJTraceSource.EmptyTracer; }
		}

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

		void ILogProviderHost.OnStatisticsChanged(LogProviderStatsFlag flags)
		{
		}

		#endregion

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
			statusTextBox.Text = msg.ToString();
			if (success.HasValue)
				if (success.Value)
					statusPictureBox.Image = LogJoint.Properties.Resources.OkCheck32x32;
				else
					statusPictureBox.Image = LogJoint.Properties.Resources.Error;
			else
				statusPictureBox.Image = null;

			statusOk = success.GetValueOrDefault(false);
		}
	}
}