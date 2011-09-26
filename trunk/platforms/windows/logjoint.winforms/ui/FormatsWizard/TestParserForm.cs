using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Threading;

namespace LogJoint.UI
{
	public partial class TestParserForm : Form, ILogProviderHost, UI.Presenters.LogViewer.IModel
	{
		readonly Threads threads;
		readonly LogSourceThreads logSourceThreads;
		readonly ILogProvider provider;
		readonly UI.Presenters.LogViewer.Presenter presenter;
		int messagesChanged;
		int stateChanged;
		bool statusOk;
		StatusReport statusReport = new StatusReport();
		FiltersList displayFilters = new FiltersList(FilterAction.Include) { FilteringEnabled = false };
		FiltersList hlFilters = new FiltersList(FilterAction.Exclude) { FilteringEnabled = false };

		private TestParserForm(ILogProviderFactory factory, IConnectionParams connectParams)
		{
			threads = new Threads();
			logSourceThreads = new LogSourceThreads(LJTraceSource.EmptyTracer, threads, null);
			provider = factory.CreateFromConnectionParams(this, connectParams);

			InitializeComponent();
			presenter = new Presenters.LogViewer.Presenter(this, viewerControl, null);
			viewerControl.SetPresenter(presenter);
			viewerControl.ShowTime = true;

			provider.NavigateTo(null, NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries);
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
			get { return provider.Messages; }
		}

		public IEnumerable<IThread> Threads 
		{
			get { return threads.Items; }
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

		public IBookmarks Bookmarks
		{
			get { return null; }
		}

		public IUINavigationHandler UINavigationHandler
		{
			get { return null; }
		}

		public FiltersList DisplayFilters
		{
			get { return displayFilters; }
		}

		public FiltersList HighlightFilters
		{
			get { return hlFilters; }
		}

		public IStatusReport GetStatusReport()
		{
			return statusReport;
		}

		public event EventHandler<Model.MessagesChangedEventArgs> OnMessagesChanged;

		#region ILogReaderHost Members


		public ITempFilesManager TempFilesManager
		{
			get { return LogJoint.TempFilesManager.GetInstance(); }
		}

		LogSourceThreads ILogProviderHost.Threads
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

		void ILogProviderHost.OnMessagesChanged()
		{
			messagesChanged = 1;
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
							msg.AppendFormat("Successfully parsed {0} messages(s)", s.MessagesCount);
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
						statusPictureBox.Image = logjoint.Properties.Resources.OkCheck32x32;
					else
						statusPictureBox.Image = logjoint.Properties.Resources.Error;
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

		class StatusReport : IStatusReport
		{
			public void SetStatusString(string text)
			{
				throw new Exception("The method or operation is not implemented.");
			}
			
			public bool AutoHide
			{
				get { return false; }
				set {}
			}

			public bool Blink 
			{
				get { return false; }
				set { }
			}

			public void Dispose()
			{
			}
		};
	}
}