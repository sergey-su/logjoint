
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public partial class SharingDialog : MonoMac.AppKit.NSWindow
	{
		#region Constructors

		// Called when created from unmanaged code
		public SharingDialog(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public SharingDialog(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion
	}
}

