using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;

namespace LogJoint.UI
{
	public static class UIUtils
	{
		static GraphicsPath focusedItemMark;
		public static Rectangle FocusedItemMarkBounds
		{
			get { return new Rectangle(0, -3, 3, 6); }
		}
		public static void DrawFocusedItemMark(Graphics g, int x, int y)
		{
			if (focusedItemMark == null)
			{
				focusedItemMark = new GraphicsPath();
				focusedItemMark.AddPolygon(new Point[]{
					new Point(0, -3),
					new Point(2, 0),
					new Point(0, 3),
				});
			}

			GraphicsState state = g.Save();
			g.TranslateTransform(x, y);
			g.SmoothingMode = SmoothingMode.AntiAlias;
			g.FillPath(Brushes.Blue, focusedItemMark);
			g.Restore(state);
		}

		public static void DrawDragEllipsis(Graphics g, Rectangle r)
		{
			int y = r.Top + 1;
			for (int i = r.Left; i < r.Right; i += 5)
			{
				g.FillRectangle(Brushes.White, i + 1, y + 1, 2, 2);
				g.FillRectangle(Brushes.DarkGray, i, y, 2, 2);
			}
		}

		public static void AddRoundRect(GraphicsPath gp, Rectangle rect, int radius)
		{
			int diameter = radius * 2;
			Size size = new Size(diameter, diameter);
			Rectangle arc = new Rectangle(rect.Location, size);

			gp.AddArc(arc, 180, 90);

			arc.X = rect.Right - diameter;
			gp.AddArc(arc, 270, 90);

			arc.Y = rect.Bottom - diameter;
			gp.AddArc(arc, 0, 90);

			arc.X = rect.Left;
			gp.AddArc(arc, 90, 90);

			gp.CloseFigure();
		}

		public class DrawShadowRect : IDisposable
		{
			readonly Color color;
			SolidBrush inner, border1, border2, edge1, edge2, edge3;

			SolidBrush CreateHalftone(int alpha)
			{
				return new SolidBrush(Color.FromArgb(alpha, color));
			}

			/// <summary>
			/// The minimum size of a rectangle that can be rendered by Draw()
			/// </summary>
			public static readonly Size MinimumRectSize = new Size(4, 4);

			public DrawShadowRect(Color cl)
			{
				color = cl;
				inner = CreateHalftone(255);
				border1 = CreateHalftone(191);
				border2 = CreateHalftone(63);
				edge1 = CreateHalftone(143);
				edge2 = CreateHalftone(47);
				edge3 = CreateHalftone(15);
			}
			public void Dispose()
			{
				inner.Dispose();
				border1.Dispose();
				border2.Dispose();
				edge1.Dispose();
				edge2.Dispose();
				edge3.Dispose();
			}

			public static bool IsValidRectToDrawShadow(Rectangle r)
			{
				return r.Width >= MinimumRectSize.Width && r.Height >= MinimumRectSize.Height;
			}

