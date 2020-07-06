using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using LogJoint.RegularExpressions;
using System.Threading.Tasks;
using System.Runtime.CompilerServices;

namespace LogJoint
{
	[Flags]
	public enum MessagesSplitterFlags
	{
		None = 0,
		Default = 0,
		PreventBufferUnderflow = 1
	};

	public class TextMessageCapture
	{
		public string HeaderBuffer;
		public IMatch HeaderMatch;
		public long BeginPosition;

		public string BodyBuffer;
		public int BodyIndex;
		public int BodyLength;
		public long EndPosition;

		public bool IsLastMessage;

		public string MessageHeader { get { return HeaderBuffer.Substring(HeaderMatch.Index, HeaderMatch.Length); } }
		public string MessageBody { get { return BodyBuffer.Substring(BodyIndex, BodyLength); } }
		public StringSlice MessageHeaderSlice { get { return new StringSlice(HeaderBuffer, HeaderMatch.Index, HeaderMatch.Length); } }
		public StringSlice MessageBodySlice { get { return new StringSlice(BodyBuffer, BodyIndex, BodyLength); } }
	};

	/// <summary>
	/// Splits a text stream (accessed through ITextAccess) to messages. A message consists of its header
	/// and body. Header is detected by a regular expression (MessageHeaderRegex). 
	/// Absolute positions of message begin and end characters are detected.
	/// Supports forward and backward modes.
	/// </summary>
	public interface IMessagesSplitter
	{
		IRegex MessageHeaderRegex { get; }
		Task BeginSplittingSession(FileRange.Range range, long startPosition, MessagesParserDirection direction);
		void EndSplittingSession();
		bool CurrentMessageIsEmpty { get; }
		ValueTask<bool> GetCurrentMessageAndMoveToNextOne(TextMessageCapture capture);
	};

	public class MessagesSplitter : IMessagesSplitter
	{
		public MessagesSplitter(ITextAccess textAccess, IRegex messageHeaderRe, MessagesSplitterFlags flags = MessagesSplitterFlags.Default)
			: base()
		{
			if (textAccess == null)
				throw new ArgumentNullException(nameof (textAccess));
			if (messageHeaderRe == null)
				throw new ArgumentNullException(nameof (messageHeaderRe));
			if ((messageHeaderRe.Options & ReOptions.RightToLeft) != 0)
				throw new ArgumentException("Header regular expression must not be RightToLeft");
			if (textAccess.MaximumSequentialAdvancesAllowed < 3)
				throw new ArgumentException("ITextAccess implementation must allow 3 or more sequential advances", "textAccess");

			this.textAccess = textAccess;
			this.forwardModeRe = messageHeaderRe;
			this.bufferLengthThreshold = 0;
			if ((flags & MessagesSplitterFlags.PreventBufferUnderflow) != 0)
			{
				this.bufferLengthThreshold = textAccess.AverageBufferLength / 4;
			}
		}

		public IRegex MessageHeaderRegex
		{
			get { return forwardModeRe; }
		}

		public async Task BeginSplittingSession(FileRange.Range range, long startPosition, MessagesParserDirection direction)
		{
			if (sessionIsOpen)
				throw new InvalidOperationException("Cannot start more than one reading session for a single splitter");

			try
			{
				await TryBeginSplittingSession(range, startPosition, direction);
			}
			catch
			{
				ReadingSessionCleanup();
				throw;
			}

			sessionIsOpen = true;
		}

		public void EndSplittingSession()
		{
			if (!sessionIsOpen)
				throw new InvalidOperationException("No reading session is started for the splitter. Nothing to end.");
			sessionIsOpen = false;
			ReadingSessionCleanup();
		}

		public bool CurrentMessageIsEmpty
		{
			get { return CurrentMessageHeaderIsEmpty; }
		}

		public async ValueTask<bool> GetCurrentMessageAndMoveToNextOne(TextMessageCapture capture)
		{
			if (capture == null)
				throw new ArgumentNullException("capture");

			CheckIsReading();

			if (CurrentMessageHeaderIsEmpty)
				return false;

			if (capture.HeaderMatch == null || capture.HeaderMatch.OwnerRegex != re)
				capture.HeaderMatch = re.CreateEmptyMatch();

			if (direction == MessagesParserDirection.Forward)
				return await GetCurrentMessageAndMoveToNextOneFwd(capture);
			else
				return await GetCurrentMessageAndMoveToNextOneBwd(capture);
		}


#region Implementation
		async ValueTask<bool> MoveBuffer(int distance)
		{
			bool moved = await textIterator.Advance(distance);
			if (moved)
				UpdateCachedCurrentBuffer();
			return moved;
		}

