using System;

namespace LogJoint
{
	public interface IMessage
	{
		long Position { get; }
		long EndPosition { get; }
		IThread Thread { get; }
		MessageTimestamp Time { get; }
		MessageFlag Flags { get; }
		SeverityFlag Severity { get; }
		IMessage Clone();

		int GetHashCode();
		int GetHashCode(bool ignoreMessageTime);

		StringSlice Text { get; }
		StringUtils.MultilineText TextAsMultilineText { get; }

		StringSlice RawText { get; }
		StringUtils.MultilineText RawTextAsMultilineText { get; }

		void Visit(IMessageVisitor visitor);

		void SetPosition(long position, long endPosition);
		void SetRawText(StringSlice rawText);

		void ReallocateTextBuffer(IStringSliceReallocator alloc);
		void WrapsTexts(int maxLineLen);
	};

	public interface IMessageVisitor
	{
		void Visit(IMessage msg);
	};

	[Flags]
	public enum SeverityFlag
	{
		Error = MessageFlag.Error,
		Warning = MessageFlag.Warning,
		Info = MessageFlag.Info,
		All = MessageFlag.ContentTypeMask
	};

	[Flags]
	public enum MessageFlag : short
	{
		None = 0,

		Error = 0x08,
		Warning = 0x10,
		Info = 0x20,
		ContentTypeMask = Error | Warning | Info,

		HiddenBecauseOfInvisibleThread = 0x200, // message is invisible because its thread is invisible
		HiddenAsFilteredOut = 0x400, // message is invisible because it's been filtered out by a filter
		HiddenAll = HiddenBecauseOfInvisibleThread | HiddenAsFilteredOut,

		IsMultiLine = 0x800,
		IsRawTextMultiLine = 0x80,
		IsMultiLineInited = 0x1000,
		IsHighlighted = 0x2000,
	};

	public struct IndexedMessage
	{
		public int Index;
		public IMessage Message;
		public IndexedMessage(int idx, IMessage m)
		{
			this.Index = idx;
			this.Message = m;
		}
	};
}
