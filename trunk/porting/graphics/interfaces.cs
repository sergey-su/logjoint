using System.Drawing;
using System;
using System.Linq;

namespace LogJoint.Drawing
{
	public partial class Graphics: IDisposable
	{
#if WIN
		public Graphics(System.Drawing.Graphics g, bool ownsGraphics = false)
		{
			Init(g, ownsGraphics);
		}
		partial void Init(System.Drawing.Graphics g, bool ownsGraphics);

		public class PerformanceCounters
		{
			internal PerformanceCounters(Profiling.Counters countersContainer)
			{
			}
		};
		partial void ConfigureProfilingImpl(PerformanceCounters counters, Profiling.Counters.Writer writer);
#endif
#if MONOMAC
		public Graphics()
		{
			var cc = AppKit.NSGraphicsContext.CurrentContext;
			InitFromContext(cc?.GraphicsPort);
		}
		public Graphics(CoreGraphics.CGContext context)
		{
			InitFromContext(context);
		}
		public void SetCurrentContext()
		{
			InitFromContext(AppKit.NSGraphicsContext.CurrentContext?.GraphicsPort);
		}
		partial void InitFromContext(CoreGraphics.CGContext context);

		public partial class PerformanceCounters
		{
			internal Profiling.Counters.CounterDescriptor drawStringPoint;
			internal Profiling.Counters.CounterDescriptor drawStringPoint_CreateAS;
			internal Profiling.Counters.CounterDescriptor drawStringPoint_Draw1;
			internal Profiling.Counters.CounterDescriptor drawStringPoint_Draw2;
			internal Profiling.Counters.CounterDescriptor fillRectangle;
			internal Profiling.Counters.CounterDescriptor fillRoundRectangle;
			internal Profiling.Counters.CounterDescriptor measureCharRange;

			public PerformanceCounters (Profiling.Counters countersContainer)
			{
				Init (countersContainer);
			}
			partial void Init (Profiling.Counters countersContainer);

			internal static PerformanceCounters Null = new PerformanceCounters (null);
		};

		partial void ConfigureProfilingImpl (PerformanceCounters counters, Profiling.Counters.Writer writer);
#endif
		public static PerformanceCounters CreateCounters (Profiling.Counters countersContainer)
		{
			return new PerformanceCounters (countersContainer);
		}

		public void ConfigureProfiling (PerformanceCounters counters, Profiling.Counters.Writer writer)
		{
			ConfigureProfilingImpl (counters, writer);
		}

		public void FillRectangle(Brush brush, RectangleF rect)
		{
			FillRectangleImp(brush, rect);
		}

		public void FillRoundRectangle(Brush brush, RectangleF rect, float radius)
		{
			FillRoundRectangleImp(brush, rect, radius);
		}

		public void DrawString(string s, Font font, Brush brush, PointF pt, StringFormat format = null)
		{
			DrawStringImp(s, font, brush, pt, format);
		}

		public void DrawString(string s, Font font, Brush brush, RectangleF frame, StringFormat format = null)
		{
			DrawStringImp(s, font, brush, frame, format);
		}

		public RectangleF MeasureCharacterRange(string str, Font font, StringFormat format, CharacterRange range)
		{
			RectangleF r = new RectangleF();
			if (range.Length != 0)
				MeasureCharacterRangeImp(str, font, format, range, ref r);
			return r;
		}

		public void DrawRectangle (Pen pen, RectangleF rect)
		{
			DrawRectangleImp(pen, rect);
		}

		public void DrawEllipse(Pen pen, RectangleF rect)
		{
			DrawEllipseImp(pen, rect);
		}

