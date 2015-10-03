
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.BookmarksManager;

namespace LogJoint.UI
{
	public partial class BookmarksManagementControlAdapter : NSViewController, IView
	{
		BookmarksListControlAdapter bookmarksListControlAdapter;
		IViewEvents viewEvents;

		#region Constructors

		// Called when created from unmanaged code
		public BookmarksManagementControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public BookmarksManagementControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}

		public BookmarksManagementControlAdapter()
			: base("BookmarksManagementControl", NSBundle.MainBundle)
		{
			Initialize();
		}
			
		void Initialize()
		{
			bookmarksListControlAdapter = new BookmarksListControlAdapter();
		}

		#endregion

		public Presenters.BookmarksList.IView ListView
		{
			get { return bookmarksListControlAdapter; }
		}

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			bookmarksListControlAdapter.View.MoveToPlaceholder(bookmarksListPlaceholder);
		}

		public new BookmarksManagementControl View
		{
			get { return (BookmarksManagementControl)base.View; }
		}


		void IView.SetPresenter(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		bool IView.ShowDeleteConfirmationPopup(int nrOfBookmarks)
		{
			var alert = new NSAlert ()
			{
				AlertStyle = NSAlertStyle.Warning,
				InformativeText = string.Format("Are you sure you to delete {0} bookmarks", nrOfBookmarks),
				MessageText = "Delete bookmarks",
			};
			alert.AddButton("Yes");
			alert.AddButton("No");
			alert.AddButton("Cancel");
			var res = alert.RunModal ();

			return res == 1000;
		}


		partial void OnAddBookmarkButtonClicked (MonoMac.Foundation.NSObject sender)
		{
			viewEvents.OnAddBookmarkButtonClicked();
		}

		partial void OnRemoveBookmarkButtonClicked (MonoMac.Foundation.NSObject sender)
		{
			viewEvents.OnDeleteBookmarkButtonClicked();
		}
	}
}

