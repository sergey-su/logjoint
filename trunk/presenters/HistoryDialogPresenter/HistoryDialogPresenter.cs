using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.Workspaces;
using LogJoint.Preprocessing;
using LogJoint.MRU;
using System.Diagnostics;

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
		List<ViewItem> items, displayItems;
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
			view.AboutToShow();
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
			if (view.ShowClearHistoryConfirmationDialog(
				string.Format("Do you want to clear the history ({0} items)?", items.Count)
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
			this.items = new List<ViewItem>();
			this.displayItems = new List<ViewItem>();
			var groups = MakeGroups();
			foreach (
				var i in 
				mru.GetMRUList()
				.Where(e =>
					!itemsFiltered
					|| e.UserFriendlyName.IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0
					|| (e.Annotation ?? "").IndexOf(filter, StringComparison.CurrentCultureIgnoreCase) >= 0
				))
			{
				var d = i.UseTimestampUtc.GetValueOrDefault(DateTime.MinValue.AddYears(1)).ToLocalTime();
				var gidx = groups.BinarySearch(0, groups.Count, g => g.begin > d);
				groups[Math.Min(gidx, groups.Count - 1)].items.Add(i);
			}
			foreach (var g in groups)
			{
				if (g.items.Count == 0)
					continue;
				displayItems.Add(new ViewItem()
				{
					Type = ViewItemType.HistoryComment,
					Text = g.name
				});
				foreach (var e in g.items)
				{
					var vi = new ViewItem()
					{
						Type = e.Type == MRU.RecentlyUsedEntityType.Workspace ? ViewItemType.Workspace : ViewItemType.Log,
						Text = e.UserFriendlyName,
						Annotation = e.Annotation,
						Data = e
					};
					items.Add(vi);
					displayItems.Add(vi);
				}
			}
			view.Update(displayItems.ToArray());
			UpdateOpenButton();
		}

		static List<ItemsGroup> MakeGroups()
		{
			var groups = new List<ItemsGroup>();
			var now = DateTime.Now.Date;
			groups.Add(new ItemsGroup()
			{
				name = "Today",
				begin = now
			});
			groups.Add(new ItemsGroup()
			{
				name = "Yesterday",
				begin = now.AddDays(-1)
			});
			for (int i = -2; i > -7; --i)
			{
				groups.Add(new ItemsGroup()
				{
					name = now.AddDays(i).ToLongDateString(),
					begin = now.AddDays(i)
				});
			}
			groups.Add(new ItemsGroup()
			{
				name = "Older than week",
				begin = now.AddDays(-30)
			});
			groups.Add(new ItemsGroup()
			{
				name = "Older than month",
				begin = DateTime.MinValue
			});
			return groups;
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

		[DebuggerDisplay("{name} {begin}")]
		class ItemsGroup
		{
			public string name;
			public DateTime begin;
			public List<IRecentlyUsedEntity> items = new List<IRecentlyUsedEntity>();
		};
	};
};