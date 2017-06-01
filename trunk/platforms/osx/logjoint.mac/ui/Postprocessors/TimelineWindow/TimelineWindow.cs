using System;
using MonoMac.Foundation;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;

namespace LogJoint.UI.Postprocessing.TimelineVisualizer
{
	public partial class TimelineWindow : MonoMac.AppKit.NSWindow
	{
		internal TimelineWindowController owner;

		#region Constructors

		// Called when created from unmanaged code
		public TimelineWindow (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TimelineWindow (NSCoder coder) : base (coder)
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
			owner.OnKeyEvent (KeyCode.Up);
		}

		[Export ("moveDown:")]
		void OnMoveDown (NSObject theEvent)
		{
			owner.OnKeyEvent (KeyCode.Down);
		}
	
		[Export ("moveLeft:")]
		void OnMoveLeft (NSObject theEvent)
		{
			owner.OnKeyEvent (KeyCode.Left);
		}

		[Export ("moveRight:")]
		void OnMoveRight (NSObject theEvent)
		{
			owner.OnKeyEvent (KeyCode.Right);
		}

		[Export ("insertText:")]
		void OnInsertText (NSObject theEvent)
		{
			// todo: not triggered
			var t = theEvent.ToString ().ToLower ();
			if (t == "+") {
				owner.OnKeyEvent (KeyCode.Plus);
			} else if (t == "-") {
				owner.OnKeyEvent (KeyCode.Minus);
			}
		}

		[Export ("insertNewline:")]
		void OnInsertNewline (NSObject theEvent)
		{
			// does not trigger
			owner.OnKeyEvent (KeyCode.Enter);
		}

		[Export ("cancelOperation:")]
		void OnCancelOp (NSObject theEvent)
		{
			owner.OnCancelOp ();
		}
	
	}
}

