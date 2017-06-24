using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public partial class TimeSeriesWindow : AppKit.NSWindow
	{
		internal TimeSeriesWindowController owner;

		#region Constructors

		// Called when created from unmanaged code
		public TimeSeriesWindow (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public TimeSeriesWindow (NSCoder coder) : base (coder)
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
			owner.OnCancelOp ();
		}

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
	}
}

