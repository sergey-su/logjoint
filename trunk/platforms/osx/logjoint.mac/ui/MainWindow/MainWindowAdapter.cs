
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.MainForm;
using MonoMac.ObjCRuntime;

namespace LogJoint.UI
{
	public partial class MainWindowAdapter : MonoMac.AppKit.NSWindowController, IView, LogJoint.UI.Presenters.SearchPanel.ISearchResultsPanelView
	{
		IViewEvents viewEvents;
		LoadedMessagesControlAdapter loadedMessagesControlAdapter;
		SourcesManagementControlAdapter sourcesManagementControlAdapter;
		SearchPanelControlAdapter searchPanelControlAdapter;
		BookmarksManagementControlAdapter bookmarksManagementControlAdapter;
		SearchResultsControlAdapter searchResultsControlAdapter;
		StatusPopupControlAdapter statusPopupControlAdapter;
		TimelinePanelControlAdapter timelinePanelControlAdapter;
		bool closing;

		#region Constructors

		// Called when created from unmanaged code
		public MainWindowAdapter (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowAdapter (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public MainWindowAdapter () : base ("MainWindow")
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
			Window.RegisterForDraggedTypes(new [] { NSPasteboard.NSFilenamesType.ToString(), NSPasteboard.NSUrlType.ToString() });
		}

		#endregion

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

		void IView.ExecuteThreadPropertiesDialog(LogJoint.IThread thread, Presenters.IPresentersFacade navHandler)
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
				case TabIDs.Search:
					tabIdx = 2;
					break;
				default:
					return;
			}
			this.toolbarTabsSelector.SelectedSegment = tabIdx;
			this.tabView.SelectAt(tabIdx); 
		}

		void IView.AddTab(string tabId, string caption, object uiControl, object tag)
		{
			var nativeView = uiControl as NSView;
			if (nativeView == null)
				throw new ArgumentException("view of wrong type passed");
			this.toolbarTabsSelector.SegmentCount += 1;
			this.toolbarTabsSelector.SetLabel(caption, toolbarTabsSelector.SegmentCount - 1);
			var tabItem = new TabViewItem() { id = tabId, tag = tag };
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

		public LoadedMessagesControlAdapter LoadedMessagesControlAdapter
		{
			get { return loadedMessagesControlAdapter; }
		}

		public SourcesManagementControlAdapter SourcesManagementControlAdapter
		{
			get { return sourcesManagementControlAdapter; }
		}

		public SearchPanelControlAdapter SearchPanelControlAdapter
		{
			get { return searchPanelControlAdapter; }
		}

		public BookmarksManagementControlAdapter BookmarksManagementControlAdapter
		{
			get { return bookmarksManagementControlAdapter; }
		}

		public SearchResultsControlAdapter SearchResultsControlAdapter
		{
			get { return searchResultsControlAdapter; }
		}

		public StatusPopupControlAdapter StatusPopupControlAdapter
		{
			get { return statusPopupControlAdapter; }
		}

		public TimelinePanelControlAdapter TimelinePanelControlAdapter
		{
			get { return timelinePanelControlAdapter; }
		}

		public new MainWindow Window 
		{
			get { return (MainWindow)base.Window; }
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			Window.SetOwner(this);
			Window.Delegate = new Delegate() { owner = this };

			loadedMessagesControlAdapter = new LoadedMessagesControlAdapter ();
			loadedMessagesControlAdapter.View.MoveToPlaceholder(loadedMessagesPlaceholder);

			sourcesManagementControlAdapter = new SourcesManagementControlAdapter();
			sourcesManagementControlAdapter.View.MoveToPlaceholder(sourcesManagementViewPlaceholder);

			searchPanelControlAdapter = new SearchPanelControlAdapter();
			searchPanelControlAdapter.View.MoveToPlaceholder(searchPanelViewPlaceholder);

			bookmarksManagementControlAdapter = new BookmarksManagementControlAdapter();
			bookmarksManagementControlAdapter.View.MoveToPlaceholder(bookmarksManagementViewPlaceholder);

			searchResultsControlAdapter = new SearchResultsControlAdapter();
			searchResultsControlAdapter.View.MoveToPlaceholder(searchResultsPlaceholder);

			statusPopupControlAdapter = new StatusPopupControlAdapter(x => SetToolbarItemVisibility(stopLongOpButton, x));
			statusPopupControlAdapter.View.MoveToPlaceholder(statusPopupPlaceholder);
			statusPopupPlaceholder.Hidden = true;

			timelinePanelControlAdapter = new TimelinePanelControlAdapter();
			timelinePanelControlAdapter.View.MoveToPlaceholder(timelinePanelPlaceholder);

			SetToolbarItemVisibility(pendingUpdateNotificationButton, false);
			pendingUpdateNotificationButton.ToolTip = "Software update downloaded. Click to restart app and apply update.";

			SetToolbarItemVisibility(stopLongOpButton, false);
			stopLongOpButton.ToolTip = "Stop";

			tabView.Delegate = new TabViewDelegate() { owner = this };

			ComponentsInitializer.WireupDependenciesAndInitMainWindow(this);

			viewEvents.OnLoad();
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
				var myItem = item as TabViewItem;
				if (myItem != null)
					owner.viewEvents.OnTabChanging(myItem.id, myItem.tag);
			}
		};

		class TabViewItem: NSTabViewItem
		{
			public string id;
			public object tag;
		};
	}
			
}

