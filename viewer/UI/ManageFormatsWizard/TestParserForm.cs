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
	public partial class TestParserForm : Form, ILogViewerControlHost, ILogReaderHost, IMainForm
	{
		readonly Threads threads;
		readonly ILogReader reader;
		int messagesChanged;
		int stateChanged;
		bool statusOk;
		StatusReport statusReport = new StatusReport();

		private TestParserForm(ILogReaderFactory factory, IConnectionParams connectParams)
		{
			threads = new Threads();
			reader = factory.CreateFromConnectionParams(this, connectParams);

			InitializeComponent();
			viewerControl.SetHost(this);
			viewerControl.ShowTime = true;

			reader.NavigateTo(null, NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries);
		}


		public static bool Execute(ILogReaderFactory factory, IConnectionParams connectParams)
		{
			using (TestParserForm f = new TestParserForm(factory, connectParams))
			{
				f.ShowDialog();
				return f.statusOk;
			}
		}

		#region ILogViewerControlHost Members

		public Source Trace
		{
			get { return Source.EmptyTracer; }
		}

		public IMessagesCollection Messages
		{
			get { return reader.Messages; }
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

		public IMainForm MainForm
		{
			get { return this; }
		}

		public FiltersList DisplayFilters
		{
			get { return null; }
		}

		public FiltersList HighlightFilters
		{
			get { return null; }
		}

		public IStatusReport GetStatusReport()
		{
			return statusReport;
		}

		#endregion

		#region ILogReaderHost Members


		public ITempFilesManager TempFilesManager
		{
			get { return LogJoint.TempFilesManager.GetInstance(Trace); }
		}

		public IThread RegisterNewThread(string id)
		{
			return threads.RegisterThread(id, null);
		}

		public void OnAboutToIdle()
		{
		}

		public void OnStatisticsChanged(StatsFlag flags)
		{
			if ((flags & StatsFlag.State) != 0)
				stateChanged = 1;
		}

		public void OnMessagesChanged()
		{
			messagesChanged = 1;
		}

		#endregion

		private void updateViewTimer_Tick(object sender, EventArgs e)
		{
			if (Interlocked.Exchange(ref stateChanged, 0) > 0)
			{
				LogReaderStats s = reader.Stats;
				StringBuilder msg = new StringBuilder();
				bool? success = null;
				switch (s.State)
				{
					case ReaderState.Idle:
					case ReaderState.Loading:
					case ReaderState.DetectingAvailableTime:
					case ReaderState.NoFile:
						if (s.MessagesCount > 0)
						{
							success = true;
							msg.AppendFormat("Successfully parsed {0} messages(s)", s.MessagesCount);
						}
						else
						{
							if (s.State == ReaderState.Idle)
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
					case ReaderState.LoadError:
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
				reader.LockMessages();
				try
				{
					viewerControl.UpdateView();
				}
				finally
				{
					reader.UnlockMessages();
				}
			}
		}


		#region IMainForm Members

		void IMainForm.AddOwnedForm(Form f)
		{
			this.AddOwnedForm(f);
		}

		#endregion

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