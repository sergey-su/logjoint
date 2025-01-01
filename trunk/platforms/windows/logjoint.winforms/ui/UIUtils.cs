using System;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using System.Text;
using Color = System.Drawing.Color;
using Rectangle = System.Drawing.Rectangle;
using Size = System.Drawing.Size;
using Point = System.Drawing.Point;
using System.Drawing;

namespace LogJoint.UI
{
    public static class UIUtils
    {
        static GraphicsPath focusedItemMark, focusedItemMarkBorder;
        public static RectangleF FocusedItemMarkBounds
        {
            get
            {
                var x = Dpi.Scale(3f);
                return new RectangleF(0, -x, x, 2 * x);
            }
        }
        public static void DrawFocusedItemMark(System.Drawing.Graphics g, float x, float y, bool drawWhiteBounds = false)
        {
            if (focusedItemMark == null)
            {
                var b = FocusedItemMarkBounds;
                focusedItemMark = new GraphicsPath();
                focusedItemMark.AddPolygon(new[]
                {
                    new PointF(0, b.Top),
                    new PointF(b.Width-1, 0),
                    new PointF(0, b.Bottom),
                });
                focusedItemMarkBorder = new GraphicsPath();
                focusedItemMarkBorder.AddPolygon(new[]
                {
                    new PointF(0, b.Top-1.5f),
                    new PointF(b.Width, 0),
                    new PointF(0, b.Bottom+1.5f),
                });
            }

            GraphicsState state = g.Save();
            g.TranslateTransform(x, y);
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.FillPath(System.Drawing.Brushes.Blue, focusedItemMark);
            if (drawWhiteBounds)
            {
                g.DrawPath(System.Drawing.Pens.White, focusedItemMarkBorder);
            }
            g.Restore(state);
        }

        public static void DrawDragEllipsis(System.Drawing.Graphics g, Rectangle r) // todo: get rid of this in favor of LJD version
        {
            int y = r.Top + 1;
            for (int i = r.Left; i < r.Right; i += 5)
            {
                g.FillRectangle(System.Drawing.Brushes.White, i + 1, y + 1, 2, 2);
                g.FillRectangle(System.Drawing.Brushes.DarkGray, i, y, 2, 2);
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

            bool IMessageFilter.PreFilterMessage(ref System.Windows.Forms.Message m)
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
                        using (var g = System.Drawing.Graphics.FromHwnd(IntPtr.Zero))
                            primaryScreenDpi = g.DpiY;
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

            public static float Scale(float value, float baseDpi = 96f)
            {
                return GetPrimaryScreenDpiScaleFactor(baseDpi) * value;
            }

            public static float ScaleUp(float value, float baseDpi = 96f)
            {
                return Math.Max(value, GetPrimaryScreenDpiScaleFactor(baseDpi) * value);
            }

            public static int ScaleUp(int value, float baseDpi = 96f)
            {
                return Math.Max(value, (int)(Math.Ceiling(GetPrimaryScreenDpiScaleFactor(baseDpi) * (float)value)));
            }

            static float? primaryScreenDpi;
        }

        static Dictionary<uint, Brush> paletteColorBrushes = new Dictionary<uint, Brush>();

        public static Brush GetPaletteColorBrush(LogJoint.Drawing.Color color)
        {
            Brush b;
            if (paletteColorBrushes.TryGetValue(color.ToUnsignedArgb(), out b))
                return b;
            paletteColorBrushes.Add(color.ToUnsignedArgb(), b = new SolidBrush(Drawing.PrimitivesExtensions.ToSystemDrawingObject(color)));
            return b;
        }

        public static LogJoint.Drawing.SizeF GetSize(this System.Drawing.Image image, float? width = null, float? height = null)
        {
            return LogJoint.Drawing.Extensions.GetImageSize(new LogJoint.Drawing.SizeF(image.Width, image.Height), width, height);
        }

        public static System.Drawing.Image DownscaleUIImage(System.Drawing.Image src, int targetWidthAndHeight)
        {
            return DownscaleUIImage(src, new Size(targetWidthAndHeight, targetWidthAndHeight));
        }

        public static System.Drawing.Image DownscaleUIImage(System.Drawing.Image src, Size targetSize)
        {
            var img = new System.Drawing.Bitmap(targetSize.Width, targetSize.Height);
            using (var g = System.Drawing.Graphics.FromImage(img))
            {
                float scaleX = (float)targetSize.Width / src.Width;
                float scaleY = (float)targetSize.Height / src.Height;
                float scale = Math.Min(scaleX, scaleY);
                SizeF targetSize2 = new SizeF(src.Width * scale, src.Height * scale);
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBilinear;
                g.DrawImage(src, new RectangleF(
                    new PointF((targetSize.Width - targetSize2.Width) / 2, (targetSize.Height - targetSize2.Height) / 2),
                    targetSize2
                ));
            }
            return img;
        }

        public static void SetLinkContents(LinkLabel link, string value) // todo: change mac version too
        {
            if (link.Tag as string == value)
                return;
            link.Tag = value;
            link.Visible = value != null;
            var parsed = Presenters.LinkLabelUtils.ParseLinkLabelString(value);
            link.Text = parsed.Text;
            link.Links.Clear();
            parsed.Links.ForEach(l => link.Links.Add(new LinkLabel.Link(l.Item1, l.Item2, l.Item3)));
        }
    }
}
