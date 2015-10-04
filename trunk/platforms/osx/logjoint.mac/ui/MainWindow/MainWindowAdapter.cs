
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.MainForm;

namespace LogJoint.UI
{
	public partial class MainWindowAdapter : MonoMac.AppKit.NSWindowController, IView
	{
		IViewEvents viewEvents;
		LoadedMessagesControlAdapter loadedMessagesControlAdapter;
		SourcesManagementControlAdapter sourcesManagementControlAdapter;
		SearchPanelControlAdapter searchPanelControlAdapter;
		BookmarksManagementControlAdapter bookmarksManagementControlAdapter;
		SearchResultsControlAdapter searchResultsControlAdapter;

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

		[Export ("performFindPanelAction:")]
		void OnPerformFindPanelAction (NSObject theEvent)
		{
			viewEvents.OnKeyPressed(KeyCode.F, false, true);
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

		void IView.ExecuteThreadPropertiesDialog(LogJoint.IThread thread, LogJoint.UI.Presenters.IPresentersFacade navHandler)
		{
			// todo
		}

		void IView.SetCancelLongRunningControlsVisibility(bool value)
		{
			// todo
		}

		void IView.SetAnalizingIndicationVisibility(bool value)
		{
			// todo
		}

		void IView.BeginSplittingSearchResults()
		{
			// todo
		}

		void IView.ActivateTab(string tabId)
		{
			int tabIdx;
			switch (tabId)
			{
				case TabIDs.Sources:
					tabIdx = 0;
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

		void IView.EnableFormControls(bool enable)
		{
			// todo
		}

		void IView.EnableOwnedForms(bool enable)
		{
			// todo
		}

		void IView.ShowOptionsMenu()
		{
			// todo
		}

		void IView.ShowAboutBox()
		{
			// todo
		}

		void IView.SetCaption(string value)
		{
			// todo
		}

		void IView.SetUpdateIconVisibility(bool value)
		{
			// todo
		}

		bool IView.ShowRestartConfirmationDialog(string caption, string text)
		{
			// todo
			return false;
		}

		void IView.SetTaskbarState(LogJoint.UI.Presenters.MainForm.TaskbarState state)
		{
			// todo
		}

		void IView.UpdateTaskbarProgress(int progressPercentage)
		{
			// todo
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

		public new MainWindow Window 
		{
			get { return (MainWindow)base.Window; }
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			Window.SetOwner(this);

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

			ComponentsInitializer.WireupDependenciesAndInitMainWindow(this);
		}
			
		partial void OnCurrentTabSelected (NSObject sender)
		{
			this.tabView.SelectAt(this.toolbarTabsSelector.SelectedSegment); 
		}

		class InputFocusState: IInputFocusState
		{
			void IInputFocusState.Restore()
			{
				// todo
			}
		};
	}
}

