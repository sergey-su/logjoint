using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LogJoint.Preprocessing;
using System.Diagnostics;
using System.IO;
using LogJoint.AutoUpdate;

namespace LogJoint.UI.Presenters.MainForm
{
	public class Presenter: IPresenter, IViewEvents
	{
		public Presenter( // todo: refactor to reduce the nr of dependencies
			IModel model,
			IView view,
			UI.Presenters.LogViewer.IPresenter viewerPresenter,
			SearchResult.IPresenter searchResultPresenter,
			SearchPanel.IPresenter searchPanelPresenter,
			SourcesList.IPresenter sourcesListPresenter,
			SourcesManager.IPresenter sourcesManagerPresenter,
			Timeline.IPresenter timelinePresenter,
			MessagePropertiesDialog.IPresenter messagePropertiesDialogPresenter,
			LoadedMessages.IPresenter loadedMessagesPresenter,
			ICommandLineHandler commandLineHandler,
			BookmarksManager.IPresenter bookmarksManagerPresenter,
			IHeartBeatTimer heartBeatTimer,
			ITabUsageTracker tabUsageTracker,
			StatusReports.IPresenter statusReportFactory,
			IDragDropHandler dragDropHandler,
			IPresentersFacade navHandler, // todo: remove this dependency
			Options.Dialog.IPresenter optionsDialogPresenter,
			IAutoUpdater autoUpdater,
			Progress.IProgressAggregator progressAggregator,
			HistoryDialog.IPresenter historyDialogPresenter,
			About.IPresenter aboutDialogPresenter
		)
		{
			this.model = model;
			this.view = view;
			this.tracer = new LJTraceSource("UI", "ui.main");
			this.tabUsageTracker = tabUsageTracker;
			this.commandLineHandler = commandLineHandler;
			this.searchPanelPresenter = searchPanelPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;
			this.viewerPresenter = viewerPresenter;
			this.timelinePresenter = timelinePresenter;
			this.navHandler = navHandler;
			this.dragDropHandler = dragDropHandler;
			this.optionsDialogPresenter = optionsDialogPresenter;
			this.heartBeatTimer = heartBeatTimer;
			this.autoUpdater = autoUpdater;
			this.progressAggregator = progressAggregator;
			this.historyDialogPresenter = historyDialogPresenter;
			this.aboutDialogPresenter = aboutDialogPresenter;

			view.SetPresenter(this);

			viewerPresenter.ManualRefresh += delegate(object sender, EventArgs args)
			{
				using (tracer.NewFrame)
				{
					tracer.Info("----> User Command: Refresh");
					model.SourcesManager.Refresh();
				}
			};
			viewerPresenter.BeginShifting += delegate(object sender, EventArgs args)
			{
				SetWaitState(true);
				view.EnableOwnedForms(false);
				statusReportFactory.CreateNewStatusReport().ShowStatusText("Moving in-memory window...", false);
				view.SetCancelLongRunningControlsVisibility(true);
				longRunningProcessCancellationRoutine = model.SourcesManager.CancelShifting;
			};
			viewerPresenter.EndShifting += delegate(object sender, EventArgs args)
			{
				longRunningProcessCancellationRoutine = null;
				view.SetCancelLongRunningControlsVisibility(false);
				statusReportFactory.CreateNewStatusReport().Dispose();
				SetWaitState(false);
				view.EnableOwnedForms(true);
			};
			viewerPresenter.FocusedMessageChanged += delegate(object sender, EventArgs args)
			{
				model.SourcesManager.OnCurrentViewPositionChanged(viewerPresenter.FocusedMessageTime);
				if (searchResultPresenter != null)
					searchResultPresenter.MasterFocusedMessage = viewerPresenter.FocusedMessage;
			};
			viewerPresenter.DefaultFocusedMessageActionCaption = "Show properties...";
			viewerPresenter.DefaultFocusedMessageAction += (s, e) =>
			{
				messagePropertiesDialogPresenter.ShowDialog();
			};

			if (searchResultPresenter != null)
			{
				searchResultPresenter.OnClose += (sender, args) => searchPanelPresenter.CollapseSearchResultPanel();
				searchResultPresenter.OnResizingStarted += (sender, args) => view.BeginSplittingSearchResults();
			}

			sourcesListPresenter.OnBusyState += (_, evt) => SetWaitState(evt.BusyStateRequired);

			sourcesManagerPresenter.OnBusyState += (_, evt) => SetWaitState(evt.BusyStateRequired);

			if (timelinePresenter != null)
			{
				timelinePresenter.RangeChanged += delegate(object sender, EventArgs args)
				{
					UpdateMillisecondsDisplayMode();
				};
			}

			searchPanelPresenter.InputFocusAbandoned += delegate(object sender, EventArgs args)
			{
				loadedMessagesPresenter.Focus();
			};
			loadedMessagesPresenter.OnResizingStarted += (s, e) => view.BeginSplittingTabsPanel();

			model.SourcesManager.OnSearchStarted += (sender, args) =>
			{
				SetWaitState(true);
				statusReportFactory.CreateNewStatusReport().ShowStatusText("Searching...", false);
				view.SetCancelLongRunningControlsVisibility(true);
				longRunningProcessCancellationRoutine = model.SourcesManager.CancelSearch;
			};
			model.SourcesManager.OnSearchCompleted += (sender, args) =>
			{
				longRunningProcessCancellationRoutine = null;
				view.SetCancelLongRunningControlsVisibility(false);
				statusReportFactory.CreateNewStatusReport().Dispose();
				SetWaitState(false);
				if (!args.SearchWasInterrupted && args.HitsCount > 0)
				{
					searchResultPresenter.ReceiveInputFocus();
				}
			};

			heartBeatTimer.OnTimer += (sender, e) =>
			{
				if (e.IsRareUpdate)
					SetAnalizingIndication(model.SourcesManager.Items.Any(s => s.TimeGaps.IsWorking));
			};
			sourcesManagerPresenter.OnViewUpdated += (sender, evt) =>
			{
				UpdateMillisecondsDisplayMode();
			};

			model.SourcesManager.OnLogSourceAdded += (sender, evt) =>
			{
				UpdateFormCaption();
			};
			model.SourcesManager.OnLogSourceRemoved += (sender, evt) =>
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

			UpdateFormCaption();
		}

