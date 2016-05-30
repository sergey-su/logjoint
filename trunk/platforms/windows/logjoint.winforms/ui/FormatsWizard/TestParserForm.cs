using System;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace LogJoint.UI
{
	public partial class TestParserForm : Form, ILogProviderHost
	{
		readonly IModelThreads threads;
		readonly ILogSourceThreads logSourceThreads;
		readonly ILogProvider provider;
		readonly UI.Presenters.LogViewer.IPresenter presenter;
		readonly ITempFilesManager tempFilesManager;
		int messagesChanged;
		int stateChanged;
		bool statusOk;

		private TestParserForm(ILogProviderFactory factory, IConnectionParams connectParams, ITempFilesManager tempFilesManager)
		{
			threads = new ModelThreads();
			logSourceThreads = new LogSourceThreads(LJTraceSource.EmptyTracer, threads, null);
			provider = factory.CreateFromConnectionParams(this, connectParams);
			this.tempFilesManager = tempFilesManager;

			InitializeComponent();
			presenter = new Presenters.LogViewer.Presenter(
				new Presenters.LogViewer.DummyModel(threads, provider.LoadedMessages),
				viewerControl,
				null, null);
			presenter.ShowTime = true;

			provider.NavigateTo(null, NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries);
		}

		public static bool Execute(ILogProviderFactory factory, IConnectionParams connectParams, ITempFilesManager tempFilesManager)
		{
			using (TestParserForm f = new TestParserForm(factory, connectParams, tempFilesManager))
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

		void ILogProviderHost.OnAboutToIdle()
		{
		}

		void ILogProviderHost.OnStatisticsChanged(LogProviderStatsFlag flags)
		{
			if ((flags & LogProviderStatsFlag.State) != 0)
				stateChanged = 1;
		}

		void ILogProviderHost.OnLoadedMessagesChanged()
		{
			messagesChanged = 1;
		}

		void ILogProviderHost.OnSearchResultChanged()
		{
		}

		#endregion

		private void updateViewTimer_Tick(object sender, EventArgs e)
		{
			if (Interlocked.Exchange(ref stateChanged, 0) > 0)
			{
				LogProviderStats s = provider.Stats;
				StringBuilder msg = new StringBuilder();
				bool? success = null;
				switch (s.State)
				{
					case LogProviderState.Idle:
					case LogProviderState.Loading:
					case LogProviderState.DetectingAvailableTime:
					case LogProviderState.NoFile:
						if (s.MessagesCount > 0)
						{
							success = true;
							msg.AppendFormat("Successfully parsed {0} message(s)", s.MessagesCount);
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
			if (Interlocked.Exchange(ref messagesChanged, 0) > 0)
			{
				provider.LockMessages();
				try
				{
					presenter.UpdateView();
				}
				finally
				{
					provider.UnlockMessages();
				}
			}
		}
	}
}