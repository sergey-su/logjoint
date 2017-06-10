
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	public partial class StateInspectorWindow : AppKit.NSWindow
	{
		internal StateInspectorWindowController owner;

		#region Constructors

		// Called when created from unmanaged code
		public StateInspectorWindow (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public StateInspectorWindow (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		[Export ("cancelOperation:")]
		void OnCancelOp (NSObject theEvent)
		{
			owner.OnCancelOperation();
		}
	}
}