		public event EventHandler Closing;
		public event EventHandler<TabChangingEventArgs> TabChanging;

		void IPresenter.ExecuteThreadPropertiesDialog(IThread thread)
		{
			view.ExecuteThreadPropertiesDialog(thread, navHandler);
		}

		void IPresenter.ActivateTab(string tabId)
		{
			view.ActivateTab(tabId);
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
					await model.Dispose();
					heartBeatTimer.Suspend();
					if (Closing != null)
						Closing(this, EventArgs.Empty);
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
			string[] args = Environment.GetCommandLineArgs();

			if (args.Length > 1)
			{
				args = args.Skip(1).ToArray();
				tracer.Info("command line arguments: {0}", string.Join(", ", args));
				commandLineHandler.HandleCommandLineArgs(args);
			}
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

		void IViewEvents.OnCancelLongRunningProcessButtonClicked()
		{
			CancelLongRunningProcess();
		}

		void CancelLongRunningProcess()
		{
			tracer.Info("----> User Command: Cancel long running process");
			if (longRunningProcessCancellationRoutine != null)
				longRunningProcessCancellationRoutine();
		}

		void IViewEvents.OnKeyPressed(KeyCode key)
		{
			if (longRunningProcessCancellationRoutine != null && key == KeyCode.Escape)
				CancelLongRunningProcess();
			if (key == KeyCode.FindShortcut)
			{
				view.ActivateTab(TabIDs.Search);
				searchPanelPresenter.ReceiveInputFocus(forceSearchAllOccurencesMode: false);
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
				historyDialogPresenter.ShowDialog();
			}
			else if (key == KeyCode.NewWindowShortcut)
			{
				Process.Start(System.Reflection.Assembly.GetEntryAssembly().Location);
			}
		}

		void IViewEvents.OnOptionsLinkClicked()
		{
			view.ShowOptionsMenu();
		}

		void IViewEvents.OnAboutMenuClicked()
		{
			aboutDialogPresenter.Show();
		}

		void IViewEvents.OnConfigurationMenuClicked()
		{
			optionsDialogPresenter.ShowDialog();
		}

		void IViewEvents.OnRestartPictureClicked()
		{
			if (view.ShowRestartConfirmationDialog(
				"App restart",
				"Updated application binaries have been downloaded and they are ready for use. " +
				"Restart application to apply update." + Environment.NewLine +
				"Restart now?"
			))
			{
				autoUpdater.TrySetRestartAfterUpdateFlag();
				view.Close();
			}
		}

		void IViewEvents.OnOpenRecentMenuClicked()
		{
			historyDialogPresenter.ShowDialog ();
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
			bool timeLineWantsMilliseconds = timelinePresenter != null && timelinePresenter.AreMillisecondsVisible;
			bool atLeastOneSourceWantMillisecondsAlways = model.SourcesManager.Items.Any(s => !s.IsDisposed && s.Visible && s.Provider.Factory.ViewOptions.AlwaysShowMilliseconds);
			viewerPresenter.ShowMilliseconds = timeLineWantsMilliseconds || atLeastOneSourceWantMillisecondsAlways;
		}

		void UpdateFormCaption()
		{
			var sources = model.SourcesManager.Items.Where(s => !s.IsDisposed).ToArray();
			StringBuilder builder = new StringBuilder();
			foreach (var source in sources)
			{
				var logName = source.Provider.GetTaskbarLogName();
				if (logName == null)
					continue;
				if (builder.Length > 0)
					builder.Append(", ");
				builder.Append(logName);
			}
			var progress = progressAggregator.ProgressValue;
			if (progress != null)
				builder.AppendFormat(" {0}%", (int)(progress.Value * 100d));
			if (builder.Length > 0)
				builder.Append(" - ");
			builder.Append("LogJoint Log Viewer");
			view.SetCaption(builder.ToString());
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

		void SetAnalizingIndication(bool analizing)
		{
			if (isAnalizing == analizing)
				return;
			view.SetAnalizingIndicationVisibility(analizing);
			isAnalizing = analizing;
		}

		void UpdateAutoUpdateIcon()
		{
			view.SetUpdateIconVisibility(autoUpdater.State == AutoUpdateState.WaitingRestart);
		}

		readonly IModel model;
		readonly IView view;
		readonly LJTraceSource tracer;
		readonly ITabUsageTracker tabUsageTracker;
		readonly LogViewer.IPresenter viewerPresenter;
		readonly ICommandLineHandler commandLineHandler;
		readonly SearchPanel.IPresenter searchPanelPresenter;
		readonly BookmarksManager.IPresenter bookmarksManagerPresenter;
		readonly Timeline.IPresenter timelinePresenter;
		readonly IPresentersFacade navHandler;
		readonly IDragDropHandler dragDropHandler;
		readonly Options.Dialog.IPresenter optionsDialogPresenter;
		readonly IHeartBeatTimer heartBeatTimer;
		readonly IAutoUpdater autoUpdater;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly HistoryDialog.IPresenter historyDialogPresenter;
		readonly About.IPresenter aboutDialogPresenter;

		IInputFocusState inputFocusBeforeWaitState;
		bool isAnalizing;
		Action longRunningProcessCancellationRoutine;
		int lastCustomTabId;


		#endregion
	};
};