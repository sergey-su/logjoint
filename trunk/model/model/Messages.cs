using System;
using System.Text;

namespace LogJoint
{
	public interface IMessageBaseVisitor
	{
		void Visit(Content msg);
		void Visit(FrameBegin msg);
		void Visit(FrameEnd msg);
	};

	public abstract class MessageBase
	{
		public abstract void Visit(IMessageBaseVisitor visitor);

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

			HiddenAsCollapsed = 0x100,
			HiddenBecauseOfInvisibleThread = 0x200, // message is invisible because its thread is invisible
			HiddenAsFilteredOut = 0x400, // message is invisible because it's been filtered out by a filter
			HiddenAll = HiddenAsCollapsed | HiddenBecauseOfInvisibleThread | HiddenAsFilteredOut,

			IsMultiLine = 0x800,
			IsRawTextMultiLine = 0x80,
			IsMultiLineInited = 0x1000,
			IsBookmarked = 0x2000,
			IsHighlighted = 0x4000,
		}
		public MessageFlag Flags { get { return flags; } }

		public bool IsMultiLine
		{
			get
			{
				if ((flags & MessageFlag.IsMultiLineInited) == 0)
					InitializeMultilineFlag();
				return (flags & MessageFlag.IsMultiLine) != 0;
			}
		}

		public bool IsRawTextMultiLine
		{
			get
			{
				if ((flags & MessageFlag.IsMultiLineInited) == 0)
					InitializeMultilineFlag();
				return (flags & MessageFlag.IsRawTextMultiLine) != 0;
			}
		}

		public int EnumLines(Func<StringSlice, int, bool> callback)
		{
			return TextAsMultilineText.EnumLines(callback);
		}

		public int GetLinesCount()
		{
			return TextAsMultilineText.GetLinesCount();
		}

		public StringSlice GetNthTextLine(int lineIdx)
		{
			return TextAsMultilineText.GetNthTextLine(lineIdx);
		}

		public abstract StringSlice Text { get; }
		public virtual StringSlice RawText { get { return rawText; } }
		public StringUtils.MultilineText TextAsMultilineText { get { return new StringUtils.MultilineText(Text, IsMultiLine); } }
		public StringUtils.MultilineText RawTextAsMultilineText { get { return new StringUtils.MultilineText(RawText, IsRawTextMultiLine); } }

