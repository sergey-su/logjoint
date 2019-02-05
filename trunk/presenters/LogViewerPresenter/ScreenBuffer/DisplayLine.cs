namespace LogJoint.UI.Presenters.LogViewer
{
	struct DisplayLine
	{
		public IMessage Message; // the message that this line belongs to
		public int LineIndex; // line number within the message
		public double LineOffsetBegin, LineOffsetEnd; // global scrolling support. todo: needed?
		public int Index; // Line's index inside the screen buffer. todo: needed?

		public DisplayLine(IMessage msg, int lineIndex, int linesCount)
		{
			Message = msg;
			LineIndex = lineIndex;
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
			Index = -1;
		}

		public DisplayLine MakeIndexed(int index)
		{
			return new DisplayLine()
			{
				Message = Message,
				LineIndex = LineIndex,
				LineOffsetBegin = LineOffsetBegin,
				LineOffsetEnd = LineOffsetEnd,
				Index = index
			};
		}
	};
};