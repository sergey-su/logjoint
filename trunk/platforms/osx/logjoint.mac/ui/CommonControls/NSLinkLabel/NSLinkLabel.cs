using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using CoreText;
using CoreGraphics;
using LogJoint.Drawing;

namespace LogJoint.UI
{
	[Register("NSLinkLabel")]
	public class NSLinkLabel : AppKit.NSView
	{
		string text = "";
		NSMutableAttributedString attrString, darkBgAttrString;
		List<Link> links = new List<Link>();
		bool linksSet;
		NSColor textColor = NSColor.Text;
		NSColor linksColor = NSColor.LinkColor;
		NSCursor cursor = NSCursor.PointingHandCursor;
		nfloat fontSize = NSFont.SystemFontSize;
		bool underlineLinks = true;
		bool isEnabled = true;

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

		public static NSLinkLabel CreateLabel(string text = "")
		{
			var view = new NSLinkLabel
			{
				BackgroundColor = NSColor.Clear,
				LinksColor = NSColor.ControlText,
				UnderlineLinks = false,
				Cursor = NSCursor.ArrowCursor,
				RespectInteriorBackgroundStyle = true,
				StringValue = text,
			};
			return view;
		}

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
				linksSet = true;
				InvalidateView ();
			}
		}

		public bool IsEnabled
		{
			get
			{
				return isEnabled;
			}
			set
			{
				isEnabled = value;
				InvalidateView();
			}
		}

		public class LinkClickEventArgs: EventArgs
		{
			public Link Link { get; private set; }
			public NSEvent NativeEvent { get; private set; }
			public bool SuppressDefault { get; set; }

			public LinkClickEventArgs(Link l, NSEvent nativeEvent)
			{
				this.Link = l;
				this.NativeEvent = nativeEvent;
			}
		};

		public EventHandler<LinkClickEventArgs> LinkClicked;

		public NSColor BackgroundColor;
		public NSColor TextColor
		{
			get { return textColor; } 
			set
			{
				textColor = value;
				InvalidateView ();
			}
		}
		public NSColor LinksColor
		{
			get { return linksColor; } 
			set
			{
				linksColor = value;
				InvalidateView ();
			}
		}
		public bool UnderlineLinks
		{
			get { return underlineLinks; }
			set { underlineLinks = value; InvalidateView(); }
		}
		public bool SingleLine = true;
		public NSCursor Cursor
		{
			get { return cursor; }
			set { cursor = value; InvalidateView(); }
		}
		public nfloat FontSize 
		{
			get { return fontSize; }
			set { fontSize = value; InvalidateView(); }
		}

		public bool RespectInteriorBackgroundStyle { get; set; }

		public override bool IsFlipped
		{
			get
			{
				return false;
			}
		}


		public override void ResetCursorRects()
		{
			if (!isEnabled)
			{
				return;
			}
			if (!linksSet)
			{
				AddCursorRect(Bounds, cursor);
			}
			else
			{
				foreach (var r in GetLinksRectsInternal())
					AddCursorRect(r.Value.ToCGRect(), cursor);
			}
		}

		public override void DrawRect(CGRect dirtyRect)
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

			if (RespectInteriorBackgroundStyle 
			 && (Superview as NSTableRowView)?.InteriorBackgroundStyle == NSBackgroundStyle.Dark)
			{
				DarkBgAttributedStringWithLinks.DrawString(Bounds);
			}
			else
			{
				AttributedStringWithLinks.DrawString(Bounds);
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
		}

		public override void MouseDown(NSEvent theEvent)
		{
			if (!isEnabled)
			{
				base.MouseDown(theEvent);
				return;
			}

			Action<Link> fire = l =>
			{
				var args = new LinkClickEventArgs (l, theEvent);
				LinkClicked?.Invoke (this, args);
				if (!args.SuppressDefault)
					base.MouseDown(theEvent);		
			};

			if (!linksSet)
			{
				fire(new Link());
			}
			else
			{
				var pt = this.ConvertPointFromView(theEvent.LocationInWindow, null).ToPointF ();
				foreach (var l in GetLinksRectsInternal().Where(l => l.Value.Contains(pt)).Take(1))
				{
					fire(l.Key);
					break;
				}
			}
		}

		public override CGSize IntrinsicContentSize
		{
			get
			{
				var sz = AttributedStringWithLinks.Size;
				return new CGSize (sz.Width + 1, sz.Height);
			}
		}

		void InvalidateView()
		{
			attrString = null;
			darkBgAttrString = null;
			var win = Window;
			if (win != null)
				win.InvalidateCursorRectsForView (this);
			this.InvalidateIntrinsicContentSize ();
			this.NeedsDisplay = true;
		}

		IEnumerable<Link> GetLinksInternal()
		{
			if (!isEnabled)
			{
				yield break;
			}
			else if (!linksSet)
			{
				yield return new Link(0, text.Length);
			}
			else
			{
				foreach (var l in links)
				{
					var s = Math.Max(l.Start, 0);
					yield return new Link(s, Math.Min(l.Length, text.Length - s), l.Tag);
				}
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
				var origins = new CGPoint[lines.Length];
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
						nfloat ascent;
						nfloat descent;
						nfloat tmp;
						runBounds.Width = (float)run.GetTypographicBounds(new NSRange(0, 0), out ascent, out descent, out tmp);
						runBounds.Height = (float)(ascent + descent);

						nfloat xOffset = line.GetOffsetForStringIndex(run.StringRange.Location, out tmp);
						runBounds.X = (float)(origins[lineIdx].X + xOffset);
						runBounds.Y = (float)origins[lineIdx].Y;
						runBounds.Y -= (float)descent;

						yield return new KeyValuePair<Link, RectangleF>(link, runBounds);
					}

					++lineIdx;
				}
			}
		}

		static NSMutableAttributedString MakeAttributedString(
			string text, IEnumerable<Link> links, NSColor textColor, NSColor linksColor, 
			nfloat fontSize, bool underline, bool singleLine)
		{
			var attrString = new NSMutableAttributedString(text);
			attrString.BeginEditing();
			if (textColor != null)
			{
				attrString.AddAttribute(NSStringAttributeKey.ForegroundColor, textColor,
					new NSRange (0, text.Length));
			}
			foreach (var l in links)
			{
				var range = new NSRange (l.Start, l.Length);
				attrString.AddAttribute(NSStringAttributeKey.ForegroundColor, linksColor, range);
				if (underline)
				{
					var NSUnderlineStyleSingle = 1;
					attrString.AddAttribute(NSStringAttributeKey.UnderlineStyle, new NSNumber(NSUnderlineStyleSingle), range);    
				}
			}
			var fullRange = new NSRange (0, text.Length);
			var para = new NSMutableParagraphStyle();
			para.Alignment = NSTextAlignment.Left;
			if (singleLine)
			{
				para.LineBreakMode = NSLineBreakMode.TruncatingTail;
				para.TighteningFactorForTruncation = 0;
			}
			else
			{
				para.LineBreakMode = NSLineBreakMode.CharWrapping;
			}
			attrString.AddAttribute(NSStringAttributeKey.ParagraphStyle, para, fullRange);
			attrString.AddAttribute(NSStringAttributeKey.Font, 
				NSFont.SystemFontOfSize(fontSize), fullRange);
			attrString.EndEditing();
			return attrString;
		}

		NSMutableAttributedString AttributedStringWithLinks
		{
			get
			{
				if (attrString != null)
					return attrString;
				attrString = MakeAttributedString (
					text, GetLinksInternal (), 
					isEnabled ? textColor : NSColor.DisabledControlText, linksColor, 
					fontSize, underlineLinks, SingleLine);
				return attrString;
			}
		}

		NSMutableAttributedString DarkBgAttributedStringWithLinks
		{
			get
			{
				if (darkBgAttrString != null)
					return darkBgAttrString;
				darkBgAttrString = MakeAttributedString (
					text, GetLinksInternal (), 
					isEnabled ? NSColor.White : NSColor.DisabledControlText, NSColor.White, 
					fontSize, underlineLinks, SingleLine);
				return darkBgAttrString;
			}
		}
	}
}