			public void Draw(Graphics g, Rectangle r, Border3DSide sides)
			{
				if (!IsValidRectToDrawShadow(r))
				{
					throw new ArgumentException("Rect is too small", "r");
				}

				r.Inflate(-2, -2);

				if ((sides & Border3DSide.Middle) != 0)
				{
					g.FillRectangle(inner, r);
				}

				if ((sides & Border3DSide.Top) != 0)
				{
					g.FillRectangle(border1, r.Left, r.Top - 1, r.Width, 1);
					g.FillRectangle(border2, r.Left, r.Top - 2, r.Width, 1);
				}
				if ((sides & Border3DSide.Right) != 0)
				{
					g.FillRectangle(border1, r.Right, r.Top, 1, r.Height);
					g.FillRectangle(border2, r.Right + 1, r.Top, 1, r.Height);
				}
				if ((sides & Border3DSide.Bottom) != 0)
				{
					g.FillRectangle(border1, r.Left, r.Bottom, r.Width, 1);
					g.FillRectangle(border2, r.Left, r.Bottom + 1, r.Width, 1);
				}
				if ((sides & Border3DSide.Left) != 0)
				{
					g.FillRectangle(border1, r.Left - 1, r.Top, 1, r.Height);
					g.FillRectangle(border2, r.Left - 2, r.Top, 1, r.Height);
				}

				if ((sides & Border3DSide.Left) != 0 && (sides & Border3DSide.Top) != 0)
				{
					g.FillRectangle(edge1, r.Left - 1, r.Top - 1, 1, 1);
					g.FillRectangle(edge2, r.Left - 2, r.Top - 1, 1, 1);
					g.FillRectangle(edge2, r.Left - 1, r.Top - 2, 1, 1);
					g.FillRectangle(edge3, r.Left - 2, r.Top - 2, 1, 1);
				}

				if ((sides & Border3DSide.Top) != 0 && (sides & Border3DSide.Right) != 0)
				{
					g.FillRectangle(edge1, r.Right, r.Top - 1, 1, 1);
					g.FillRectangle(edge2, r.Right, r.Top - 2, 1, 1);
					g.FillRectangle(edge2, r.Right + 1, r.Top - 1, 1, 1);
					g.FillRectangle(edge3, r.Right + 1, r.Top - 2, 1, 1);
				}

				if ((sides & Border3DSide.Right) != 0 && (sides & Border3DSide.Bottom) != 0)
				{
					g.FillRectangle(edge1, r.Right, r.Bottom, 1, 1);
					g.FillRectangle(edge2, r.Right + 1, r.Bottom, 1, 1);
					g.FillRectangle(edge2, r.Right, r.Bottom + 1, 1, 1);
					g.FillRectangle(edge3, r.Right + 1, r.Bottom + 1, 1, 1);
				}

				if ((sides & Border3DSide.Bottom) != 0 && (sides & Border3DSide.Left) != 0)
				{
					g.FillRectangle(edge1, r.Left - 1, r.Bottom, 1, 1);
					g.FillRectangle(edge2, r.Left - 1, r.Bottom + 1, 1, 1);
					g.FillRectangle(edge2, r.Left - 2, r.Bottom, 1, 1);
					g.FillRectangle(edge3, r.Left - 2, r.Bottom + 1, 1, 1);
				}
			}
		};

		public static void DrawRectangle(this Graphics g, Pen pen, RectangleF r)
		{
			g.DrawRectangle(pen, r.X, r.Y, r.Width, r.Height);
		}

		public class ToolTipInfo
		{
			public string Text;
			public string Title;
			public int? Duration;
			public Point? Location;
		};

		public class ToolTipHelper : IDisposable
		{
			readonly Control control;
			readonly ToolTip toolTip;
			readonly Timer timer;
			readonly Func<Point, ToolTipInfo> tootTipCallback;

			Point lastMousePosition;
			enum State
			{
				MouseLeft,
				PupupNotShown,
				PupupShown
			};
			State state;

			public ToolTipHelper(Control ctrl, Func<Point, ToolTipInfo> tootTipCallback, int toolTipDelay = 500)
			{
				this.control = ctrl;
				this.tootTipCallback = tootTipCallback;
				this.timer = new Timer() { Enabled = false, Interval = toolTipDelay };
				this.toolTip = new ToolTip() { UseAnimation = false, UseFading = false };

				control.MouseEnter += (s, e) =>
				{
					state = State.PupupNotShown;
				};
				control.MouseLeave += (s, e) =>
				{
					if (state == State.PupupShown)
						toolTip.Hide(control);
					state = State.MouseLeft;
				};
				control.MouseMove += (s, e) =>
				{
					if (lastMousePosition != e.Location)
					{
						lastMousePosition = e.Location;
						if (state == State.PupupShown)
						{
							state = State.PupupNotShown;
							toolTip.Hide(control);
						}
						timer.Stop();
						timer.Start();
					}
				};
				timer.Tick += (s, e) =>
				{
					timer.Stop();
					if (state == State.PupupNotShown)
					{
						var toolTipInfo = tootTipCallback(lastMousePosition);
						if (toolTipInfo != null)
						{
							state = State.PupupShown;
							toolTip.ToolTipTitle = toolTipInfo.Title ?? "";
							var location = toolTipInfo.Location.GetValueOrDefault(lastMousePosition);
							toolTip.Show(
								toolTipInfo.Text ?? "",
								control,
								location.X,
								location.Y + Cursor.Current.Size.Height,
								toolTipInfo.Duration.GetValueOrDefault(1000));
						}
					}
				};
			}

