using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.Workspaces;
using LogJoint.Preprocessing;
using LogJoint.MRU;
using System.Diagnostics;
using System.Threading.Tasks;
using LogJoint.UI.Presenters.Reactive;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.HistoryDialog
{
	public class Presenter : IPresenter, IViewModel
	{
		readonly IView view;
		readonly ILogSourcesManager logSourcesManager;
		readonly MRU.IRecentlyUsedEntities mru;
		readonly Preprocessing.IManager sourcesPreprocessingManager;
		readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		readonly QuickSearchTextBox.IPresenter searchBoxPresenter;
		readonly LJTraceSource trace;
		readonly IAlertPopup alerts;
		readonly IChangeNotification changeNotification;

		bool visible;
		string acceptedFilter = "";
		ImmutableHashSet</* item key */string> selected = ImmutableHashSet<string>.Empty;
		ImmutableHashSet</* item key */string> expanded = ImmutableHashSet<string>.Empty;
		int lastKey = 0;

		readonly Func<Items> items;
		readonly Func<ViewItem> rootViewItem;
		readonly Func<IReadOnlyList<ViewItem>> actuallySelected;
		readonly Func<bool> openButtonEnabled;

		public Presenter(
			ILogSourcesManager logSourcesManager,
			IChangeNotification changeNotification,
			IView view,
			Preprocessing.IManager sourcesPreprocessingManager,
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			MRU.IRecentlyUsedEntities mru,
			QuickSearchTextBox.IPresenter searchBoxPresenter,
			IAlertPopup alerts,
			ITraceSourceFactory traceSourceFactory
		)
		{
			this.view = view;
			this.changeNotification = changeNotification;
			this.logSourcesManager = logSourcesManager;
			this.sourcesPreprocessingManager = sourcesPreprocessingManager;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.mru = mru;
			this.searchBoxPresenter = searchBoxPresenter;
			this.trace = traceSourceFactory.CreateTraceSource("UI", "hist-dlg");
			this.alerts = alerts;

			items = Selectors.Create(() => visible, () => acceptedFilter, MakeItems);
			actuallySelected = Selectors.Create(() => items().displayItems, () => selected,
				(items, selected) => items.SelectMany(i => i.Flatten()).Where(i => selected.Contains(i.key)).ToImmutableList());
			openButtonEnabled = Selectors.Create(actuallySelected, selected => selected.Any(IsOpenable));
			rootViewItem = Selectors.Create(() => items().displayItems, () => selected, () => expanded, MakeRootItem);

			searchBoxPresenter.OnSearchNow += (s, e) =>
			{
				acceptedFilter = searchBoxPresenter.Text;
				FocusItemsListAndSelectFirstItem();
				changeNotification.Post();
			};
			searchBoxPresenter.OnRealtimeSearch += (s, e) =>
			{
				acceptedFilter = searchBoxPresenter.Text;
				changeNotification.Post();
			};
			searchBoxPresenter.OnCancelled += (s, e) =>
			{
				if (acceptedFilter != "")
				{
					acceptedFilter = "";
					searchBoxPresenter.Focus(null);
				}
				else
				{
					visible = false;
				}
				changeNotification.Post();
			};

			view.SetViewModel(this);
		}


		void IPresenter.ShowDialog()
		{
			if (!visible)
			{
				visible = true;
				changeNotification.Post();
			}
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		bool IViewModel.IsVisible => visible;

		IViewItem IViewModel.RootViewItem => rootViewItem();

		IReadOnlyList<IViewItem> IViewModel.ItemsIgnoringTreeState => items().displayItems;

		bool IViewModel.OpenButtonEnabled => openButtonEnabled();

		void IViewModel.OnSelect(IEnumerable<IViewItem> items)
		{
			selected = ImmutableHashSet.CreateRange(items.Select(i => i.Key));
			changeNotification.Post();
		}

		void IViewModel.OnExpand(IViewItem item)
		{
			expanded = expanded.Add(item.Key);
			changeNotification.Post();
		}

		void IViewModel.OnCollapse(IViewItem item)
		{
			expanded = expanded.Remove(item.Key);
			changeNotification.Post();
		}

		void IViewModel.OnDialogShown()
		{
			searchBoxPresenter.Focus("");
		}

		void IViewModel.OnOpenClicked()
		{
			OpenEntries();
		}

		void IViewModel.OnCancelClicked()
		{
			if (visible)
			{
				visible = false;
				changeNotification.Post();
			}
		}

		void IViewModel.OnDoubleClick()
		{
			OpenEntries();
		}

		void IViewModel.OnFindShortcutPressed()
		{
			searchBoxPresenter.Focus(null);
		}

		void IViewModel.OnClearHistoryButtonClicked()
		{
			if (alerts.ShowPopup(
				"Clear history",
				string.Format("Do you want to clear the history ({0} items)?", items().items.Count),
				AlertFlags.YesNoCancel | AlertFlags.WarningIcon
			) == AlertFlags.Yes)
			{
				mru.ClearRecentLogsList();
			}
		}

		private async void OpenEntries()
		{
			var selected = actuallySelected();
			if (selected.All(i => i.data == null))
				return;
			visible = false;
			changeNotification.Post();
			if (selected.Any(i => i.data is RecentWorkspaceEntry))
			{
				await Task.WhenAll(logSourcesManager.DeleteAllLogs(), sourcesPreprocessingManager.DeleteAllPreprocessings());
			}
			var tasks = selected.Select(async item => 
			{
				try
				{
					if (item.data is RecentLogEntry log)
						await sourcesPreprocessingManager.Preprocess(log);
					else if (item.data is RecentWorkspaceEntry ws)
						await sourcesPreprocessingManager.OpenWorkspace(preprocessingStepsFactory, ws.Url);
					else if (item.data is IRecentlyUsedEntity[] container)
						await Task.WhenAll(container.OfType<RecentLogEntry>().Select(innerLog => sourcesPreprocessingManager.Preprocess(innerLog)));
				}
				catch (Exception e)
				{
					trace.Error(e, "failed to open '{0}'", item.text);
					alerts.ShowPopup("Error", "Failed to open " + item.text, AlertFlags.Ok | AlertFlags.WarningIcon);
				}
			});
			await Task.WhenAll(tasks);
		}

		string NextKey() => (++lastKey).ToString();

		private Items MakeItems(bool visible, string filter)
		{
			var itemsBuilder = ImmutableList<ViewItem>.Empty.ToBuilder();
			var displayItemsBuilder = ImmutableList<ViewItem>.Empty.ToBuilder();
			Items makeResult() => new Items
			{
				displayItems = displayItemsBuilder.ToImmutable(),
				items = itemsBuilder.ToImmutable()
			};
			if (!visible)
			{
				return makeResult();
			}

			var timeGroups = MakeTimeGroups();
			var itemsFiltered = filter != "";
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
				var gidx = timeGroups.BinarySearch(0, timeGroups.Count, g => g.begin > d);
				timeGroups[Math.Min(gidx, timeGroups.Count - 1)].items.Add(i);
			}
			foreach (var timeGroup in timeGroups.Where(g => g.items.Count > 0))
			{
				var timeGroupItem = new ViewItem()
				{
					type = ViewItemType.Comment,
					text = timeGroup.name,
					key = NextKey()
				};
				displayItemsBuilder.Add(timeGroupItem);
				foreach (var containerGroup in timeGroup.items.GroupBy(i =>
				{
					string containerName = null;
					var cp = i.ConnectionParams;
					if (cp != null)
						containerName = sourcesPreprocessingManager.ExtractContentsContainerNameFromConnectionParams(cp);
					if (containerName == null)
						containerName = i.GetHashCode().ToString();
					return containerName;
				}))
				{
					var groupItems = containerGroup.ToArray();
					var containerGroupItem = timeGroupItem;
					if (groupItems.Length > 1)
					{
						containerGroupItem = new ViewItem()
						{
							key = NextKey(),
							type = ViewItemType.ItemsContainer,
							text = containerGroup.Key,
							data = groupItems
						};
						itemsBuilder.Add(containerGroupItem);
						timeGroupItem.children.Add(containerGroupItem);
					}
					foreach (var e in groupItems)
					{
						var vi = new ViewItem()
						{
							key = NextKey(),
							type = ViewItemType.Leaf,
							text = e.UserFriendlyName,
							annotation = e.Annotation,
							data = e
						};
						itemsBuilder.Add(vi);
						containerGroupItem.children.Add(vi);
					}
				}
			}
			return makeResult();
		}

		static List<ItemsGroup> MakeTimeGroups()
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

		static ViewItem MakeRootItem(ImmutableList<ViewItem> displayItems,
			ImmutableHashSet<string> selected, ImmutableHashSet<string> expanded)
		{
			void copyChildren(ViewItem target, IEnumerable<ViewItem> source)
			{
				foreach (var src in source)
				{
					var dst = new ViewItem()
					{
						type = src.type,
						key = src.key,
						text = src.text,
						annotation = src.annotation,
						data = src.data,
						expanded = src.type == ViewItemType.Comment || expanded.Contains(src.key),
						selected = selected.Contains(src.key)
					};
					target.children.Add(dst);
					copyChildren(dst, src.children);
				}
			};
			var result = new ViewItem()
			{
				type = ViewItemType.ItemsContainer,
				key = "root",
			};
			copyChildren(result, displayItems);
			return result;
		}

		private void FocusItemsListAndSelectFirstItem()
		{
			view.PutInputFocusToItemsList();
			selected = items().displayItems.SelectMany(i => i.Flatten()).Where(IsOpenable).Take(1).Select(i => i.key).ToImmutableHashSet();
			changeNotification.Post();
		}

		static bool IsOpenable(ViewItem i) => i.type == ViewItemType.Leaf || i.type == ViewItemType.ItemsContainer;

		[DebuggerDisplay("{name} {begin}")]
		class ItemsGroup
		{
			public string name;
			public DateTime begin;
			public List<IRecentlyUsedEntity> items = new List<IRecentlyUsedEntity>();
		};

		class ViewItem : IViewItem
		{
			internal ViewItemType type;
			internal string key = "";
			internal string text = "";
			internal string annotation = "";
			internal object data;
			internal List<ViewItem> children = new List<ViewItem>();
			internal bool expanded, selected;

			ViewItemType IViewItem.Type => type;
			string IViewItem.Text => text;
			string IViewItem.Annotation => annotation;
			string ITreeNode.Key => key;
			IReadOnlyList<ITreeNode> ITreeNode.Children => children;
			bool ITreeNode.IsExpanded => expanded;
			bool ITreeNode.IsSelected => selected;
			bool ITreeNode.IsExpandable => type == ViewItemType.ItemsContainer;

			internal IEnumerable<ViewItem> Flatten()
			{
				yield return this;
				foreach (var c in children)
					foreach (var i in c.Flatten())
						yield return i;
			}
		};

		class Items
		{
			public ImmutableList<ViewItem> items;
			public ImmutableList<ViewItem> displayItems;
		};
	};
};