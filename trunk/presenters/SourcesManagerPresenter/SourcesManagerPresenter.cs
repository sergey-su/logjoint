using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint;
using LogJoint.MRU;
using LogJoint.Preprocessing;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SourcesManager
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			ILogSourcesManager logSources,
			IUserDefinedFormatsManager udfManager,
			IRecentlyUsedEntities mru,
			Preprocessing.IManager logSourcesPreprocessings,
			IView view,
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			Workspaces.IWorkspacesManager workspacesManager,
			SourcesList.IPresenter sourcesListPresenter,
			NewLogSourceDialog.IPresenter newLogSourceDialogPresenter,
			IHeartBeatTimer heartbeat,
			SharingDialog.IPresenter sharingDialogPresenter,
			HistoryDialog.IPresenter historyDialogPresenter,
			IPresentersFacade facade,
			SourcePropertiesWindow.IPresenter sourcePropertiesWindowPresenter,
			IAlertPopup alerts,
			ITraceSourceFactory traceSourceFactory
		)
		{
			this.logSources = logSources;
			this.udfManager = udfManager;
			this.mru = mru;
			this.view = view;
			this.logSourcesPreprocessings = logSourcesPreprocessings;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.workspacesManager = workspacesManager;
			this.newLogSourceDialogPresenter = newLogSourceDialogPresenter;
			this.sourcesListPresenter = sourcesListPresenter;
			this.tracer = traceSourceFactory.CreateTraceSource("UI", "smgr-ui");
			this.sharingDialogPresenter = sharingDialogPresenter;
			this.historyDialogPresenter = historyDialogPresenter;
			this.sourcePropertiesWindowPresenter = sourcePropertiesWindowPresenter;
			this.alerts = alerts;
			this.presentersFacade = facade;

			sourcesListPresenter.DeleteRequested += delegate(object sender, EventArgs args)
			{
				DeleteSelectedSources();
			};
			logSources.OnLogSourceAdded += (sender, args) =>
			{
				UpdateRemoveAllButton();
			};
			logSources.OnLogSourceRemoved += (sender, args) =>
			{
				updateTracker.Invalidate();
				UpdateRemoveAllButton();
			};
			logSourcesPreprocessings.PreprocessingAdded += (sender, args) =>
			{
				updateTracker.Invalidate();
				UpdateRemoveAllButton();
				if ((args.LogSourcePreprocessing.Flags & PreprocessingOptions.HighlightNewPreprocessing) != 0)
				{
					preprocessingAwaitingHighlighting = args.LogSourcePreprocessing;
				}
			};
			logSourcesPreprocessings.PreprocessingDisposed += (sender, args) =>
			{
				updateTracker.Invalidate();
				UpdateRemoveAllButton();
			};
			logSourcesPreprocessings.PreprocessingChangedAsync += (sender, args) =>
			{
				updateTracker.Invalidate();
			};
			sourcesListPresenter.SelectionChanged += delegate(object sender, EventArgs args)
			{
				bool anySourceSelected = sourcesListPresenter.SelectedSources.Any();
				bool anyPreprocSelected = sourcesListPresenter.SelectedPreprocessings.Any();
				view.EnableDeleteSelectedSourcesButton(anySourceSelected || anyPreprocSelected);
				view.EnableTrackChangesCheckBox(anySourceSelected);
				UpdateTrackChangesCheckBox();
				view.SetPropertiesButtonState(
					sourcePropertiesWindowPresenter != null && sourcesListPresenter.SelectedSources.Count() == 1);
			};

			logSources.OnLogSourceVisiblityChanged += (sender, args) =>
			{
				updateTracker.Invalidate();
			};
			logSources.OnLogSourceAnnotationChanged += (sender, args) =>
			{
				updateTracker.Invalidate();
			};
			logSources.OnLogSourceTrackingFlagChanged += (sender, args) =>
			{
				updateTracker.Invalidate();
			};
			logSources.OnLogSourceStatsChanged += (sender, args) =>
			{
				if ((args.Flags & (LogProviderStatsFlag.Error | LogProviderStatsFlag.CachedMessagesCount | LogProviderStatsFlag.State | LogProviderStatsFlag.BytesCount | LogProviderStatsFlag.BackgroundAcivityStatus)) != 0)
					updateTracker.Invalidate();
			};
			heartbeat.OnTimer += (sender, args) =>
			{
				if (updateTracker.Validate())
					UpdateView();
			};
			
			this.sharingDialogPresenter.AvailabilityChanged += (sender, args) =>
			{
				UpdateShareButton();
			};
			this.sharingDialogPresenter.IsBusyChanged += (sender, args) =>
			{
				UpdateShareButton();
			};

			view.SetPresenter(this);

			UpdateShareButton();
		}

		public event EventHandler<BusyStateEventArgs> OnBusyState;

		async void IPresenter.StartDeletionInteraction(ILogSource[] forSources)
		{
			await DeleteSources(forSources, Enumerable.Empty<ILogSourcePreprocessing>());
		}

		void IViewEvents.OnAddNewLogButtonClicked()
		{
			udfManager.ReloadFactories(); // todo: move it away from this presenter

			newLogSourceDialogPresenter.ShowTheDialog();
		}

		void IViewEvents.OnOpenSingleFileButtonClicked()
		{
			string fileName = view.ShowOpenSingleFileDialog();
			if (fileName != null)
			{
				logSourcesPreprocessings.Preprocess(
					new [] {preprocessingStepsFactory.CreateLocationTypeDetectionStep(new PreprocessingStepParams(fileName))},
					"Opening selected file"
				);
			}
		}

		void IViewEvents.OnPropertiesButtonClicked()
		{
			var sel = sourcesListPresenter.SelectedSources.FirstOrDefault();
			if (sel != null && sourcePropertiesWindowPresenter != null)
				sourcePropertiesWindowPresenter.ShowWindow(sel);
		}

		void IViewEvents.OnDeleteSelectedLogSourcesButtonClicked()
		{
			DeleteSelectedSources();
		}

		void IViewEvents.OnDeleteAllLogSourcesButtonClicked()
		{
			DeleteAllSources();
			workspacesManager.DetachFromWorkspace();
		}

		void IViewEvents.OnMRUButtonClicked()
		{
			udfManager.ReloadFactories();
			var items = new List<MRUMenuItem>();
			foreach (var entry in mru.GetMRUList().Take(20))
			{
				items.Add(new MRUMenuItem()
				{
					Text = entry.UserFriendlyName,
					Data = entry,
					ToolTip = entry.Annotation,
					InplaceAnnotation = MakeInplaceAnnotation(entry.Annotation)
				});
			}
			if (items.Count == 0)
			{
				items.Add(new MRUMenuItem()
				{
					Text = "<No recent files>",
					Data = null,
					Disabled = true
				});
			}
			else
			{
				items.Add(new MRUMenuItem()
				{
					Text = "View full history...",
					Data = "history",
					InplaceAnnotation = "Ctrl + H"
				});
			}

			view.ShowMRUMenu(items);
		}

		async void IViewEvents.OnMRUMenuItemClicked(object data)
		{
			if (data == null)
				return;
			try
			{
				var log = data as RecentLogEntry;
				var ws = data as RecentWorkspaceEntry;
				if (log != null)
				{
					logSourcesPreprocessings.Preprocess(log);
				}
				else if (ws != null)
				{
					await Task.WhenAll(logSources.DeleteAllLogs(), logSourcesPreprocessings.DeleteAllPreprocessings());
					logSourcesPreprocessings.OpenWorkspace(preprocessingStepsFactory, ws.Url);
				}
				else if (object.ReferenceEquals(data, "history"))
				{
					historyDialogPresenter.ShowDialog();
				}
			}
			catch (Exception e)
			{
				tracer.Error(e, "failed to open mru item {0}", data);
				alerts.ShowPopup(
					"Error",
					"Failed to open file",
					AlertFlags.Ok | AlertFlags.WarningIcon
				);
			}
		}

		void IViewEvents.OnShowHistoryDialogButtonClicked()
		{
			historyDialogPresenter.ShowDialog();
		}

		void IViewEvents.OnTrackingChangesCheckBoxChecked(bool value)
		{
			foreach (ILogSource s in sourcesListPresenter.SelectedSources)
				s.TrackingEnabled = value;
			UpdateTrackChangesCheckBox();
		}

		void IViewEvents.OnShareButtonClicked()
		{
			sharingDialogPresenter.ShowDialog();
		}

		#region Implementation

		void UpdateView()
		{
			sourcesListPresenter.UpdateView();
			UpdateTrackChangesCheckBox();
			ExecurePendingHighlightings();
		}

		private void ExecurePendingHighlightings()
		{
			if (preprocessingAwaitingHighlighting != null && !preprocessingAwaitingHighlighting.IsDisposed)
				presentersFacade.ShowPreprocessing(preprocessingAwaitingHighlighting);
			preprocessingAwaitingHighlighting = null;
		}

		private async void DeleteSelectedSources()
		{
			await DeleteSources(sourcesListPresenter.SelectedSources, sourcesListPresenter.SelectedPreprocessings);
		}

		private async void DeleteAllSources()
		{
			await DeleteSources(logSources.Items, logSourcesPreprocessings.Items);
		}

		private async Task DeleteSources(IEnumerable<ILogSource> sourcesToDelete, IEnumerable<Preprocessing.ILogSourcePreprocessing> preprocessingToDelete)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Delete sources");

				var toDelete = new List<ILogSource>();
				var toDelete2 = new List<Preprocessing.ILogSourcePreprocessing>();
				foreach (ILogSource s in sourcesToDelete)
				{
					tracer.Info("-- source to delete: {0}", s.ToString());
					toDelete.Add(s);
				}
				foreach (Preprocessing.ILogSourcePreprocessing p in preprocessingToDelete)
				{
					if (p.IsDisposed)
						continue;
					tracer.Info("-- preprocessing to delete: {0}", p.ToString());
					toDelete2.Add(p);
				}

				if (toDelete.Count == 0 && toDelete2.Count == 0)
				{
					tracer.Info("Nothing to delete");
					return;
				}

				int goodItemsCount = 
					toDelete.Count(s => s.Provider.Stats.State != LogProviderState.LoadError) + 
					toDelete2.Count(p => p.Failure == null);
				if (goodItemsCount > 0) // do not ask about failed preprocessors or sources
				{
					if (alerts.ShowPopup(
						"Delete",
						string.Format("Are you sure you want to close {0} log (s)", toDelete.Count + toDelete2.Count),
						AlertFlags.YesNoCancel
					) != AlertFlags.Yes)
					{
						tracer.Info("User didn't confirm the deletion");
						return;
					}
				}

				SetWaitState(true);
				try
				{
					await logSources.DeleteLogs(toDelete.ToArray());
					await logSourcesPreprocessings.DeletePreprocessings(toDelete2.ToArray());
				}
				finally
				{
					SetWaitState(false);
				}
			}
		}

		void SetWaitState(bool value)
		{
			if (OnBusyState != null)
				OnBusyState(this, new BusyStateEventArgs(value));
		}

		void UpdateRemoveAllButton()
		{
			view.EnableDeleteAllSourcesButton(logSources.Items.Any() || logSourcesPreprocessings.Items.Any());
		}

		void UpdateShareButton()
		{
			view.SetShareButtonState(
				visible: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.PermanentlyUnavaliable, 
				enabled: sharingDialogPresenter.Availability != SharingDialog.DialogAvailability.TemporarilyUnavailable,
				progress: sharingDialogPresenter.IsBusy
			);
		}

		void UpdateTrackChangesCheckBox()
		{
			bool f1 = false;
			bool f2 = false;
			foreach (ILogSource s in sourcesListPresenter.SelectedSources)
			{
				if (s.Visible && s.TrackingEnabled)
					f1 = true;
				else
					f2 = true;
				if (f1 && f2)
					break;
			}

			TrackingChangesCheckBoxState newState;
			if (f1 && f2)
				newState = TrackingChangesCheckBoxState.Indeterminate;
			else if (f1)
				newState = TrackingChangesCheckBoxState.Checked;
			else
				newState = TrackingChangesCheckBoxState.Unchecked;

			view.SetTrackingChangesCheckBoxState(newState);
		}

		static string MakeInplaceAnnotation(string ann)
		{
			if (string.IsNullOrEmpty(ann))
				return null;

			ann = string.Join(" ", ann.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries));

			int lengthLimit = 35;
			if (ann.Length > lengthLimit)
				ann = ann.Substring(0, lengthLimit - 3) + "...";

			return ann;
		}

		readonly ILogSourcesManager logSources;
		readonly IUserDefinedFormatsManager udfManager;
		readonly IRecentlyUsedEntities mru;
		readonly IView view;
		readonly Preprocessing.IManager logSourcesPreprocessings;
		readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		readonly Workspaces.IWorkspacesManager workspacesManager;
		readonly SourcesList.IPresenter sourcesListPresenter;
		readonly NewLogSourceDialog.IPresenter newLogSourceDialogPresenter;
		readonly SharingDialog.IPresenter sharingDialogPresenter;
		readonly HistoryDialog.IPresenter historyDialogPresenter;
		readonly SourcePropertiesWindow.IPresenter sourcePropertiesWindowPresenter;
		readonly LJTraceSource tracer;
		readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();
		readonly IAlertPopup alerts;
		readonly IPresentersFacade presentersFacade;
		ILogSourcePreprocessing preprocessingAwaitingHighlighting;

		#endregion
	};
};