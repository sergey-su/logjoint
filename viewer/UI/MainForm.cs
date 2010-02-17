using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Reflection;
using System.Diagnostics;

using MessageFlag = LogJoint.MessageBase.MessageFlag;

namespace LogJoint.UI
{
	public partial class MainForm : Form, IModelHost, IUINavigationHandler, IMainForm
	{
		Source tracer = Source.EmptyTracer;
		Model model;
		NewLogSourceDialog newLogSourceDialog;
		int liveUpdateLock;
		StatusReport statusReport;
		StatusReport autoHideStatusReport;
		int updateTimeTick = 0;
		int liveLogsTick = 0;
		DateTime? lastTimeTabHasBeenUsed;
		TempFilesManager tempFilesManager;
		int filterIndex;
		Control focusedControlBeforeWaitState;
		bool isAnalizing;
		List<object> plugins = new List<object>();

		public MainForm()
		{
			Thread.CurrentThread.Name = "Main thread";

			try
			{
				tracer = new Source("TraceSourceApp");
			}
			catch (Exception)
			{
				Debug.Write("Failed to create tracer");
			}

			using (tracer.NewFrame)
			{

				tempFilesManager = LogJoint.TempFilesManager.GetInstance(tracer);

				InitializeComponent();
				AppDomain.CurrentDomain.UnhandledException += delegate(object sender, UnhandledExceptionEventArgs e)
				{
					string msg = "Unhahdled domain exception occured";
					if (e.ExceptionObject is Exception)
						tracer.Error((Exception)e.ExceptionObject, msg);
					else
						tracer.Error("{0}: ({1}) {2}", msg, e.ExceptionObject.GetType(), e.ExceptionObject);
				};

				model = new Model(this);

				timeLineControl.SetHost(model);
				timeLineControl.BeginTimeRangeDrag += delegate(object sender, EventArgs args)
				{
					liveUpdateLock++;
				};
				timeLineControl.EndTimeRangeDrag += delegate(object sender, EventArgs args)
				{
					liveUpdateLock--;
				};
				logViewerControl.SetHost(model);
				logViewerControl.ManualRefresh += delegate(object sender, EventArgs args)
				{
					using (tracer.NewFrame)
					{
						tracer.Info("----> User Command: Refresh");
						model.Refresh();
					}
				};
				logViewerControl.BeginShifting += delegate(object sender, EventArgs args)
				{
					SetWaitState(true);
					GetStatusReport().SetStatusString("Moving in-memory window...");
					cancelShiftingLabel.Visible = true;
					cancelShiftingDropDownButton.Visible = true;
				};
				logViewerControl.EndShifting += delegate(object sender, EventArgs args)
				{
					cancelShiftingLabel.Visible = false;
					cancelShiftingDropDownButton.Visible = false;
					GetStatusReport().Dispose();
					SetWaitState(false);
				};
				logViewerControl.FocusedMessageChanged += delegate(object sender, EventArgs args)
				{
					timeLineControl.Invalidate();
					model.OnCurrentViewPositionChanged(logViewerControl.FocusedMessageTime);
					sourcesListView.InvalidateFocusedMessageArea();
					threadsListView.InvalidateFocusedMessageArea();
				};
				threadsListView.SetHost(model);
				threadsListView.ThreadChecked += delegate(object sender, EventArgs args)
				{
					UpdateView(false);
				};

				sourcesListView.SetHost(model);
				sourcesListView.SelectionChanged += delegate(object sender, EventArgs args)
				{
					deleteButton.Enabled = sourcesListView.SelectedCount > 0;
					trackChangesCheckBox.Enabled = sourcesListView.SelectedCount > 0;
					UpdateTrackChangesCheckBox();
				};

				timeLineControl.RangeChanged += delegate(object sender, EventArgs args)
				{
					logViewerControl.ShowMilliseconds = timeLineControl.AreMillisecondsVisible;
					model.Updates.InvalidateTimeGaps();
				};

				displayFiltersListView.SetHost(model.DisplayFiltersListViewHost);
				displayFiltersListView.FilterChecked += delegate(object sender, EventArgs args)
				{
					UpdateView(false);
				};
				displayFiltersListView.SelectionChanged += delegate(object sender, EventArgs args)
				{
					int count = displayFiltersListView.SelectedCount;
					deleteDisplayFilterButton.Enabled = count > 0;
					moveDisplayFilterDownButton.Enabled = count == 1;
					moveDisplayFilterUpButton.Enabled = count == 1;
				};

				hlFiltersListView.SetHost(model.HighlightFiltersListViewHost);
				hlFiltersListView.FilterChecked += delegate(object sender, EventArgs args)
				{
					UpdateView(false);
				};
				hlFiltersListView.SelectionChanged += delegate(object sender, EventArgs args)
				{
					int count = hlFiltersListView.SelectedCount;
					deleteHLFilterButton.Enabled = count > 0;
					moveHLFilterUpButton.Enabled = count == 1;
					moveHLFilterDownButton.Enabled = count == 1;
				};


				InitLogFactories();

				InitPlugins();
			}
		}

