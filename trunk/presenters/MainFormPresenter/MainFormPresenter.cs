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
			IModel model,
			IView view,
			UI.Presenters.LogViewer.IPresenter viewerPresenter,
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
			IShutdown shutdown
		)
		{
			this.tracer = new LJTraceSource("UI", "ui.main");
			this.model = model;
			this.view = view;
			this.tabUsageTracker = tabUsageTracker;
			this.searchPanelPresenter = searchPanelPresenter;
			this.bookmarksManagerPresenter = bookmarksManagerPresenter;
			this.viewerPresenter = viewerPresenter;
			this.presentersFacade = presentersFacade;
			this.dragDropHandler = dragDropHandler;
			this.heartBeatTimer = heartBeatTimer;
			this.autoUpdater = autoUpdater;
			this.progressAggregator = progressAggregator;
			this.alerts = alerts;
			this.sharingDialogPresenter = sharingDialogPresenter;
			this.shutdown = shutdown;
			this.statusRepors = statusReportFactory;

			view.SetPresenter(this);

			viewerPresenter.ManualRefresh += delegate(object sender, EventArgs args)
			{
				using (tracer.NewFrame)
				{
					tracer.Info("----> User Command: Refresh");
					model.SourcesManager.Refresh();
				}
			};
			viewerPresenter.FocusedMessageBookmarkChanged += delegate(object sender, EventArgs args)
			{
				if (searchResultPresenter != null)
					searchResultPresenter.MasterFocusedMessage = viewerPresenter.GetFocusedMessageBookmark();
			};
			viewerPresenter.DefaultFocusedMessageActionCaption = "Show properties...";
			viewerPresenter.DefaultFocusedMessageAction += (s, e) =>
			{
				if (messagePropertiesDialogPresenter != null)
					messagePropertiesDialogPresenter.ShowDialog();
			};

			if (searchResultPresenter != null)
			{
				searchResultPresenter.OnClose += (sender, args) => searchPanelPresenter.CollapseSearchResultPanel();
				searchResultPresenter.OnResizingStarted += (sender, args) => view.BeginSplittingSearchResults();
			}

			sourcesListPresenter.OnBusyState += (_, evt) => SetWaitState(evt.BusyStateRequired);

			sourcesManagerPresenter.OnBusyState += (_, evt) => SetWaitState(evt.BusyStateRequired);

			searchPanelPresenter.InputFocusAbandoned += delegate(object sender, EventArgs args)
			{
				loadedMessagesPresenter.Focus();
			};
			loadedMessagesPresenter.OnResizingStarted += (s, e) => view.BeginSplittingTabsPanel();

			this.heartBeatTimer.OnTimer += (sender, e) =>
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
			}


			// todo: move that logic to a separate class
			HashSet<ILogSource> logSourcesRequiringReordering = new HashSet<ILogSource>();
			var updateFlag = new LazyUpdateFlag();
			StatusReports.IReport currentReport = null;
			model.SourcesManager.OnLogSourceStatsChanged += (sender, e) => 
			{
				if ((e.Flags & LogProviderStatsFlag.FirstMessageWithTimeConstraintViolation) != 0)
				{
					var msg = ((ILogSource)sender).Provider.Stats.FirstMessageWithTimeConstraintViolation;
					bool updated;
					if (msg != null)
						updated = logSourcesRequiringReordering.Add((ILogSource)sender);
					else
						updated = logSourcesRequiringReordering.Remove((ILogSource)sender);
					if (updated)
						updateFlag.Invalidate();
				}
			};
			heartBeatTimer.OnTimer += (sender, e) => 
			{
				if (e.IsNormalUpdate && updateFlag.Validate())
				{
					if (currentReport != null)
						currentReport.Dispose();
					currentReport = statusRepors.CreateNewStatusReport();
					currentReport.ShowStatusPopup(
						"Log source problem",
						new [] {
							new StatusReports.MessagePart(string.Format("{0} logs have problem with timestamps. {1}", 
								logSourcesRequiringReordering.Count, Environment.NewLine)),
							new StatusReports.MessageLink("Reorder the log", () => 
							{
								
							}),
							new StatusReports.MessagePart("  "),
							new StatusReports.MessageLink("Ignore", () =>
							{
								if (currentReport != null)
								{
									currentReport.Dispose();
									currentReport = null;
								}
							})
						},
						autoHide: false
					);
				}
			};


			UpdateFormCaption();
			UpdateShareButton();
		}

		public event EventHandler Loaded;
		public event EventHandler<TabChangingEventArgs> TabChanging;

		void IPresenter.ExecuteThreadPropertiesDialog(IThread thread)
		{
			view.ExecuteThreadPropertiesDialog(thread, presentersFacade);
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
					heartBeatTimer.Suspend();
					await model.Dispose();
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

		void IViewEvents.OnKeyPressed(KeyCode key)
		{
			if (key == KeyCode.Escape)
			{
				statusRepors.CancelActiveStatus();
			}
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
				presentersFacade.ShowHistoryDialog();
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
			bool atLeastOneSourceWantMillisecondsAlways = model.SourcesManager.Items.Any(s => !s.IsDisposed && s.Visible && s.Provider.Factory.ViewOptions.AlwaysShowMilliseconds);
			viewerPresenter.ShowMilliseconds = atLeastOneSourceWantMillisecondsAlways;
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
		readonly SearchPanel.IPresenter searchPanelPresenter;
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

		IInputFocusState inputFocusBeforeWaitState;
		bool isAnalizing;
		int lastCustomTabId;


		#endregion
	};
};