		bool MatchHeader()
		{
			int startIdx = headerPointer1;
			if (quickNewLineOptimizationAvailable && headersCounter > 0)
			{
				startIdx = FindNewLine(cachedCurrentBuffer, cachedCurrentBuffer.Length, startIdx);
			}
			return re.Match(cachedCurrentBuffer, startIdx, ref currentMessageHeaderMatch);
		}

		static int FindNewLine(string p, int len, int startIndex)
		{
			int idx = startIndex;
			for (; idx < len; ++idx)
			{
				var c = p[idx];
				if (c == '\r' || c == '\n')
				{
					++idx;
					if (c == '\r' && idx < len && p[idx] == '\n')
						++idx;
					break;
				}
			}
			return idx;
		}

		bool ItsTimeToMoveBuffer()
		{
			if (direction == MessagesParserDirection.Forward)
				return (cachedCurrentBuffer.Length - headerPointer1) < bufferLengthThreshold;
			else
				return headerPointer1 < bufferLengthThreshold;
		}

		async ValueTask<bool> FindNextMessageStart()
		{
			bool timeToMoveBuffer = ItsTimeToMoveBuffer();
			bool matched = false;

			if (!timeToMoveBuffer)
			{
				matched = MatchHeader();
				timeToMoveBuffer = !matched;
			}

			if (timeToMoveBuffer)
			{
				if (direction == MessagesParserDirection.Forward)
				{
					if (await MoveBuffer(headerPointer1))
						headerPointer1 = 0;
				}
				else
				{
					if (await MoveBuffer(cachedCurrentBuffer.Length - prevHeaderPointer1))
						headerPointer1 += (cachedCurrentBuffer.Length - prevHeaderPointer1);
				}

				matched = MatchHeader();
			}
			if (matched)
			{
				var m = currentMessageHeaderMatch;

				if (m.Length == 0)
				{
					// This is protection againts header regexps that can match empty strings.
					// Normally, FindNextMessageStart() returns null when it has reached the end of the stream
					// because the regex can't find the next line. The problem is that regex can be composed so
					// that is can match empty strings. In that case without this check we would never 
					// stop parsing the stream producing more and more empty messages.

					throw new Exception("Error in regular expression: empty string matched");
				}

				++headersCounter;

				prevHeaderPointer1 = headerPointer1;

				if (direction == MessagesParserDirection.Forward)
				{
					headerPointer2 = m.Index;
					headerPointer1 = m.Index + m.Length;
					UpdateHeaderBeginPosition(headerPointer2);
				}
				else
				{
					headerPointer1 = m.Index;
					headerPointer2 = m.Index + m.Length;
					UpdateHeaderBeginPosition(headerPointer1);
				}
			}
			return matched;
		}

		void UpdateHeaderBeginPosition(int beginCharIdx)
		{
			prevHeaderBeginPosition = headerBeginPosition;
			headerBeginPosition = textIterator.CharIndexToPosition(beginCharIdx);
		}

		bool CurrentMessageHeaderIsEmpty
		{
			get { return currentMessageHeaderMatch == null || !currentMessageHeaderMatch.Success; }
		}

		void CheckIsReading()
		{
			if (!sessionIsOpen)
				throw new InvalidOperationException("Operation is not allowed when no reading session is open");
		}

		void SetCurrentDirection(MessagesParserDirection direction)
		{
			this.direction = direction;
			if (direction == MessagesParserDirection.Forward)
			{
				re = forwardModeRe;
				if (forwardModeMatch == null)
					forwardModeMatch = re.CreateEmptyMatch();
				currentMessageHeaderMatch = forwardModeMatch;

				// It happened that grepping newline with ^ is expensive.
				// It's cheaper to manually find newline in the input string and 
				// then run the regexp starting from found position.
				// This flag enables manual search.
				quickNewLineOptimizationAvailable = re.Pattern.StartsWith("^");
			}
			else
			{
				if (backwardModeRe == null)
					backwardModeRe = forwardModeRe.Factory.Create(forwardModeRe.Pattern, forwardModeRe.Options | ReOptions.RightToLeft);
				re = backwardModeRe;
				if (backwardModeMatch == null)
					backwardModeMatch = re.CreateEmptyMatch();
				currentMessageHeaderMatch = backwardModeMatch;
				quickNewLineOptimizationAvailable = false;
			}
		}

