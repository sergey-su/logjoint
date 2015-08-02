using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.Workspaces;
using LogJoint.Preprocessing;
using LogJoint.MRU;

namespace LogJoint.UI.Presenters.HistoryDialog
{
	public class Presenter : IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IModel model;
		readonly MRU.IRecentlyUsedEntities mru;
		readonly Preprocessing.ILogSourcesPreprocessingManager sourcesPreprocessingManager;
		readonly Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly QuickSearchTextBox.IPresenter searchBoxPresenter;
		readonly LJTraceSource trace;
		ViewItem[] items;
		bool itemsFiltered;

		public Presenter(
			IView view,
			IModel model,
			Preprocessing.ILogSourcesPreprocessingManager sourcesPreprocessingManager,
			Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory,
			MRU.IRecentlyUsedEntities mru,
			QuickSearchTextBox.IPresenter searchBoxPresenter
		)
		{
			this.view = view;
			this.model = model;
			this.sourcesPreprocessingManager = sourcesPreprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.mru = mru;
			this.searchBoxPresenter = searchBoxPresenter;
			this.trace = new LJTraceSource("UI", "hist-dlg");

			searchBoxPresenter.SearchNow += (s, e) =>
			{
				UpdateItems();
				FocusItemsListAndSelectFirstItem();
			};
			searchBoxPresenter.RealtimeSearch += (s, e) => UpdateItems();
			searchBoxPresenter.Cancelled += (s, e) =>
			{
				if (itemsFiltered)
				{
					UpdateItems();
					searchBoxPresenter.Focus(null);
				}
				else
				{
					view.Hide();
				}
			};

			view.SetEventsHandler(this);
		}


		void IPresenter.ShowDialog()
		{
			UpdateItems();
			view.Show();
		}

		void IViewEvents.OnDialogShown()
		{
			searchBoxPresenter.Focus("");
			UpdateOpenButton();
		}

		void IViewEvents.OnOpenClicked()
		{
			OpenEntries();
		}

		void IViewEvents.OnDoubleClick()
		{
			OpenEntries();
		}

		void IViewEvents.OnFindShortcutPressed()
		{
			searchBoxPresenter.Focus(null);
		}

		void IViewEvents.OnSelectedItemsChanged()
		{
			UpdateOpenButton();
		}

		void IViewEvents.OnClearHistoryButtonClicked()
		{
			if (view.ShowClearHistroConfirmationDialog(
				string.Format("Do you want to clear the history ({0} items)?", items.Length)
			))
			{
				mru.ClearRecentLogsList();
			}
		}

		private void OpenEntries()
		{
			var selected = view.SelectedItems;
			var openingWorkspace = selected.Any(i => i.Type == ViewItemType.Workspace);
			var openingLog = selected.Any(i => i.Type == ViewItemType.Log);
			if (!(openingLog || openingWorkspace))
				return;
			view.Hide();
			if (openingWorkspace)
				model.DeleteAllLogsAndPreprocessings();
			foreach (var item in selected)
			{
				try
				{
					var log = item.Data as RecentLogEntry;
					var ws = item.Data as RecentWorkspaceEntry;
					if (log != null)
						sourcesPreprocessingManager.Preprocess(log, makeHiddenLog: false);
					else if (ws != null)
						sourcesPreprocessingManager.OpenWorkspace(preprocessingStepsFactory, ws.Url);
				}
				catch (Exception e)
				{
					trace.Error(e, "failed to open '{0}'", item.Text);
					view.ShowOpeningFailurePopup("Failed to open " + item.Text);
				}
			}
		}

		private void UpdateItems()
		{
			var filter = searchBoxPresenter.Text;
			this.itemsFiltered = !string.IsNullOrEmpty(filter);
			this.items =
				mru.GetMRUList()
				.Where(e =>
					!itemsFiltered
					|| e.UserFriendlyName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0
					|| (e.Annotation ?? "").IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0
				)
				.Select(e => new ViewItem()
				{
					Type = e.Type == MRU.RecentlyUsedEntityType.Workspace ? ViewItemType.Workspace : ViewItemType.Log,
					Text = e.UserFriendlyName,
					Annotation = e.Annotation,
					Data = e
				}).ToArray();
			view.Update(items);
			UpdateOpenButton();
		}

		private void FocusItemsListAndSelectFirstItem()
		{
			view.PutInputFocusToItemsList();
			view.SelectedItems = items.Take(1).ToArray();
		}

		void UpdateOpenButton()
		{
			var canOpen = view.SelectedItems.Any(i => i.Type == ViewItemType.Log || i.Type == ViewItemType.Workspace);
			view.EnableOpenButton(canOpen);
		}
	};
};