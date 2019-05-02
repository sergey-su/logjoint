using System;
using System.Text;
using System.Linq;
using System.Diagnostics;
using LogJoint.AutoUpdate;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.MainForm
{
	public class Presenter: IPresenter, IViewEvents
	{
		public Presenter(
			ILogSourcesManager logSources,
			Preprocessing.ILogSourcesPreprocessingManager preprocessingsManager,
			IView view,
			LogViewer.IPresenter viewerPresenter,
			SearchResult.IPresenter searchResultPresenter,
			SearchPanel.IPresenter searchPanelPresenter,
			SourcesList.IPresenter sourcesListPresenter,
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
			IShutdown shutdown,
			IColorTheme theme
		)
		{
			this.tracer = new LJTraceSource("UI", "ui.main");
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

			view.SetPresenter(this);

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
					messagePropertiesDialogPresenter.ShowDialog();
				};
			}

			if (searchResultPresenter != null)
			{
				searchResultPresenter.OnClose += (sender, args) => searchPanelPresenter.CollapseSearchResultPanel();
				searchResultPresenter.OnResizingStarted += (sender, args) => view.BeginSplittingSearchResults();
			}

			sourcesListPresenter.OnBusyState += (_, evt) => SetWaitState(evt.BusyStateRequired);

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
			sourcesManagerPresenter.OnViewUpdated += (sender, evt) =>
			{
				UpdateMillisecondsDisplayMode();
			};

			logSources.OnLogSourceAdded += (sender, evt) =>
			{
				UpdateFormCaption();
			};
			logSources.OnLogSourceRemoved += (sender, evt) =>
			{
				UpdateFormCaption();
			};

			if (autoUpdater != null)
			{
				autoUpdater.Changed += (sender, args) =>
				{
					UpdateAutoUpdateIcon();
				};
			}

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
			view.ActivateTab(tabId);
		}

		void IPresenter.Close()
		{
			view.Close();
		}

		string IPresenter.AddCustomTab(object uiControl, string caption, object tag)
		{
			string tabId = string.Format ("tab#{0}", ++lastCustomTabId);
			view.AddTab(tabId, caption, uiControl, tag);
			return tabId;
		}

		async void IViewEvents.OnClosing()
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
			
		void IViewEvents.OnLoad()
		{
			if (Loaded != null)
				Loaded(this, EventArgs.Empty);
		}

		void IViewEvents.OnTabChanging(string tabId, object tag)
		{
			if (TabChanging != null)
				TabChanging (this, new TabChangingEventArgs (tabId, tag));
		}

		void IViewEvents.OnTabPressed()
		{
			tabUsageTracker.OnTabPressed();
		}

		void IViewEvents.OnShareButtonClicked()
		{
			sharingDialogPresenter.ShowDialog();
		}
		
		void IViewEvents.OnReportProblemMenuItemClicked()
		{
			issueReportDialogPresenter.ShowDialog();
		}

		void IViewEvents.OnKeyPressed(KeyCode key)
		{
			if (key == KeyCode.Escape)
			{
				statusRepors.CancelActiveStatus();
			}
			if (key == KeyCode.FindShortcut)
			{
				searchPanelPresenter.ReceiveInputFocusByShortcut(forceSearchAllOccurencesMode: false);
				view.ActivateTab(TabIDs.Search);
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

		void IViewEvents.OnOptionsLinkClicked()
		{
			view.ShowOptionsMenu();
		}

		void IViewEvents.OnAboutMenuClicked()
		{
			presentersFacade.ShowAboutDialog();
		}

		void IViewEvents.OnConfigurationMenuClicked()
		{
			presentersFacade.ShowOptionsDialog();
		}

		void IViewEvents.OnRestartPictureClicked()
		{
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

		void IViewEvents.OnOpenRecentMenuClicked()
		{
			presentersFacade.ShowHistoryDialog();
		}

		bool IViewEvents.OnDragOver(object data)
		{
			return dragDropHandler.ShouldAcceptDragDrop(data);
		}

		void IViewEvents.OnDragDrop(object data, bool controlKeyHeld)
		{
			dragDropHandler.AcceptDragDrop(data, controlKeyHeld);
		}

		void IViewEvents.OnRawViewButtonClicked()
		{
			viewerPresenter.ShowRawMessages = !viewerPresenter.ShowRawMessages;
		}

		#region Implementation

		void UpdateMillisecondsDisplayMode()
		{
			bool atLeastOneSourceWantMillisecondsAlways = logSources.Items.Any(s => !s.IsDisposed && s.Visible && s.Provider.Factory.ViewOptions.AlwaysShowMilliseconds);
			viewerPresenter.ShowMilliseconds = atLeastOneSourceWantMillisecondsAlways;
		}

		void UpdateFormCaption()
		{
			var sources = logSources.Items.Where(s => !s.IsDisposed).ToArray();
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
			view.SetShareButtonState(
				visible: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.PermanentlyUnavaliable,
				enabled: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.TemporarilyUnavailable,
				progress: sharingDialogPresenter.IsBusy
			);
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

		void UpdateAutoUpdateIcon()
		{
			view.SetUpdateIconVisibility(autoUpdater.State == AutoUpdateState.WaitingRestart);
		}

		readonly ILogSourcesManager logSources;
		readonly Preprocessing.ILogSourcesPreprocessingManager preprocessingsManager;
		readonly IView view;
		readonly LJTraceSource tracer;
		readonly ITabUsageTracker tabUsageTracker;
		readonly LogViewer.IPresenter viewerPresenter;
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
		readonly IShutdown shutdown;
		readonly StatusReports.IPresenter statusRepors;
		readonly IssueReportDialogPresenter.IPresenter issueReportDialogPresenter;
		readonly IColorTheme theme;

		IInputFocusState inputFocusBeforeWaitState;
		bool isAnalyzing;
		int lastCustomTabId;


		#endregion
	};
};