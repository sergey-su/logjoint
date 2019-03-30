using System;
using System.Diagnostics;
using System.Text;

namespace LogJoint
{
	[DebuggerDisplay("{flags} {DoGetRawText().Value}")]
	public abstract class MessageBase : IMessage
	{
		protected MessageBase(long position, long endPosition, IThread t, MessageTimestamp time, StringSlice rawText = new StringSlice())
		{
			if (endPosition < position)
				throw new ArgumentException("bad message positions");
			this.thread = t;
			this.time = time;
			this.position = position;
			this.endPosition = endPosition;
			this.rawText = rawText;
		}

		public override int GetHashCode()
		{
			return GetHashCodeInternal(false);
		}

		MessageFlag IMessage.Flags { get { return flags; } }

		long IMessage.Position => position;
		long IMessage.EndPosition => endPosition;
		IThread IMessage.Thread => thread;
		MessageTimestamp IMessage.Time => time;
		StringSlice IMessage.Text => DoGetText();
		StringUtils.MultilineText IMessage.TextAsMultilineText { get { return GetTextAsMultilineText(); } }
		StringSlice IMessage.RawText => DoGetRawText();
		StringUtils.MultilineText IMessage.RawTextAsMultilineText { get { return new StringUtils.MultilineText(DoGetRawText(), GetIsRawTextMultiLine()); } }
		SeverityFlag IMessage.Severity => (SeverityFlag) (flags & MessageFlag.ContentTypeMask);

		void IMessage.Visit(IMessageVisitor visitor) { DoVisit(visitor); }

		IMessage IMessage.Clone() => throw new NotImplementedException();

		void IMessage.ReallocateTextBuffer(IStringSliceReallocator alloc) { DoReallocateTextBuffer(alloc); }

		void IMessage.WrapsTexts(int maxLineLen)
		{
			if (DoWrapTooLongText(maxLineLen))
 				flags &= ~MessageFlag.IsMultiLineInited;
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

		void IMessage.SetRawText(StringSlice rawText)
		{
			this.rawText = rawText;
		}


		#region Protected overridable interface

		protected abstract void DoVisit(IMessageVisitor visitor);
		protected abstract StringSlice DoGetText();
		protected virtual StringSlice DoGetRawText() { return rawText; }
		protected virtual void DoReallocateTextBuffer(IStringSliceReallocator alloc)
		{
			rawText = alloc.Reallocate(rawText);
		}
		protected virtual bool DoWrapTooLongText(int maxLineLen) { return WrapIfTooLong(ref rawText, maxLineLen); }

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

			ret ^= DoGetText().GetStableHashCode();

			if (!ignoreMessageTime)
				ret = MessagesUtils.XORTimestampHash(ret, time);
			if (thread != null)
				ret ^= Hashing.GetStableHashCode(thread.ID);
			ret ^= (int)(flags & MessageFlag.ContentTypeMask);

			return ret;
		}

		protected static bool WrapIfTooLong(ref StringSlice text, int lineLen)
		{
			if (text.Length < lineLen)
				return false;
			var ret = new StringBuilder(text.Length + Environment.NewLine.Length * text.Length / lineLen);
			for (var idx = 0; idx < text.Length; )
			{
				var len = Math.Min(lineLen, text.Length - idx);
				text.SubString(idx, len).Append(ret);
				ret.AppendLine();
				idx += len;
			}
			text = new StringSlice(ret.ToString());
			return true;
		}

		#endregion

		#region Data

		readonly MessageTimestamp time;
		readonly IThread thread;
		protected MessageFlag flags;
		long position, endPosition;
		StringSlice rawText;

		#endregion
	};

}
