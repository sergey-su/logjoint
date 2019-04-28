using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LogJoint.Preprocessing;
using LogJoint;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.SourcesList
{
	public class Presenter: IPresenter, IViewEvents
	{
		public Presenter(
			ILogSourcesManager logSources,
			IView view,
			ILogSourcesPreprocessingManager logSourcesPreprocessings,
			SourcePropertiesWindow.IPresenter propertiesWindowPresenter,
			LogViewer.IPresenter logViewerPresenter,
			IPresentersFacade navHandler,
			IAlertPopup alerts,
			IFileDialogs fileDialogs,
			IClipboardAccess clipboard,
			IShellOpen shellOpen,
			SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter,
			IColorTheme theme
		)
		{
			this.logSources = logSources;
			this.view = view;
			this.propertiesWindowPresenter = propertiesWindowPresenter;
			this.logViewerPresenter = logViewerPresenter;
			this.logSourcesPreprocessings = logSourcesPreprocessings;
			this.alerts = alerts;
			this.fileDialogs = fileDialogs;
			this.clipboard = clipboard;
			this.shellOpen = shellOpen;
			this.saveJointLogInteractionPresenter = saveJointLogInteractionPresenter;
			this.theme = theme;

			logViewerPresenter.FocusedMessageChanged += (sender, args) =>
			{
				view.InvalidateFocusedMessageArea();
			};

			view.SetPresenter(this);
		}

		public event EventHandler DeleteRequested;
		public event EventHandler SelectionChanged;
		public event EventHandler<BusyStateEventArgs> OnBusyState;

		void IPresenter.UpdateView()
		{
			view.BeginUpdate();
			updateLock++;
			try
			{
				viewItemsCache.MarkAllInvalid();
				foreach (var item in EnumItemsData())
				{
					var lvi = viewItemsCache.Get(new ViewItemsCacheKey(item), i =>
						view.AddItem(i.Item, i.Parent != null ? viewItemsCache.Get(new ViewItemsCacheKey(i.Parent)) : null));
					lvi.Checked = item.Checked;
					lvi.SetText(item.Description);
					lvi.SetBackColor(item.ItemColor, item.IsFailed);
				}
				viewItemsCache.Cleanup(entry => view.Remove(entry.Value));
				if (propertiesWindowPresenter != null)
					propertiesWindowPresenter.UpdateOpenWindow();
			}
			finally
			{
				updateLock--;
				view.EndUpdate();
			}
		}

		IEnumerable<ILogSource> IPresenter.SelectedSources
		{
			get 
			{ 
				return GetSelectedItems().SelectMany(i =>
				{
					var singleSource = i.Datum as LogSourceItemData;
					if (singleSource != null)
						return Enumerable.Repeat(singleSource.LogSource, 1);
					var container = i.Datum as SourcesContainerItemData;
					if (container != null)
						return container.LogSources.Select(x => x.LogSource);
					return Enumerable.Empty<ILogSource>();
				});
			}
		}

		IEnumerable<ILogSourcePreprocessing> IPresenter.SelectedPreprocessings
		{
			get { return GetSelectedItems().Select(i => (i.Datum as PreprocessingItemData)?.Preprocessing).Where(lsp => lsp != null); }
		}

		void IPresenter.SelectSource(ILogSource source)
		{
			SelectItemInternal(i => (i.Datum as LogSourceItemData)?.LogSource == source);
		}

		void IPresenter.SelectPreprocessing(ILogSourcePreprocessing lsp)
		{
			SelectItemInternal(i => (i.Datum as PreprocessingItemData)?.Preprocessing == lsp);
		}

		void IPresenter.SaveLogSourceAs(ILogSource logSource)
		{
			SaveLogSourceAsInternal(logSource);
		}

		void IViewEvents.OnSourceProprtiesMenuItemClicked()
		{
			ExecutePropsDialog();
		}

		void IViewEvents.OnEnterKeyPressed()
		{
			ExecutePropsDialog();
		}

		void IViewEvents.OnDeleteButtonPressed()
		{
			if (DeleteRequested != null)
				DeleteRequested(this, EventArgs.Empty);
		}

		void IViewEvents.OnMenuItemOpening(bool ctrl, out MenuItem visibleItems, out MenuItem checkedItems)
		{
			visibleItems = MenuItem.None;
			checkedItems = MenuItem.None;
			ILogSource s = GetLogSource();
			if (s != null)
			{
				visibleItems |= (MenuItem.SourceVisible | MenuItem.SourceProprties);
				if ((s.Provider is ISaveAs) && ((ISaveAs)s.Provider).IsSavableAs)
					visibleItems |= MenuItem.SaveLogAs;
				if (logSourcesPreprocessings.ExtractUserBrowsableFileLocationFromConnectionParams(s.Provider.ConnectionParams) != null)
					visibleItems |= MenuItem.OpenContainingFolder;
				if (s.Visible)
					checkedItems |= MenuItem.SourceVisible;
			}
			if (GetSelectedItems().Any(i => (i.Datum as PreprocessingItemData)?.Preprocessing?.Failure != null))
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

			if (totalSourcesCount > 1 && GetLogSource() != null)
				visibleItems |= (MenuItem.ShowOnlyThisLog | MenuItem.CloseOthers);
			if (visibeSourcesCount != totalSourcesCount)
				visibleItems |= MenuItem.ShowAllLogs;

			if (visibleItems == (MenuItem.SaveMergedFilteredLog | MenuItem.Separator1))
				visibleItems = MenuItem.SaveMergedFilteredLog; // hide unneeded separator
		}

		void IViewEvents.OnItemChecked(IViewItem item)
		{
			if (updateLock > 0 || !item.Checked.HasValue)
				return;
			var singleSource = item.Datum as LogSourceItemData;
			if (singleSource != null)
			{
				singleSource.LogSource.Visible = item.Checked.GetValueOrDefault();
				if (singleSource.Parent != null)
					singleSource.Parent.Checked = singleSource.Parent.LogSources.All(s => s.Checked.GetValueOrDefault());
				return;
			}
			var items = (item.Datum as SourcesContainerItemData)?.LogSources;
			if (items != null)
			{
				foreach (var s in items)
				{
					s.LogSource.Visible = item.Checked.GetValueOrDefault();
				}
				return;
			}
		}

		void IViewEvents.OnSourceVisisbleMenuItemClicked(bool menuItemChecked)
		{
			if (updateLock != 0)
				return;
			ILogSource s = GetLogSource();
			if (s == null)
				return;
			s.Visible = !menuItemChecked;
		}

		void IViewEvents.OnShowOnlyThisLogClicked()
		{
			ILogSource selected = GetLogSource();
			foreach (var src in logSources.Items)
				src.Visible = src == selected;
		}

		void IViewEvents.OnShowAllLogsClicked()
		{
			foreach (var src in logSources.Items)
				src.Visible = true;
		}

		void IViewEvents.OnCloseOthersClicked()
		{
			var selected = GetSelectedItems().ToArray();
			if (selected.Length != 1)
				return;
			var selectedItem = selected[0];
			var selectedPreprocessing = (selectedItem.Datum as PreprocessingItemData)?.Preprocessing;
			var selectedLogSource = (selectedItem.Datum as LogSourceItemData)?.LogSource;
			Task.WhenAll(
				logSourcesPreprocessings.DeletePreprocessings(
					logSourcesPreprocessings.Items.Where(i => i != selectedPreprocessing).ToArray()),
				logSources.DeleteLogs(
					logSources.Items.Where(i => i != selectedLogSource).ToArray())
			);
		}

		void IViewEvents.OnSelectAllShortcutPressed()
		{
			view.BeginUpdate();
			foreach (var i in view.Items)
				i.Selected = true;
			view.EndUpdate();
		}

		IViewItem IViewEvents.OnFocusedMessageSourcePainting()
		{
			var msg = logViewerPresenter.FocusedMessage;
			if (msg == null)
				return null;
			if (msg.GetLogSource() == null)
				return null;
			var dataItem = sourcesDataCache.Get(msg.GetLogSource());
			if (dataItem == null)
				return null;
			return viewItemsCache.Get(new ViewItemsCacheKey(dataItem));
		}

		void IViewEvents.OnSaveLogAsMenuItemClicked()
		{
			if (GetLogSource() != null)
				SaveLogSourceAsInternal(GetLogSource());
		}

		void IViewEvents.OnSaveMergedFilteredLogMenuItemClicked()
		{
			saveJointLogInteractionPresenter.StartInteraction();
		}

		void IViewEvents.OnOpenContainingFolderMenuItemClicked()
		{
			if (GetLogSource() != null)
				OpenContainingFolder(GetLogSource());
		}

		void IViewEvents.OnSelectionChanged()
		{
			if (SelectionChanged != null)
				SelectionChanged(this, EventArgs.Empty);
		}

		void IViewEvents.OnCopyShortcutPressed()
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

		void IViewEvents.OnCopyErrorMessageCliecked()
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

		#region Implementation

		class ItemData
		{
			public bool? Checked;
			public string Description;
			public ModelColor ItemColor;
			public bool IsFailed;
			public SourcesContainerItemData Parent;
		};

		class LogSourceItemData: ItemData
		{
			public ILogSource LogSource;
			public string ContainerName;
		};

		class ViewItemsCacheKey : Tuple<ItemData, SourcesContainerItemData>
		{
			public ViewItemsCacheKey(ItemData i): base(i, i.Parent)
			{
			}

			public ItemData Item { get { return Item1; } }
			public SourcesContainerItemData Parent { get { return Item2; } }
		};

		class PreprocessingItemData: ItemData
		{
			public ILogSourcePreprocessing Preprocessing;
		};

		class SourcesContainerItemData: ItemData
		{
			public string ContainerName;
			public List<LogSourceItemData> LogSources;
		};

		IEnumerable<LogSourceItemData> EnumSourceItemsData()
		{
			sourcesDataCache.MarkAllInvalid();
			foreach (ILogSource s in logSources.Items) 
			{
				if (s.IsDisposed)
					continue;
				var itemData = sourcesDataCache.Get(s, src => new LogSourceItemData() 
				{ 
					LogSource = src,
					ContainerName = logSourcesPreprocessings.ExtractContentsContainerNameFromConnectionParams(
						src.Provider.ConnectionParams)
				});
				LogProviderStats stats = s.Provider.Stats;
				itemData.Checked = s.Visible;
				itemData.Description = GetLogSourceDescription (s, stats);
				itemData.IsFailed = stats.Error != null;
				itemData.ItemColor = stats.Error != null ? failedSourceColor : theme.ThreadColors.GetByIndex(s.ColorIndex);
				yield return itemData;
			}
			sourcesDataCache.Cleanup();
		}

		IEnumerable<PreprocessingItemData> EnumPreprocItemsData()
		{
			preprocsDataCache.MarkAllInvalid();
			foreach (ILogSourcePreprocessing pls in logSourcesPreprocessings.Items)
			{
				if (pls.IsDisposed)
					continue;
				var itemData = preprocsDataCache.Get(pls, p => new PreprocessingItemData()
				{
					Preprocessing = p,
					Checked = null, // unchackable
				});
				itemData.Description = pls.CurrentStepDescription;
				if (pls.Failure != null)
					itemData.Description = string.Format("{0}. Error: {1}", itemData.Description, pls.Failure.Message);
				itemData.ItemColor = pls.Failure == null ? successfulSourceColor : failedSourceColor;
				itemData.IsFailed = pls.Failure != null;
				yield return itemData;
			}
			preprocsDataCache.Cleanup();
		}

		IEnumerable<ItemData> EnumItemsData()
		{
			containerItemsCache.MarkAllInvalid();
			foreach (var containerGroup in EnumSourceItemsData().GroupBy(src => src.ContainerName))
			{
				SourcesContainerItemData container = null;
				var groupSources = containerGroup.ToList();
				if (containerGroup.Key != null && groupSources.Count > 1)
				{
					var item = containerItemsCache.Get(containerGroup.Key, containerName => new SourcesContainerItemData() 
					{
						ContainerName = containerName,
					});
					item.LogSources = groupSources;
					item.Description = string.Format("{0} ({1} logs)", containerGroup.Key, groupSources.Count);
					item.ItemColor = groupSources[0].ItemColor;
					item.Checked = groupSources.All(s => s.Checked.GetValueOrDefault());
					yield return item;
					container = item;
				}
				foreach (var item in groupSources)
				{
					item.Parent = container;
					yield return item;
				}
			}
			containerItemsCache.Cleanup();
			
			foreach (var item in EnumPreprocItemsData())
				yield return item;
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

		IEnumerable<IViewItem> GetSelectedItems()
		{
			foreach (var item in view.Items)
				if (item.Selected)
					yield return item;
		}

		ILogSource GetLogSource()
		{
			return GetSelectedItems().Select(i => (i.Datum as LogSourceItemData)?.LogSource).FirstOrDefault(ls => ls != null);
		}

		void ExecutePropsDialog()
		{
			ILogSource src = GetLogSource();
			if (src == null)
				return;
			propertiesWindowPresenter.ShowWindow(src);
		}

		void OpenContainingFolder(ILogSource logSource)
		{
			var fileToShow = logSourcesPreprocessings.ExtractUserBrowsableFileLocationFromConnectionParams(logSource.Provider.ConnectionParams);
			if (string.IsNullOrWhiteSpace(fileToShow))
				return;
			shellOpen.OpenFileBrowser(fileToShow);
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

		void SetWaitState(bool value)
		{
			if (OnBusyState != null)
				OnBusyState(this, new BusyStateEventArgs(value));
		}

		private void SelectItemInternal(Predicate<IViewItem> pred)
		{
			view.BeginUpdate();
			try
			{
				foreach (var lvi in view.Items)
				{
					lvi.Selected = pred(lvi);
					if (lvi.Selected)
						view.SetTopItem(lvi);
				}
			}
			finally
			{
				view.EndUpdate();
			}
		}

		readonly ILogSourcesManager logSources;
		readonly ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly IView view;
		readonly SourcePropertiesWindow.IPresenter propertiesWindowPresenter;
		readonly LogViewer.IPresenter logViewerPresenter;
		readonly IAlertPopup alerts;
		readonly IFileDialogs fileDialogs;
		readonly IClipboardAccess clipboard;
		readonly IShellOpen shellOpen;
		readonly SaveJointLogInteractionPresenter.IPresenter saveJointLogInteractionPresenter;
		readonly IColorTheme theme;

		readonly CacheDictionary<ILogSource, LogSourceItemData> sourcesDataCache = 
			new CacheDictionary<ILogSource, LogSourceItemData>();
		readonly CacheDictionary<ILogSourcePreprocessing, PreprocessingItemData> preprocsDataCache = 
			new CacheDictionary<ILogSourcePreprocessing, PreprocessingItemData>();
		readonly CacheDictionary<string, SourcesContainerItemData> containerItemsCache = 
			new CacheDictionary<string, SourcesContainerItemData>();
		readonly CacheDictionary<ViewItemsCacheKey, IViewItem> viewItemsCache = 
			new CacheDictionary<ViewItemsCacheKey, IViewItem>();

		int updateLock;

		static readonly ModelColor successfulSourceColor = new ModelColor(255, 255, 255, 255);
		static readonly ModelColor failedSourceColor = new ModelColor(255, 255, 128, 128);

		#endregion
	};
};