using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using LogJoint.Preprocessing;
using System.Diagnostics;
using System.IO;
using LogJoint;

namespace LogJoint.UI.Presenters.SourcesList
{
	public class Presenter: IPresenter, IViewEvents
	{
		#region Public interface

		public Presenter(
			IModel model,
			IView view,
			Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings,
			SourcePropertiesWindow.IPresenter propertiesWindowPresenter,
			LogViewer.IPresenter logViewerPresenter,
			IPresentersFacade navHandler)
		{
			this.model = model;
			this.view = view;
			this.propertiesWindowPresenter = propertiesWindowPresenter;
			this.logViewerPresenter = logViewerPresenter;
			this.navHandler = navHandler;
			this.logSourcesPreprocessings = logSourcesPreprocessings;

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
				for (int i = view.ItemsCount - 1; i >= 0; --i)
				{
					var viewItem = view.GetItem(i);
					ILogSource ls = viewItem.LogSource;
					if (ls != null)
					{
						if (ls.IsDisposed)
							view.RemoveAt(i);
						continue;
					}
					ILogSourcePreprocessing pls = viewItem.LogSourcePreprocessing;
					if (pls != null)
					{
						if (pls.IsDisposed)
							view.RemoveAt(i);
						continue;
					}
				}
				foreach (var item in EnumItemsData())
				{
					IViewItem lvi;
					int idx = view.IndexOfKey(item.HashCode.ToString());
					if (idx < 0)
					{
						lvi = view.CreateItem(item.HashCode.ToString(), item.LogSource, item.LogSourcePreprocessing);
						view.Add(lvi);
					}
					else
					{
						lvi = view.GetItem(idx);
					}

					lvi.Checked = item.Checked;
					lvi.SetText(item.Description);
					lvi.SetBackColor(item.ItemColor);
				}
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
			get { return GetSelectedItems().Select(i => i.LogSource).Where(ls => ls != null); }
		}

		IEnumerable<ILogSourcePreprocessing> IPresenter.SelectedPreprocessings
		{
			get { return GetSelectedItems().Select(i => i.LogSourcePreprocessing).Where(lsp => lsp != null); }
		}

		void IPresenter.SelectSource(ILogSource source)
		{
			view.BeginUpdate();
			try
			{
				for (int sourceIdx = 0; sourceIdx < view.ItemsCount; ++sourceIdx)
				{
					var lvi = view.GetItem(sourceIdx);
					lvi.Selected = lvi.LogSource == source;
					if (lvi.Selected)
						view.SetTopItem(lvi);
				}
			}
			finally
			{
				view.EndUpdate();
			}
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
				if ((s.Provider is IOpenContainingFolder) && ((IOpenContainingFolder)s.Provider).PathOfFileToShow != null)
					visibleItems |= MenuItem.OpenContainingFolder;
				if (s.Visible)
					checkedItems |= MenuItem.SourceVisible;
			}

			int totalSourcesCount = 0;
			int visibeSourcesCount = 0;

			foreach (var ls in model.SourcesManager.Items)
			{
				++totalSourcesCount;
				if (ls.Visible)
					visibeSourcesCount++;
			}

			bool saveMergedLogFeatureEnabled = ctrl;
			if (saveMergedLogFeatureEnabled && visibeSourcesCount > 0)
				visibleItems |= (MenuItem.SaveMergedFilteredLog | MenuItem.Separator1);

			if (totalSourcesCount > 1)
				visibleItems |= MenuItem.ShowOnlyThisLog;
			if (visibeSourcesCount != totalSourcesCount)
				visibleItems |= MenuItem.ShowAllLogs;
		}

