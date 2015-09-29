using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace LogJoint.UI
{
	static class Native
	{
		[StructLayout(LayoutKind.Sequential)]
		public struct SCROLLINFO
		{
			public int cbSize;
			public SIF fMask;
			public int nMin;
			public int nMax;
			public int nPage;
			public int nPos;
			public int nTrackPos;
		}

		[StructLayout(LayoutKind.Sequential)]
		public struct RECT
		{
			public int left;
			public int top;
			public int right;
			public int bottom;
			public RECT(Rectangle r)
			{
				left = r.Left;
				top = r.Top;
				right = r.Right;
				bottom = r.Bottom;
			}
			public Rectangle ToRectangle()
			{
				return new Rectangle(left, top, right - left, bottom - top);
			}
		}
			

#if WINDOWS

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern int SetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si, bool redraw);

		[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
		public static extern bool GetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern int ScrollWindowEx(
			HandleRef hWnd,
			int nXAmount, int nYAmount,
			ref RECT rectScrollRegion,
			ref RECT rectClip,
			HandleRef hrgnUpdate,
			ref RECT prcUpdate,
			int flags);

		[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
		public static extern int RedrawWindow(
			HandleRef hWnd,
			IntPtr rectClip,
			IntPtr hrgnUpdate,
			UInt32 flags
		);

		[DllImport("user32.dll", CharSet = CharSet.Auto)]
		public static extern bool PostMessage(HandleRef hwnd,
			int msg, IntPtr wparam, IntPtr lparam);
			

#else

		public static int SetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si, bool redraw) { return 0; }
		public static bool GetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si) { return false; }

		public static int ScrollWindowEx(
			HandleRef hWnd,
			int nXAmount, int nYAmount,
			ref RECT rectScrollRegion,
			ref RECT rectClip,
			HandleRef hrgnUpdate,
			ref RECT prcUpdate,
			int flags)
		{ return 0; }

		public static int RedrawWindow(
			HandleRef hWnd,
			IntPtr rectClip,
			IntPtr hrgnUpdate,
			UInt32 flags
		)
		{ return 0; }

		public static bool PostMessage(HandleRef hwnd, int msg, IntPtr wparam, IntPtr lparam)
		{ return false; }
#endif

		public const int WM_USER = 0x0400;

		public static int LOWORD(int n)
		{
			return (n & 0xffff);
		}

		public static int LOWORD(IntPtr n)
		{
			return LOWORD((int)((long)n));
		}

		public enum SB : int
		{
			LINEUP = 0,
			LINELEFT = 0,
			LINEDOWN = 1,
			LINERIGHT = 1,
			PAGEUP = 2,
			PAGELEFT = 2,
			PAGEDOWN = 3,
			PAGERIGHT = 3,
			THUMBPOSITION = 4,
			THUMBTRACK = 5,
			TOP = 6,
			LEFT = 6,
			BOTTOM = 7,
			RIGHT = 7,
			ENDSCROLL = 8,

			HORZ = 0,
			VERT = 1,
			BOTH = 3,
		}

		public enum SIF : uint
		{
			RANGE = 0x0001,
			PAGE = 0x0002,
			POS = 0x0004,
			DISABLENOSCROLL = 0x0008,
			TRACKPOS = 0x0010,
			ALL = (RANGE | PAGE | POS | TRACKPOS),
		}
	};
}