		async Task TryBeginSplittingSession(FileRange.Range range, long startPosition, MessagesParserDirection direction)
		{
			bool posIsOutOfRange = DetectOutOfRangeCondition(range, startPosition, direction);

			if (!posIsOutOfRange)
			{
				TextAccessDirection accessDirection = direction == MessagesParserDirection.Forward ?
					TextAccessDirection.Forward : TextAccessDirection.Backward;

				textIterator = await textAccess.OpenIterator(startPosition, accessDirection);

				try
				{
					headerPointer1 = textIterator.PositionToCharIndex(startPosition);
					prevHeaderPointer1 = headerPointer1;
				}
				catch (ArgumentOutOfRangeException)
				{
					posIsOutOfRange = true;
				}
			}

			headersCounter = 0;

			if (posIsOutOfRange)
			{
				this.range = new FileRange.Range();
				SetCachedCurrentBuffer("");
				currentMessageHeaderMatch = null;
				ReadingSessionCleanup();
			}
			else
			{
				this.range = range;
				SetCurrentDirection(direction);
				UpdateCachedCurrentBuffer();
				await FindNextMessageStart();
			}
		}

		static bool DetectOutOfRangeCondition(FileRange.Range range, long startPosition, MessagesParserDirection direction)
		{
			bool posIsOutOfRange = !range.IsInRange(startPosition);

			if (posIsOutOfRange
			 && direction == MessagesParserDirection.Backward
			 && startPosition == range.End)
			{
				// it's ok to start reading from end position when we move backward
				posIsOutOfRange = false;
			}

			return posIsOutOfRange;
		}

		void SetCachedCurrentBuffer(string value)
		{
			cachedCurrentBuffer = value;
		}

		void UpdateCachedCurrentBuffer()
		{
			SetCachedCurrentBuffer(textIterator.CurrentBuffer);
		}

		void ReadingSessionCleanup()
		{
			if (textIterator != null)
			{
				textIterator.Dispose();
				textIterator = null;
			}
		}

		async ValueTask<bool> GetCurrentMessageAndMoveToNextOneFwd(TextMessageCapture capture)
		{
			if (headerBeginPosition >= range.End)
				return false;

			capture.HeaderBuffer = cachedCurrentBuffer;
			capture.HeaderMatch.CopyFrom(currentMessageHeaderMatch);
			capture.BeginPosition = headerBeginPosition;

			bool nextMessageFound = await FindNextMessageStart();
			
			capture.BodyBuffer = cachedCurrentBuffer;

			if (nextMessageFound)
			{
				capture.BodyIndex = prevHeaderPointer1;
				capture.BodyLength = headerPointer2 - prevHeaderPointer1;
				capture.EndPosition = headerBeginPosition;
				capture.IsLastMessage = capture.EndPosition >= range.End;
			}
			else
			{
				capture.BodyIndex = headerPointer1;
				capture.BodyLength = capture.BodyBuffer.Length - headerPointer1;
				capture.EndPosition = textIterator.CharIndexToPosition(capture.BodyBuffer.Length);
				if (capture.EndPosition > range.End)
				{
					capture.EndPosition = range.End;
					capture.BodyLength = Math.Max(0, capture.BodyLength - (capture.BodyBuffer.Length - textIterator.PositionToCharIndex(range.End)));
				}
				capture.IsLastMessage = true;
			}

			return true;
		}

		async ValueTask<bool> GetCurrentMessageAndMoveToNextOneBwd(TextMessageCapture capture)
		{
			int headerEnd = headerPointer2;
			long captureEndPos;

			if (headersCounter == 1) // first message when reading backward is the last message
				captureEndPos = textIterator.CharIndexToPosition(prevHeaderPointer1);
			else
				captureEndPos = prevHeaderBeginPosition;

			if (captureEndPos <= range.Begin)
				return false;

			capture.HeaderBuffer = cachedCurrentBuffer;
			capture.HeaderMatch.CopyFrom(currentMessageHeaderMatch);
			capture.BeginPosition = headerBeginPosition;
			capture.EndPosition = captureEndPos;
			capture.BodyBuffer = cachedCurrentBuffer;
			capture.BodyIndex = headerEnd;

			if (headersCounter == 1) // first message when reading backward is the last message
			{
				capture.BodyLength = prevHeaderPointer1 - headerEnd;
				capture.IsLastMessage = true;
			}
			else
			{
				capture.BodyLength = prevHeaderPointer1 - headerEnd;
				capture.IsLastMessage = false;
			}

			await FindNextMessageStart();

			return true;
		}

		readonly ITextAccess textAccess;

		IRegex forwardModeRe, backwardModeRe;
		IMatch forwardModeMatch, backwardModeMatch;
		readonly int bufferLengthThreshold;

