using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint;

namespace LogJoint.UI.Presenters.FiltersManager
{
	public class Presenter : IPresenter, IDisposable, IViewEvents
	{
		public Presenter(
			IFiltersList filtersList,
			IView view,
			FiltersListBox.IPresenter filtersListPresenter,
			FilterDialog.IPresenter filtersDialogPresenter,
			LogViewer.IPresenter logViewerPresenter,
			IViewUpdates viewUpdates,
			IHeartBeatTimer heartbeat,
			IFiltersFactory filtersFactory,
			IAlertPopup alerts
		)
		{
			this.filtersList = filtersList;
			this.view = view;
			this.heartbeat = heartbeat;
			this.filtersListPresenter = filtersListPresenter;
			this.filtersDialogPresenter = filtersDialogPresenter;
			this.logViewerPresenter = logViewerPresenter;
			this.viewUpdates = viewUpdates;
			this.filtersFactory = filtersFactory;
			this.alerts = alerts;

			view.SetFiltertingEnabledCheckBoxLabel (
				filtersList.Purpose == FiltersListPurpose.Highlighting ? "Enabled highlighting" : "Enable filtering");

			UpdateControls ();

			filtersListPresenter.SelectionChanged += delegate (object sender, EventArgs args) {
				UpdateControls ();
			};
			filtersListPresenter.FilterChecked += delegate (object sender, EventArgs args) {
				NotifyAboutFilteringResultChange ();
			};
			filtersListPresenter.DeleteRequested += (s, a) => {
				DoRemoveSelected ();
			};
			filtersList.OnPropertiesChanged += HandleFiltersListChange;
			filtersList.OnFilteringEnabledChanged += HandleFiltersListChange;
			filtersList.OnFiltersListChanged += HandleFiltersListChange;
			heartbeat.OnTimer += PeriodicUpdate;

			view.SetPresenter (this);

			updateTracker.Invalidate ();
		}

		void IDisposable.Dispose ()
		{
			heartbeat.OnTimer -= PeriodicUpdate;
			filtersList.OnFilteringEnabledChanged -= HandleFiltersListChange;
			filtersList.OnFiltersListChanged -= HandleFiltersListChange;
			filtersList.OnPropertiesChanged -= HandleFiltersListChange;
		}

		void IViewEvents.OnEnableFilteringChecked(bool value)
		{
			filtersList.FilteringEnabled = value;
			NotifyAboutFilteringResultChange();
		}

		async void IViewEvents.OnAddFilterClicked()
		{
			string defaultTemplate = "";
			string selectedText = "";
			if (logViewerPresenter != null)
				selectedText = await logViewerPresenter.GetSelectedText().IgnoreCancellation(s => s, "");
			if (selectedText.Split(new[] { '\r', '\n' }).Length < 2) // is single-line
				defaultTemplate = selectedText;
			IFilter f = filtersFactory.CreateFilter(
				filtersList.Purpose == FiltersListPurpose.Highlighting ? 
					FilterAction.IncludeAndColorizeFirst : FilterAction.Include,
				string.Format("New filter {0}", ++lastFilterIndex),
				enabled: true,
				searchOptions: new Search.Options()
				{
					Template = defaultTemplate,
					Scope = filtersFactory.CreateScope()
				}
			);
			try
			{
				if (!filtersDialogPresenter.ShowTheDialog(f))
				{
					return;
				}
				filtersList.Insert(0, f);
				f = null;
				NotifyAboutFilteringResultChange();
			}
			finally
			{
				if (f != null)
				{
					f.Dispose();
				}
			}
		}

		void IViewEvents.OnRemoveFilterClicked()
		{
			DoRemoveSelected();
		}

		void IViewEvents.OnMoveFilterUpClicked()
		{
			MoveFilterInternal(true);
		}

		void IViewEvents.OnMoveFilterDownClicked()
		{
			MoveFilterInternal(false);
		}

		void IViewEvents.OnPrevClicked()
		{
			logViewerPresenter?.GoToPrevHighlightedMessage();
		}

