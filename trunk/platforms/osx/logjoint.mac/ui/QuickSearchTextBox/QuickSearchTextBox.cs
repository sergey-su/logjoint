using System;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.QuickSearchTextBox;

namespace LogJoint.UI
{
	public partial class QuickSearchTextBox : AppKit.NSSearchField
	{
		internal QuickSearchTextBoxAdapter owner;

		#region Constructors

		// Called when created from unmanaged code
		public QuickSearchTextBox(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public QuickSearchTextBox(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public override bool PerformKeyEquivalent (NSEvent theEvent)
		{
			Key key = Key.None;

			ushort KEY_DOWN = 125;
			ushort KEY_UP = 126;
			ushort KEY_SPACE = 49;
			if (this.GetChildViewLevel(Window.FirstResponder as NSView) == null)
			{
				key = Key.None;
			}
			else if ((theEvent.ModifierFlags & NSEventModifierMask.CommandKeyMask) != 0
			    && theEvent.KeyCode == KEY_DOWN)
			{
				key = Key.ShowListShortcut;
			}
			else if ((theEvent.ModifierFlags & NSEventModifierMask.CommandKeyMask) != 0
			    && theEvent.KeyCode == KEY_UP)
			{
				key = Key.HideListShortcut;
			}
			else if ((theEvent.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0
			    && theEvent.KeyCode == KEY_SPACE)
			{
				key = Key.ShowListShortcut;
			}
			else if (theEvent.KeyCode == KEY_DOWN)
			{
				key = Key.Down;
			}
			else if (theEvent.KeyCode == KEY_UP)
			{
				key = Key.Up;
			}
			if (key != Key.None)
			{
				owner.viewEvents.OnKeyDown(key);
				return true;
			}
			return base.PerformKeyEquivalent (theEvent);;
		}

		[Export ("cancelOperation:")]
		void OnCancelOp (NSEvent theEvent)
		{
			if (StringValue == "")
			{
				// this makes behavior on ESC consistent regardless of
				// whether the field is already empty or not:
				// leave the field empty and release the focus.
				Window.MakeFirstResponder(Window.ContentView);
			}
		}
	}
}

