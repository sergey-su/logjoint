using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public interface IMessage
	{
		long Position { get; }
		IThread Thread { get; }
		ILogSource LogSource { get; }
		MessageTimestamp Time { get; }
		int Level { get; }
		MessageFlag Flags { get; }

		int GetHashCode();
		int GetHashCode(bool ignoreMessageTime);

		StringSlice Text { get; }
		StringUtils.MultilineText TextAsMultilineText { get; }
		bool IsTextMultiline { get; }

		StringSlice RawText { get; }
		StringUtils.MultilineText RawTextAsMultilineText { get; }
		bool IsRawTextMultiLine { get; }

		void Visit(IMessageVisitor visitor);

		void SetPosition(long value);
		void SetLevel(int level);
		void SetHighlighted(bool value);
		void SetHidden(bool collapsed, bool hiddenBecauseOfInvisibleThread, bool hiddenAsFilteredOut);
		void SetRawText(StringSlice rawText);

		int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer);
	};

	public interface IFrameBegin: IMessage
	{
		void SetEnd(IFrameEnd e);
		StringSlice Name { get; }
		bool Collapsed { get; set; }
		IFrameEnd End { get; }
	};

	public interface IFrameEnd: IMessage
	{
		IFrameBegin Start { get; }
		void SetStart(IFrameBegin start);
		void SetCollapsed(bool value);
	};

	public interface IContent : IMessage
	{
		SeverityFlag Severity { get; }
	};

	public interface IMessageVisitor
	{
		void Visit(IContent msg);
		void Visit(IFrameBegin msg);
		void Visit(IFrameEnd msg);
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
