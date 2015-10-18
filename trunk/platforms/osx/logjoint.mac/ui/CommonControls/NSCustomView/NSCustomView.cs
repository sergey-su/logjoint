using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Drawing;
using MonoMac.CoreText;
using MonoMac.CoreGraphics;

namespace LogJoint.UI
{
	[Register("NSCustomView")]
	public class NSCustomView : MonoMac.AppKit.NSView
	{
		#region Constructors

		// Called when created from unmanaged code
		public NSCustomView (IntPtr handle) : base (handle)
		{
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public NSCustomView (NSCoder coder) : base (coder)
		{
		}

		public NSCustomView(): base()
		{
		}

		#endregion


		public Action<RectangleF> Paint;


		public override void ResetCursorRects()
		{
			base.ResetCursorRects();
		}

		public override void DrawRect(RectangleF dirtyRect)
		{
			base.DrawRect(dirtyRect);
			NSColor.Blue.SetFill();
			NSBezierPath.FillRect(dirtyRect);
			if (Paint != null)
				Paint(dirtyRect);
		}

		public override void MouseDown(NSEvent evt)
		{
			base.MouseDown(evt);
		}

		public override SizeF IntrinsicContentSize
		{
			get
			{
				return new SizeF(200, 200);
			}
		}

	}
}