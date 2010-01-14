using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing.Drawing2D;
using System.Drawing;
using System.Diagnostics;

namespace LogJoint
{
	public sealed class FrameBegin : MessageBase
	{
		public override void DrawOutline(DrawContext ctx, Metrics m)
		{
			Pen murkupPen = ctx.OutlineMarkupPen;
			ctx.Canvas.DrawRectangle(murkupPen, m.OulineBox);
			Point p = m.OulineBoxCenter;
			ctx.Canvas.DrawLine(murkupPen, p.X - FixedMetrics.OutlineCrossSize / 2, p.Y, p.X + FixedMetrics.OutlineCrossSize / 2, p.Y);
			bool collapsed = Collapsed;
			if (collapsed)
				ctx.Canvas.DrawLine(murkupPen, p.X, p.Y - FixedMetrics.OutlineCrossSize / 2, p.X, p.Y + FixedMetrics.OutlineCrossSize / 2);
			if (IsBookmarked)
			{
				Image icon = ctx.SmallBookmarkIcon;
				ctx.Canvas.DrawImage(icon,
					FixedMetrics.CollapseBoxesAreaSize - icon.Width - 1,
					m.MessageRect.Y + (ctx.MessageHeight - icon.Height) / 2,
					icon.Width,
					icon.Height
				);
			}
		}

		public override void Draw(DrawContext ctx, Metrics m)
		{
			FillBackground(ctx, m);
			DrawTime(ctx, m);

			Rectangle r = m.OffsetTextRect;

			bool collapsed = Collapsed;

			Brush txtBrush = Selected ? GetSelectedTextBrush(ctx) : ctx.InfoMessagesBrush;
			Brush commentsBrush = Selected ? GetSelectedTextBrush(ctx) : ctx.CommentsBrush;

			string mark = GetCollapseMark(collapsed);
			ctx.Canvas.DrawString(
				mark,
				ctx.Font,
				txtBrush,
				r.X, r.Y);

			r.X += (int)(ctx.CharSize.Width * (mark.Length + 1));

			if (IsMultiLine)
			{
				ctx.Canvas.DrawString(name, ctx.Font, commentsBrush, r,
					ctx.SingleLineFormat);
			}
			else
			{
				ctx.Canvas.DrawString(name, ctx.Font, commentsBrush, r.X, r.Y);
			}

			DrawSelection(ctx, m);
		}
		public override void DrawHighligt(DrawContext ctx, HighlightRange lh, Metrics m)
		{
			DrawHighlight(ctx, m,
				ctx.CharSize.Width * (GetCollapseMark(Collapsed).Length + 1), lh);
		}
		public override string Text
		{
			get
			{
				return name;
			}
		}
		static string GetCollapseMark(bool collapsed)
		{
			return collapsed ? "{...}" : "{";
		}

		public FrameBegin(long position, IThread t, DateTime time, string name)
			:
			base(position, t, time)
		{
			this.name = name;
			this.flags = MessageFlag.StartFrame;
			InitializeMultilineFlag();
		}
		public void SetEnd(FrameEnd e)
		{
			end = e;
		}
		public string Name { get { return name; } }
		public bool Collapsed
		{
			get
			{
				return (Flags & MessageFlag.Collapsed) != 0;
			}
			set
			{
				SetCollapsedFlag(ref flags, value);
				if (end != null)
					end.SetCollapsed(value);
			}
		}
		public FrameEnd End { get { return end; } }

		internal static void SetCollapsedFlag(ref MessageFlag f, bool value)
		{
			if (value)
				f |= MessageFlag.Collapsed;
			else
				f &= ~MessageFlag.Collapsed;
		}
		string name;
		FrameEnd end;
	};

	public sealed class FrameEnd : MessageBase
	{
		public override void DrawOutline(DrawContext ctx, Metrics m)
		{
			if (this.IsBookmarked)
			{
				Image icon = ctx.BookmarkIcon;
				ctx.Canvas.DrawImage(icon,
					(FixedMetrics.CollapseBoxesAreaSize - icon.Width) / 2,
					m.MessageRect.Y + (ctx.MessageHeight - icon.Height) / 2,
					icon.Width,
					icon.Height
				);
			}
		}

		public override void Draw(DrawContext ctx, Metrics m)
		{
			FillBackground(ctx, m);
			DrawTime(ctx, m);

			RectangleF r = m.OffsetTextRect;
		
			ctx.Canvas.DrawString("}", ctx.Font, Selected ? GetSelectedTextBrush(ctx) : ctx.InfoMessagesBrush, r.X, r.Y);
			if (start != null)
			{
				r.X += ctx.CharSize.Width * 2;
				Brush commentsBrush = Selected ? GetSelectedTextBrush(ctx) : ctx.CommentsBrush;
				ctx.Canvas.DrawString("//", ctx.Font, commentsBrush, r.X, r.Y);
				r.X += ctx.CharSize.Width * 3;
				if (IsMultiLine)
				{
					ctx.Canvas.DrawString(start.Name, ctx.Font, commentsBrush, r,
						ctx.SingleLineFormat);
				}
				else
				{
					ctx.Canvas.DrawString(start.Name, ctx.Font, commentsBrush, r.X, r.Y);
				}

			}
			DrawSelection(ctx, m);
		}
		public override void DrawHighligt(DrawContext ctx, HighlightRange lh, Metrics m)
		{
			DrawHighlight(ctx, m, ctx.CharSize.Width * 5, lh);
		}
		public override string Text
		{
			get
			{
				return start != null ? start.Name : "";
			}
		}
		public FrameBegin Begin { get { return start; } }

