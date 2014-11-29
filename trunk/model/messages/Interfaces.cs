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
		int GetHashCode();
		int GetHashCode(bool ignoreMessageTime);
		MessageFlag Flags { get; }
		StringSlice Text { get; }
		StringSlice RawText { get; }

		void Visit(IMessageBaseVisitor visitor);

		// helpers. todo: try extract to an extension class
		bool IsMultiLine { get; }
		bool IsRawTextMultiLine { get; }
		bool IsHiddenAsFilteredOut { get; }
		bool IsHiddenBecauseOfInvisibleThread { get; }
		bool IsBookmarked { get; }
		bool IsHighlighted { get; }
		bool IsVisible { get; }
		bool IsStartFrame { get; }
		StringUtils.MultilineText RawTextAsMultilineText { get; }
		StringUtils.MultilineText TextAsMultilineText { get; }
		int EnumLines(Func<StringSlice, int, bool> callback);
		int GetLinesCount();
		StringSlice GetNthTextLine(int lineIdx);


		void SetPosition(long value);
		void SetLevel(int level);
		void SetBookmarked(bool value);
		void SetHighlighted(bool value);
		void SetHidden(bool collapsed, bool hiddenBecauseOfInvisibleThread, bool hiddenAsFilteredOut);

		int ReallocateTextBuffer(string newBuffer, int positionWithinBuffer);
		void __SetRawText(StringSlice rawText); // todo: get rid of it
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

	public interface IMessageBaseVisitor
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
		IsBookmarked = 0x2000,
		IsHighlighted = 0x4000,
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
