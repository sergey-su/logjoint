
namespace LogJoint.Drawing
{
	public struct RectangleF
	{
		public float X, Y, Width, Height;

		public float Left { get { return X; } }
		public float Top { get { return Y; } }
		public float Right { get { return X + Width; } }
		public float Bottom { get { return Y + Height; } }

		public PointF Location { get { return new PointF (X, Y); } }
		public SizeF Size { get { return new SizeF (Width, Height); } }

		public RectangleF (float x, float y, float w, float h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}

		public RectangleF (PointF pt, SizeF sz) : this (pt.X, pt.Y, sz.Width, sz.Height)
		{
		}

		public static RectangleF FromLTRB (float l, float t, float r, float b)
		{
			return new RectangleF (l, t, r - l, b - t);
		}

		public bool Contains (float x, float y)
		{
			return x >= X && x < Right && y >= Y && y < Bottom;
		}

		public bool Contains (PointF pt)
		{
			return Contains (pt.X, pt.Y);
		}

		public static implicit operator RectangleF (Rectangle r)
		{
			return new RectangleF (r.X, r.Y, r.Width, r.Height);
		}

		public void Inflate (float dx, float dy)
		{
			X -= dx;
			Y -= dy;
			Width += dx * 2;
			Height += dy * 2;
		}

		public static RectangleF Inflate (RectangleF r, float dx, float dy)
		{
			r.Inflate (dx, dy);
			return r;
		}
	};

	public struct Rectangle
	{
		public int X, Y, Width, Height;

		public int Left { get { return X; } }
		public int Top { get { return Y; } }
		public int Right { get { return X + Width; } }
		public int Bottom { get { return Y + Height; } }
		public int MidX() => (Left + Right) / 2;
		public int MidY() => (Top + Bottom) / 2;

		public Point Location { get { return new Point (X, Y); } }
		public Size Size { get { return new Size (Width, Height); } }

		public Rectangle (int x, int y, int w, int h)
		{
			X = x;
			Y = y;
			Width = w;
			Height = h;
		}

		public Rectangle (Point pt, Size sz) : this (pt.X, pt.Y, sz.Width, sz.Height)
		{
		}

		public static Rectangle FromLTRB (int l, int t, int r, int b)
		{
			return new Rectangle (l, t, r - l, b - t);
		}

		public bool Contains (int x, int y)
		{
			return x >= X && x < Right && y >= Y && y < Bottom;
		}

		public bool Contains (Point pt)
		{
			return Contains (pt.X, pt.Y);
		}

		public void Inflate (int dx, int dy)
		{
			X -= dx;
			Y -= dy;
			Width += dx * 2;
			Height += dy * 2;
		}

		static public Rectangle Inflate(Rectangle r, int dx, int dy)
		{
			r.Inflate(dx, dy);
			return r;
		}

		public void Offset (int dx, int dy)
		{
			X += dx;
			Y += dy;
		}
	};

	public struct Point
	{
		public int X, Y;

		public Point (int x, int y)
		{
			X = x;
			Y = y;
		}

		public static implicit operator PointF (Point p)
		{
			return new PointF (p.X, p.Y);
		}
	};

	public struct PointF
	{
		public float X, Y;

		public PointF (float x, float y)
		{
			X = x;
			Y = y;
		}
	};

	public struct Size
	{
		public int Width, Height;

		public Size (int w, int h)
		{
			Width = w;
			Height = h;
		}

		public Size (Point pt)
		{
			Width = pt.X;
			Height = pt.Y;
		}

		public static implicit operator SizeF (Size p)
		{
			return new SizeF (p.Width, p.Height);
		}
	};

	public struct SizeF
	{
		public float Width, Height;

		public SizeF (float w, float h)
		{
			Width = w;
			Height = h;
		}

		public Size ToSize ()
		{
			return new Size ((int)Width, (int)Height);
		}
	};

	public struct Color
	{
		int v;

		public Color(uint argb) { unchecked { this.v = (int)argb; } }
		public Color(int argb) { this.v = argb; }

		public byte A { get { return (byte)((v >> 24) & 0xff); } }
		public byte R { get { return (byte)((v >> 16) & 0xff); } }
		public byte G { get { return (byte)((v >> 8) & 0xff); } }
		public byte B { get { return (byte)((v) & 0xff); } }

		public static Color FromArgb (int argb)
		{
			return new Color (argb);
		}

		public static Color FromArgb (int alpha, Color color)
		{
			return new Color (
				unchecked(((alpha & 0xff) << 24) | (color.v & 0x00ffffff))
			);
		}

		public static Color FromArgb (int a, int r, int g, int b)
		{
			return new Color (unchecked(
				((a & 0xff) << 24) |
				((r & 0xff) << 16) |
				((g & 0xff) << 8) |
				((b & 0xff) << 0)
			));
		}

		public static Color FromArgb (int r, int g, int b)
		{
			return FromArgb (0xff, r, g, b);
		}

		static Color FromArgb (uint argb)
		{
			return FromArgb (unchecked((int)argb));
		}

		public int ToArgb ()
		{
			return v;
		}

		public uint ToUnsignedArgb()
		{
			return (uint)v;
		}

		public override string ToString()
		{
			return string.Format("[Color: A={0}, R={1}, G={2}, B={3}]", A, R, G, B);
		}

		public override bool Equals(object o)
		{
			if (o is Color c)
				return c.v == this.v;
			return false;
		}

		public override int GetHashCode()
		{
			return v;
		}

		public static bool operator ==(Color c1, Color c2)
		{
			return c1.v == c2.v;
		}
		public static bool operator !=(Color c1, Color c2)
		{
			return c1.v != c2.v;
		}

		public static Color Red = FromArgb (0xffff0000);
		public static Color Green = FromArgb (0xff008000);
		public static Color Blue = FromArgb (0xff0000ff);
		public static Color White = FromArgb (0xffffffff);
		public static Color Black = FromArgb (0xff000000);
		public static Color DarkGray = FromArgb (0xFFA9A9A9);
		public static Color LightSalmon = FromArgb (0xFFFFA07A);
		public static Color Gray = FromArgb (0xFF808080);
		public static Color LightGray = FromArgb (0xFFD3D3D3);
		public static Color Cyan = FromArgb (0xFF00FFFF);
		public static Color LightBlue = FromArgb (0xFFADD8E6);
		public static Color DimGray = FromArgb (0xFF696969);
		public static Color LightGreen = FromArgb (0xFF90EE90);
		public static Color SteelBlue = FromArgb (0xFF4682B4);
		public static Color Salmon = FromArgb (0xFFFA8072);
		public static Color DarkGreen = FromArgb (0xFF006400);
		public static Color Transparent = FromArgb(0x00000000);
	};
}
