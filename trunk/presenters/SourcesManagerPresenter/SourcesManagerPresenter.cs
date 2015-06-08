using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint;

namespace LogJoint.UI.Presenters.SourcesManager
{
	public class Presenter : IPresenter, IViewEvents
	{
		public Presenter(
			IModel model,
			IView view,
			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings,
			SourcesList.IPresenter sourcesListPresenter,
			NewLogSourceDialog.IPresenter newLogSourceDialogPresenter,
			IHeartBeatTimer heartbeat)
		{
			this.model = model;
			this.view = view;
			this.logSourcesPreprocessings = logSourcesPreprocessings;
			this.newLogSourceDialogPresenter = newLogSourceDialogPresenter;
			this.sourcesListPresenter = sourcesListPresenter;
			this.newLogSourceDialogPresenter = newLogSourceDialogPresenter;
			this.tracer = model.Tracer;

			sourcesListPresenter.DeleteRequested += delegate(object sender, EventArgs args)
			{
				DeleteSelectedSources();
			};
			model.SourcesManager.OnLogSourceAdded += (sender, args) =>
			{
				UpdateRemoveAllButton();
			};
			model.SourcesManager.OnLogSourceRemoved += (sender, args) =>
			{
				updateTracker.Invalidate();
				UpdateRemoveAllButton();
			};
			logSourcesPreprocessings.PreprocessingAdded += (sender, args) =>
			{
				updateTracker.Invalidate();
				UpdateRemoveAllButton();
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
			};

			model.SourcesManager.OnLogSourceVisiblityChanged += (sender, args) =>
			{
				updateTracker.Invalidate();
			};
			model.SourcesManager.OnLogSourceAnnotationChanged += (sender, args) =>
			{
				updateTracker.Invalidate();
			};
			model.SourcesManager.OnLogSourceTrackingFlagChanged += (sender, args) =>
			{
				updateTracker.Invalidate();
			};
			model.SourcesManager.OnLogSourceStatsChanged += (sender, args) =>
			{
				if ((args.Flags & (LogProviderStatsFlag.Error | LogProviderStatsFlag.FileName | LogProviderStatsFlag.LoadedMessagesCount | LogProviderStatsFlag.State | LogProviderStatsFlag.BytesCount | LogProviderStatsFlag.BackgroundAcivityStatus)) != 0)
					updateTracker.Invalidate();
			};
			heartbeat.OnTimer += (sender, args) =>
			{
				if (updateTracker.Validate())
					UpdateView();
			};

			view.SetPresenter(this);
		}

		public event EventHandler<BusyStateEventArgs> OnBusyState;
		public event EventHandler OnViewUpdated;

		void IViewEvents.OnAddNewLogButtonClicked()
		{
			model.UserDefinedFormatsManager.ReloadFactories(); // todo: move it away from this presenter
			newLogSourceDialogPresenter.ShowTheDialog();
		}

		void IViewEvents.OnDeleteSelectedLogSourcesButtonClicked()
		{
			DeleteSelectedSources();
		}

		void IViewEvents.OnDeleteAllLogSourcesButtonClicked()
		{
			DeleteAllSources();
		}

		void IViewEvents.OnMRUButtonClicked()
		{
			model.UserDefinedFormatsManager.ReloadFactories();
			var items = new List<MRUMenuItem>();
			foreach (RecentLogEntry entry in model.MRU.GetMRUList())
			{
				items.Add(new MRUMenuItem()
				{
					Text = entry.Factory.GetUserFriendlyConnectionName(entry.ConnectionParams),
					ID = entry.ToString()
				});
			}
			if (items.Count == 0)
			{
				items.Add(new MRUMenuItem()
				{
					Text = "<No recent files>",
					ID = null,
					Disabled = true
				});
			}

			view.ShowMRUMenu(items);
		}

		void IViewEvents.OnMRUMenuItemClicked(string itemId)
		{
			if (string.IsNullOrEmpty(itemId))
				return;
			try
			{
				RecentLogEntry entry = RecentLogEntry.Parse(model.LogProviderFactoryRegistry, itemId);
				logSourcesPreprocessings.Preprocess(entry);
			}
			catch (Exception)
			{
				view.ShowMRUOpeningFailurePopup();
			}
		}

		void IViewEvents.OnTrackingChangesCheckBoxChecked(bool value)
		{
			foreach (ILogSource s in sourcesListPresenter.SelectedSources)
				s.TrackingEnabled = value;
			UpdateTrackChangesCheckBox();
		}

		#region Implementation

		void UpdateView()
		{
			sourcesListPresenter.UpdateView();
			UpdateTrackChangesCheckBox();
			if (OnViewUpdated != null)
				OnViewUpdated(this, EventArgs.Empty);
		}

		private void DeleteSelectedSources()
		{
			DeleteSources(sourcesListPresenter.SelectedSources, sourcesListPresenter.SelectedPreprocessings);
		}

		private void DeleteAllSources()
		{
			DeleteSources(model.SourcesManager.Items, logSourcesPreprocessings.Items);
		}

		private void DeleteSources(IEnumerable<ILogSource> sourcesToDelete, IEnumerable<Preprocessing.ILogSourcePreprocessing> preprocessingToDelete)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("----> User Command: Delete sources");

				var toDelete = new List<ILogSource>();
				var toDelete2 = new List<Preprocessing.ILogSourcePreprocessing>();
				foreach (ILogSource s in sourcesToDelete)
				{
					if (s.IsDisposed)
						continue;
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

				if (!view.ShowDeletionConfirmationDialog(toDelete.Count + toDelete2.Count))
				{
					tracer.Info("User didn't confirm the deletion");
					return;
				}

				SetWaitState(true);
				try
				{
					model.DeleteLogs(toDelete.ToArray());
					model.DeletePreprocessings(toDelete2.ToArray());
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
			view.EnableDeleteAllSourcesButton(model.SourcesManager.Items.Any() || logSourcesPreprocessings.Items.Any());
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

		readonly IModel model;
		readonly IView view;
		readonly Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly SourcesList.IPresenter sourcesListPresenter;
		readonly NewLogSourceDialog.IPresenter newLogSourceDialogPresenter;
		readonly LJTraceSource tracer;
		readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();

		#endregion
	};
};