		public void DrawRoundRectangle(Pen pen, RectangleF rect, float radius)
		{
			DrawRoundRectangleImp(pen, rect, radius);
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

		public SizeF MeasureString(string text, Font font, StringFormat format, SizeF frameSz)
		{
			SizeF ret = new SizeF();
			MeasureStringImp(text, font, format, frameSz, ref ret);
			return ret;
		}

		public void DrawImage(Image image, RectangleF bounds)
		{
			DrawImageImp(image, bounds);
		}

		public void DrawLines(Pen pen, PointF[] points)
		{
			DrawLinesImp(pen, points);
		}

		public void FillPolygon(Brush brush, PointF[] points)
		{
			FillPolygonImp(brush, points);
		}

		public void PushState()
		{
			PushStateImp();
		}

		public void PopState()
		{
			PopStateImp();
		}

		public void EnableAntialiasing(bool value)
		{
			EnableAntialiasingImp(value);
		}

		public void EnableTextAntialiasing(bool value)
		{
			EnableTextAntialiasingImp(value);
		}

		public void TranslateTransform(float dx, float dy)
		{
			TranslateTransformImp(dx, dy);
		}

		public void ScaleTransform(float sx, float sy)
		{
			ScaleTransformImp(sx, sy);
		}

		public void RotateTransform(float degrees)
		{
			RotateTransformImp(degrees);
		}


		public void IntsersectClip(RectangleF r)
		{
			IntersectClipImp(r);
		}

		partial void FillRectangleImp(Brush brush, RectangleF rect);
		partial void FillRoundRectangleImp(Brush brush, RectangleF rect, float radius);
		partial void DrawStringImp(string s, Font font, Brush brush, PointF pt, StringFormat format);
		partial void DrawStringImp(string s, Font font, Brush brush, RectangleF frame, StringFormat format);
		partial void DrawRectangleImp(Pen pen, RectangleF rect);
		partial void DrawEllipseImp(Pen pen, RectangleF rect);
		partial void DrawRoundRectangleImp(Pen pen, RectangleF rect, float radius);
		partial void DrawLineImp(Pen pen, PointF pt1, PointF pt2);
		partial void MeasureStringImp(string text, Font font, ref SizeF ret);
		partial void MeasureStringImp(string text, Font font, StringFormat format, SizeF frameSz, ref SizeF ret);
		partial void MeasureCharacterRangeImp(string str, Font font, StringFormat format, CharacterRange range, ref RectangleF ret);
		partial void DrawImageImp(Image image, RectangleF bounds);
		partial void DrawLinesImp(Pen pen, PointF[] points);
		partial void FillPolygonImp(Brush brush, PointF[] points);
		partial void PushStateImp();
		partial void PopStateImp();
		partial void EnableAntialiasingImp(bool value);
		partial void EnableTextAntialiasingImp(bool value);
		partial void IntersectClipImp(RectangleF r);
		partial void TranslateTransformImp(float x, float y);
		partial void ScaleTransformImp(float x, float y);
		partial void RotateTransformImp(float degrees);
	};

	public partial class Pen
	{
		public Pen(Color color, float width, float[] dashPattern = null)
		{
			Init(color, width, dashPattern);
		}

		partial void Init(Color color, float width, float[] dashPattern);
	};

	public partial class Brush: IDisposable
	{
		public Brush(Color color)
		{
			Init(color);
		}

		internal Brush (Func<Color> color)
		{
			Init(color);
		}

		partial void Init(Color color);
		partial void Init(Func<Color> color);
	}; 

	public partial class Font: IDisposable
	{
		public Font(string familyName, float emSize, FontStyle style = FontStyle.Regular)
		{
			Init(familyName, emSize, style);
		}

		partial void Init(string familyName, float emSize, FontStyle style);
	};

	public partial class Image: IDisposable
	{
#if WIN
		public Image(System.Drawing.Image img) { Init(img); }

		partial void Init(System.Drawing.Image img);
#endif
#if MONOMAC
		public Image(CoreGraphics.CGImage img) { Init(img); }
		public Image(AppKit.NSImage img)
		{
			var tmp = new CoreGraphics.CGRect();
			Init(img.AsCGImage(ref tmp, null, null));
		}