			public void Dispose()
			{
				timer.Dispose();
				toolTip.Dispose();
			}
		};

		public class FocuslessMouseWheelMessagingFilter : IMessageFilter, IDisposable
		{
			Control control;
			
			public FocuslessMouseWheelMessagingFilter(Control control)
			{
				this.control = control;
				Application.AddMessageFilter(this);
			}

			public void Dispose()
			{
				Application.RemoveMessageFilter(this);
			}

			static bool IsWindowPointVisibleToUser(IntPtr wnd, Point screenPoint)
			{
				uint GW_HWNDPREV = 3;
				for (IntPtr prev = GetWindow(wnd, GW_HWNDPREV); prev != IntPtr.Zero; prev = GetWindow(prev, GW_HWNDPREV))
				{
					RECT r;
					if (IsWindowVisible(prev)
					&& !IsIconic(prev)
					&& GetWindowRect(prev, out r)
					&& new Rectangle(r.L, r.T, r.R - r.L, r.B - r.T).Contains(screenPoint))
					{
						return false;
					}
				}
				return true;
			}

			bool IMessageFilter.PreFilterMessage(ref Message m)
			{
				int WM_MOUSEWHEEL = 0x20A;
				if (m.Msg == WM_MOUSEWHEEL && control.CanFocus && !control.Focused)
				{
					var p = Cursor.Position;
					if (control.ClientRectangle.Contains(control.PointToClient(p)))
					{
						var form = control.FindForm();
						if (form != null && IsWindowPointVisibleToUser(form.Handle, p))
						{
							unchecked
							{
								SendMessage(control.Handle, m.Msg, m.WParam, m.LParam);
							}
							return true;
						}
					}
				}
				return false;
			}

			[DllImport("User32.dll")]
			static extern Int32 SendMessage(IntPtr hWnd, int Msg, IntPtr wParam, IntPtr lParam);

			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
			static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

			[DllImport("user32.dll", SetLastError = true)]
			static extern IntPtr GetWindow(IntPtr hWnd, uint uCmd);

			[DllImport("user32.dll", SetLastError = true)]
			static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int L, T, R, B;
			}

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool IsWindowVisible(IntPtr hWnd);

			[DllImport("user32.dll")]
			[return: MarshalAs(UnmanagedType.Bool)]
			static extern bool IsIconic(IntPtr hWnd);

			static string GetWindowText(IntPtr hWnd)
			{
				StringBuilder lpString = new StringBuilder(1000);
				GetWindowText(hWnd, lpString, lpString.Capacity);
				return lpString.ToString();
			}
		};

		public static class Dpi
		{
			public static float PrimaryScreenDpi
			{
				get
				{
					if (primaryScreenDpi == null)
					{
						primaryScreenDpi = 96;
					}
					return primaryScreenDpi.Value;
				}
			}

			public static float GetPrimaryScreenDpiScaleFactor(float baseDpi = 96f)
			{
				return PrimaryScreenDpi / baseDpi;
			}

			public static int Scale(int value, float baseDpi = 96f)
			{
				return (int)(GetPrimaryScreenDpiScaleFactor(baseDpi) * (float)value);
			}

			public static int ScaleUp(int value, float baseDpi = 96f)
			{
				return Math.Max(value, (int)(GetPrimaryScreenDpiScaleFactor(baseDpi) * (float)value));
			}

			static float? primaryScreenDpi;
		}
	}
}
