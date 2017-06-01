using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using LogJoint.UI.Presenters.Postprocessing.SequenceDiagramVisualizer;

namespace LogJoint.UI.Postprocessing.SequenceDiagramVisualizer
{
	public partial class SequenceDiagramWindow : MonoMac.AppKit.NSWindow
	{
		internal SequenceDiagramWindowController owner;

		#region Constructors

		// Called when created from unmanaged code
		public SequenceDiagramWindow (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public SequenceDiagramWindow (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion


		[Export ("moveUp:")]
		void OnMoveUp (NSObject theEvent)
		{
			owner.OnKeyEvent (Key.MoveSelectionUp);
		}

		[Export ("moveDown:")]
		void OnMoveDown (NSObject theEvent)
		{
			owner.OnKeyEvent (Key.MoveSelectionDown);
		}

		[Export ("moveLeft:")]
		void OnMoveLeft (NSObject theEvent)
		{
			owner.OnKeyEvent (Key.Left);
		}

		[Export ("moveRight:")]
		void OnMoveRight (NSObject theEvent)
		{
			owner.OnKeyEvent (Key.Right);
		}

		[Export ("cancelOperation:")]
		void OnCancelOp (NSObject theEvent)
		{
			owner.OnCancelOperation();
		}

		[Export ("insertText:")]
		void OnInsertText (NSObject theEvent)
		{
			var s = theEvent.ToString();
			if (s == "b" || s == "B")
			{
				owner.OnKeyEvent(Key.Bookmark);
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
	}
}