		partial void Init(CoreGraphics.CGImage img);
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

	public enum LineBreakMode
	{
		WrapWords,
		WrapChars,
		SingleLineEndEllipsis
	};

	public partial class StringFormat
	{
		public StringFormat(StringAlignment horizontalAlignment, StringAlignment verticalAlignment, 
			LineBreakMode lineBreakMode = LineBreakMode.WrapWords)
		{
			Init(horizontalAlignment, verticalAlignment, lineBreakMode);
		}

		partial void Init(StringAlignment horizontalAlignment, StringAlignment verticalAlignment, LineBreakMode lineBreakMode);
#if WIN
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

		public static PointF ToPointF(this Point pt)
		{
			return new PointF(pt.X, pt.Y);
		}

		public static void DrawRectangle (this Graphics g, Pen pen, Rectangle rect)
		{
			g.DrawRectangle(pen, rect.ToRectangleF());
		}

		public static void DrawLine(this Graphics g, Pen pen, float x1, float y1, float x2, float y2)
		{
			g.DrawLine(pen, new PointF(x1, y1), new PointF(x2, y2));
		}

		public static void DrawLine(this Graphics g, Pen pen, int x1, int y1, int x2, int y2)
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

		public static void DrawLines(this Graphics g, Pen pen, Point[] points)
		{
			g.DrawLines(pen, points.Select(p => p.ToPointF()).ToArray());
		}

		public static void FillPolygon(this Graphics g, Brush brush, Point[] points)
		{
			g.FillPolygon(brush, points.Select(p => p.ToPointF()).ToArray());
		}

		public static void FillRectangle(this Graphics g, Brush brush, Rectangle rect)
		{
			g.FillRectangle(brush, rect.ToRectangleF());
		}

		public static void FillRectangle(this Graphics g, Brush brush, int x, int y, int w, int h)
		{
			g.FillRectangle(brush, new RectangleF(x, y, w, h));
		}

		public static void DrawRectangle(this Graphics g, Pen pen, int x, int y, int w, int h)
		{
			g.DrawRectangle(pen, new RectangleF(x, y, w, h));
		}

		public static SizeF Scale(this SizeF sz, float sx, float sy)
		{
			return new SizeF(sz.Width * sx, sz.Height * sy);
		}

		public static SizeF Scale(this SizeF sz, float s)
		{
			return new SizeF(sz.Width * s, sz.Height * s);
		}

		public static SizeF GetImageSize(SizeF physicalSize, float? width = null, float? height = null)
		{
			if (width == null && height == null)
				return physicalSize;
			if (width != null && height != null)
				return new SizeF(width.Value, height.Value);
			if (width != null)
				return new SizeF(width.Value, physicalSize.Height * width.Value / physicalSize.Width);
			if (height != null)
				return new SizeF(physicalSize.Width * height.Value / physicalSize.Height, height.Value);
			return physicalSize;
		}

		public static SizeF GetSize(this Image img, float? width = null, float? height = null)
		{
			return GetImageSize(img.Size, width, height);
		}