		void IViewEvents.OnNextClicked()
		{
			logViewerPresenter?.GoToNextHighlightedMessage();
		}

		void IViewEvents.OnOptionsClicked()
		{
			var f = filtersListPresenter.SelectedFilters.FirstOrDefault();
			if (f != null)
				filtersDialogPresenter.ShowTheDialog(f);
		}

		#region Implementation

		void MoveFilterInternal(bool up)
		{
			foreach (var f in filtersListPresenter.SelectedFilters)
			{
				if (filtersList.Move(f, up))
				{
					NotifyAboutFilteringResultChange();
				}
				break;
			}
		}

		void NotifyAboutFilteringResultChange()
		{
			viewUpdates.RequestUpdate();
		}

		void UpdateView()
		{
			filtersListPresenter.UpdateView();
			UpdateControls();
		}

		void UpdateControls()
		{
			ViewControl visibleCtrls = 
				ViewControl.AddFilterButton | ViewControl.RemoveFilterButton | 
				ViewControl.MoveUpButton | ViewControl.MoveDownButton | ViewControl.FilterOptions;
			if (filtersList.Purpose == FiltersListPurpose.Highlighting)
				visibleCtrls |= (ViewControl.FilteringEnabledCheckbox | ViewControl.PrevButton | ViewControl.NextButton);
			view.SetControlsVisibility(visibleCtrls);

			int count = filtersListPresenter.SelectedFilters.Count();
			ViewControl enabledCtrls = 
				ViewControl.FilteringEnabledCheckbox | ViewControl.AddFilterButton;
			if (count > 0)
				enabledCtrls |= ViewControl.RemoveFilterButton;
			if (count == 1)
				enabledCtrls |= (ViewControl.MoveDownButton | ViewControl.MoveUpButton | ViewControl.FilterOptions);
			if (filtersList.Purpose == FiltersListPurpose.Highlighting && IsNavigationOverHighlightedMessagesEnabled())
				enabledCtrls |= (ViewControl.PrevButton | ViewControl.NextButton);
			view.EnableControls(enabledCtrls);

			if (filtersList.Purpose == FiltersListPurpose.Highlighting)
			{
				view.SetFiltertingEnabledCheckBoxValue(
					filtersList.FilteringEnabled,
					filtersList.FilteringEnabled ? 
						"Unckeck to disable all highlighting temporarily" : "Check to enable highlighting"
				);
			}
		}

		bool IsNavigationOverHighlightedMessagesEnabled()
		{
			return filtersList.FilteringEnabled && filtersList.Count > 0;
		}

		private void DoRemoveSelected()
		{
			var toDelete = new List<IFilter>();
			foreach (IFilter f in filtersListPresenter.SelectedFilters)
			{
				toDelete.Add(f);
			}

			if (toDelete.Count == 0)
			{
				return;
			}

			if (alerts.ShowPopup (
				"Filters", 
				string.Format("You are about to delete ({0}) filter(s).\nAre you sure?", toDelete.Count), 
				AlertFlags.YesNoCancel | AlertFlags.QuestionIcon) != AlertFlags.Yes)
			{
				return;
			}

			filtersList.Delete(toDelete);
		}

		void PeriodicUpdate (object sender, HeartBeatEventArgs args)
		{
			if (args.IsNormalUpdate && updateTracker.Validate ())
				UpdateView ();
		}

		void HandleFiltersListChange(object sender, EventArgs args)
		{
			updateTracker.Invalidate ();
		}

		readonly IFiltersFactory filtersFactory;
		readonly IFiltersList filtersList;
		readonly IView view;
		readonly IHeartBeatTimer heartbeat;
		readonly FilterDialog.IPresenter filtersDialogPresenter;
		readonly FiltersListBox.IPresenter filtersListPresenter;
		readonly LogViewer.IPresenter logViewerPresenter;
		readonly IViewUpdates viewUpdates;
		readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();
		readonly IAlertPopup alerts;
		int lastFilterIndex;

		#endregion
	};
};