
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using LogJoint.UI.Presenters.LogViewer;
using MonoMac.ObjCRuntime;
using System.Diagnostics;

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
			var s = theEvent.ToString();
			if (s == "b" || s == "B")
			{
				owner.viewEvents.OnKeyPressed(Key.BookmarkShortcut);
			}

			if (s == "[")
			{
				owner.viewEvents.OnKeyPressed(Key.PageUp);
			}
			else if (s == "]")
			{
				owner.viewEvents.OnKeyPressed(Key.PageDown);
			}
			else if (s == "h")
			{
				owner.viewEvents.OnKeyPressed(Key.BeginOfLine);
			}
			else if (s == "H")
			{
				owner.viewEvents.OnKeyPressed(Key.BeginOfDocument);
			}
			else if (s == "e")
			{
				owner.viewEvents.OnKeyPressed(Key.EndOfLine);
			}
			else if (s == "E")
			{
				owner.viewEvents.OnKeyPressed(Key.EndOfDocument);
			}
			else if (s == "w" || s == "W")
			{
				owner.viewEvents.OnKeyPressed(Key.Up | Key.AlternativeModeModifier);
			}
			else if (s == "s" || s == "S")
			{
				owner.viewEvents.OnKeyPressed(Key.Down | Key.AlternativeModeModifier);
			}
		}


		[Export ("moveUp:")]
		void OnMoveUp (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Up | GetModifiers());
		}

		[Export ("moveDown:")]
		void OnMoveDown (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Down | GetModifiers());
		}

		[Export ("moveRight:")]
		void OnMoveRight (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Right | GetModifiers());
		}

		[Export ("moveLeft:")]
		void OnMoveLeft (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Left | GetModifiers());
		}

		[Export ("moveLeftAndModifySelection:")]
		void OnMoveLeftAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Left | Key.ModifySelectionModifier);
		}

		[Export ("moveRightAndModifySelection:")]
		void OnMoveRightAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Right | Key.ModifySelectionModifier);
		}

		[Export ("moveUpAndModifySelection:")]
		void OnMoveUpAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Up | Key.ModifySelectionModifier);
		}

		[Export ("moveDownAndModifySelection:")]
		void OnMoveDownAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Down | Key.ModifySelectionModifier);
		}

		[Export ("moveToBeginningOfLine:")]
		void OnMoveToBeginningOfLine (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.BeginOfLine);
		}

		[Export ("moveToBeginningOfLineAndModifySelection:")]
		void OnMoveToBeginningOfLineAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.BeginOfLine | Key.ModifySelectionModifier);
		}

		[Export ("moveToEndOfLine:")]
		void OnMoveToEndOfLine (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.EndOfLine);
		}

		[Export ("moveToEndOfLineAndModifySelection:")]
		void OnMoveToEndOfLineAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.EndOfLine  | Key.ModifySelectionModifier);
		}

		[Export ("pageDown:")]
		void OnPageDown (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageDown);
		}

		[Export ("pageDownAndModifySelection:")]
		void OnPageDownAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageDown | Key.ModifySelectionModifier);
		}

		[Export ("pageUp:")]
		void OnPageUp (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageUp);
		}

		[Export ("pageUpAndModifySelection:")]
		void OnPageUpAndModifySelection (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageUp | Key.ModifySelectionModifier);
		}


		#region: scrollXXX actions that are deliberately implemented to modify selection

		[Export ("scrollPageUp:")]
		void OnScrollPageUp (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageUp | GetModifiers());
		}
			
		[Export ("scrollPageDown:")]
		void OnScrollPageDown (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.PageDown | GetModifiers());
		}

		[Export ("scrollToBeginningOfDocument:")]
		void OnScrollToBeginningOfDocument (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.BeginOfLine | GetModifiers());
		}

		[Export ("scrollToEndOfDocument:")]
		void OnScrollToEndOfDocument (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.EndOfLine | GetModifiers());
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
			owner.viewEvents.OnKeyPressed(Key.Copy | GetModifiers());
		}

		[Export ("insertNewline:")]
		void OnInsertNewline (NSObject theEvent)
		{
			owner.viewEvents.OnKeyPressed(Key.Enter | GetModifiers());
		}

		public override void ScrollWheel(NSEvent theEvent)
		{
			owner.OnScrollWheel(theEvent);
		}

		public override void MouseDown(NSEvent theEvent)
		{
			owner.OnMouseDown(theEvent);
			base.MouseDown(theEvent);
		}

		public override void MouseMoved(NSEvent theEvent)
		{
			owner.OnMouseMove(theEvent, false);
			base.MouseMoved(theEvent);
		}

		public override void MouseDragged(NSEvent theEvent)
		{
			owner.OnMouseMove(theEvent, true);
			base.MouseDragged(theEvent);
		}

		public override void ResetCursorRects()
		{
			var r = Bounds; 
			r.Offset(FixedMetrics.CollapseBoxesAreaSize, 0);
			var visiblePart = this.ConvertRectFromView(Superview.Bounds, Superview);
			r.Intersect(visiblePart);
			AddCursorRect(r, NSCursor.IBeamCursor);
		}

		public override void DiscardCursorRects()
		{
			base.DiscardCursorRects();
		}

		static Key GetModifiers()
		{
			var ret = Key.None;
			var theEvent = NSApplication.SharedApplication.CurrentEvent;
			if (theEvent != null)
			{
				if ((theEvent.ModifierFlags & NSEventModifierMask.AlternateKeyMask) != 0)
					ret |= Key.AlternativeModeModifier;
				if ((theEvent.ModifierFlags & NSEventModifierMask.ShiftKeyMask) != 0)
					ret |= Key.ModifySelectionModifier;
				if ((theEvent.ModifierFlags & NSEventModifierMask.ControlKeyMask) != 0)
					ret |= Key.JumpOverWordsModifier;
			}
			return ret; 
		}

		LogViewerControlAdapter owner;
	}
}

