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
			Unused = 0x80,

			HiddenAsCollapsed = 0x100,
			HiddenBecauseOfInvisibleThread = 0x200, // message is invisible because its thread is invisible
			HiddenAsFilteredOut = 0x400, // message is invisible because it's been filtered out by a filter
			HiddenAll = HiddenAsCollapsed | HiddenBecauseOfInvisibleThread | HiddenAsFilteredOut,

			IsMultiLine = 0x800,
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

		public abstract int GetDisplayTextLength();

		protected int GetFirstTextLineLength()
		{
			StringSlice txt = this.Text;
			if (IsMultiLine)
				return GetFirstLineLength(txt);
			return txt.Length;
		}

		public int EnumLines(Func<StringSlice, int, bool> callback)
		{
			if (!IsMultiLine)
			{
				if (callback != null)
					callback(Text, 0);
				return 1;
			}
			int currentIdx = 0;
			bool lastWasR = false;
			StringSlice txt = Text;
			int currentStart = 0;
			for (int i = 0; i < txt.Length; ++i)
			{
				bool yieldLine = false;
				int newCurrentStart = currentStart;
				int currentEnd = 0;
				switch (txt[i])
				{
					case '\r':
						if (lastWasR)
						{
							yieldLine = true;
							newCurrentStart = i;
							currentEnd = i - 1;
						}
						lastWasR = true;
						break;
					case '\n':
						yieldLine = true;
						if (lastWasR)
							currentEnd = i - 1;
						else
							currentEnd = i;
						lastWasR = false;
						newCurrentStart = i + 1;
						break;
					default:
						if (lastWasR)
						{
							yieldLine = true;
							newCurrentStart = i;
							currentEnd = i - 1;
						}
						lastWasR = false;
						break;
				}
				if (yieldLine)
				{
					if (callback != null)
						if (!callback(txt.SubString(currentStart, currentEnd - currentStart), currentIdx))
							return currentIdx + 1;
					++currentIdx;
					currentStart = newCurrentStart;
				}
			}
			if (lastWasR)
			{
				if (callback != null)
					if (!callback(txt.SubString(currentStart, txt.Length - currentStart - 1), currentIdx))
						return currentIdx + 1;
				++currentIdx;
			}
			else
			{
				if (callback != null)
					callback(txt.SubString(currentStart, txt.Length - currentStart), currentIdx);
			}
			return currentIdx + 1;
		}

		public int GetLinesCount()
		{
			return EnumLines(null);
		}

		public StringSlice GetNthTextLine(int lineIdx)
		{
			StringSlice ret = StringSlice.Empty;
			EnumLines((s, idx) =>
			{
				if (idx == lineIdx)
				{
					ret = s;
					return false;
				}
				return true;
			});
			return ret;
		}

		public abstract StringSlice Text { get; }
		internal abstract int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer);
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
		public ILogSource LogSource
		{
			get { return thread != null ? thread.LogSource : null; }
		}
		public bool IsVisible
		{
			get
			{
				return (Flags & MessageFlag.HiddenAll) == 0;
			}
		}
		public bool IsHiddenAsFilteredOut
		{
			get { return (Flags & MessageFlag.HiddenAsFilteredOut) != 0; }
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
		private static char[] newLineChars = new char[] { '\r', '\n' };

		internal static int GetFirstLineLength(StringSlice s)
		{
			return s.IndexOfAny(newLineChars);
		}
		internal void InitializeMultilineFlag()
		{
			if (GetFirstLineLength(this.Text) >= 0)
				flags |= MessageFlag.IsMultiLine;
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
			if (!showMilliseconds)
				return time.ToString();
			else
				return string.Format("{0} ({1})", time.ToString(), time.Millisecond);
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

		DateTime time;
		IThread thread;
		protected MessageFlag flags;
		UInt16 level;
		long position;
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
		public override int GetDisplayTextLength()
		{
			return GetCollapseMark(Collapsed).Length + 1 + GetFirstTextLineLength();
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
		internal override int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer)
		{
			return positionWithinBuffer + Text.Length;
		}
		public override int GetDisplayTextLength()
		{
			return 5 + GetFirstTextLineLength();
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

		public override int GetDisplayTextLength()
		{
			return GetFirstTextLineLength();
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
