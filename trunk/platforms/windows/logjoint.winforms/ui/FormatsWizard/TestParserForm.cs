using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using LogJoint.UI.Presenters;

namespace LogJoint.UI
{
	public partial class TestParserForm : Form, ILogProviderHost, UI.Presenters.LogViewer.IModel
	{
		readonly IModelThreads threads;
		readonly ILogSourceThreads logSourceThreads;
		readonly ILogProvider provider;
		readonly UI.Presenters.LogViewer.Presenter presenter;
		int messagesChanged;
		int stateChanged;
		bool statusOk;
		StatusReport statusReport = new StatusReport();
		IFiltersList displayFilters = new FiltersList(FilterAction.Include);
		IFiltersList hlFilters = new FiltersList(FilterAction.Exclude);

		private TestParserForm(ILogProviderFactory factory, IConnectionParams connectParams)
		{
			threads = new ModelThreads();
			logSourceThreads = new LogSourceThreads(LJTraceSource.EmptyTracer, threads, null);
			provider = factory.CreateFromConnectionParams(this, connectParams);

			InitializeComponent();
			presenter = new Presenters.LogViewer.Presenter(this, viewerControl, null);
			viewerControl.SetPresenter(presenter);
			presenter.ShowTime = true;

			provider.NavigateTo(null, NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries);

			displayFilters.FilteringEnabled = false;
			hlFilters.FilteringEnabled = false;
		}


		public static bool Execute(ILogProviderFactory factory, IConnectionParams connectParams)
		{
			using (TestParserForm f = new TestParserForm(factory, connectParams))
			{
				f.ShowDialog();
				return f.statusOk;
			}
		}

		public LJTraceSource Tracer
		{
			get { return LJTraceSource.EmptyTracer; }
		}

		public LJTraceSource Trace
		{
			get { return Tracer; }
		}

		public IMessagesCollection Messages
		{
			get { return provider.LoadedMessages; }
		}

		public IModelThreads Threads 
		{
			get { return threads; }
		}

		public TimeSpan TimeOffset
		{
			get { return new TimeSpan(); }
		}

		public Settings.IGlobalSettingsAccessor GlobalSettings
		{
			get { return new Settings.DefaultSettingsAccessor(); }
		}

		public string MessageToDisplayWhenMessagesCollectionIsEmpty
		{
			get { return null; }
		}

		public void ShiftUp()
		{
		}

		public void ShiftDown()
		{
		}

		public bool IsShiftableUp
		{
			get { return false; } 
		}

		public bool IsShiftableDown
		{
			get { return false; }
		}

		public void ShiftAt(DateTime t)
		{
		}

		public void ShiftHome()
		{
		}

		public void ShiftToEnd()
		{
		}

		public IBookmarks Bookmarks
		{
			get { return null; }
		}

		public IFiltersList DisplayFilters
		{
			get { return displayFilters; }
		}

		public IFiltersList HighlightFilters
		{
			get { return hlFilters; }
		}

		public Presenters.StatusReports.IReport GetStatusReport()
		{
			return statusReport;
		}

		public event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;

		public bool GetAndResetPendingUpdateFlag() { return true; }

		#region ILogReaderHost Members


		public ITempFilesManager TempFilesManager
		{
			get { return LogJoint.TempFilesManager.GetInstance(); }
		}

		ILogSourceThreads ILogProviderHost.Threads
		{
			get { return logSourceThreads; }
		}

		public void OnAboutToIdle()
		{
		}

		public void OnStatisticsChanged(LogProviderStatsFlag flags)
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

		class StatusReport : Presenters.StatusReports.IReport
		{
			public void ShowStatusPopup(string caption, string text, bool autoHide) {}
			public void ShowStatusPopup(string caption, IEnumerable<Presenters.StatusReports.MessagePart> parts, bool autoHide) { }
			public void ShowStatusText(string text, bool autoHide) {}
			
			public void Dispose() {}
		};
	}
}