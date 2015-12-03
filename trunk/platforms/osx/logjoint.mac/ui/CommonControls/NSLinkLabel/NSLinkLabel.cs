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
	[Register("NSLinkLabel")]
	public class NSLinkLabel : MonoMac.AppKit.NSView
	{
		string text = "";
		NSMutableAttributedString attrString;
		List<Link> links = new List<Link>();

		#region Constructors

		// Called when created from unmanaged code
		public NSLinkLabel (IntPtr handle) : base (handle)
		{
		}

		// Called when created directly from a XIB file
		[Export ("initWithCoder:")]
		public NSLinkLabel (NSCoder coder) : base (coder)
		{
		}

		public NSLinkLabel(): base()
		{
		}

		#endregion

		public string StringValue
		{
			get
			{
				return text;
			}
			set
			{
				value = value ?? "";
				if (text == value)
					return;
				text = value;
				InvalidateView ();
			}
		}

		public struct Link
		{
			int start;
			int length;
			object tag;

			public int Start { get { return start; } }
			public int Length { get { return length; } }
			public object Tag { get { return tag; } }

			public Link(int start, int len, object tag = null)
			{
				this.start = start;
				this.length = len;
				this.tag = tag;
			}
		};

		public IList<Link> Links
		{
			get
			{ 
				return links.AsReadOnly(); 
			}
			set
			{
				links.Clear ();
				if (value != null)
					links.AddRange (value);
				InvalidateView ();
			}
		}

		public class LinkClickEventArgs: EventArgs
		{
			public Link Link { get; private set; }
			public NSEvent NativeEvent { get; private set; }

			public LinkClickEventArgs(Link l, NSEvent nativeEvent)
			{
				this.Link = l;
				this.NativeEvent = nativeEvent;
			}
		};

		public EventHandler<LinkClickEventArgs> LinkClicked;

		public NSColor BackgroundColor;
		public bool SingleLine = true;

		public override bool IsFlipped
		{
			get
			{
				return false;
			}
		}


		public override void ResetCursorRects()
		{
			if (links.Count == 0)
			{
				AddCursorRect(Bounds, NSCursor.PointingHandCursor);
			}
			else
			{
				foreach (var r in GetLinksRectsInternal())
					AddCursorRect(r.Value, NSCursor.PointingHandCursor);
			}
		}

		public override void DrawRect(RectangleF dirtyRect)
		{
			if (BackgroundColor != null)
			{
				BackgroundColor.SetFill();
				NSBezierPath.FillRect(dirtyRect);
			}
			else
			{
				base.DrawRect(dirtyRect);
			}

			#if NSLINKLABEL_DEBUG
			int i = 0;
			foreach (var r in GetLinksRectsInternal())
			{
			if (((++i) % 2) == 0)
			NSColor.Yellow.SetFill();
			else
			NSColor.Green.SetFill();
			NSBezierPath.FillRect(r.Value);
			}
			}
			#endif

			AttributedStringWithLinks.DrawString(Bounds);
		}

		public override void MouseDown(NSEvent evt)
		{
			base.MouseDown(evt);

			if (links.Count == 0)
			{
				if (LinkClicked != null)
					LinkClicked(this, new LinkClickEventArgs(new Link(), evt));
			}
			else
			{
				var pt = this.ConvertPointFromView(evt.LocationInWindow, null);
				foreach (var l in GetLinksRectsInternal().Where(l => l.Value.Contains(pt)).Take(1))
				{
					if (LinkClicked != null)
						LinkClicked(this, new LinkClickEventArgs(l.Key, evt));
				}
			}
		}

		public override SizeF IntrinsicContentSize
		{
			get
			{
				return AttributedStringWithLinks.Size;
			}
		}

		void InvalidateView()
		{
			attrString = null;
			var win = Window;
			if (win != null)
				win.InvalidateCursorRectsForView (this);
			this.InvalidateIntrinsicContentSize ();
			this.NeedsDisplay = true;
		}

		IEnumerable<Link> GetLinksInternal()
		{
			if (links.Count == 0)
				yield return new Link (0, text.Length);
			else
				foreach (var l in links) 
				{
					var s = Math.Max (l.Start, 0);
					yield return new Link(s, Math.Min(l.Length, text.Length - s), l.Tag);
				}
		}

		IEnumerable<KeyValuePair<Link, RectangleF>> GetLinksRectsInternal()
		{
			var links = GetLinksInternal().ToArray();

			using (var path = new CGPath())
			using (var framesetter = new CTFramesetter(AttributedStringWithLinks))
			{
				var boundsRect = Bounds;
				path.AddRect(boundsRect);
				var ctframe = framesetter.GetFrame(new NSRange(0, 0), path, null);
				var lines = ctframe.GetLines();
				var origins = new PointF[lines.Length];
				ctframe.GetLineOrigins(new NSRange(0, 0), origins);
				int lineIdx = 0;
				foreach (var line in lines)
				{
					foreach (var run in line.GetGlyphRuns ())
					{
						var runRange = run.StringRange;

						var link = links.FirstOrDefault(l => 
							runRange.Location >= l.Start && (runRange.Location + runRange.Length) <= (l.Start + l.Length));
						if (link.Length == 0)
							continue;

						RectangleF runBounds = new RectangleF();
						float ascent;
						float descent;
						float tmp;
						runBounds.Width = (float)run.GetTypographicBounds(new NSRange(0, 0), out ascent, out descent, out tmp);
						runBounds.Height = ascent + descent;

						float xOffset = line.GetOffsetForStringIndex(run.StringRange.Location, out tmp);
						runBounds.X = origins[lineIdx].X + xOffset;
						runBounds.Y = origins[lineIdx].Y;
						runBounds.Y -= descent;

						yield return new KeyValuePair<Link, RectangleF>(link, runBounds);
					}

					++lineIdx;
				}
			}
		}

		static NSMutableAttributedString MakeAttributedString(string text, IEnumerable<Link> links, bool singleLine)
		{
			var attrString = new NSMutableAttributedString(text);
			attrString.BeginEditing();
			foreach (var l in links)
			{
				var range = new NSRange (l.Start, l.Length);
				attrString.AddAttribute(NSAttributedString.ForegroundColorAttributeName, NSColor.Blue, range);
				var NSUnderlineStyleSingle = 1;
				attrString.AddAttribute(NSAttributedString.UnderlineStyleAttributeName, new NSNumber(NSUnderlineStyleSingle), range);    
			}
			var fullRange = new NSRange (0, text.Length);
			var para = new NSMutableParagraphStyle();
			para.Alignment = NSTextAlignment.Left;
			if (singleLine)
				para.LineBreakMode = NSLineBreakMode.TruncatingTail;
			else
				para.LineBreakMode = NSLineBreakMode.CharWrapping;
			attrString.AddAttribute(NSAttributedString.ParagraphStyleAttributeName, para, fullRange);
			attrString.AddAttribute(NSAttributedString.FontAttributeName, 
				NSFont.SystemFontOfSize(NSFont.SystemFontSize), fullRange);
			attrString.EndEditing();
			return attrString;
		}

		NSMutableAttributedString AttributedStringWithLinks
		{
			get
			{
				if (attrString != null)
					return attrString;
				attrString = MakeAttributedString (text, GetLinksInternal (), SingleLine);
				return attrString;
			}
		}
	}
}