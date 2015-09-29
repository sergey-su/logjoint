using System.Drawing;
using System;

namespace LogJoint.Drawing
{
	public partial class Graphics: IDisposable
	{
#if SYS_DRAWING
		public Graphics(System.Drawing.Graphics g)
		{
			Init(g);
		}
		partial void Init(System.Drawing.Graphics g);
#endif
#if MONOMAC
		public Graphics()
		{
			InitFromCurrentContext();
		}
		partial void InitFromCurrentContext();
#endif

		public void FillRectangle(Brush brush, Rectangle rect) // todo: get rid of this
		{
			FillRectangleImp(brush, rect);
		}

		public void FillRectangle(Brush brush, RectangleF rect)
		{
			FillRectangleImp(brush, rect);
		}

		public void DrawString(string s, Font font, Brush brush, PointF pt, StringFormat format = null)
		{
			DrawStringImp(s, font, brush, pt, format);
		}

		public RectangleF MeasureCharacterRange(string str, Font font, StringFormat format, CharacterRange range)
		{
			RectangleF r = new RectangleF();
			MeasureCharacterRangeImp(str, font, format, range, ref r);
			return r;
		}

		public void DrawRectangle (Pen pen, RectangleF rect)
		{
			DrawRectangleImp(pen, rect);
		}

		public void DrawLine(Pen pen, PointF pt1, PointF pt2)
		{
			DrawLineImp(pen, pt1, pt2);
		}

		public SizeF MeasureString (string text, Font font)
		{
			SizeF ret = new SizeF();
			MeasureStringImp(text, font, ref ret);
			return ret;
		}

		public void DrawImage(Image image, RectangleF bounds)
		{
			DrawImageImp(image, bounds);
		}

		partial void FillRectangleImp(Brush brush, Rectangle rect);
		partial void FillRectangleImp(Brush brush, RectangleF rect);
		partial void DrawStringImp(string s, Font font, Brush brush, PointF pt, StringFormat format);
		partial void DrawRectangleImp (Pen pen, RectangleF rect);
		partial void DrawLineImp(Pen pen, PointF pt1, PointF pt2);
		partial void MeasureStringImp(string text, Font font, ref SizeF ret);
		partial void MeasureCharacterRangeImp(string str, Font font, StringFormat format, CharacterRange range, ref RectangleF ret);
		partial void DrawImageImp(Image image, RectangleF bounds);
	};

	public partial class Pen
	{
		public Pen(Color color, float width)
		{
			Init(color, width);
		}

		partial void Init(Color color, float width);
	};

	public partial class Brush: IDisposable
	{
		public Brush(Color color)
		{
			Init(color);
		}

		partial void Init(Color color);
	}; 

	public partial class Font: IDisposable
	{
		public Font(string familyName, float emSize)
		{
			Init(familyName, emSize);
		}

		partial void Init(string familyName, float emSize);
	};

	public partial class Image: IDisposable
	{
#if SYS_DRAWING
		public Image(System.Drawing.Image img) { Init(img); }

		partial void Init(System.Drawing.Image img);
#endif
#if MONOMAC
		public Image(MonoMac.CoreGraphics.CGImage img) { Init(img); }
		public Image(MonoMac.AppKit.NSImage img)
		{
			RectangleF tmp = new RectangleF();
			Init(img.AsCGImage(ref tmp, null, null));
		}

		partial void Init(MonoMac.CoreGraphics.CGImage img);
#endif

		public int Width { get { return Size.Width; } }
		public int Height { get { return Size.Height; } }

		public Size Size
		{
			get
			{
				Size ret = new Size();
				SizeImp(ref ret);
				return ret;
			}
		}

		partial void SizeImp(ref Size ret);
	};

	public partial class StringFormat
	{
#if SYS_DRAWING
		// todo: get rid of this ctr
		public StringFormat(System.Drawing.StringFormat f) { Init(f); }

		partial void Init(System.Drawing.StringFormat f);
#endif
	};

	public static class Extensions
	{
		public static Rectangle ToRectangle(this RectangleF rect)
		{
			return new Rectangle((int)rect.Left, (int)rect.Top, (int)rect.Width, (int)rect.Height);
		}

		public static RectangleF ToRectangleF(this Rectangle rect)
		{
			return new RectangleF(rect.Left, rect.Top, rect.Width, rect.Height);
		}

		public static Point ToPoint(this PointF pt)
		{
			return new Point((int)pt.X, (int)pt.Y);
		}

		public static void DrawRectangle (this Graphics g, Pen pen, Rectangle rect)
		{
			g.DrawRectangle(pen, rect.ToRectangleF());
		}

		public static void DrawLine(this Graphics g, Pen pen, float x1, float y1, float x2, float y2)
		{
			g.DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2));
		}

		public static void DrawImage(this Graphics g, Image img, float x, float y, float width, float height)
		{
			g.DrawImage(img, new RectangleF(
				x, y, width, height
			));
		}

		public static void DrawString(this Graphics g, string s, Font font, Brush brush, float x, float y, StringFormat format = null)
		{
			g.DrawString(s, font, brush, new PointF(x, y), format);
		}
	};
}