using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LogJoint;
using LogJoint.Preprocessing;
using System.Threading.Tasks;
using LogJoint.Drawing;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.SourcesList
{
	public class Presenter: IPresenter, IViewModel
	{
		static readonly Color successfulSourceColor = Color.FromArgb(255, 255, 255, 255);
		static readonly Color failedSourceColor = Color.FromArgb(255, 255, 128, 128);

		readonly ILogSourcesManager logSources;
		readonly Preprocessing.IManager logSourcesPreprocessings;
		readonly IView view;
		readonly SourcePropertiesWindow.IPresenter propertiesWindowPresenter;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly IClipboardAccess clipboard;
		readonly IShellOpen shellOpen;
		readonly SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter;
		readonly IChangeNotification changeNotification;

		ImmutableHashSet<string> selectedKeys = ImmutableHashSet.Create<string>();
		ImmutableHashSet<string> expandedKeys = ImmutableHashSet.Create<string>();
		int itemsRevision;

		readonly Func<IViewItem> getRoot;
		readonly Func<ImmutableArray<ILogSource>> getSelectedSources;
		readonly Func<ImmutableArray<ILogSourcePreprocessing>> getSelectedPreprocessings;
		readonly Func<IViewItem> getFocusedMessageItem;

		public Presenter(
			ILogSourcesManager logSources,
			IView view,
			IManager logSourcesPreprocessings,
			SourcePropertiesWindow.IPresenter propertiesWindowPresenter,
			LogViewer.IPresenterInternal logViewerPresenter,
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			IClipboardAccess clipboard,
			IShellOpen shellOpen,
			SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter,
			IColorTheme theme,
			IChangeNotification changeNotification,
			ISynchronizationContext uiSynchronizationContext
		)
		{
			this.logSources = logSources;
			this.view = view;
			this.propertiesWindowPresenter = propertiesWindowPresenter;
			this.logSourcesPreprocessings = logSourcesPreprocessings;
			this.alerts = alerts;
			this.fileDialogs = fileDialogs;
			this.clipboard = clipboard;
			this.shellOpen = shellOpen;
			this.saveJointLogInteractionPresenter = saveJointLogInteractionPresenter;
			this.changeNotification = changeNotification;

			void updateItems()
			{
				itemsRevision++;
				changeNotification.Post();
			}

			var invokeUpdateHelper = new AsyncInvokeHelper(uiSynchronizationContext, updateItems);

			logSources.OnLogSourceVisiblityChanged += (s, e) => updateItems();
			logSources.OnLogSourceAnnotationChanged += (s, e) => updateItems();
			logSources.OnLogSourceColorChanged += (s, e) => updateItems();

			logSourcesPreprocessings.PreprocessingChangedAsync += (s, e) => invokeUpdateHelper.Invoke();
			logSources.OnLogSourceStatsChanged += (s, e) =>
			{
				if ((e.Flags & (LogProviderStatsFlag.Error | LogProviderStatsFlag.CachedMessagesCount | LogProviderStatsFlag.State | LogProviderStatsFlag.BytesCount | LogProviderStatsFlag.BackgroundAcivityStatus)) != 0)
				{
					invokeUpdateHelper.Invoke();
				}
			};

			this.getRoot = Selectors.Create(
				() => logSources.Items,
				() => logSourcesPreprocessings.Items,
				() => theme.ThreadColors,
				() => expandedKeys,
				() => selectedKeys,
				() => itemsRevision,
				(sources, preprocessings, themeColors, expanded, selected, rev) => new RootViewItem
				{
					Items = ImmutableArray.CreateRange(
						EnumItemsData(sources, preprocessings, themeColors, expanded, selected, logSourcesPreprocessings))
				}
			);

			this.getSelectedSources = Selectors.Create(
				getRoot,
				root => ImmutableArray.CreateRange(
					ViewItem.Flatten(root).Where(i => i.IsSelected).SelectMany(i =>
					{
						if (i is LogSourceViewItem singleSource)
							return new[] { singleSource.LogSource };
						if (i is SourcesContainerViewItem container)
							return container.LogSources.Select(x => x.LogSource);
						return Enumerable.Empty<ILogSource>();
					})
					.Distinct()
				)
			);

			this.getSelectedPreprocessings = Selectors.Create(
				getRoot,
				root => ImmutableArray.CreateRange(
					ViewItem.Flatten(root).OfType<PreprocessingViewItem>().Select(p => p.Preprocessing)
				)
			);

			this.getFocusedMessageItem = Selectors.Create(
				() => logViewerPresenter.FocusedMessage,
				getRoot,
				(msg, root) =>
				{
					var ls = msg?.GetLogSource();
					return (IViewItem)ViewItem.Flatten(root).FirstOrDefault(
						i => (i as LogSourceViewItem)?.LogSource == ls
					);
				}
			);

			view.SetViewModel(this);
		}

		public event EventHandler DeleteRequested;

		IReadOnlyList<ILogSource> IPresenter.SelectedSources => getSelectedSources();

		IReadOnlyList<ILogSourcePreprocessing> IPresenter.SelectedPreprocessings => getSelectedPreprocessings();

		void IPresenter.SelectSource(ILogSource source)
		{
			SelectItem(i => (i as LogSourceViewItem)?.LogSource == source);
		}

		void IPresenter.SelectPreprocessing(ILogSourcePreprocessing lsp)
		{
			SelectItem(i => (i as PreprocessingViewItem)?.Preprocessing == lsp);
		}

		void IPresenter.SaveLogSourceAs(ILogSource logSource)
		{
			SaveLogSourceAsInternal(logSource);
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IViewItem IViewModel.RootItem => getRoot();
		IViewItem IViewModel.FocusedMessageItem => getFocusedMessageItem();

		void IViewModel.OnSourceProprtiesMenuItemClicked()
		{
			ExecutePropsDialog();
		}

		void IViewModel.OnEnterKeyPressed()
		{
			ExecutePropsDialog();
		}

		void IViewModel.OnDeleteButtonPressed()
		{
			DeleteRequested?.Invoke(this, EventArgs.Empty);
		}

		(MenuItem visibleItems, MenuItem checkedItems) IViewModel.OnMenuItemOpening(bool ctrl)
		{
			MenuItem visibleItems = MenuItem.None;
			MenuItem checkedItems = MenuItem.None;
			ILogSource s = GetSingleSelectedLogSource();
			if (s != null)
			{
				visibleItems |= (MenuItem.SourceVisible | MenuItem.SourceProperties);
				if ((s.Provider is ISaveAs) && ((ISaveAs)s.Provider).IsSavableAs)
					visibleItems |= MenuItem.SaveLogAs;
				if (s.Visible)
					checkedItems |= MenuItem.SourceVisible;
			}
			if (GetSelectionBrowsableFileLocation() != null)
			{
				visibleItems |= MenuItem.OpenContainingFolder;
			}
			if (getSelectedPreprocessings().Any(p => p.Failure != null))
			{
				visibleItems |= MenuItem.CopyErrorMessage;
			}

			int totalSourcesCount = 0;
			int visibeSourcesCount = 0;

			foreach (var ls in logSources.Items)
			{
				++totalSourcesCount;
				if (ls.Visible)
					visibeSourcesCount++;
			}

			bool saveMergedLogFeatureEnabled = true;
			if (saveMergedLogFeatureEnabled && visibeSourcesCount >= 2)
				visibleItems |= (MenuItem.SaveMergedFilteredLog | MenuItem.Separator1);

			if (totalSourcesCount > 1 && s != null)
				visibleItems |= (MenuItem.ShowOnlyThisLog | MenuItem.CloseOthers);
			if (visibeSourcesCount != totalSourcesCount)
				visibleItems |= MenuItem.ShowAllLogs;

			if (visibleItems == (MenuItem.SaveMergedFilteredLog | MenuItem.Separator1))
				visibleItems = MenuItem.SaveMergedFilteredLog; // hide unneeded separator

			return (visibleItems, checkedItems);
		}

		void IViewModel.OnItemCheck(IViewItem item, bool value)
		{
			if (!item.Checked.HasValue)
				return;
			if (item is LogSourceViewItem singleSource)
			{
				singleSource.LogSource.Visible = value;
				return;
			}
			var items = (item as SourcesContainerViewItem)?.LogSources;
			if (items != null)
			{
				foreach (var s in items)
				{
					s.LogSource.Visible = value;
				}
				return;
			}
		}

		void IViewModel.OnItemExpand(IViewItem item)
		{
			expandedKeys = expandedKeys.Add(item.Key);
			changeNotification.Post();
		}

		void IViewModel.OnItemCollapse(IViewItem item)
		{
			expandedKeys = expandedKeys.Remove(item.Key);
			changeNotification.Post();
		}

		void IViewModel.OnSourceVisisbleMenuItemClicked(bool menuItemChecked)
		{
			ILogSource s = GetSingleSelectedLogSource();
			if (s == null)
				return;
			s.Visible = !menuItemChecked;
		}

		void IViewModel.OnShowOnlyThisLogClicked()
		{
			ILogSource selected = GetSingleSelectedLogSource();
			foreach (var src in logSources.Items)
				src.Visible = src == selected;
		}

		void IViewModel.OnShowAllLogsClicked()
		{
			foreach (var src in logSources.Items)
				src.Visible = true;
		}

		void IViewModel.OnCloseOthersClicked()
		{
			if (getSelectedPreprocessings().Length + getSelectedSources().Length != 1)
				return;
			var selectedPreprocessing = getSelectedPreprocessings().FirstOrDefault();
			var selectedLogSource = getSelectedSources().FirstOrDefault();
			Task.WhenAll(
				logSourcesPreprocessings.DeletePreprocessings(
					logSourcesPreprocessings.Items.Where(i => i != selectedPreprocessing).ToArray()),
				logSources.DeleteLogs(
					logSources.Items.Where(i => i != selectedLogSource).ToArray())
			);
		}

		void IViewModel.OnSelectAllShortcutPressed()
		{
			selectedKeys = ImmutableHashSet.CreateRange(ViewItem.Flatten(getRoot()).Select(i => i.Key));
			changeNotification.Post();
		}

		void IViewModel.OnSaveLogAsMenuItemClicked()
		{
			if (GetSingleSelectedLogSource() != null)
				SaveLogSourceAsInternal(GetSingleSelectedLogSource());
		}

		void IViewModel.OnSaveMergedFilteredLogMenuItemClicked()
		{
			saveJointLogInteractionPresenter.StartInteraction();
		}

		void IViewModel.OnOpenContainingFolderMenuItemClicked()
		{
			var folder = GetSelectionBrowsableFileLocation();
			if (folder != null)
				shellOpen.OpenFileBrowser(folder);
		}

		void IViewModel.OnSelectionChange(IReadOnlyList<IViewItem> proposedSelection)
		{
			selectedKeys = ImmutableHashSet.CreateRange(proposedSelection.Select(i => i.Key));
			changeNotification.Post();
		}

		void IViewModel.OnCopyShortcutPressed()
		{
			var textToCopy = string.Join(
				Environment.NewLine,
				((IPresenter)this).SelectedSources
				.Select(s => logSourcesPreprocessings.ExtractCopyablePathFromConnectionParams(s.Provider.ConnectionParams))
				.Union(((IPresenter)this).SelectedPreprocessings.Select(p => p.Failure != null ? p.Failure.Message : null))
				.Where(str => str != null)
				.Distinct()
			);
			if (textToCopy != "")
			{
				clipboard.SetClipboard(textToCopy);
			}
		}

		void IViewModel.OnCopyErrorMessageClicked()
		{
			var textToCopy = string.Join(
				Environment.NewLine,
				((IPresenter)this).SelectedPreprocessings.Select(p => p.Failure != null ? p.Failure.Message : null)
				.Where(s => s != null)
			);
			if (textToCopy != "")
			{
				clipboard.SetClipboard(textToCopy);
			}
		}

		static IEnumerable<LogSourceViewItem> EnumSourceItemsData(
			IEnumerable<ILogSource> sources,
			ImmutableArray<Color> themeColors,
			Preprocessing.IManager logSourcesPreprocessings
		)
		{
			foreach (ILogSource s in sources) 
			{
				var itemData = new LogSourceViewItem() 
				{ 
					LogSource = s,
					ContainerName = logSourcesPreprocessings.ExtractContentsContainerNameFromConnectionParams(
						s.Provider.ConnectionParams)
				};
				LogProviderStats stats = s.Provider.Stats;
				itemData.Checked = s.Visible;
				itemData.Description = GetLogSourceDescription (s, stats);
				itemData.IsFailed = stats.Error != null;
				itemData.ItemColor = stats.Error != null ? failedSourceColor : themeColors.GetByIndex(s.ColorIndex);
				yield return itemData;
			}
		}

		static IEnumerable<PreprocessingViewItem> EnumPreprocItemsData(
			IEnumerable<ILogSourcePreprocessing> preprocessings
		)
		{
			foreach (ILogSourcePreprocessing pls in preprocessings)
			{
				if (pls.IsDisposed)
					continue;
				var itemData = new PreprocessingViewItem
				{
					Preprocessing = pls,
					Checked = null, // uncheckable
				};
				itemData.Description = pls.CurrentStepDescription;
				if (pls.Failure != null)
					itemData.Description = string.Format("{0}. Error: {1}", itemData.Description, pls.Failure.Message);
				itemData.ItemColor = pls.Failure == null ? successfulSourceColor : failedSourceColor;
				itemData.IsFailed = pls.Failure != null;
				yield return itemData;
			}
		}

		static IEnumerable<ViewItem> EnumItemsData(
			IEnumerable<ILogSource> sources,
			IEnumerable<ILogSourcePreprocessing> preprocessings,
			ImmutableArray<Color> themeColors,
			ImmutableHashSet<string> expanded,
			ImmutableHashSet<string> selected,
			Preprocessing.IManager logSourcesPreprocessings
		)
		{
			void initSelected(ViewItem item)
			{
				item.IsSelected = selected.Contains(item.GetKey());
			}

			foreach (var containerGroup in EnumSourceItemsData(
				sources, themeColors, logSourcesPreprocessings).GroupBy(src => src.ContainerName))
			{
				var groupSources = ImmutableArray.CreateRange(containerGroup);
				if (containerGroup.Key != null && groupSources.Length > 1)
				{
					var item = new SourcesContainerViewItem
					{
						ContainerName = containerGroup.Key
					};
					item.LogSources = groupSources;
					item.Description = string.Format("{0} ({1} logs)", containerGroup.Key, groupSources.Length);
					item.ItemColor = groupSources[0].ItemColor;
					item.Checked = groupSources.All(s => s.Checked.GetValueOrDefault());
					item.IsExpanded = expanded.Contains(item.GetKey());
					foreach (var c in groupSources)
					{
						c.Parent = item;
						initSelected(c);
					}
					initSelected(item);
					yield return item;
				}
				else
				{
					foreach (var item in groupSources)
					{
						initSelected(item);
						yield return item;
					}
				}
			}

			foreach (var item in EnumPreprocItemsData(preprocessings))
			{
				initSelected(item);
				yield return item;
			}
		}

		static string GetLogSourceDescription (ILogSource s, LogProviderStats stats)
		{
			StringBuilder msg = new StringBuilder();
			string annotation = "";
			if (!string.IsNullOrWhiteSpace (s.Annotation))
				annotation = s.Annotation + "    ";
			switch (stats.State) {
			case LogProviderState.NoFile:
				msg.Append ("(No trace file)");
				break;
			case LogProviderState.DetectingAvailableTime:
				msg.AppendFormat ("{1} {0}: processing...", s.DisplayName, annotation);
				break;
			case LogProviderState.LoadError:
				msg.AppendFormat (
					"{0}: loading failed ({1})",
					s.DisplayName,
					stats.Error != null ? stats.Error.Message : "");
				break;
			case LogProviderState.Idle:
				if (stats.BackgroundAcivityStatus == LogProviderBackgroundAcivityStatus.Active) {
					msg.AppendFormat ("{1}{0}: processing", s.DisplayName, annotation);
				} else {
					msg.AppendFormat ("{1}{0}", s.DisplayName, annotation);
					if (stats.TotalBytes != null) {
						msg.Append (" (");
						StringUtils.FormatBytesUserFriendly (stats.TotalBytes.Value, msg);
						msg.Append (")");
					}
				}
				break;
			}
			return msg.ToString();
		}

		ILogSource GetSingleSelectedLogSource()
		{
			var sources = getSelectedSources();
			return sources.Length == 1 ? sources[0] : null;
		}

		void ExecutePropsDialog()
		{
			ILogSource src = GetSingleSelectedLogSource();
			if (src == null)
				return;
			propertiesWindowPresenter.ShowWindow(src);
		}

		string GetSelectionBrowsableFileLocation()
		{
			var selected = ViewItem.Flatten(getRoot()).Where(i => i.IsSelected).Take(2).ToArray();
			if (selected.Length != 1)
				return null;
			ILogSource selectedSource = null;
			if (selected[0] is LogSourceViewItem singleSource)
				selectedSource = singleSource.LogSource;
			else if (selected[0] is SourcesContainerViewItem container)
				selectedSource = container.LogSources[0].LogSource;
			if (selectedSource == null)
				return null;
			var fileToShow = logSourcesPreprocessings.ExtractUserBrowsableFileLocationFromConnectionParams(
				selectedSource.Provider.ConnectionParams);
			if (string.IsNullOrWhiteSpace(fileToShow))
				return null;
			return fileToShow;
		}

		void SaveLogSourceAsInternal(ILogSource logSource)
		{
			ISaveAs saveAs = logSource.Provider as ISaveAs;
			if (saveAs == null || !saveAs.IsSavableAs)
				return;
			string filename = fileDialogs.SaveFileDialog(new SaveFileDialogParams()
			{
				SuggestedFileName = saveAs.SuggestedFileName ?? "log.txt"
			});
			if (filename == null)
				return;
			try
			{
				saveAs.SaveAs(filename);
			}
			catch (Exception ex)
			{
				alerts.ShowPopup("Error", "Failed to save file: " + ex.Message, AlertFlags.Ok | AlertFlags.WarningIcon);
			}
		}

		private void SelectItem(Func<IViewItem, bool> pred)
		{
			var matched = ViewItem.Flatten(getRoot())
				.OfType<IViewItem>()
				.Where(pred)
				.Take(1)
				.ToList();
			selectedKeys = ImmutableHashSet.CreateRange(matched.Select(i => i.Key));
			for (var i = matched.FirstOrDefault() as ViewItem; i != null; i = i.Parent)
				if (i is SourcesContainerViewItem container)
					expandedKeys = expandedKeys.Add(i.GetKey());
			matched.ForEach(view.SetTopItem);
			changeNotification.Post();
		}
	};
};
