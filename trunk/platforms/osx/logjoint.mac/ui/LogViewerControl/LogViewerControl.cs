
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using LogJoint.UI.Presenters.LogViewer;
using MonoMac.ObjCRuntime;

namespace LogJoint.UI
{
	public partial class LogViewerControl : MonoMac.AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public LogViewerControl(IntPtr handle)
			: base(handle)
		{
			Initialize();
		}
		
		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public LogViewerControl(NSCoder coder)
			: base(coder)
		{
			Initialize();
		}
		
		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		public void Init(LogViewerControlAdapter owner)
		{
			this.owner = owner;
		}

		public override void DrawRect(RectangleF dirtyRect)
		{
			owner.OnPaint(dirtyRect);
		}

		public override bool IsFlipped
		{
			get
			{
				return true;
			}
		}

		public override bool AcceptsFirstResponder()
		{
			return true;
		}
			

		public override void KeyDown(NSEvent theEvent)
		{
			this.InterpretKeyEvents(new [] { theEvent });
		}

		public override bool BecomeFirstResponder()
		{
			owner.isFocused = true;
			return base.BecomeFirstResponder();
		}

		public override bool ResignFirstResponder()
		{
			owner.isFocused = false;
			return base.ResignFirstResponder();
		}

		[Export ("insertText:")]
		void OnInsertText (NSObject theEvent)
		{
			if (theEvent.ToString().ToLower() == "b")
			{
				owner.viewEvents.OnKeyPressed(Key.B, false, false, false);
			}
		}


		[Export ("moveUp:")]
		void OnMoveUp (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Up, m.alt, false, false);
		}

		[Export ("moveDown:")]
		void OnMoveDown (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Down, m.alt, false, false);
		}

		[Export ("moveRight:")]
		void OnMoveRight (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Right, m.alt, false, false);
		}

		[Export ("moveLeft:")]
		void OnMoveLeft (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.Left, m.alt, false, false);
		}

		[Export ("moveLeftAndModifySelection:")]
		void OnMoveLeftAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Left, false, false, true);
		}

		[Export ("moveRightAndModifySelection:")]
		void OnMoveRightAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Right, false, false, true);
		}

		[Export ("moveUpAndModifySelection:")]
		void OnMoveUpAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Up, false, false, true);
		}

		[Export ("moveDownAndModifySelection:")]
		void OnMoveDownAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Down, false, false, true);
		}

		[Export ("moveToBeginningOfLine:")]
		void OnMoveToBeginningOfLine (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Home, false, false, false);
		}

		[Export ("moveToBeginningOfLineAndModifySelection:")]
		void OnMoveToBeginningOfLineAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Home, false, false, true);
		}

		[Export ("moveToEndOfLine:")]
		void OnMoveToEndOfLine (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.End, false, false, false);
		}

		[Export ("moveToEndOfLineAndModifySelection:")]
		void OnMoveToEndOfLineAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.End, false, false, true);
		}

		[Export ("pageDown:")]
		void OnPageDown (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageDown, false, false, false);
		}

		[Export ("pageDownAndModifySelection:")]
		void OnPageDownAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageDown, false, false, true);
		}

		[Export ("pageUp:")]
		void OnPageUp (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageUp, false, false, false);
		}

		[Export ("pageUpAndModifySelection:")]
		void OnPageUpAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageUp, false, false, true);
		}


		#region: scrollXXX actions that are deliberately implemented to modify selection

		[Export ("scrollPageUp:")]
		void OnScrollPageUp (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.PageUp, false, false, m.shift);
		}
			
		[Export ("scrollPageDown:")]
		void OnScrollPageDown (NSObject theEvent)
		{
			var m = new Modifiers();

			owner.viewEvents.OnKeyPressed(Key.PageDown, false, false, m.shift);
		}

		[Export ("scrollToBeginningOfDocument:")]
		void OnScrollToBeginningOfDocument (NSObject theEvent)
		{
			var m = new Modifiers();

			owner.viewEvents.OnKeyPressed(Key.Home, false, false, m.shift);
		}
			
		[Export ("scrollToEndOfDocument:")]
		void OnScrollToEndOfDocument (NSObject theEvent)
		{
			var m = new Modifiers();
			owner.viewEvents.OnKeyPressed(Key.End, false, false, m.shift);
		}

		#endregion


		[Export ("validateMenuItem:")]
		bool OnValidateMenuItem (NSMenuItem item)
		{
			return true;
		}

		[Export ("copy:")]
		void OnCopy (NSObject item)
		{
			owner.viewEvents.OnKeyPressed(Key.Copy, false, false, false);
		}

		[Export ("insertNewline:")]
		void OnInsertNewline (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Enter, false, false, false);
		}

		public override void MouseDown(NSEvent theEvent)
		{
			owner.OnMouseDown(theEvent);
			base.MouseDown(theEvent);
		}

		struct Modifiers
		{
			public bool command, alt, shift;
			public Modifiers()
			{
				var theEvent = NSApplication.SharedApplication.CurrentEvent;
				if (theEvent != null)
				{
					command = (theEvent.ModifierFlags & NSEventModifierMask.CommandKeyMask) != 0;
					alt = (theEvent.ModifierFlags & NSEventModifierMask.AlternateKeyMask) != 0;
					shift = (theEvent.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0;
				}
				else
				{
					command = false;
					alt = false;
					shift = false;
				}
			}
		}

		LogViewerControlAdapter owner;
	}
}

