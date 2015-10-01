
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public partial class BookmarksListControlAdapter : MonoMac.AppKit.NSViewController
	{
		#region Constructors

		// Called when created from unmanaged code
		public BookmarksListControlAdapter(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public BookmarksListControlAdapter(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Call to load from the XIB/NIB file
		public BookmarksListControlAdapter()
			: base("BookmarksListControl", NSBundle.MainBundle)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed view accessor
		public new BookmarksListControl View
		{
			get
			{
				return (BookmarksListControl)base.View;
			}
		}
	}
}

