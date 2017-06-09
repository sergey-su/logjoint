using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI.Postprocessing.MainWindowTabPage
{
	public partial class MainWindowTabPage : AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public MainWindowTabPage (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public MainWindowTabPage (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion
	}
}

