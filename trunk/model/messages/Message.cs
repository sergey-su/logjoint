using System;

namespace LogJoint
{
	public sealed class Message : IMessage
	{
		public Message(
			long position,
			long endPosition,
			IThread t,
			MessageTimestamp time,
			StringSlice text,
			SeverityFlag s,
			StringSlice rawText = new StringSlice(),
			int? maxLineLen = null
		)
		{
			if (endPosition < position)
				throw new ArgumentException("bad message positions");
			this.thread = t;
			this.time = time;
			this.position = position;
			this.endPosition = endPosition;
			this.text = text;
			this.rawText = rawText;
			this.flags = (MessageFlag)s;
			if (maxLineLen != null)
			{
				this.rawText = this.rawText.Wrap(maxLineLen.Value);
				this.text = this.text.Wrap(maxLineLen.Value);
			}
		}

		public override int GetHashCode()
		{
			return GetHashCodeInternal(false);
		}

		public override string ToString()
		{
			return rawText.IsInitialized ? rawText : text;
		}

		MessageFlag IMessage.Flags { get { return flags; } }

		long IMessage.Position => position;
		long IMessage.EndPosition => endPosition;
		IThread IMessage.Thread => thread;
		MessageTimestamp IMessage.Time => time;
		StringSlice IMessage.Text => text;
		StringUtils.MultilineText IMessage.TextAsMultilineText { get { return new StringUtils.MultilineText(text, true); } }
		StringSlice IMessage.RawText => rawText;
		StringUtils.MultilineText IMessage.RawTextAsMultilineText { get { return new StringUtils.MultilineText(rawText, true); } }
		SeverityFlag IMessage.Severity => (SeverityFlag) (flags & MessageFlag.ContentTypeMask);

		void IMessage.Visit(IMessageVisitor visitor) { visitor.Visit(this); }

		IMessage IMessage.Clone()
		{
			IMessage intf = this;
			return new Message(position, endPosition, thread, time, text, intf.Severity, rawText);
		}

		void IMessage.ReallocateTextBuffer(IStringSliceReallocator alloc)
		{
			rawText = alloc.Reallocate(rawText);
			text = alloc.Reallocate(text);
		}

		void IMessage.SetPosition(long position, long endPosition)
		{
			if (endPosition < position)
				throw new ArgumentException("bad message positions");
			this.position = position;
			this.endPosition = endPosition;
		}

		int IMessage.GetHashCode(bool ignoreMessageTime)
		{
			return GetHashCodeInternal(ignoreMessageTime);
		}

		#region Implementation

		int GetHashCodeInternal(bool ignoreMessageTime)
		{
			// The primary source of the hash is message's position. But it is not the only source,
			// we have to use the other fields because messages might be at the same position
			// but be different. That might happen, for example, when a message was at the end 
			// of the live stream and wasn't read completely. As the stream grows the same message 
			// will be fully written and might be eventually read again.
			// Those two message might be different, thought they are at the same position.

			int ret = Hashing.GetStableHashCode(position);

			ret ^= text.GetStableHashCode();

			if (!ignoreMessageTime)
				ret = MessagesUtils.XORTimestampHash(ret, time);
			if (thread != null)
				ret ^= Hashing.GetStableHashCode(thread.ID);
			ret ^= (int)(flags & MessageFlag.ContentTypeMask);

			return ret;
		}

		#endregion

		#region Data

		readonly MessageTimestamp time;
		readonly IThread thread;
		readonly MessageFlag flags;
		long position, endPosition;
		StringSlice text;
		StringSlice rawText;

		#endregion
	};

}
