
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;

namespace LogJoint.UI
{
	public partial class TagsSelectionSheet : MonoMac.AppKit.NSPanel
	{
		#region Constructors

		// Called when created from unmanaged code
		public TagsSelectionSheet (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TagsSelectionSheet (NSCoder coder) : base (coder)
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

