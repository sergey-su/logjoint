
using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;

namespace LogJoint.UI
{
	public partial class SourcesListControl : AppKit.NSView
	{
		internal SourcesListControlAdapter owner;

		#region Constructors

		// Called when created from unmanaged code
		public SourcesListControl(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public SourcesListControl(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion


		public override void KeyDown(NSEvent evt)
		{
			var NSDeleteCharacter = (char)0x007f; // backspace
			var NSDeleteCharacter2 = (char)0xf728; // delete 
			if ((evt.CharactersIgnoringModifiers ?? "").IndexOfAny(new [] {NSDeleteCharacter, NSDeleteCharacter2}) >= 0)
			{
				owner.viewEvents.OnDeleteButtonPressed();
			}
			else
			{
				base.KeyDown(evt);
			}
		}

		[Export ("copy:")]
		void OnCopy (NSObject theEvent)
		{
			owner.viewEvents.OnCopyShortcutPressed();
		}
	}
}

