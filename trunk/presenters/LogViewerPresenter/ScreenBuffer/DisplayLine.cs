using System;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	[DebuggerDisplay("{Message.Position}#{LineIndex}/{TotalLinesInMessage}({ToString()})")]
	struct DisplayLine
	{
		public IMessage Message { get; private set; } // the message that this line belongs to
		public int LineIndex { get; private set; } // line number within the message
		public int TotalLinesInMessage { get; private set; } // nr of lines in the Message
		public MessageTextGetter DisplayTextGetter { get; private set; }
		public IMessagesSource Source { get; private set; }
		public int Index { get; private set; } // Line's index inside the screen buffer.
		public double LineOffsetBegin { get; private set; } // global scrolling support
		public double LineOffsetEnd { get; private set; } // global scrolling support

		public bool IsEmpty { get { return Message == null; } }

		public DisplayLine(IMessage msg, int lineIndex, int linesCount, MessageTextGetter displayTextGetter, IMessagesSource source, int index = -1)
		{
			Message = msg;
			LineIndex = lineIndex;
			TotalLinesInMessage = linesCount;
			DisplayTextGetter = displayTextGetter;
			var msgLen = msg.EndPosition - msg.Position;
			if (linesCount > 1)
			{
				var lineLen = msgLen / (double)linesCount;
				LineOffsetBegin = lineLen * lineIndex;
				LineOffsetEnd = (lineIndex + 1) * lineLen;
				if (lineIndex == linesCount - 1)
				{
					// this it to ensure the offset is strictly equal to the beginning of next message.
					// generic formula with floating point arithmetics leads to inequality.
					LineOffsetEnd = msgLen;
				}
			}
			else
			{
				LineOffsetBegin = 0;
				LineOffsetEnd = msgLen;
			}
			Index = index;
			Source = source;
		}

		public DisplayLine MakeIndexed(int index)
		{
			return new DisplayLine()
			{
				Message = Message,
				LineIndex = LineIndex,
				TotalLinesInMessage = TotalLinesInMessage,
				DisplayTextGetter = DisplayTextGetter,
				LineOffsetBegin = LineOffsetBegin,
				LineOffsetEnd = LineOffsetEnd,
				Index = index,
				Source = Source
			};
		}

		public override string ToString()
		{
			return DisplayTextGetter(Message).GetNthTextLine(LineIndex);
		}

		public ScreenBufferEntry ToScreenBufferEntry()
		{
			return new ScreenBufferEntry()
			{
				Index = Index,
				Message = Message,
				TextLineIndex = LineIndex,
				Source = Source
			};
		}
	};
};