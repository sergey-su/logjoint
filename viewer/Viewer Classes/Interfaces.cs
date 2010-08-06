using System;
using System.Collections.Generic;
using System.Text;
using System.Drawing;
using System.IO;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace LogJoint
{
	public enum ReaderState
	{
		NoFile,
		DetectingAvailableTime,
		LoadError,
		Loading,
		Idle
	};

	public struct LogReaderStats
	{
		public ReaderState State;
		public DateRange? AvailableTime;
		public DateRange LoadedTime;
		public IConnectionParams ConnectionParams;
		public Exception Error;
		public int MessagesCount;
		public int? TotalMessagesCount;
		public long? LoadedBytes;
		public long? TotalBytes;
		public bool? IsFullyLoaded;
		public bool? IsShiftableDown;
		public bool? IsShiftableUp;
	};

	[Flags]
	public enum StatsFlag
	{
		State = 1,
		LoadedTime = 2,
		AvailableTime = 4,
		FileName = 8,
		Error = 16,
		MessagesCount = 32,
		BytesCount = 64,
		AvailableTimeUpdatedIncrementallyFlag = 0xA1
	}

	public interface ILogReaderHost: IDisposable
	{
		Source Trace { get; }
		ITempFilesManager TempFilesManager { get; }
		LogSourceThreads Threads { get; }

		void OnAboutToIdle();
		void OnStatisticsChanged(StatsFlag flags);
		void OnMessagesChanged();
	}

	[Flags]
	public enum NavigateFlag
	{
		None = 0,

		AlignCenter = 1,
		AlignTop = 2,
		AlignBottom = 4,
		AlignMask = AlignCenter | AlignTop | AlignBottom,

		OriginDate = 8,
		OriginStreamBoundaries = 16,
		OriginLoadedRangeBoundaries = 32,
		OriginMask = OriginDate | OriginStreamBoundaries | OriginLoadedRangeBoundaries,

		StickyCommandMask = AlignBottom | OriginStreamBoundaries,

		ShiftingMode = 64,
	};

	public delegate void CompletionHandler(ILogReader sender, object result);

	public class DateBoundPositionResponceData
	{
		public long Position;
		public bool IsEndPosition;
		public bool IsBeforeBeginPosition;
		public DateTime? Date;
	};

	public interface ILogReader : IDisposable
	{
		ILogReaderHost Host { get; }
		ILogReaderFactory Factory { get; }

		bool IsDisposed { get; }

		LogReaderStats Stats { get; }

		void LockMessages();
		IMessagesCollection Messages { get; }
		void UnlockMessages();

		void Interrupt();
		void NavigateTo(DateTime? date, NavigateFlag align);
		void Cut(DateRange range);
		void LoadHead(DateTime endDate);
		void LoadTail(DateTime beginDate);
		bool WaitForAnyState(bool idleState, bool finishedState, int timeout);
		void Refresh();
		void GetDateBoundPosition(DateTime d, PositionedMessagesUtils.ValueBound bound, CompletionHandler completionHandler);

		IEnumerable<IThread> Threads { get; }
	}

	public interface IFactoryUICallback
	{
		ILogReaderHost CreateHost();
		ILogReader FindExistingReader(IConnectionParams connectParams);
		void AddNewReader(ILogReader reader);
	};

	public interface ILogReaderFactoryUI: IDisposable
	{
		Control UIControl { get; }
		void Apply(IFactoryUICallback callback);
	};

	public interface IConnectionParams
	{
		string this[string key] { get; set; }
		void Assign(IConnectionParams other);
		bool AreEqual(IConnectionParams other);
	};

	public interface ILogReaderFactory
	{
		string CompanyName { get; }
		string FormatName { get; }
		string FormatDescription { get; }
		ILogReaderFactoryUI CreateUI();
		string GetUserFriendlyConnectionName(IConnectionParams connectParams);
		ILogReader CreateFromConnectionParams(ILogReaderHost host, IConnectionParams connectParams);
	};

	public interface IFileReaderFactory: ILogReaderFactory
	{
		IEnumerable<string> SupportedPatterns { get; }
		IConnectionParams CreateParams(string fileName);
	};

	public interface ILogReaderFactoryRegistry
	{
		void Register(ILogReaderFactory fact);
		IEnumerable<ILogReaderFactory> Items { get; }
		ILogReaderFactory Find(string companyName, string formatName);
	};

	public interface IMessagesCollection
	{
		int Count { get; }
		IEnumerable<IndexedMessage> Forward(int begin, int end);
		IEnumerable<IndexedMessage> Reverse(int begin, int end);
	};

	public interface IBookmark
	{
		DateTime Time { get; }
		int MessageHash { get; }
		IThread Thread { get; }
		IBookmark Clone();
	};

	public interface INextBookmarkCallback
	{
		IEnumerable<MessageBase> EnumMessages(DateTime tim, bool forward);
	};

	public interface IBookmarks
	{
		IBookmark ToggleBookmark(MessageBase msg);
		void Clear();
		IBookmark GetNext(MessageBase current, bool forward, INextBookmarkCallback callback);
		IEnumerable<IBookmark> Items { get; }
		IBookmarksHandler CreateHandler();
	};

	public interface IBookmarksHandler: IDisposable
	{
		bool ProcessNextMessageAndCheckIfItIsBookmarked(MessageBase l);
	};

	public interface IStatusReport: IDisposable
	{
		void SetStatusString(string text);
		bool AutoHide { get; set; }
		bool Blink { get; set; }
	};

	static class FixedMetrics
	{
		public const int CollapseBoxesAreaSize = 25;
		public const int OutlineBoxSize = 10;
		public const int OutlineCrossSize = 7;
		public const int LevelOffset = 15;
	}

	[Flags]
	public enum ThreadCounter
	{
		None = 0,
		Messages = 1,
		FramesInfo = 2,
		FilterRegions = 4,
		All = Messages | FramesInfo | FilterRegions,
	};

	public class FilterContext
	{
		public void Reset()
		{
			filterRegionDepth = 0;
			regionFilter = null;
		}

		public void BeginRegion(Filter filter)
		{
			if (filterRegionDepth == 0)
				regionFilter = filter;
			else
				System.Diagnostics.Debug.Assert(filter == regionFilter);
			++filterRegionDepth;
		}

		public void EndRegion()
		{
			--filterRegionDepth;
			if (filterRegionDepth == 0)
				regionFilter = null;
		}

		public Filter RegionFilter
		{
			get { return regionFilter; }
		}

		int filterRegionDepth;
		Filter regionFilter;
	};


	public interface IThread : IDisposable
	{
		bool IsInitialized { get; }
		bool IsDisposed { get; }
		void Init(string description);
		string ID { get; }
		string Description { get; }
		string DisplayName { get; }
		bool Visible { get; set; }
		bool ThreadMessagesAreVisible { get; }
		Color ThreadColor { get; }
		Brush ThreadBrush { get; }
		int MessagesCount { get; }
		IBookmark FirstKnownMessage { get; }
		IBookmark LastKnownMessage { get; }
		ILogSource LogSource { get; }
		
		Stack<MessageBase> Frames { get; }

		void BeginCollapsedRegion();
		void EndCollapsedRegion();
		bool IsInCollapsedRegion { get; }

		FilterContext DisplayFilterContext { get; }
		FilterContext HighlightFilterContext { get; }

		void CountLine(MessageBase line);

		void ResetCounters(ThreadCounter counterFlags);
	}

	public interface ILogSource : IDisposable, ILogReaderHost
	{
		void Init(ILogReader reader);

		ILogReader Reader { get; }
		bool IsDisposed { get; }
		Color Color { get; }
		bool Visible { get; set; }
		string DisplayName { get; }
		bool TrackingEnabled { get; set; }
	}

	public struct HighlightRange
	{
		public int Begin, End;
		public HighlightRange(int begin, int end)
		{
			Begin = begin;
			End = end;
		}
		public HighlightRange(int beginEnd)
		{
			Begin = beginEnd;
			End = beginEnd;
		}
		public bool IsEmpty
		{
			get
			{
				return Begin == End;
			}
		}
	};

	public class DrawContext
	{
		public SizeF CharSize;
		public double CharWidthDblPrecision;
		public int MessageHeight;
		public int TimeAreaSize;
		public Brush InfoMessagesBrush;
		public Font Font;
		public Font NewlineFont;
		public Brush CommentsBrush;
		public Brush DefaultBackgroundBrush;
		public Pen OutlineMarkupPen, SelectedOutlineMarkupPen;
		public Brush SelectedBkBrush;
		public Brush SelectedFocuslessBkBrush;
		public Brush SelectedTextBrush;
		public Brush SelectedFocuslessTextBrush;
		public Brush HighlightBrush;
		public Image ErrorIcon, WarnIcon, BookmarkIcon, SmallBookmarkIcon;
		public Pen HighlightPen;
		public Pen TimeSeparatorLine;
		public StringFormat SingleLineFormat;

		public Graphics Canvas;
		public int MessageIdx;
		public bool ShowTime;
		public bool ShowMilliseconds;
		public bool MessageFocused;
		public bool ControlFocused;
		public Point ScrollPos;
		public Rectangle ClientRect;
		public Point GetTextOffset(int level)
		{
			int x = FixedMetrics.CollapseBoxesAreaSize + FixedMetrics.LevelOffset * level - ScrollPos.X;
			if (ShowTime)
				x += TimeAreaSize;
			int y = MessageIdx * MessageHeight - ScrollPos.Y;
			return new Point(x, y);
		}
	};

	public abstract class MessageBase
	{
		public abstract void Draw(DrawContext ctx, Metrics m);
		public abstract void DrawOutline(DrawContext ctx, Metrics m);
		public abstract void DrawHighligt(DrawContext ctx, HighlightRange lh, Metrics m);

		[Flags]
		public enum MessageFlag : short
		{
			None = 0,

			StartFrame = 0x01,
			EndFrame = 0x02,
			Content = 0x04,
			TypeMask = StartFrame | EndFrame | Content,

			Error = 0x08,
			Warning = 0x10,
			Info = 0x20,
			ContentTypeMask = Error | Warning | Info,

			Collapsed = 0x40,
			Selected = 0x80,

			HiddenAsCollapsed = 0x100,
			HiddenBecauseOfInvisibleThread = 0x200, // message is invisible because its thread is invisible
			HiddenAsFilteredOut = 0x400, // message is invisible because it's been filtered out by a filter
			HiddenAll = HiddenAsCollapsed | HiddenBecauseOfInvisibleThread | HiddenAsFilteredOut,

			IsMultiLine = 0x800,
			IsBookmarked = 0x1000,
			IsHighlighted = 0x2000,
		}
		public MessageFlag Flags { get { return flags; } }

		public bool IsMultiLine { get { return (flags & MessageFlag.IsMultiLine) != 0; } }

		protected abstract int GetDisplayTextLength();

		protected int GetFirstTextLineLength()
		{
			string txt = this.Text;
			if (IsMultiLine)
				return GetFirstLineLength(txt);
			return txt.Length;
		}

		public abstract string Text { get; }
		public int Level { get { return level; } }

		public MessageBase(long position, IThread t, DateTime time)
		{
			this.thread = t;
			this.time = time;
			this.position = position;
		}

		public long Position
		{
			get { return position; }
		}
		public IThread Thread 
		{ 
			get { return thread; } 
		}
		public bool Selected
		{
			get
			{
				return (Flags & MessageFlag.Selected) != 0;
			}
		}
		public bool Visible
		{
			get
			{
				return (Flags & MessageFlag.HiddenAll) == 0;
			}
		}
		public DateTime Time { get { return time; } }

		public bool IsBookmarked
		{
			get { return (Flags & MessageFlag.IsBookmarked) != 0; }
		}

		public bool IsHighlighted
		{
			get { return (Flags & MessageFlag.IsHighlighted) != 0; }
		}

		internal void SetHidden(bool collapsed, bool hiddenBecauseOfInvisibleThread, bool hiddenAsFilteredOut)
		{
			flags = flags & ~MessageFlag.HiddenAll;
			if (collapsed)
				flags |= MessageFlag.HiddenAsCollapsed;
			if (hiddenBecauseOfInvisibleThread)
				flags |= MessageFlag.HiddenBecauseOfInvisibleThread;
			if (hiddenAsFilteredOut)
				flags |= MessageFlag.HiddenAsFilteredOut;
		}
		internal void SetSelected(bool value)
		{
			if (value) flags |= MessageFlag.Selected;
			else flags &= ~MessageFlag.Selected;
		}
		internal void SetBookmarked(bool value)
		{
			if (value) flags |= MessageFlag.IsBookmarked;
			else flags &= ~MessageFlag.IsBookmarked;
		}
		internal void SetHighlighted(bool value)
		{
			if (value) flags |= MessageFlag.IsHighlighted;
			else flags &= ~MessageFlag.IsHighlighted;
		}
		internal void SetLevel(int level)
		{
			if (level < 0)
				level = 0;
			else if (level > UInt16.MaxValue)
				level = UInt16.MaxValue;
			unchecked
			{
				this.level = (UInt16)level;
			}
		}
		internal static int GetFirstLineLength(string s)
		{
			return s.IndexOfAny(new char[] { '\r', '\n' });
		}
		internal void InitializeMultilineFlag()
		{
			if (GetFirstLineLength(this.Text) >= 0)
				flags |= MessageFlag.IsMultiLine;
		}
		/*internal void SetExtraHash(UInt16 value)
		{
			extraHash = value;
		}
		internal void SetExtraHash(long value)
		{
			extraHash = (UInt16)(
				((value >> 0x00) & 0xFFFF) ^
				((value >> 0x10) & 0xFFFF) ^
				((value >> 0x20) & 0xFFFF) ^
				((value >> 0x30) & 0xFFFF)
			);
		}*/

		public struct Metrics
		{
			public Rectangle MessageRect;
			public Point TimePos;
			public Rectangle OffsetTextRect;
			public Point OulineBoxCenter;
			public Rectangle OulineBox;
		};

		public Metrics GetMetrics(DrawContext dc)
		{
			Point offset = dc.GetTextOffset(this.Level);

			Metrics m;

			m.MessageRect = new Rectangle(
				0,
				offset.Y,
				dc.ClientRect.Width,
				dc.MessageHeight
			);

			m.TimePos = new Point(
				FixedMetrics.CollapseBoxesAreaSize - dc.ScrollPos.X,
				m.MessageRect.Y
			);

			int charCount = GetDisplayTextLength();
			if (IsMultiLine)
			{
				charCount++;
			}

			m.OffsetTextRect = new Rectangle(
				offset.X,
				m.MessageRect.Y,
				(int)((double)charCount * dc.CharWidthDblPrecision),
				m.MessageRect.Height
			);

			m.OulineBoxCenter = new Point(
				IsBookmarked ?
					FixedMetrics.OutlineBoxSize / 2 + 1:
					FixedMetrics.CollapseBoxesAreaSize / 2,
				m.MessageRect.Y + dc.MessageHeight / 2
			);
			m.OulineBox = new Rectangle(
				m.OulineBoxCenter.X - FixedMetrics.OutlineBoxSize / 2,
				m.OulineBoxCenter.Y - FixedMetrics.OutlineBoxSize / 2,
				FixedMetrics.OutlineBoxSize,
				FixedMetrics.OutlineBoxSize
			);

			return m;
		}

		protected void FillBackground(DrawContext dc, Metrics m)
		{
			Rectangle r = m.MessageRect;
			Brush b = null;
			if (Selected)
			{
				if (dc.ControlFocused)
					b = dc.SelectedBkBrush;
				else
					b = dc.SelectedFocuslessBkBrush;
			}
			else if (IsHighlighted)
			{
				b = dc.HighlightBrush;
			}
			else if (thread != null)
			{
				if (thread.IsDisposed)
					b = dc.DefaultBackgroundBrush;
				else
					b = thread.ThreadBrush;
			}
			if (b == null)
			{
				b = dc.DefaultBackgroundBrush;
			}
			dc.Canvas.FillRectangle(b, r);
		}
		protected void DrawSelection(DrawContext dc, Metrics m)
		{
			if (dc.MessageFocused)
			{
				ControlPaint.DrawFocusRectangle(dc.Canvas, new Rectangle(
					FixedMetrics.CollapseBoxesAreaSize, m.MessageRect.Y,
					dc.ClientRect.Width - FixedMetrics.CollapseBoxesAreaSize, dc.MessageHeight
				), Color.Black, Color.Black);
			}
		}
		public static GraphicsPath RoundRect(RectangleF rectangle, float roundRadius)
		{
			RectangleF innerRect = RectangleF.Inflate(rectangle, -roundRadius, -roundRadius);
			GraphicsPath path = new GraphicsPath();
			path.StartFigure();
			path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Bottom - 1, roundRadius), 0, 90);
			path.AddArc(RoundBounds(innerRect.Left, innerRect.Bottom - 1, roundRadius), 90, 90);
			path.AddArc(RoundBounds(innerRect.Left, innerRect.Top, roundRadius), 180, 90);
			path.AddArc(RoundBounds(innerRect.Right - 1, innerRect.Top, roundRadius), 270, 90);
			path.CloseFigure();
			return path;
		}
		private static RectangleF RoundBounds(float x, float y, float rounding)
		{
			return new RectangleF(x - rounding, y - rounding, 2 * rounding, 2 * rounding);
		}
		protected void DrawHighlight(DrawContext dc, Metrics m, float textXPos, HighlightRange hl)
		{
			GraphicsState state = dc.Canvas.Save();
			try
			{
				dc.Canvas.SmoothingMode = SmoothingMode.HighQuality;

				RectangleF tmp = new RectangleF(
					m.OffsetTextRect.X + textXPos + dc.CharSize.Width * hl.Begin,
					m.OffsetTextRect.Y,
					dc.CharSize.Width * (hl.End - hl.Begin),
					m.OffsetTextRect.Height
				);

				using (GraphicsPath path = RoundRect(
						RectangleF.Inflate(tmp, 3, 2), dc.CharSize.Width / 2))
				{
					dc.Canvas.DrawPath(dc.HighlightPen, path);
				}
			}
			finally
			{
				dc.Canvas.Restore(state);
			}
		}

		protected void DrawMultiline(DrawContext ctx, Metrics m)
		{
			if (IsMultiLine)
			{
				ctx.Canvas.DrawString("\u00bf", ctx.NewlineFont,
					Selected ? GetSelectedTextBrush(ctx) : ctx.CommentsBrush,
					m.OffsetTextRect.Right - ctx.CharSize.Width, m.MessageRect.Y);
			}
		}
		
		public static string FormatTime(DateTime time, bool showMilliseconds)
		{
			if (!showMilliseconds)
				return time.ToString();
			else
				return string.Format("{0} ({1})", time.ToString(), time.Millisecond);
		}
		
		protected void DrawTime(DrawContext ctx, Metrics m)
		{
			if (ctx.ShowTime)
			{
				ctx.Canvas.DrawString(FormatTime(this.Time, ctx.ShowMilliseconds),
					ctx.Font, 
					Selected ? GetSelectedTextBrush(ctx) : ctx.InfoMessagesBrush,
					m.TimePos.X, m.TimePos.Y);
			}
		}

		public override int GetHashCode()
		{
			return GetHashCode(false);
		}

		public int GetHashCode(bool ignoreMessageTime)
		{
			// The primary source of the hash is message's position. But it is not the only source,
			// we have to use the other fields because messages might be at the same position
			// but be different. That might happen, for example, when a message was at the end 
			// of the stream and wasn't read completely. As the stream grows a new message might be
			// read and at this time completely. Those two message might be different, thought they
			// are at the same position.

			int ret = position.GetHashCode();

			// Don't hash Text for frame-end beacause it doesn't have its own permanent text. 
			// It takes the text from brame begin instead. The link to frame begin may change 
			// during the time (it may get null or not null).
			if ((flags & MessageFlag.TypeMask) != MessageFlag.EndFrame) 
				ret ^= Text.GetHashCode();

			if (!ignoreMessageTime)
				ret ^= time.GetHashCode();
			if (thread != null)
				ret ^= thread.GetHashCode();
			ret ^= (int)(flags & (MessageFlag.TypeMask | MessageFlag.ContentTypeMask));

			return ret;
		}

		protected static Brush GetSelectedTextBrush(DrawContext ctx)
		{
			return ctx.ControlFocused ? ctx.SelectedTextBrush : ctx.SelectedFocuslessTextBrush;
		}

		DateTime time;
		IThread thread;
		protected MessageFlag flags;
		UInt16 level;
		long position;
	};
	
	public interface ITempFilesManager
	{
		string GenerateNewName();
	};

	public interface IUINavigationHandler
	{
		void ShowLine(IBookmark bmk);
		void ShowThread(IThread thread);
		void ShowLogSource(ILogSource source);
	};

	public interface IMainForm
	{
		void AddOwnedForm(Form f);
	};

	public interface IPlugin: IDisposable
	{
	};

	public class MediaInitParams
	{
		public readonly Source Trace;
		public MediaInitParams(Source trace)
		{
			Trace = trace;
		}
	};

	public interface ILogMedia: IDisposable
	{
		void Update();
		bool IsAvailable { get; }
		Stream DataStream { get; }
		DateTime LastModified { get; }
		long Size { get; }
	};

	public class InvalidFormatException : Exception
	{
		public InvalidFormatException()
			: base("Unable to parse the stream. The data seems to have incorrect format.")
		{ }
	};
}