		#region IModelHost

		public Source Tracer { get { return tracer; } }

		public ISynchronizeInvoke Invoker { get { return this; } }

		public ITempFilesManager TempFilesManager 
		{
			get { return tempFilesManager; }
		}

		public void OnNewReader(ILogReader reader)
		{
			RegisterRecentLogEntry(reader);
		}

		public void OnUpdateView()
		{
			UpdateView(false);
		}

		public IStatusReport GetStatusReport()
		{
			if (statusReport != null)
				statusReport.Dispose();
			statusReport = new StatusReport(this);
			return statusReport;
		}

		public IMainForm MainFormObject { get { return this; } }

		public DateTime? CurrentViewTime
		{
			get { return logViewerControl.FocusedMessageTime; }
		}

		public void SetCurrentViewTime(DateTime? time, NavigateFlag flags)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("time={0}, flags={1}", time, flags);
				UI.LogViewerControl ctrl = logViewerControl;
				NavigateFlag origin = flags & NavigateFlag.OriginMask;
				NavigateFlag align = flags & NavigateFlag.AlignMask;
				switch (origin)
				{
					case NavigateFlag.OriginDate:
						tracer.Info("Selecting the line at the certain time");
						ctrl.SelectMessageAt(time.Value, align);
						break;
					case NavigateFlag.OriginStreamBoundaries:
						switch (align)
						{
							case NavigateFlag.AlignTop:
								tracer.Info("Selecting the first line");
								ctrl.SelectFirstMessage();
								break;
							case NavigateFlag.AlignBottom:
								tracer.Info("Selecting the last line");
								ctrl.SelectLastMessage();
								break;
						}
						break;
				}
			}
		}

		public MessageBase FocusedMessage
		{
			get
			{
				return logViewerControl.FocusedMessage;
			}
		}

		public bool FocusRectIsRequired
		{
			get 
			{
				if (!lastTimeTabHasBeenUsed.HasValue)
					return false;
				if (DateTime.Now - lastTimeTabHasBeenUsed > TimeSpan.FromSeconds(10))
				{
					lastTimeTabHasBeenUsed = null;
					return false;
				}
				return true;
			}
		}

		public IUINavigationHandler UINavigationHandler
		{
			get { return this; }
		}

		#endregion

		void UpdateTrackChangesCheckBox()
		{
			bool f1 = false;
			bool f2 = false;
			foreach (ILogSource s in sourcesListView.SelectedSources)
			{
				if (s.Visible && s.TrackingEnabled)
					f1 = true;
				else
					f2 = true;
				if (f1 && f2)
					break;
			}
			CheckState newState;
			if (f1 && f2)
				newState = CheckState.Indeterminate;
			else if (f1)
				newState = CheckState.Checked;
			else
				newState = CheckState.Unchecked;

			if (trackChangesCheckBox.CheckState != newState)
				trackChangesCheckBox.CheckState = newState;
		}

		void RegisterRecentLogEntry(ILogReader reader)
		{
			string mruEntry = (new RecentLogEntry(reader)).ToString();
			using (RegistryKey key = Registry.CurrentUser.CreateSubKey(MRUSettingsRegKey))
			{
				if (key != null)
				{
					List<string> mru = new List<string>();
					foreach (string s in key.GetValueNames())
					{
						string tmp = key.GetValue(s, "") as string;
						if (!string.IsNullOrEmpty(tmp))
							mru.Add(tmp);
						key.DeleteValue(s);
					}
					int idx = mru.IndexOf(mruEntry);
					if (idx >= 0)
					{
						for (int j = idx; j > 0; --j)
							mru[j] = mru[j - 1];
						mru[0] = mruEntry;
					}
					else
					{
						mru.Insert(0, mruEntry);
					}
					if (mru.Count > 40)
						mru.RemoveAt(mru.Count - 1);
					int i = 0;
					foreach (string s in mru)
						key.SetValue((i++).ToString("00"), s);
				}
			}
		}

		static class Win32Native
		{
			public const int SC_CLOSE = 0xF060;
			public const int MF_GRAYED = 0x1;
			public const int MF_ENABLED = 0x0;

			[DllImport("user32.dll")]
			public static extern IntPtr GetSystemMenu(IntPtr hWnd, bool bRevert);

			[DllImport("user32.dll")]
			public static extern int EnableMenuItem(IntPtr hMenu, int wIDEnableItem, int wEnable);

			[DllImport("user32.dll")]
			public static extern IntPtr GetFocus();
		}

		void SetWaitState(bool wait)
		{
			using (tracer.NewFrame)
			{
				if (wait)
				{
					tracer.Info("Setting wait state");
					focusedControlBeforeWaitState = Control.FromHandle(Win32Native.GetFocus());
				}
				else
				{
					tracer.Info("Exiting from wait state");
				}
				splitContainer2.Enabled = !wait;
				splitContainer2.ForeColor = wait ? Color.Gray : Color.Black;
				Win32Native.EnableMenuItem(Win32Native.GetSystemMenu(this.Handle, false), Win32Native.SC_CLOSE,
					wait ? Win32Native.MF_GRAYED : Win32Native.MF_ENABLED);
				if (!wait)
				{
					if (focusedControlBeforeWaitState != null
					 && !focusedControlBeforeWaitState.IsDisposed
					 && focusedControlBeforeWaitState.Enabled)
					{
						focusedControlBeforeWaitState.Focus();
					}
					focusedControlBeforeWaitState = null;
				}
			}
		}

		static readonly MessageFlag[] checkListBoxFlags = new MessageFlag[]
		{ 
			MessageFlag.Content | MessageFlag.Error, 
			MessageFlag.Content | MessageFlag.Warning,
			MessageFlag.Content | MessageFlag.Info,
			MessageFlag.StartFrame | MessageFlag.EndFrame
		};

		void DoSearch(bool invertDirection)
		{
			UI.LogViewerControl.SearchOptions so;
			so.Template = this.searchTextBox.Text;
			so.WholeWord = this.wholeWordCheckbox.Checked;
			so.ReverseSearch = this.searchUpCheckbox.Checked;
			if (invertDirection)
				so.ReverseSearch = !so.ReverseSearch;
			so.Regexp = this.regExpCheckBox.Checked;
			so.SearchHiddenText = this.hiddenTextCheckBox.Checked;
			so.TypesToLookFor = MessageFlag.None;
			so.MatchCase = this.matchCaseCheckbox.Checked;
			so.StartFrom = new UI.LogViewerControl.SearchOptions.SearchPosition?();
			foreach (int i in this.messageTypesCheckedListBox.CheckedIndices)
				so.TypesToLookFor |= checkListBoxFlags[i];
			UI.LogViewerControl.SearchResult sr = logViewerControl.Search(so);
			if (!sr.Succeeded)
			{
				IStatusReport rpt = GetStatusReport();
				rpt.AutoHide = true;
				rpt.Blink = true;
				rpt.SetStatusString(GetUnseccessfulSearchMessage(so));
			}
		}

		string GetUnseccessfulSearchMessage(UI.LogViewerControl.SearchOptions so)
		{
			List<string> options = new List<string>();
			if (!string.IsNullOrEmpty(so.Template))
				options.Add(so.Template);
			if ((so.TypesToLookFor & MessageFlag.StartFrame) != 0)
				options.Add("Frames");
			if ((so.TypesToLookFor & MessageFlag.Info) != 0)
				options.Add("Infos");
			if ((so.TypesToLookFor & MessageFlag.Warning) != 0)
				options.Add("Warnings");
			if ((so.TypesToLookFor & MessageFlag.Error) != 0)
				options.Add("Errors");
			if (so.WholeWord)
				options.Add("Whole word");
			if (so.MatchCase)
				options.Add("Match case");
			if (so.ReverseSearch)
				options.Add("Search up");
			StringBuilder msg = new StringBuilder();
			msg.Append("No messages not found");
			if (options.Count > 0)
			{
				msg.Append(" (");
				for (int optIdx = 0; optIdx < options.Count; ++optIdx)
					msg.AppendFormat("{0}{1}", (optIdx > 0 ? ", " : ""), options[optIdx]);
				msg.Append(")");
			}
			return msg.ToString();
		}

		private void doSearchButton_Click(object sender, EventArgs e)
		{
			DoSearch(false);
		}

		private void searchTextBox_KeyDown(object sender, KeyEventArgs e)
		{
			if (e.KeyCode == Keys.Enter)
			{
				e.SuppressKeyPress = true;
				e.Handled = true;
				DoSearch(false);
			}
		}

		private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
		{
			using (tracer.NewFrame)
			{
				SetWaitState(true);
				try
				{
					model.Dispose();
					DisposePlugins();
				}
				finally
				{
					SetWaitState(false);
				}
			}
		}

		static readonly string SettingsRegKey = @"Software\LogJoint";
		static readonly string MRUSettingsRegKey = SettingsRegKey + @"\MRU";

		private void MainForm_Load(object sender, EventArgs e)
		{
			UserDefinedFormatsManager.Instance.ReloadFactories();
			
			string[] args = Environment.GetCommandLineArgs();

			if (args.Length > 1)
			{
				for (int i = 1; i < args.Length; ++i)
				{
					FormatAutodetect.DetectedFormat fmt = FormatAutodetect.DetectFormat(args[i]);
					if (fmt == null)
						continue;
					model.LoadFrom(fmt);
				}
			}
		}

		private void recentButton_Click(object sender, EventArgs e)
		{
			UserDefinedFormatsManager.Instance.ReloadFactories();
			mruContextMenuStrip.Items.Clear();
			using (RegistryKey key = Registry.CurrentUser.OpenSubKey(MRUSettingsRegKey, false))
				if (key != null)
				{
					foreach (string s in key.GetValueNames())
					{
						string fname = key.GetValue(s, "") as string;
						if (string.IsNullOrEmpty(fname))
							continue;
						RecentLogEntry entry;
						try
						{
							entry = new RecentLogEntry(fname);
						}
						catch (RecentLogEntry.FormatNotRegistedException)
						{
							continue;
						}
						ToolStripMenuItem item = new ToolStripMenuItem(entry.Factory.GetUserFriendlyConnectionName(entry.ConnectionParams));
						item.Tag = fname;
						mruContextMenuStrip.Items.Add(item);
					}
				}
			if (mruContextMenuStrip.Items.Count == 0)
			{
				mruContextMenuStrip.Items.Add("<No recent files>").Enabled = false;
			}

			mruContextMenuStrip.Show(recentButton, new Point(0, recentButton.Height));
		}

		private void mruContextMenuStrip_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			// Hide the menu here to simplify debugging. Without this call the menu remains 
			// on the screen if execution stops at a breakepoint.
			mruContextMenuStrip.Hide(); 

			string connectString = e.ClickedItem.Tag as string;
			if (string.IsNullOrEmpty(connectString))
				return;
			RecentLogEntry entry = new RecentLogEntry(connectString);
			RegisterRecentLogEntry(model.LoadFrom(entry));
		}

		private void deleteButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Delete sources");

				List<ILogSource> toDelete = new List<ILogSource>();
				foreach (ILogSource s in sourcesListView.SelectedSources)
				{
					tracer.Info("-- source to delete: {0}", s.ToString());
					toDelete.Add(s);
				}

				if (toDelete.Count == 0)
				{
					tracer.Info("Nothing to delete");
					return;
				}

				if (MessageBox.Show(
					string.Format("You are about to remove ({0}) source(s).\nAre you sure?", toDelete.Count),
					this.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) != DialogResult.Yes)
				{
					tracer.Info("User didn't confirm the deletion");
					return;
				}

				SetWaitState(true);
				try
				{
					model.DeleteLogs(toDelete.ToArray());
				}
				finally
				{
					SetWaitState(false);
				}
			}
		}

		void UpdateView(bool liteUpdate)
		{
			if (!liteUpdate)
			{
				if (model.Updates.ValidateThreads())
				{
					threadsListView.UpdateView();
					model.Bookmarks.PurgeBookmarksForDisposedThreads();
				}

				if (model.Updates.ValidateTimeline())
				{
					timeLineControl.UpdateView();
				}

				if (model.Updates.ValidateMessages())
				{
					logViewerControl.UpdateView();
					model.SetCurrentViewPositionIfNeeded();
				}

				if (model.Updates.ValidateFilters())
					displayFiltersListView.UpdateView();

				if (model.Updates.ValidateHighlightFilters())
					hlFiltersListView.UpdateView();

				if (model.Updates.ValidateTimeGaps())
					model.TimeGaps.Update(timeLineControl.TimeRange);

				SetAnalizingIndication(model.TimeGaps.IsWorking);
			}
			if (model.Updates.ValidateSources())
			{
				sourcesListView.UpdateView();
				UpdateTrackChangesCheckBox();
			}
		}

		private void updateViewTimer_Tick(object sender, EventArgs e)
		{
			++updateTimeTick;
			++liveLogsTick;

			if (liveUpdateLock == 0)
			{
				UpdateView((updateTimeTick % 3) != 0);
				if ((liveLogsTick % 6) == 0)
					model.Refresh();
			}

			if (autoHideStatusReport != null)
			{
				autoHideStatusReport.AutoHideIfItIsTime();
			}
		}

		void InitLogFactories()
		{
			Assembly[] asmsToAnalize = new Assembly[] { Assembly.GetEntryAssembly() };

			foreach (Assembly asm in asmsToAnalize)
			{
				foreach (Type t in asm.GetTypes())
				{
					if (t.IsClass && typeof(ILogReaderFactory).IsAssignableFrom(t))
					{
						System.Runtime.CompilerServices.RuntimeHelpers.RunClassConstructor(t.TypeHandle);
					}
				}
			}
		}

		void InitPlugins()
		{
			using (tracer.NewFrame)
			{
				string thisPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				tracer.Info("Plugins directory: {0}", thisPath);
				foreach (string pluginPath in Directory.GetFiles(thisPath, "*.plugin.dll"))
				{
					tracer.Info("---> Plugin found {0}", pluginPath);
					Assembly pluginAsm;
					try
					{
						pluginAsm = Assembly.LoadFrom(pluginPath);
					}
					catch (Exception e)
					{
						tracer.Error(e, "Filed to load plugin");
						continue;
					}
					Type pluginType = pluginAsm.GetType("LogJoint.Plugin");
					if (pluginType == null)
					{
						tracer.Warning("Plugin class not found in plugin assembly");
						continue;
					}
					if (!typeof(IPlugin).IsAssignableFrom(pluginType))
					{
						tracer.Warning("Plugin object doesn't support IPlugin");
						continue;
					}
					try
					{
						plugins.Add((IPlugin)Activator.CreateInstance(pluginType));
					}
					catch (Exception e)
					{
						tracer.Error(e, "Filed to create an instance of plugin");
						continue;
					}
				}
			}
		}

		void DisposePlugins()
		{
			foreach (IPlugin plugin in plugins)
			{
				plugin.Dispose();
			}
		}

		private void addNewLogButton_Click(object sender, EventArgs e)
		{
			UserDefinedFormatsManager.Instance.ReloadFactories();
			if (newLogSourceDialog == null)
				newLogSourceDialog = new NewLogSourceDialog(model);
			newLogSourceDialog.Execute();
		}

		void SetAnalizingIndication(bool analizing)
		{
			if (isAnalizing == analizing)
				return;
			toolStripAnalizingImage.Visible = analizing;
			toolStripAnalizingLabel.Visible = analizing;
			isAnalizing = analizing;
		}

		class StatusReport : IStatusReport
		{
			MainForm owner;
			int ticksWhenAutoHideStarted;

			public StatusReport(MainForm owner)
			{
				this.owner = owner;
			}

			#region IStatusReport Members

			public void SetStatusString(string text)
			{
				owner.toolStripStatusLabel.Text = !string.IsNullOrEmpty(text) ? text : "";
			}

			public bool AutoHide
			{
				get 
				{
					return IsAutoHide; 
				}
				set 
				{
					if (!IsActive)
						return;
					if (value == AutoHide)
						return;
					if (value)
					{
						ticksWhenAutoHideStarted = Environment.TickCount;
						owner.autoHideStatusReport = this;
					}
					else
					{
						owner.autoHideStatusReport = this;
					}
				}
			}

			public bool Blink
			{
				get 
				{ 
					return IsBlinking; 
				}
				set 
				{
					if (IsBlinking == value)
						return;
					if (!IsActive)
						return;
					owner.toolStripStatusImage.Visible = value;
				}
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				if (IsAutoHide)
				{
					owner.autoHideStatusReport = null;
				}
				if (IsBlinking)
				{
					owner.toolStripStatusImage.Visible = false;
				}
				if (IsActive)
				{
					SetStatusString(null);
					owner.statusReport = null;
				}
			}

			#endregion

			public void AutoHideIfItIsTime()
			{
				if (Environment.TickCount - this.ticksWhenAutoHideStarted > 1000 * 3)
					Dispose();
			}

			bool IsActive
			{
				get { return owner.statusReport == this; }
			}
			bool IsAutoHide
			{
				get { return owner.autoHideStatusReport == this; }
			}
			bool IsBlinking
			{
				get { return IsActive && owner.toolStripStatusImage.Visible; }
			}
		};

		protected override bool ProcessTabKey(bool forward)
		{
			this.lastTimeTabHasBeenUsed = DateTime.Now;
			return base.ProcessTabKey(forward);
		}

		private void DoToggleBookmark()
		{
			MessageBase l = logViewerControl.FocusedMessage;
			if (l != null)
			{
				model.Bookmarks.ToggleBookmark(l);
				UpdateView(false);
			}
			else
			{
				tracer.Warning("There is no lines selected");
			}
		}

		private void toggleBookmarkButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Toggle Bookmark.");
				DoToggleBookmark();
			}
		}

		private void deleteAllBookmarksButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Clear Bookmarks.");

				if (model.Bookmarks.Count == 0)
				{
					tracer.Info("Nothing to clear");
					return;
				}

				if (MessageBox.Show(
					string.Format("You are about to delete ({0}) bookmark(s).\nAre you sure?", model.Bookmarks.Count),
					this.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) != DialogResult.Yes)
				{
					tracer.Info("User didn't confirm the cleaning");
					return;
				}

				model.Bookmarks.Clear();
				UpdateView(false);
			}
		}

		void NextBookmark(bool forward)
		{
			if (!logViewerControl.NextBookmark(forward))
			{
				IStatusReport rpt = GetStatusReport();
				rpt.SetStatusString("Bookmark not found");
				rpt.AutoHide = true;
				rpt.Blink = true;
			}
		}

		private void prevBookmarkButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Prev Bookmark.");
				NextBookmark(false);
			}
		}

		private void nextBookmarkButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Next Bookmark.");
				NextBookmark(true);
			}
		}

		private void timeLineControl1_Navigate(object sender, LogJoint.UI.TimeNavigateEventArgs args)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Navigate from timeline. Date='{0}', Flags={1}.", args.Date, args.Flags);
				model.NavigateTo(args.Date, args.Flags);
			}
		}

		private void cancelShiftingDropDownButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Cancel Shifting");
				model.CancelShifting();
			}
		}

		private void MainForm_KeyDown(object sender, KeyEventArgs e)
		{
			if (cancelShiftingDropDownButton.Visible && e.KeyData == Keys.Escape)
				cancelShiftingDropDownButton_Click(sender, e);
			if (e.KeyData == (Keys.F | Keys.Control))
			{
				menuTabControl.SelectedTab = searchTabPage;
			}
			else if (e.KeyData == Keys.F3)
			{
				DoSearch(false);
			}
			else if (e.KeyData == (Keys.F3 | Keys.Shift))
			{
				DoSearch(true);
			}
			else if (e.KeyData == (Keys.K | Keys.Control))
			{
				DoToggleBookmark();
			}
			else if (e.KeyData == Keys.F2)
			{
				NextBookmark(true);
			}
			else if (e.KeyData == (Keys.F2 | Keys.Shift))
			{
				NextBookmark(false);
			}
		}

		#region IUINavigationHandler Members

		public void ShowLine(IBookmark bmk)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Boomark={0}", bmk);
				logViewerControl.SelectMessageAt(bmk);
			}
		}

		public void ShowThread(IThread thread)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Thread={0}", thread.DisplayName);
				menuTabControl.SelectedTab = threadsTabPage;
				threadsListView.Select(thread);
			}
		}

		public void ShowLogSource(ILogSource source)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Source={0}", source.DisplayName);
				menuTabControl.SelectedTab = sourcesTabPage;
				sourcesListView.Select(source);
			}
		}

		#endregion

		private void addDisplayFilterClick(object sender, EventArgs e)
		{
			using (FilterDialog d = new FilterDialog(model.DisplayFiltersListViewHost))
			{
				Filter f = new Filter(FilterAction.Include, string.Format("New filter {0}", ++filterIndex), true, "", false, false, false);
				try
				{
					if (!d.Execute(f))
					{
						return;
					}
					model.DisplayFilters.Insert(0, f);
					f = null;
					UpdateView(false);
				}
				finally
				{
					if (f != null)
					{
						f.Dispose();
					}
				}
			}
		}

		void DeleteFiltersInternal(FiltersListView lv, FiltersList fl)
		{
			List<Filter> toDelete = new List<Filter>();
			foreach (Filter f in lv.SelectedFilters)
			{
				tracer.Info("-- filter to delete: {0}", f.ToString());
				toDelete.Add(f);
			}

			if (toDelete.Count == 0)
			{
				tracer.Info("Nothing to delete");
				return;
			}

			if (MessageBox.Show(
				string.Format("You are about to delete ({0}) filter(s).\nAre you sure?", toDelete.Count),
				this.Text, MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) != DialogResult.Yes)
			{
				tracer.Info("User didn't confirm the deletion");
				return;
			}

			fl.Delete(toDelete);
			UpdateView(false);
		}

		private void deleteDisplayFilterButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Delete filters");
				DeleteFiltersInternal(displayFiltersListView, model.DisplayFilters);
			}
		}

		void MoveFilterInternal(FiltersListView lv, FiltersList fl, bool up)
		{
			foreach (Filter f in lv.SelectedFilters)
			{
				fl.Move(f, up);
				UpdateView(false);
				break;
			}
		}

		private void moveDisplayFilterUpButton_Click(object sender, EventArgs e)
		{
			MoveFilterInternal(displayFiltersListView, model.DisplayFilters, sender == this.moveDisplayFilterUpButton);
		}

		private void aboutLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
		{
			using (AboutBox aboutBox = new AboutBox())
			{
				aboutBox.ShowDialog();
			}
		}

		private void trackChangesCheckBox_Click(object sender, EventArgs e)
		{
			bool value = trackChangesCheckBox.CheckState == CheckState.Unchecked;
			foreach (ILogSource s in sourcesListView.SelectedSources)
				s.TrackingEnabled = value;
			UpdateTrackChangesCheckBox();
		}

		private void removeHLButton_Click(object sender, EventArgs e)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Delete highlight filters");
				DeleteFiltersInternal(hlFiltersListView, model.HighlightFilters);
			}
		}

		private void moveHLFilterUpButton_Click(object sender, EventArgs e)
		{
			MoveFilterInternal(hlFiltersListView, model.HighlightFilters, sender == moveHLFilterUpButton);
		}

		private void addHLFilterButton_Click(object sender, EventArgs e)
		{
			using (FilterDialog d = new FilterDialog(model.HighlightFiltersListViewHost))
			{
				Filter f = new Filter(
					FilterAction.Include, 
					string.Format("New filter {0}", ++filterIndex), 
					true, "", false, false, false);
				try
				{
					if (!d.Execute(f))
					{
						return;
					}
					model.HighlightFilters.Insert(0, f);
					f = null;
					UpdateView(false);
				}
				finally
				{
					if (f != null)
					{
						f.Dispose();
					}
				}
			}
		}


	}

}