		#if MONOMAC
		public static Color ToColor(this AppKit.NSColor cl)
		{
			cl = cl.UsingColorSpace(AppKit.NSColorSpace.GenericRGBColorSpace);
			return Color.FromArgb(
				(int) (cl.AlphaComponent * 255),
				(int) (cl.RedComponent * 255),
				(int) (cl.GreenComponent * 255),
				(int) (cl.BlueComponent * 255)
			);
		}
		public static AppKit.NSColor ToNSColor(this Color cl)
		{
			return AppKit.NSColor.FromCalibratedRgba(
				(float)cl.R / 255f,
				(float)cl.G / 255f,
				(float)cl.B / 255f,
				(float)cl.A / 255f
			);
		}
		public static CoreGraphics.CGColor ToCGColor (this Color cl)
		{
			return new CoreGraphics.CGColor (
				(float)cl.R / 255f,
				(float)cl.G / 255f,
				(float)cl.B / 255f,
				(float)cl.A / 255f
			);
		}
		public static RectangleF ToRectangleF(this CoreGraphics.CGRect r)
		{
			return new RectangleF ((float)r.X, (float)r.Y, (float)r.Width, (float)r.Height); 
		}
		public static Rectangle ToRectangle (this CoreGraphics.CGRect r)
		{
			return new Rectangle ((int)r.X, (int)r.Y, (int)r.Width, (int)r.Height);
		}
		public static SizeF ToSizeF (this CoreGraphics.CGSize s)
		{
			return new SizeF ((float)s.Width, (float)s.Height);
		}
		public static Size ToSize (this CoreGraphics.CGSize s)
		{
			return new Size ((int)s.Width, (int)s.Height);
		}
		public static PointF ToPointF (this CoreGraphics.CGPoint p)
		{
			return new PointF ((float)p.X, (float)p.Y);
		}
		public static Point ToPoint (this CoreGraphics.CGPoint p)
		{
			return new Point ((int)p.X, (int)p.Y);
		}
		public static CoreGraphics.CGRect ToCGRect (this RectangleF r)
		{
			return new CoreGraphics.CGRect (r.X, r.Y, r.Width, r.Height);
		}
		public static CoreGraphics.CGPoint ToCGPoint (this PointF p)
		{
			return new CoreGraphics.CGPoint (p.X, p.Y);
		}
		public static CoreGraphics.CGSize ToCGSize (this SizeF s)
		{
			return new CoreGraphics.CGSize (s.Width, s.Height);
		}
		#endif
	};

	public static class SystemColors
	{
		public static Color Text { get { return SystemColorsImpl.instance.text(); } }
		public static Color TextBackground { get { return SystemColorsImpl.instance.textBackground(); } }
		public static Color LightGray { get { return SystemColorsImpl.instance.lightGray (); } }
	};

	internal partial class SystemColorsImpl
	{
		public Func<Color> text;
		public Func<Color> textBackground;
		public Func<Color> lightGray;

		public SystemColorsImpl ()
		{
			Init ();
		}
		partial void Init ();
		internal static SystemColorsImpl instance = new SystemColorsImpl ();
	};

	public static class Brushes
	{
		public static readonly Brush White = new Brush(Color.White);
		public static readonly Brush Red = new Brush(Color.Red);
		public static readonly Brush Green = new Brush(Color.Green);
		public static readonly Brush Blue = new Brush(Color.Blue);
		public static readonly Brush DarkGray = new Brush(Color.DarkGray);
		public static readonly Brush Black = new Brush(Color.Black);
		public static readonly Brush Transparent = new Brush(Color.Transparent);
		public static readonly Brush Text = new Brush(SystemColorsImpl.instance.text);
		public static readonly Brush TextBackground = new Brush(SystemColorsImpl.instance.textBackground);
		public static readonly Brush SystemLightGray = new Brush (SystemColorsImpl.instance.lightGray);
	};

	public static class Pens
	{
		public static readonly Pen Red = new Pen(Color.Red, 1);
		public static readonly Pen Green = new Pen(Color.Green, 1);
		public static readonly Pen Blue = new Pen(Color.Blue, 1);
		public static readonly Pen White = new Pen(Color.White, 1);
		public static readonly Pen Black = new Pen(Color.Black, 1);
		public static readonly Pen DarkGray = new Pen(Color.DarkGray, 1);
	};
}

namespace System.Drawing
{
	public static class LogJointExtensions
	{
		public static float MidX(this RectangleF r)
		{
			return (r.Left + r.Right) / 2f;
		}

		public static float MidY(this RectangleF r)
		{
			return (r.Top + r.Bottom) / 2f;
		}

		public static int MidX(this Rectangle r)
		{
			return (r.Left + r.Right) / 2;
		}

		public static int MidY(this Rectangle r)
		{
			return (r.Top + r.Bottom) / 2;
		}

	}
}