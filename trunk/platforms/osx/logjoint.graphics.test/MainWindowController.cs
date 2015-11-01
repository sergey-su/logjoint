
using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using SD = System.Drawing;
using LJD = LogJoint.Drawing;
using LogJoint.Drawing;
using PointF = System.Drawing.PointF;
using RectangleF = System.Drawing.RectangleF;
using StringAlignment = System.Drawing.StringAlignment;
using Color = System.Drawing.Color;

namespace logjoint.graphics.test
{
	public partial class GraphicsTestMainWindowController : MonoMac.AppKit.NSWindowController
	{

		#region Constructors

		// Called when created from unmanaged code
		public GraphicsTestMainWindowController (IntPtr handle) : base (handle)
		{
			Initialize ();
		}
		
		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public GraphicsTestMainWindowController (NSCoder coder) : base (coder)
		{
			Initialize ();
		}
		
		// Call to load from the XIB/NIB file
		public GraphicsTestMainWindowController () : base ("GraphicsTestMainWindow")
		{
			Initialize ();
		}
		
		// Shared initialization code
		void Initialize ()
		{
		}

		#endregion

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();
		}

		//strongly typed window accessor
		public new GraphicsTestMainWindow Window {
			get {
				return (GraphicsTestMainWindow)base.Window;
			}
		}

	}

	[Register ("GraphicsTestView")]
	public class GraphicsTestView: NSView
	{
		Font font = new Font(NSFont.SystemFontOfSize(NSFont.SystemFontSize).FamilyName, NSFont.SystemFontSize);
		Brush textBrush = new Brush(Color.Black);
		Brush measuredBoxBrush = new Brush(Color.FromArgb(200, Color.Orange));
		Brush frameBrush = new Brush(Color.LightBlue);


		public GraphicsTestView (IntPtr handle) : base (handle)
		{
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public GraphicsTestView (NSCoder coder) : base (coder)
		{
		}

		public override bool IsFlipped {
			get {
				return true;
			}
		}

		void TestStringWithOrigin(Graphics g)
		{
			for (int multiline = 0; multiline <= 1; ++multiline) {
				for (int h = (int)StringAlignment.Near; h <= (int)StringAlignment.Far; ++h) {
					for (int v = (int)StringAlignment.Near; v <= (int)StringAlignment.Far; ++v) {
						var fmt = new StringFormat ((StringAlignment)h, (StringAlignment)v);
						PointF origin = new PointF (10 + multiline * 250 + h * 100, 10 + v * 100);

						var txt = "test";
						if (multiline == 1)
							txt += "\nfoo bar";
						var sz = g.MeasureString (txt, font);

						var measuredBox = new RectangleF (origin, sz);
						measuredBox.X -= h*sz.Width/2;
						measuredBox.Y -= v*sz.Height/2;
						g.FillRectangle (measuredBoxBrush, measuredBox);
						g.DrawString (txt, font, textBrush, origin, fmt);

						NSColor.Red.SetFill ();
						var originRect = new RectangleF (origin, new SD.SizeF ());
						originRect.Inflate (2, 2);
						NSBezierPath.FillRect (originRect);
					}
				}
			}
		}

		void TestStringWithRect(Graphics g)
		{
			for (int partialFit = 0; partialFit <= 3; ++partialFit) {
				for (int h = (int)StringAlignment.Near; h <= (int)StringAlignment.Far; ++h) {
					for (int v = (int)StringAlignment.Near; v <= (int)StringAlignment.Far; ++v) {
						var fmt = new StringFormat ((StringAlignment)h, (StringAlignment)v,
							partialFit >= 1 ? (LineBreakMode)(partialFit - 1) : LineBreakMode.WrapWords);

						PointF origin = new PointF (10 + h * 70 + partialFit * 250, 250 + v * 70);
						RectangleF frame = new RectangleF (origin, new SD.SizeF (58, partialFit >= 1 ? 25 : 60));
						g.FillRectangle (frameBrush, frame);

						var txt = "test foo bar";
						var sz = g.MeasureString (txt, font, fmt, frame.Size);

						RectangleF measuredBox = new RectangleF (new PointF (), sz);

						if (h == (int)StringAlignment.Near)
							measuredBox.X = origin.X;
						else if (h == (int)StringAlignment.Center)
							measuredBox.X = origin.X + (frame.Width - sz.Width) / 2;
						else
							measuredBox.X = frame.Right - sz.Width;
					
						if (v == (int)StringAlignment.Near)
							measuredBox.Y = origin.Y;
						else if (v == (int)StringAlignment.Center)
							measuredBox.Y = origin.Y + (frame.Height - sz.Height) / 2;
						else
							measuredBox.Y = frame.Bottom - sz.Height;

						g.FillRectangle (measuredBoxBrush, measuredBox);
						g.DrawString (txt, font, textBrush, frame, fmt);
					}
				}
			}
		}

		void TestWrapping(Graphics g)
		{
			for (int i = (int)LineBreakMode.WrapWords; i <= (int)LineBreakMode.SingleLineEndEllipsis; ++i) {
				var fmt = new StringFormat (StringAlignment.Near, StringAlignment.Near);

			}
		}

		public override void DrawRect (RectangleF dirtyRect)
		{
			base.DrawRect (dirtyRect);

			using (var g = new Graphics ()) {
				TestStringWithOrigin (g);
				TestStringWithRect (g);
				TestWrapping (g);
			}
		}
	};
}