		internal abstract int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer);
		public int Level { get { return level; } }

		public MessageBase(long position, IThread t, DateTime time, StringSlice rawText = new StringSlice())
		{
			this.thread = t;
			this.time = time;
			this.position = position;
			this.rawText = rawText;
		}

		public long Position
		{
			get { return position; }
		}
		public IThread Thread
		{
			get { return thread; }
		}
		public ILogSource LogSource
		{
			get { return thread != null ? thread.LogSource : null; }
		}
		public DateTime Time 
		{ 
			get { return time; } 
		}

		public bool IsVisible
		{
			get	{ return (Flags & MessageFlag.HiddenAll) == 0; }
		}
		public bool IsHiddenAsFilteredOut
		{
			get { return (Flags & MessageFlag.HiddenAsFilteredOut) != 0; }
		}
		public bool IsBookmarked
		{
			get { return (Flags & MessageFlag.IsBookmarked) != 0; }
		}
		public bool IsHighlighted
		{
			get { return (Flags & MessageFlag.IsHighlighted) != 0; }
		}

		public bool IsStartFrame
		{
			get { return (Flags & MessageFlag.TypeMask) == MessageFlag.StartFrame; }
		}

		public void SetHidden(bool collapsed, bool hiddenBecauseOfInvisibleThread, bool hiddenAsFilteredOut)
		{
			flags = flags & ~MessageFlag.HiddenAll;
			if (collapsed)
				flags |= MessageFlag.HiddenAsCollapsed;
			if (hiddenBecauseOfInvisibleThread)
				flags |= MessageFlag.HiddenBecauseOfInvisibleThread;
			if (hiddenAsFilteredOut)
				flags |= MessageFlag.HiddenAsFilteredOut;
		}
		public void SetBookmarked(bool value)
		{
			if (value) flags |= MessageFlag.IsBookmarked;
			else flags &= ~MessageFlag.IsBookmarked;
		}
		public void SetHighlighted(bool value)
		{
			if (value) flags |= MessageFlag.IsHighlighted;
			else flags &= ~MessageFlag.IsHighlighted;
		}
		public void SetLevel(int level)
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
		internal void InitializeMultilineFlag()
		{
			if (StringUtils.GetFirstLineLength(this.Text) >= 0)
				flags |= MessageFlag.IsMultiLine;
			if (rawText.IsInitialized && StringUtils.GetFirstLineLength(rawText) >= 0)
				flags |= MessageFlag.IsRawTextMultiLine;
			flags |= MessageFlag.IsMultiLineInited;
		}
		public void SetPosition(long value)
		{
			position = value;
		}

		public override int GetHashCode()
		{
			return GetHashCode(false);
		}

		public static string FormatTime(DateTime time, bool showMilliseconds)
		{
			return time.ToString(showMilliseconds ? "yyyy-MM-dd HH:mm:ss.fff" : "yyyy-MM-dd HH:mm:ss");
		}

		public static string FormatTime(DateTime time)
		{
			return FormatTime(time, time.Millisecond != 0);
		}

		public int GetHashCode(bool ignoreMessageTime)
		{
			// The primary source of the hash is message's position. But it is not the only source,
			// we have to use the other fields because messages might be at the same position
			// but be different. That might happen, for example, when a message was at the end 
			// of the stream and wasn't read completely. As the stream grows a new message might be
			// read and at this time completely. Those two message might be different, thought they
			// are at the same position.

			int ret = Hashing.GetStableHashCode(position);

			// Don't hash Text for frame-end beacause it doesn't have its own permanent text. 
			// It takes the text from brame begin instead. The link to frame begin may change 
			// during the time (it may get null or not null).
			if ((flags & MessageFlag.TypeMask) != MessageFlag.EndFrame)
				ret ^= Text.GetStableHashCode();

			if (!ignoreMessageTime)
				ret ^= Hashing.GetStableHashCode(time);
			if (thread != null)
				ret ^= Hashing.GetStableHashCode(thread.ID);
			ret ^= (int)(flags & (MessageFlag.TypeMask | MessageFlag.ContentTypeMask));

			return ret;
		}

		public void __SetRawText(StringSlice rawText) // todo: get rid of it
		{
			this.rawText = rawText;
		}

		DateTime time;
		IThread thread;
		protected MessageFlag flags;
		UInt16 level;
		long position;
		StringSlice rawText;
	};
	
	public sealed class FrameBegin : MessageBase
	{
		public override void Visit(IMessageBaseVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override StringSlice Text
		{
			get
			{
				return name;
			}
		}
		internal override int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer)
		{
			name = new StringSlice(newBuffer, positionWithinBuffer, name.Length);
			return positionWithinBuffer + name.Length;
		}
		public static string GetCollapseMark(bool collapsed)
		{
			return collapsed ? "{...}" : "{";
		}

		public FrameBegin(long position, IThread t, DateTime time, StringSlice name)
			:
			base(position, t, time)
		{
			this.name = name;
			this.flags = MessageFlag.StartFrame;
		}
		public void SetEnd(FrameEnd e)
		{
			end = e;
		}
		public StringSlice Name { get { return name; } }
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
		StringSlice name;
		FrameEnd end;
	};

	public sealed class FrameEnd : MessageBase
	{
		public override void Visit(IMessageBaseVisitor visitor)
		{
			visitor.Visit(this);
		}
		
		public override StringSlice Text
		{
			get
			{
				return start != null ? start.Name : StringSlice.Empty;
			}
		}
		public override StringSlice RawText 
		{ 
			get 
			{
				return start != null ? start.RawText : base.RawText;
			} 
		}
		internal override int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer)
		{
			return positionWithinBuffer + Text.Length;
		}
		public FrameBegin Start { get { return start; } }

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

		public void SetStart(FrameBegin start)
		{
			this.start = start;
		}
		FrameBegin start;
	};

	public class Content : MessageBase
	{
		public override void Visit(IMessageBaseVisitor visitor)
		{
			visitor.Visit(this);
		}

		public override StringSlice Text
		{
			get
			{
				return message;
			}
		}
		internal override int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer)
		{
			message = new StringSlice(newBuffer, positionWithinBuffer, message.Length);
			return positionWithinBuffer + message.Length;
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

		public Content(long position, IThread t, DateTime time, StringSlice msg, SeverityFlag s)
			:
			base(position, t, time)
		{
			this.message = msg;
			this.flags = MessageFlag.Content | (MessageFlag)s;
		}

		StringSlice message;
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
			base(position, t, time, new StringSlice(string.Format("{0}. Exception: {1}", contextMsg, ei.Message)), SeverityFlag.Error)
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

	public static class Utils
	{
		public static int PutInRange(int min, int max, int x)
		{
			if (x < min)
				return min;
			if (x > max)
				return max;
			return x;
		}
		public static long PutInRange(long min, long max, long x)
		{
			if (x < min)
				return min;
			if (x > max)
				return max;
			return x;
		}
	}
}
