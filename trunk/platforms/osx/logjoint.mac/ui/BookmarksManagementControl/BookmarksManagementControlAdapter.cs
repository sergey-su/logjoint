
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public partial class BookmarksManagementControlAdapter : NSViewController
	{
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

		public override void AwakeFromNib()
		{
			base.AwakeFromNib();

			bookmarksListControlAdapter.View.MoveToPlaceholder(bookmarksListPlaceholder);
		}

		public new BookmarksManagementControl View
		{
			get
			{
				return (BookmarksManagementControl)base.View;
			}
		}

		BookmarksListControlAdapter bookmarksListControlAdapter;
	}
}

