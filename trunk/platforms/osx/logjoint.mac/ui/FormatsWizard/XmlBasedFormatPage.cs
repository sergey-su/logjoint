using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class XmlBasedFormatPage : AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public XmlBasedFormatPage (IntPtr handle) : base (handle)
		{
			Initialize ();
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public XmlBasedFormatPage (NSCoder coder) : base (coder)
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
