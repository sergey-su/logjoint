using System;
using System.Diagnostics;

namespace LogJoint
{
	[DebuggerDisplay("{flags} {DoGetText().Value}")]
	public abstract class MessageBase : IMessage
	{
		public MessageBase(long position, IThread t, MessageTimestamp time, StringSlice rawText = new StringSlice())
		{
			this.thread = t;
			this.time = time;
			this.position = position;
			this.rawText = rawText;
		}

		public override int GetHashCode()
		{
			return GetHashCodeInternal(false);
		}

		MessageFlag IMessage.Flags { get { return flags; } }

		int IMessage.Level { get { return level; } }

		StringUtils.MultilineText IMessage.TextAsMultilineText { get { return GetTextAsMultilineText(); } }

		StringUtils.MultilineText IMessage.RawTextAsMultilineText { get { return new StringUtils.MultilineText(DoGetRawText(), GetIsRawTextMultiLine()); } }

		long IMessage.Position { get { return position; } }
		IThread IMessage.Thread { get { return thread; } }
		ILogSource IMessage.LogSource { get { return thread != null ? thread.LogSource : null; } }
		MessageTimestamp IMessage.Time { get { return time; } }
		StringSlice IMessage.Text { get { return DoGetText(); } }
		StringSlice IMessage.RawText { get { return DoGetRawText(); } }
		bool IMessage.IsTextMultiline { get { return GetIsTextMultiline(); } }
		bool IMessage.IsRawTextMultiLine { get { return GetIsRawTextMultiLine(); } }

		void IMessage.Visit(IMessageVisitor visitor) { DoVisit(visitor); }

		int IMessage.ReallocateTextBuffer(string newBuffer, int positionWithinBuffer) { return DoReallocateTextBuffer(newBuffer, positionWithinBuffer); }


		void IMessage.SetHidden(bool collapsed, bool hiddenBecauseOfInvisibleThread, bool hiddenAsFilteredOut)
		{
			flags = flags & ~MessageFlag.HiddenAll;
			if (collapsed)
				flags |= MessageFlag.HiddenAsCollapsed;
			if (hiddenBecauseOfInvisibleThread)
				flags |= MessageFlag.HiddenBecauseOfInvisibleThread;
			if (hiddenAsFilteredOut)
				flags |= MessageFlag.HiddenAsFilteredOut;
		}
		void IMessage.SetHighlighted(bool value)
		{
			if (value) flags |= MessageFlag.IsHighlighted;
			else flags &= ~MessageFlag.IsHighlighted;
		}
		void IMessage.SetLevel(int level)
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
		void IMessage.SetPosition(long value)
		{
			position = value;
		}

		int IMessage.GetHashCode(bool ignoreMessageTime)
		{
			return GetHashCodeInternal(ignoreMessageTime);
		}

		void IMessage.SetRawText(StringSlice rawText)
		{
			this.rawText = rawText;
		}


		#region Protected overridable interface

		protected abstract void DoVisit(IMessageVisitor visitor);
		protected abstract StringSlice DoGetText();
		protected virtual StringSlice DoGetRawText() { return rawText; }
		protected abstract int DoReallocateTextBuffer(string newBuffer, int positionWithinBuffer);

		#endregion

		#region Protected helpers
		protected void SetCollapsedFlag(bool value)
		{
			if (value)
				flags |= MessageFlag.Collapsed;
			else
				flags &= ~MessageFlag.Collapsed;
		}
		#endregion


		#region Implementation

		private bool GetIsTextMultiline()
		{
			if ((flags & MessageFlag.IsMultiLineInited) == 0)
				InitializeMultilineFlag();
			return (flags & MessageFlag.IsMultiLine) != 0;
		}

		private bool GetIsRawTextMultiLine()
		{
			if ((flags & MessageFlag.IsMultiLineInited) == 0)
				InitializeMultilineFlag();
			return (flags & MessageFlag.IsRawTextMultiLine) != 0;
		}

		private StringUtils.MultilineText GetTextAsMultilineText()
		{
			return new StringUtils.MultilineText(DoGetText(), GetIsTextMultiline());
		}

		void InitializeMultilineFlag()
		{
			if (StringUtils.GetFirstLineLength(this.DoGetText()) >= 0)
				flags |= MessageFlag.IsMultiLine;
			if (rawText.IsInitialized && StringUtils.GetFirstLineLength(rawText) >= 0)
				flags |= MessageFlag.IsRawTextMultiLine;
			flags |= MessageFlag.IsMultiLineInited;
		}

		int GetHashCodeInternal(bool ignoreMessageTime)
		{
			// The primary source of the hash is message's position. But it is not the only source,
			// we have to use the other fields because messages might be at the same position
			// but be different. That might happen, for example, when a message was at the end 
			// of the live stream and wasn't read completely. As the stream grows the same message 
			// will be fully written and might be eventually read again.
			// Those two message might be different, thought they are at the same position.

			int ret = Hashing.GetStableHashCode(position);

			// Don't hash Text for frame-end beacause it doesn't have its own permanent text. 
			// It takes the text from brame begin instead. The link to frame begin may change 
			// during the time (it may get null or not null).
			if ((flags & MessageFlag.TypeMask) != MessageFlag.EndFrame)
				ret ^= DoGetText().GetStableHashCode();

			if (!ignoreMessageTime)
				ret = MessagesUtils.XORTimestampHash(ret, time);
			if (thread != null)
				ret ^= Hashing.GetStableHashCode(thread.ID);
			ret ^= (int)(flags & (MessageFlag.TypeMask | MessageFlag.ContentTypeMask));

			return ret;
		}

		#endregion

		#region Data

		readonly MessageTimestamp time;
		readonly IThread thread;
		protected MessageFlag flags;
		UInt16 level;
		long position;
		StringSlice rawText;

		#endregion
	};

}