		void IViewEvents.OnItemChecked(IViewItem item)
		{
			if (updateLock > 0)
				return;
			ILogSource s = item.LogSource;
			if (s != null && item.Checked.HasValue && s.Visible != item.Checked.GetValueOrDefault())
			{
				s.Visible = item.Checked.GetValueOrDefault();
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
			foreach (var src in model.SourcesManager.Items)
				src.Visible = src == selected;
		}

		void IViewEvents.OnShowAllLogsClicked()
		{
			foreach (var src in model.SourcesManager.Items)
				src.Visible = true;
		}

		void IViewEvents.OnFocusedMessageSourcePainting(out ILogSource logSourceToPaint)
		{
			logSourceToPaint = null;
			var msg = logViewerPresenter.FocusedMessage;
			if (msg == null)
				return;
			logSourceToPaint = msg.LogSource;
		}

		void IViewEvents.OnSaveLogAsMenuItemClicked()
		{
			if (GetLogSource() != null)
				SaveLogSourceAsInternal(GetLogSource());
		}

		void IViewEvents.OnSaveMergedFilteredLogMenuItemClicked()
		{
			SaveJointAndFilteredLog();
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

		#endregion

		#region Implementation

		struct ItemData
		{
			public int HashCode;
			public ILogSource LogSource;
			public ILogSourcePreprocessing LogSourcePreprocessing;
			public bool? Checked;
			public string Description;
			public ModelColor ItemColor;
		};

		IEnumerable<ItemData> EnumItemsData()
		{
			foreach (ILogSource s in model.SourcesManager.Items)
			{
				StringBuilder msg = new StringBuilder();
				string annotation = "";
				if (!string.IsNullOrWhiteSpace(s.Annotation))
					annotation = s.Annotation + "    ";
				LogProviderStats stats = s.Provider.Stats;
				switch (stats.State)
				{
					case LogProviderState.NoFile:
						msg.Append("(No trace file)");
						break;
					case LogProviderState.DetectingAvailableTime:
						msg.AppendFormat("{1} {0}: processing...", s.DisplayName, annotation);
						break;
					case LogProviderState.LoadError:
						msg.AppendFormat(
							"{0}: loading failed ({1})",
							s.DisplayName,
							stats.Error != null ? stats.Error.Message : "");
						break;
					case LogProviderState.Loading:
						msg.AppendFormat("{2}{0}: loading ({1} messages loaded)", s.DisplayName, stats.MessagesCount, annotation);
						break;
					case LogProviderState.Searching:
						msg.AppendFormat("{1}{0}: searching", s.DisplayName, annotation);
						break;
					case LogProviderState.Idle:
						if (stats.BackgroundAcivityStatus == LogProviderBackgroundAcivityStatus.Active)
						{
							msg.AppendFormat("{1}{0}: processing ({2} messages loaded)", s.DisplayName, annotation, stats.MessagesCount);
						}
						else
						{
							msg.AppendFormat("{2}{0} ({1} messages in memory", s.DisplayName, stats.MessagesCount, annotation);
							if (stats.LoadedBytes != null)
							{
								msg.Append(", ");
								if (stats.TotalBytes != null)
								{
									StringUtils.FormatBytesUserFriendly(stats.LoadedBytes.Value, msg);
									msg.Append(" of ");
									StringUtils.FormatBytesUserFriendly(stats.TotalBytes.Value, msg);
								}
								else
								{
									StringUtils.FormatBytesUserFriendly(stats.LoadedBytes.Value, msg);
								}
							}
							msg.Append(")");
						}
						break;
				}
				ModelColor color;
				if (stats.Error != null)
					color = failedSourceColor;
				else
					color = s.Color;
				yield return new ItemData()
				{
					HashCode = s.GetHashCode(),
					LogSource = s,
					Checked = s.Visible,
					Description = msg.ToString(),
					ItemColor = color
				};
			}
			foreach (ILogSourcePreprocessing pls in logSourcesPreprocessings.Items)
			{
				string description = pls.CurrentStepDescription;
				if (pls.Failure != null)
					description = string.Format("{0}. Error: {1}", description, pls.Failure.Message);
				yield return new ItemData()
				{
					HashCode = pls.GetHashCode(),
					LogSourcePreprocessing = pls,
					Checked = null,
					Description = description,
					ItemColor = pls.Failure == null ? successfulSourceColor : failedSourceColor
				};
			}
		}

		IEnumerable<IViewItem> GetSelectedItems()
		{
			for (int i = 0; i < view.ItemsCount; ++i)
			{
				var item = view.GetItem(i);
				if (item.Selected)
					yield return item;
			}
		}

		ILogSource GetLogSource()
		{
			return GetSelectedItems().Select(i => i.LogSource).FirstOrDefault(ls => ls != null);
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
			var intf = logSource.Provider as IOpenContainingFolder;
			if (intf == null)
				return;
			var fileToShow = intf.PathOfFileToShow;
			if (string.IsNullOrWhiteSpace(fileToShow))
				return;
			Process.Start("explorer.exe", "/select,\"" + fileToShow + "\"");
		}

		void SaveJointAndFilteredLog()
		{
			if (!model.ContainsEnumerableLogSources)
				return;
			string filename = view.ShowSaveLogDialog("joint-log.xml");
			if (filename == null)
				return;
			SetWaitState(true);
			try
			{
				using (var fs = new FileStream(filename, FileMode.Create))
				using (var writer = new LogJoint.Writers.NativeLogWriter(fs))
					model.SaveJointAndFilteredLog(writer);
			}
			catch (Exception e)
			{
				view.ShowSaveLogError(e.Message);
			}
			finally
			{
				SetWaitState(false);
			}
		}

		void SaveLogSourceAsInternal(ILogSource logSource)
		{
			ISaveAs saveAs = logSource.Provider as ISaveAs;
			if (saveAs == null || !saveAs.IsSavableAs)
				return;
			string filename = view.ShowSaveLogDialog(saveAs.SuggestedFileName ?? "log.txt");
			if (filename == null)
				return;
			try
			{
				saveAs.SaveAs(filename);
			}
			catch (Exception ex)
			{
				view.ShowSaveLogError("Failed to save file: " + ex.Message);
			}
		}

		void SetWaitState(bool value)
		{
			if (OnBusyState != null)
				OnBusyState(this, new BusyStateEventArgs(value));
		}

		readonly IModel model;
		readonly IView view;
		readonly Preprocessing.ILogSourcesPreprocessingManager logSourcesPreprocessings;
		readonly SourcePropertiesWindow.IPresenter propertiesWindowPresenter;
		readonly LogViewer.IPresenter logViewerPresenter;
		readonly IPresentersFacade navHandler;
		readonly LazyUpdateFlag updateTracker = new LazyUpdateFlag();

		int updateLock;

		static readonly ModelColor successfulSourceColor = new ModelColor(255, 255, 255, 255);
		static readonly ModelColor failedSourceColor = new ModelColor(255, 255, 128, 128);

		#endregion
	};
};