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
		MultilineText TextAsMultilineText { get; }

		StringSlice RawText { get; }
		MultilineText RawTextAsMultilineText { get; }

		void SetPosition(long position, long endPosition);
		void ReallocateTextBuffer(IStringSliceReallocator alloc);

		StringSlice Link { get; }
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
		ContentTypeMask = Error | Warning | Info
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

	public delegate MultilineText MessageTextGetter(IMessage message);

	public static class MessageTextGetters
	{
		public static MessageTextGetter SummaryTextGetter = MessageExtentions.GetText;
		public static MessageTextGetter RawTextGetter = MessageExtentions.GetRawText;

		public static MessageTextGetter Get(bool rawTextGetter) => rawTextGetter ? RawTextGetter : SummaryTextGetter;
	};
}
