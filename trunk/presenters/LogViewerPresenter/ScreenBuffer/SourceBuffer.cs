using System.Collections.Generic;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	class SourceBuffer : IMessagesCollection
	{
		public SourceBuffer(IMessagesSource src)
		{
			this.source = src;
		}

		public SourceBuffer(SourceBuffer other)
		{
			this.source = other.source;
			this.beginPosition = other.beginPosition;
			this.endPosition = other.endPosition;
			this.lines.AddRange(other.lines);
			this.loggableId = source.LogSourceHint?.ConnectionId ?? this.GetHashCode().ToString("x8");
		}

		public IMessagesSource Source { get { return source; } }

		public string LoggableId { get { return loggableId; }}

		/// Position of the first message in the buffer 
		/// or, if buffer is empty, log source's BEGIN/END depending on whether buffer is above/below 
		/// currently viewed time respectively.
		public long BeginPosition { get { return beginPosition; } }
		/// Position of the message following the last message in the buffer
		/// or, if buffer is empty, log source's BEGIN/END depending on whether buffer is above/below 
		/// currently viewed time respectively.
		public long EndPosition { get { return endPosition; } }

		// todo: delete these
		public int UnnededTopMessages;
		public int CurrentIndex;

		public void Set(ScreenBufferLinesRange range)
		{
			lines.Clear();
			lines.AddRange(range.Lines);
			beginPosition = range.BeginPosition;
			endPosition = range.EndPosition;
		}

		public void Append(ScreenBufferLinesRange range)
		{
			Debug.Assert(endPosition == range.BeginPosition);
			lines.AddRange(range.Lines);
			endPosition = range.EndPosition;
		}

		public void Prepend(ScreenBufferLinesRange range)
		{
			Debug.Assert(beginPosition == range.EndPosition);
			lines.InsertRange(0, range.Lines);
			beginPosition = range.BeginPosition;
		}

		public void Finalize(int maxSz)
		{
			if (UnnededTopMessages != 0)
			{
				if (lines.Count > UnnededTopMessages)
				{
					var newBeginMsg = lines[UnnededTopMessages];
					beginPosition = newBeginMsg.Message.Position;
				}
				else
				{
					beginPosition = endPosition;
				}
				lines.RemoveRange(0, UnnededTopMessages);
				UnnededTopMessages = 0;
			}
			if (lines.Count > maxSz)
			{
				lines.RemoveRange(maxSz, lines.Count - maxSz);
				if (lines.Count > 0)
					endPosition = lines[lines.Count - 1].Message.EndPosition;
			}
			CurrentIndex = 0;
		}

		IEnumerable<IndexedMessage> IMessagesCollection.Forward (int begin, int end)
		{
			for (var i = begin; i != end; ++i)
				yield return new IndexedMessage(i, lines[i].Message);
		}

		IEnumerable<IndexedMessage> IMessagesCollection.Reverse (int begin, int end)
		{
			for (var i = begin; i != end; --i)
				yield return new IndexedMessage(i, lines[i].Message);
		}

		int IMessagesCollection.Count
		{
			get { return lines.Count; }
		}

		public int Count
		{
			get { return lines.Count; }
		}

		public DisplayLine Get(int idx)
		{
			return lines[idx];
		}

		public override string ToString()
		{
			return string.Format("[{0}, {1}), count={2}", beginPosition, endPosition, lines.Count);
		}

		readonly string loggableId;
		readonly IMessagesSource source;
		readonly List<DisplayLine> lines = new List<DisplayLine>();
		long beginPosition;
		long endPosition;
	};
};