		public FrameEnd(long position, IThread thread, DateTime time)
			:
			base(position, thread, time)
		{
			this.flags = MessageFlag.EndFrame;
		}

		internal void SetCollapsed(bool value)
		{
			FrameBegin.SetCollapsedFlag(ref flags, value);
		}

		internal void SetStart(FrameBegin start)
		{
			this.start = start;
			InitializeMultilineFlag();
		}
		FrameBegin start;
	};

	public class Content : MessageBase
	{
		public override void DrawOutline(DrawContext ctx, Metrics m)
		{
			Image icon = null;
			Image icon2 = null;
			if (Severity == SeverityFlag.Error)
				icon = ctx.ErrorIcon;
			else if (Severity == SeverityFlag.Warning)
				icon = ctx.WarnIcon;
			if (IsBookmarked)
				if (icon == null)
					icon = ctx.BookmarkIcon;
				else
					icon2 = ctx.SmallBookmarkIcon;
			if (icon == null)
				return;
			int w = FixedMetrics.CollapseBoxesAreaSize;
			ctx.Canvas.DrawImage(icon,
				icon2 == null ? (w - icon.Width) / 2 : 1,
				m.MessageRect.Y + (ctx.MessageHeight - icon.Height) / 2,
				icon.Width, 
				icon.Height
			);
			if (icon2 != null)
				ctx.Canvas.DrawImage(icon2,
					w - icon2.Width - 1,
					m.MessageRect.Y + (ctx.MessageHeight - icon2.Height) / 2,
					icon2.Width,
					icon2.Height
				);
		}

		public override void Draw(DrawContext ctx, Metrics m)
		{
			FillBackground(ctx, m);
			DrawTime(ctx, m);

			Brush b = Selected ? GetSelectedTextBrush(ctx) : ctx.InfoMessagesBrush;
			if (IsMultiLine)
			{
				ctx.Canvas.DrawString(Text, ctx.Font, b, m.OffsetTextRect,
					ctx.SingleLineFormat);
			}
			else
			{
				ctx.Canvas.DrawString(Text, ctx.Font, b, m.OffsetTextRect.Location);
			}

			DrawMultiline(ctx, m);
			DrawSelection(ctx, m);
		}

		public override void DrawHighligt(DrawContext ctx, HighlightRange lh, Metrics m)
		{
			DrawHighlight(ctx, m, 0, lh);
		}

		public override string Text
		{
			get
			{
				return message;
			}
		}

		public string FullText
		{
			get { return message; }
		}

		[Flags]
		public enum SeverityFlag
		{
			Error = MessageFlag.Error,
			Warning = MessageFlag.Warning,
			Info = MessageFlag.Info,
			All = MessageFlag.ContentTypeMask
		};

		public SeverityFlag Severity
		{
			get
			{
				return (SeverityFlag)(Flags & MessageFlag.ContentTypeMask);
			}
		}

		public Content(long position, IThread t, DateTime time, string msg, SeverityFlag s)
			:
			base(position, t, time)
		{
			message = msg;
			this.flags = MessageFlag.Content | (MessageFlag)s;
			InitializeMultilineFlag();
		}

		string message;
	};

	public sealed class ExceptionContent : Content
	{
		public class ExceptionInfo
		{
			public readonly string Message;
			public readonly string Stack;
			public readonly ExceptionInfo InnerException;
			public ExceptionInfo(string msg, string stack, ExceptionInfo inner)
			{
				Message = msg;
				Stack = stack;
				InnerException = inner;
			}
		};
		public ExceptionContent(long position, IThread t, DateTime time, string contextMsg, ExceptionInfo ei)
			:
			base(position, t, time, string.Format("{0}. Exception: {1}", contextMsg, ei.Message), SeverityFlag.Error)
		{
			Exception = ei;
		}

		public readonly ExceptionInfo Exception;
	};

	public struct IndexedMessage
	{
		public int Index;
		public MessageBase Message;
		public IndexedMessage(int idx, MessageBase m)
		{
			this.Index = idx;
			this.Message = m;
		}
	};

	internal static class Utils
	{
		internal static int PutInRange(int min, int max, int x)
		{
			if (x < min)
				return min;
			if (x > max)
				return max;
			return x;
		}
		internal static long PutInRange(long min, long max, long x)
		{
			if (x < min)
				return min;
			if (x > max)
				return max;
			return x;
		}
	}
}
