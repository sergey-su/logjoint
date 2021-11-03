using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using LogJoint.AutoUpdate;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace LogJoint.UI.Presenters.MainForm
{
	public class Presenter: IPresenter, IViewModel
	{
		public Presenter(
			ILogSourcesManager logSources,
			Preprocessing.IManager preprocessingsManager,
			IView view,
			LogViewer.IPresenterInternal viewerPresenter,
			SearchResult.IPresenter searchResultPresenter,
			SearchPanel.IPresenter searchPanelPresenter,
			SourcesManager.IPresenter sourcesManagerPresenter,
			MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			BookmarksManager.IPresenter bookmarksManagerPresenter,
			IHeartBeatTimer heartBeatTimer,
			ITabUsageTracker tabUsageTracker,
			StatusReports.IPresenter statusReportFactory,
			IDragDropHandler dragDropHandler,
			IPresentersFacade presentersFacade,
			IAutoUpdater autoUpdater,
			Progress.IProgressAggregator progressAggregator,
			IAlertPopup alerts,
			SharingDialog.IPresenter sharingDialogPresenter,
			IssueReportDialogPresenter.IPresenter issueReportDialogPresenter,
			IShutdownSource shutdown,
			IColorTheme theme,
			IChangeNotification changeNotification,
			ITraceSourceFactory traceSourceFactory
		)
		{
			this.tracer = traceSourceFactory.CreateTraceSource("UI", "ui.main");
			this.logSources = logSources;
			this.preprocessingsManager = preprocessingsManager;
			this.view = view;
			this.tabUsageTracker = tabUsageTracker;
			this.searchPanelPresenter = searchPanelPresenter;
			this.searchResultPresenter = searchResultPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;
			this.viewerPresenter = viewerPresenter;
			this.presentersFacade = presentersFacade;
			this.dragDropHandler = dragDropHandler;
			this.heartBeatTimer = heartBeatTimer;
			this.autoUpdater = autoUpdater;
			this.progressAggregator = progressAggregator;
			this.alerts = alerts;
			this.sharingDialogPresenter = sharingDialogPresenter;
			this.issueReportDialogPresenter = issueReportDialogPresenter;
			this.shutdown = shutdown;
			this.statusRepors = statusReportFactory;
			this.theme = theme;
			this.changeNotification = changeNotification;

			var sourcesTab = new TabInfo { Id = TabIDs.Sources, Caption = "Log Sources" };
			var threadsTab = new TabInfo { Id = TabIDs.Threads, Caption = "Threads" };
			var highlightingRulesTab = new TabInfo { Id = TabIDs.HighlightingFilteringRules, Caption = "Highlighting Rules" };
			var searchTab = new TabInfo { Id = TabIDs.Search, Caption = "Search" };
			var bookmarksTab = new TabInfo { Id = TabIDs.Bookmarks, Caption = "Bookmarks" };
			var postprocessingTab = new TabInfo { Id = TabIDs.Postprocessing, Caption = "Postprocessing" };
			var debugTab = new TabInfo { Id = TabIDs.Debug, Caption = "[Debug]" };

			visibleTabs =
				IsBrowser.Value ?
					new [] { sourcesTab, bookmarksTab, searchTab, postprocessingTab, debugTab } :
				RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
					new [] { sourcesTab, bookmarksTab, highlightingRulesTab, searchTab, postprocessingTab }
				:
					new [] { sourcesTab, threadsTab, highlightingRulesTab, searchTab, bookmarksTab, postprocessingTab };
			activeTab = Selectors.Create(() => visibleTabs, () => lastActivatedTab,
				(visible, lastActivated) => visible.IndexOf(i => i.Id == lastActivated).GetValueOrDefault(0));

			view.SetViewModel(this);

			viewerPresenter.ManualRefresh += delegate(object sender, EventArgs args)
			{
				using (tracer.NewFrame)
				{
					tracer.Info("----> User Command: Refresh");
					logSources.Refresh();
				}
			};
			viewerPresenter.FocusedMessageBookmarkChanged += delegate(object sender, EventArgs args)
			{
				if (searchResultPresenter != null)
					searchResultPresenter.MasterFocusedMessage = viewerPresenter.FocusedMessageBookmark;
			};
			if (messagePropertiesDialogPresenter != null)
			{
				viewerPresenter.DefaultFocusedMessageActionCaption = "Show properties...";
				viewerPresenter.DefaultFocusedMessageAction += (s, e) =>
				{
					messagePropertiesDialogPresenter.Show();
				};
			}

			if (searchResultPresenter != null)
			{
				searchResultPresenter.OnClose += (sender, args) => searchPanelPresenter.CollapseSearchResultPanel();
				searchResultPresenter.OnResizingStarted += (sender, args) => view.BeginSplittingSearchResults();
			}

			sourcesManagerPresenter.OnBusyState += (_, evt) => SetWaitState(evt.BusyStateRequired);

			searchPanelPresenter.InputFocusAbandoned += delegate(object sender, EventArgs args)
			{
				loadedMessagesPresenter.LogViewerPresenter.ReceiveInputFocus();
			};
			loadedMessagesPresenter.OnResizingStarted += (s, e) => view.BeginSplittingTabsPanel();

			this.heartBeatTimer.OnTimer += (sender, e) =>
			{
				if (e.IsRareUpdate)
					SetAnalyzingIndication(logSources.Items.Any(s => s.TimeGaps.IsWorking));
			};

			logSources.OnLogSourceAdded += (sender, evt) =>
			{
				UpdateFormCaption();
			};
			logSources.OnLogSourceRemoved += (sender, evt) =>
			{
				UpdateFormCaption();
			};

			progressAggregator.ProgressStarted += (sender, args) =>
			{
				view.SetTaskbarState(TaskbarState.Progress);
				UpdateFormCaption();
			};

			progressAggregator.ProgressEnded += (sender, args) =>
			{
				view.SetTaskbarState(TaskbarState.Idle);
				UpdateFormCaption();
			};

			progressAggregator.ProgressChanged += (sender, args) =>
			{
				view.UpdateTaskbarProgress(args.ProgressPercentage);
				UpdateFormCaption();
			};

			if (sharingDialogPresenter != null)
			{
				sharingDialogPresenter.AvailabilityChanged += (sender, args) =>
				{
					UpdateShareButton();
				};
				sharingDialogPresenter.IsBusyChanged += (sender, args) =>
				{
					UpdateShareButton();
				};
			};

			UpdateFormCaption();
			UpdateShareButton();

			view.SetIssueReportingMenuAvailablity(issueReportDialogPresenter.IsAvailable);
		}

		public event EventHandler Loaded;
		public event EventHandler<TabChangingEventArgs> TabChanging;

		void IPresenter.ExecuteThreadPropertiesDialog(IThread thread)
		{
			view.ExecuteThreadPropertiesDialog(thread, presentersFacade, theme);
		}

		void IPresenter.ActivateTab(string tabId)
		{
			this.lastActivatedTab = tabId;
			changeNotification.Post();
		}

		void IPresenter.Close()
		{
			view.Close();
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IReadOnlyList<TabInfo> IViewModel.VisibleTabs => visibleTabs;

		int IViewModel.ActiveTab => activeTab();

		async void IViewModel.OnClosing()
		{
			using (tracer.NewFrame)
			{
				SetWaitState(true);
				try
				{
					heartBeatTimer.Suspend();
					await shutdown.Shutdown();
				}
				finally
				{
					SetWaitState(false);
				}
				view.ForceClose();
			}
		}
			
		void IViewModel.OnLoad()
		{
			if (Loaded != null)
				Loaded(this, EventArgs.Empty);
		}

		void IViewModel.OnChangeTab(string tabId)
		{
			var old = activeTab();
			lastActivatedTab = tabId;
			changeNotification.Post();
			if (old != activeTab())
			{
				TabChanging?.Invoke(this, new TabChangingEventArgs(visibleTabs[activeTab()].Id));
			}
		}

		void IViewModel.OnTabPressed()
		{
			tabUsageTracker.OnTabPressed();
		}

		void IViewModel.OnShareButtonClicked()
		{
			sharingDialogPresenter.ShowDialog();
		}
		
		void IViewModel.OnReportProblemMenuItemClicked()
		{
			issueReportDialogPresenter.ShowDialog();
		}

		void IViewModel.OnKeyPressed(KeyCode key)
		{
			if (key == KeyCode.Escape)
			{
				statusRepors.CancelActiveStatus();
			}
			if (key == KeyCode.FindShortcut)
			{
				lastActivatedTab = TabIDs.Search;
				changeNotification.Post();
				searchPanelPresenter.ReceiveInputFocusByShortcut(forceSearchAllOccurencesMode: false);
			}
			else if (key == KeyCode.FindNextShortcut)
			{
				searchPanelPresenter.PerformSearch();
			}
			else if (key == KeyCode.FindPrevShortcut)
			{
				searchPanelPresenter.PerformReversedSearch();
			}
			else if (key == KeyCode.ToggleBookmarkShortcut)
			{
				bookmarksManagerPresenter.ToggleBookmark();
			}
			else if (key == KeyCode.NextBookmarkShortcut)
			{
				bookmarksManagerPresenter.ShowNextBookmark();
			}
			else if (key == KeyCode.PrevBookmarkShortcut)
			{
				bookmarksManagerPresenter.ShowPrevBookmark();
			}
			else if (key == KeyCode.HistoryShortcut)
			{
				presentersFacade.ShowHistoryDialog();
			}
			else if (key == KeyCode.NewWindowShortcut)
			{
				Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
			}
			else if (key == KeyCode.FindCurrentTimeShortcut)
			{
				searchResultPresenter.FindCurrentTime();
			}
		}

		void IViewModel.OnOptionsLinkClicked()
		{
			view.ShowOptionsMenu();
		}

		void IViewModel.OnAboutMenuClicked()
		{
			presentersFacade.ShowAboutDialog();
		}

		void IViewModel.OnConfigurationMenuClicked()
		{
			presentersFacade.ShowOptionsDialog();
		}

		void IViewModel.OnRestartPictureClicked()
		{
			if (autoUpdater?.State != AutoUpdateState.WaitingRestart)
				return;
			if (alerts.ShowPopup(
				"App restart",
				"Updated application binaries have been downloaded and they are ready for use. " +
				"Restart application to apply update." + Environment.NewLine +
				"Restart now?",
				AlertFlags.YesNoCancel | AlertFlags.QuestionIcon
			) == AlertFlags.Yes)
			{
				autoUpdater.TrySetRestartAfterUpdateFlag();
				view.Close();
			}
		}

		void IViewModel.OnOpenRecentMenuClicked()
		{
			presentersFacade.ShowHistoryDialog();
		}

		bool IViewModel.OnDragOver(object data)
		{
			return dragDropHandler.ShouldAcceptDragDrop(data);
		}

		void IViewModel.OnDragDrop(object data, bool controlKeyHeld)
		{
			dragDropHandler.AcceptDragDrop(data, controlKeyHeld);
		}

		void IViewModel.OnRawViewButtonClicked()
		{
			viewerPresenter.ShowRawMessages = !viewerPresenter.ShowRawMessages;
		}

		(AutoUpdateButtonState, string) IViewModel.AutoUpdateButton
		{
			get
			{
				if (autoUpdater != null)
				{
					switch (autoUpdater.State)
					{
						case AutoUpdateState.WaitingRestart: return (AutoUpdateButtonState.WaitingRestartIcon, "New software update is available. Restart application to apply it.");
						case AutoUpdateState.Checking: return (AutoUpdateButtonState.ProgressIcon, "Software update procedure is in progress");
					}
				}
				return (AutoUpdateButtonState.Hidden, "");
			}
		}

		double? IViewModel.Size => size;

		void IViewModel.OnResizing(double size)
		{
			this.size = Math.Max(0, size);
			changeNotification.Post();
		}

		string IViewModel.ResizerTooltip => "Resize the tabs panel";

		#region Implementation

		void UpdateFormCaption()
		{
			var sources = logSources.Items.ToArray();
			var builder = new StringBuilder();
			HashSet<string> reportedContainers = null;
			foreach (var srcEntry in sources.Select(source => new { 
				ls = source,
				container = preprocessingsManager.ExtractContentsContainerNameFromConnectionParams(source.Provider.ConnectionParams)
			}))
			{
				string name = null;
				if (srcEntry.container == null)
					name = srcEntry.ls.Provider.GetTaskbarLogName();
				else if ((reportedContainers ?? (reportedContainers = new HashSet<string>())).Add(srcEntry.container))
					name = srcEntry.container;
				if (name == null)
					continue;
				if (builder.Length > 0)
					builder.Append(", ");
				builder.Append(name);
			}
			var progress = progressAggregator.ProgressValue;
			if (progress != null)
				builder.AppendFormat(" {0}%", (int)(progress.Value * 100d));
			if (builder.Length > 0)
				builder.Append(" - ");
			builder.Append("LogJoint Log Viewer");
			view.SetCaption(builder.ToString());
		}

		void UpdateShareButton()
		{
			if (sharingDialogPresenter != null)
			{
				view.SetShareButtonState(
					visible: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.PermanentlyUnavaliable,
					enabled: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.TemporarilyUnavailable,
					progress: sharingDialogPresenter.IsBusy
				);
			}
		}

		void SetWaitState(bool wait)
		{
			if (wait)
			{
				tracer.Info("setting view wait state ON");
				inputFocusBeforeWaitState = view.CaptureInputFocusState();
			}
			else
			{
				tracer.Info("setting view wait state OFF");
			}
			view.EnableFormControls(!wait);
			if (!wait)
			{
				inputFocusBeforeWaitState.Restore();
			}
		}

		void SetAnalyzingIndication(bool analyzing)
		{
			if (isAnalyzing == analyzing)
				return;
			view.SetAnalyzingIndicationVisibility(analyzing);
			isAnalyzing = analyzing;
		}

		readonly ILogSourcesManager logSources;
		readonly Preprocessing.IManager preprocessingsManager;
		readonly IView view;
		readonly LJTraceSource tracer;
		readonly ITabUsageTracker tabUsageTracker;
		readonly LogViewer.IPresenterInternal viewerPresenter;
		readonly SearchPanel.IPresenter searchPanelPresenter;
		readonly SearchResult.IPresenter searchResultPresenter;
		readonly BookmarksManager.IPresenter bookmarksManagerPresenter;
		readonly IPresentersFacade presentersFacade;
		readonly IDragDropHandler dragDropHandler;
		readonly IHeartBeatTimer heartBeatTimer;
		readonly IAutoUpdater autoUpdater;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly IAlertPopup alerts;
		readonly SharingDialog.IPresenter sharingDialogPresenter;
		readonly IShutdownSource shutdown;
		readonly StatusReports.IPresenter statusRepors;
		readonly IssueReportDialogPresenter.IPresenter issueReportDialogPresenter;
		readonly IColorTheme theme;
		readonly IChangeNotification changeNotification;
		readonly IReadOnlyList<TabInfo> visibleTabs;

		IInputFocusState inputFocusBeforeWaitState;
		bool isAnalyzing;
		string lastActivatedTab;
		readonly Func<int> activeTab;
		double? size = null;

		#endregion
	};
};