		bool sessionIsOpen;
		IRegex re;
		bool quickNewLineOptimizationAvailable;
		IMatch currentMessageHeaderMatch;
		MessagesParserDirection direction;
		FileRange.Range range;
		ITextAccessIterator textIterator;
		string cachedCurrentBuffer;

		// These fields relate to current message's header. Are idxs in cachedCurrentBuffer.
		int headerPointer1; // index of a) header's end in forward mode b) header's begin in backward mode
		int headerPointer2; // index of a) header's start in forward mode b) header's end in backward mode
		long headerBeginPosition; // position of header's begin (current message). obtained via ITextAccess.
		long prevHeaderBeginPosition; // position of header's begin (prev message). obtained via ITextAccess.

		// Those fields relate to prev message's header
		int prevHeaderPointer1; // same as headerPointer1 but for previously found message

		long headersCounter; // how many headers we have found so far
#endregion
	};

	/// <summary>
	/// Solves 'read-message-from-the-middle' problem. See the descirpion
	/// for IPositionedMessagesReader interface for details.
	/// ReadMessageFromTheMiddleProblem implements IMessagesSplitter. 
	/// It is a decorator for another IMessagesSplitter.
	/// </summary>
	public class ReadMessageFromTheMiddleProblem : IMessagesSplitter
	{
		public ReadMessageFromTheMiddleProblem(IMessagesSplitter underlyingSplitter)
		{
			if (underlyingSplitter == null)
				throw new ArgumentNullException("underlyingSplitter");

			this.underlyingSplitter = underlyingSplitter;
		}

		public IRegex MessageHeaderRegex
		{
			get { return underlyingSplitter.MessageHeaderRegex; }
		}

		public async Task BeginSplittingSession(FileRange.Range range, long startPosition, MessagesParserDirection direction)
		{
			if (direction == MessagesParserDirection.Forward)
			{
				if (startPosition > range.Begin)
				{
					long? fixedStartPosition = null;
					await underlyingSplitter.BeginSplittingSession(range, startPosition, MessagesParserDirection.Backward);
					try
					{
						TextMessageCapture capt = new TextMessageCapture();
						if (await underlyingSplitter.GetCurrentMessageAndMoveToNextOne(capt))
							fixedStartPosition = capt.EndPosition;
					}
					finally
					{
						underlyingSplitter.EndSplittingSession();
					}
					if (fixedStartPosition != null)
					{
						await underlyingSplitter.BeginSplittingSession(range, fixedStartPosition.Value, direction);
						try
						{
							TextMessageCapture capt = new TextMessageCapture();
							while (await underlyingSplitter.GetCurrentMessageAndMoveToNextOne(capt))
							{
								if (capt.BeginPosition >= startPosition)
									break;
								fixedStartPosition = capt.EndPosition;
							}
						}
						finally
						{
							underlyingSplitter.EndSplittingSession();
						}
						startPosition = fixedStartPosition.Value;
					}
				}
			}
			else
			{
				if (startPosition < range.End)
				{
					long? fixedStartPosition = null;
					await underlyingSplitter.BeginSplittingSession(range, startPosition, MessagesParserDirection.Forward);
					try
					{
						TextMessageCapture capt = new TextMessageCapture();
						if (await underlyingSplitter.GetCurrentMessageAndMoveToNextOne(capt))
							fixedStartPosition = capt.BeginPosition;
					}
					finally
					{
						underlyingSplitter.EndSplittingSession();
					}
					if (fixedStartPosition != null)
					{
						await underlyingSplitter.BeginSplittingSession(range, fixedStartPosition.Value, direction);
						try
						{
							TextMessageCapture capt = new TextMessageCapture();
							while (await underlyingSplitter.GetCurrentMessageAndMoveToNextOne(capt))
							{
								if (capt.EndPosition <= startPosition)
									break;
								fixedStartPosition = capt.BeginPosition;
							}
						}
						finally
						{
							underlyingSplitter.EndSplittingSession();
						}
						startPosition = fixedStartPosition.Value;
					}
				}
			}

			await underlyingSplitter.BeginSplittingSession(range, startPosition, direction);
		}

		public void EndSplittingSession()
		{
			underlyingSplitter.EndSplittingSession();
		}

		public bool CurrentMessageIsEmpty
		{
			get 
			{
				return underlyingSplitter.CurrentMessageIsEmpty;
			}
		}

		public ValueTask<bool> GetCurrentMessageAndMoveToNextOne(TextMessageCapture capture)
		{
			return underlyingSplitter.GetCurrentMessageAndMoveToNextOne(capture);
		}

		private readonly IMessagesSplitter underlyingSplitter;
	};
}
