using System;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.MainForm;
using LogJoint.MultiInstance;
using LogJoint.UI.Presenters;

namespace LogJoint.UI
{
	public partial class MainWindowAdapter : AppKit.NSWindowController,
		IView,
		LogJoint.UI.Presenters.SearchPanel.ISearchResultsPanelView,
		Presenters.ISystemThemeDetector
	{
		IViewEvents viewEvents;
		LoadedMessagesControlAdapter loadedMessagesControlAdapter;
		SourcesManagementControlAdapter sourcesManagementControlAdapter;
		SearchPanelControlAdapter searchPanelControlAdapter;
		BookmarksManagementControlAdapter bookmarksManagementControlAdapter;
		SearchResultsControlAdapter searchResultsControlAdapter;
		StatusPopupControlAdapter statusPopupControlAdapter;
		TimelinePanelControlAdapter timelinePanelControlAdapter;
		FiltersManagerControlController hlFiltersManagerControlAdapter;
		bool closing;
		AppDelegate appDelegate;
		ColorThemeMode colorThemeMode = ColorThemeMode.Light;
		IDisposable effectiveAppearanceObserver;
		IInstancesCounter instancesCounter;

		public MainWindowAdapter (AppDelegate appDelegate) : base ("MainWindow")
		{
			this.appDelegate = appDelegate;
		}

		public void Init (IInstancesCounter instancesCounter)
		{
			this.instancesCounter = instancesCounter;
			Window.EnsureCreated ();
			Window.RegisterForDraggedTypes (new [] { NSPasteboard.NSFilenamesType.ToString (), NSPasteboard.NSUrlType.ToString () });
		}

		public bool DraggingEntered(object dataObject)
		{
			return viewEvents.OnDragOver(dataObject);
		}

		public void PerformDragOperation(object dataObject)
		{
			viewEvents.OnDragDrop(dataObject, false /*todo*/);
		}

		public void OnAboutDialogMenuClicked()
		{
			viewEvents.OnAboutMenuClicked();
		}

		public void OnOpenRecentMenuClicked()
		{
			viewEvents.OnOpenRecentMenuClicked();
		}
		
		public void OnReportProblemMenuItemClicked()
		{
			viewEvents.OnReportProblemMenuItemClicked();
		}

		public void OnNewDocumentClicked ()
		{
			viewEvents.OnKeyPressed (KeyCode.NewWindowShortcut);
		}

		public void OnOptionsClicked()
		{
			viewEvents.OnConfigurationMenuClicked();
		}


		[Export ("performFindPanelAction:")]
		void OnPerformFindPanelAction (NSObject sender)
		{
			var mi = sender as NSMenuItem;
			var key = KeyCode.FindShortcut;
			if (mi != null)
			{
				switch (mi.Tag)
				{
					case 1:
						key = KeyCode.FindShortcut;
						break;
					case 2:
						key = KeyCode.FindNextShortcut;
						break;
					case 3:
						key = KeyCode.FindPrevShortcut;
						break;
				}
			}
			viewEvents.OnKeyPressed(key);
		}

		[Export ("validateMenuItem:")]
		bool OnValidateMenuItem (NSMenuItem item)
		{
			return true;
		}

		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		IInputFocusState IView.CaptureInputFocusState()
		{
			return new InputFocusState();
			// todo
		}

		void IView.ExecuteThreadPropertiesDialog(LogJoint.IThread thread, Presenters.IPresentersFacade navHandler, UI.Presenters.IColorTheme theme)
		{
			// todo
		}

		void IView.SetAnalyzingIndicationVisibility(bool value)
		{
			// todo
		}

		void IView.BeginSplittingSearchResults()
		{
			// todo
		}

		void IView.BeginSplittingTabsPanel()
		{
			// todo
		}

		void IView.SetIssueReportingMenuAvailablity(bool value)
		{
			var i = appDelegate?.ReportProblemMenuItem;
			if (i != null && !value)
			{
				i.Menu.RemoveItem(i);
			}
		}

		void IView.ForceClose()
		{
			closing = true;
			Window.Close();
		}


		void IView.ActivateTab(string tabId)
		{
			int tabIdx;
			switch (tabId)
			{
				case TabIDs.Sources:
					tabIdx = 0;
					break;
				case TabIDs.Bookmarks:
					tabIdx = 1;
					break;
				case TabIDs.HighlightingFilteringRules:
					tabIdx = 2;
					break;
				case TabIDs.Search:
					tabIdx = 3;
					break;
				default:
					return;
			}
			this.toolbarTabsSelector.SelectedSegment = tabIdx;
			this.tabView.SelectAt(tabIdx); 
		}

		void IView.AddTab(string tabId, string caption, object uiControl)
		{
			var nativeView = uiControl as NSView;
			if (nativeView == null)
				throw new ArgumentException("view of wrong type passed");
			this.toolbarTabsSelector.SegmentCount += 1;
			this.toolbarTabsSelector.SetLabel(caption, toolbarTabsSelector.SegmentCount - 1);
			var tabItem = new TabViewItem { id = tabId };
			this.tabView.Add(tabItem);
			nativeView.MoveToPlaceholder(tabItem.View);
		}

		void IView.EnableFormControls(bool enable)
		{
			// todo: need that?
		}

		void IView.EnableOwnedForms(bool enable)
		{
			// todo
		}

		void IView.ShowOptionsMenu()
		{
			// todo
		}

		void IView.SetCaption(string value)
		{
			Window.Title = value;
		}

		void IView.SetUpdateIconVisibility(bool value)
		{
			SetToolbarItemVisibility(pendingUpdateNotificationButton, value);
		}

		void IView.SetTaskbarState(TaskbarState state)
		{
			// todo
		}

		void IView.UpdateTaskbarProgress(int progressPercentage)
		{
			// todo
			// http://stackoverflow.com/questions/4004941/adding-an-nsprogressindicator-to-the-dock-icon
		}

		void IView.SetShareButtonState(bool visible, bool enabled, bool progress)
		{
			shareToolbarItem.Enabled = visible && enabled;
		}

		bool LogJoint.UI.Presenters.SearchPanel.ISearchResultsPanelView.Collapsed
		{
			get { return searchResultsPlaceholder.Hidden; }
			set { searchResultsPlaceholder.Hidden = value; searchResultsSplitter.AdjustSubviews(); }
		}

		public LoadedMessagesControlAdapter LoadedMessagesControlAdapter => loadedMessagesControlAdapter;

		public SourcesManagementControlAdapter SourcesManagementControlAdapter => sourcesManagementControlAdapter;

		public SearchPanelControlAdapter SearchPanelControlAdapter => searchPanelControlAdapter;

		public BookmarksManagementControlAdapter BookmarksManagementControlAdapter => bookmarksManagementControlAdapter;

		public FiltersManagerControlController HighlightingFiltersManagerControlAdapter => hlFiltersManagerControlAdapter;

		public SearchResultsControlAdapter SearchResultsControlAdapter => searchResultsControlAdapter;

		public StatusPopupControlAdapter StatusPopupControlAdapter => statusPopupControlAdapter;

		public TimelinePanelControlAdapter TimelinePanelControlAdapter => timelinePanelControlAdapter;

		public new MainWindow Window => (MainWindow)base.Window;

		ColorThemeMode ISystemThemeDetector.Mode => colorThemeMode;

		public override void AwakeFromNib()
		{
			base.AwakeFromNib ();

			Window.SetOwner (this);
			Window.Delegate = new Delegate () { owner = this };

			loadedMessagesControlAdapter = new LoadedMessagesControlAdapter ();
			loadedMessagesControlAdapter.View.MoveToPlaceholder (loadedMessagesPlaceholder);

			sourcesManagementControlAdapter = new SourcesManagementControlAdapter ();
			sourcesManagementControlAdapter.View.MoveToPlaceholder (sourcesManagementViewPlaceholder);

			searchPanelControlAdapter = new SearchPanelControlAdapter ();
			searchPanelControlAdapter.View.MoveToPlaceholder (searchPanelViewPlaceholder);

			bookmarksManagementControlAdapter = new BookmarksManagementControlAdapter ();
			bookmarksManagementControlAdapter.View.MoveToPlaceholder (bookmarksManagementViewPlaceholder);

			searchResultsControlAdapter = new SearchResultsControlAdapter ();
			searchResultsControlAdapter.View.MoveToPlaceholder (searchResultsPlaceholder);

			hlFiltersManagerControlAdapter = new FiltersManagerControlController ();
			hlFiltersManagerControlAdapter.View.MoveToPlaceholder (highlightingManagementPlaceholder);

			statusPopupControlAdapter = new StatusPopupControlAdapter (x => SetToolbarItemVisibility (stopLongOpButton, x));
			statusPopupControlAdapter.View.MoveToPlaceholder (statusPopupPlaceholder);
			statusPopupPlaceholder.Hidden = true;

			timelinePanelControlAdapter = new TimelinePanelControlAdapter ();
			timelinePanelControlAdapter.View.MoveToPlaceholder (timelinePanelPlaceholder);

			SetToolbarItemVisibility (pendingUpdateNotificationButton, false);
			pendingUpdateNotificationButton.ToolTip = "Software update downloaded. Click to restart app and apply update.";

			SetToolbarItemVisibility (stopLongOpButton, false);
			stopLongOpButton.ToolTip = "Stop";

			tabView.Delegate = new TabViewDelegate () { owner = this };

			InitTheme();

			ComponentsInitializer.WireupDependenciesAndInitMainWindow (this);

			viewEvents.OnLoad ();

			var instancesCount = instancesCounter.Count;
			if (instancesCount > 1) {
				var index = instancesCount % 5;
				Window.SetFrame (new CoreGraphics.CGRect (
					index * 20,
					index * 20,
					Window.Frame.Width, Window.Frame.Height), true, true);
			}
		}

		void SetToolbarItemVisibility(NSToolbarItem item, bool value)
		{
			var currentIndex = mainToolbar.Items.IndexOf(x => x == item);
			if (value == currentIndex.HasValue)
				return;
			if (value)
			{
				var placeToInsert = mainToolbar.Items.IndexOf(x => x == shareToolbarItem);
				if (placeToInsert == null)
					throw new InvalidOperationException("cannot modifty toolbar");
				mainToolbar.InsertItem(item.Identifier, placeToInsert.GetValueOrDefault());
			}
			else
			{
				mainToolbar.RemoveItem(currentIndex.Value);
			}
		}
			
		partial void OnCurrentTabSelected (NSObject sender)
		{
			this.tabView.SelectAt(this.toolbarTabsSelector.SelectedSegment); 
		}

		partial void OnRestartButtonClicked (NSObject sender)
		{
			viewEvents.OnRestartPictureClicked();
		}

		partial void OnStopLongOpButtonPressed (NSObject sender)
		{
			statusPopupControlAdapter.FireCancelLongOpEvent();
		}

		partial void OnShareButtonClicked (NSObject sender)
		{
			viewEvents.OnShareButtonClicked();
		}

		void InitTheme()
		{
			if (NSProcessInfo.ProcessInfo.IsOperatingSystemAtLeastVersion (
				new NSOperatingSystemVersion(10, 14, 0)))
			{
				DetectTheme();
				effectiveAppearanceObserver = Window.ContentView.AddObserver (
					new NSString ("effectiveAppearance"),
					NSKeyValueObservingOptions.New,
					_ => DetectTheme()
				);
			}
		}

		void DetectTheme()
		{
			NSAppearance appearance = Window.ContentView.EffectiveAppearance;
			string basicAppearance = appearance?.FindBestMatch (new [] {
				NSAppearance.NameAqua.ToString(),
				NSAppearance.NameDarkAqua.ToString()
			});
			var value = NSAppearance.NameDarkAqua == basicAppearance ?
				ColorThemeMode.Dark : ColorThemeMode.Light; ;
			if (value != colorThemeMode) {
				colorThemeMode = value;
				viewEvents?.ChangeNotification.Post ();
			}
		}

		class InputFocusState: IInputFocusState
		{
			void IInputFocusState.Restore()
			{
				// todo
			}
		};

		class Delegate: NSWindowDelegate
		{
			public MainWindowAdapter owner;

			public override bool WindowShouldClose(NSObject sender)
			{
				if (owner.closing)
					return true;
				owner.viewEvents.OnClosing();
				return false;
			}
		};

		class TabViewDelegate: NSTabViewDelegate
		{
			public MainWindowAdapter owner;

			public override void WillSelect(NSTabView tabView, NSTabViewItem item)
			{
				if (item is TabViewItem tabViewItem)
					owner.viewEvents.OnTabChanging (tabViewItem.id);
			}
		};

		class TabViewItem: NSTabViewItem
		{
			public string id;
		};
	}
			
}

