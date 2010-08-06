using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.IO;
using System.Diagnostics;

namespace LogJoint
{
	public interface ITextFileStreamHost
	{
		Encoding DetectEncoding(TextFileStreamBase stream);
		long BeginPosition { get; }
		long EndPosition { get; }
	};

	public struct TextStreamPosition
	{
		public const int AlignmentBlockSize = 64 * 1024;
		const long TextPosMask = AlignmentBlockSize - 1;
		const long StreamPosMask = unchecked((long)(0xffffffffffffffff - TextPosMask));

		[DebuggerStepThrough]
		public TextStreamPosition(long streamPositionAlignedToBufferSize, int textPositionInsideBuffer)
		{
			if ((streamPositionAlignedToBufferSize & TextPosMask) != 0)
				throw new ArgumentException("Stream position must be aligned to buffer boundaries");

			data = streamPositionAlignedToBufferSize + textPositionInsideBuffer;
		}

		[DebuggerStepThrough]
		public TextStreamPosition(long positionValue)
		{
			data = positionValue;
		}
		public long StreamPositionAlignedToBlockSize
		{
			[DebuggerStepThrough]
			get { return data & StreamPosMask; }
		}
		public int CharPositionInsideBuffer
		{
			[DebuggerStepThrough]
			get { return unchecked((int)(data & TextPosMask)); }
		}
		public long Value
		{
			[DebuggerStepThrough]
			get { return data; }
		}

		long data;
	};

	public class TextFileStreamBase : DelegatingStream
	{
		const int BinaryBufferSize = TextStreamPosition.AlignmentBlockSize;
		const int TextBufferSize = BinaryBufferSize;

		public const long MaximumMessageSize = TextBufferSize;

		readonly ITextFileStreamHost host;
		readonly ILogMedia media;
		readonly Regex headerRe;
		readonly Encoding encoding;
		readonly StringBuilder buf;
		readonly byte[] binBuf;
		readonly char[] charBuf;
		readonly Decoder decoder;
		readonly int maxBytesPerChar;

		bool isReading;
		FileRange.Range? currentRange;

		string bufString = "";
		int headerEnd;
		int headerStart;
		int prevHeaderEnd;
		int bufferOrigin;
		long streamPos;
		Match currMessageStart;

		public TextFileStreamBase(ILogMedia media, Regex headerRe, ITextFileStreamHost host)
			: base()
		{
			this.media = media;

			base.SetStream(media.DataStream, false);

			this.host = host;
			this.headerRe = headerRe;

			this.encoding = host.DetectEncoding(this);

			this.decoder = this.encoding.GetDecoder();
			this.maxBytesPerChar = this.encoding.GetMaxByteCount(1);

			binBuf = new byte[BinaryBufferSize];
			charBuf = new char[TextBufferSize];
			buf = new StringBuilder(TextBufferSize);

			SetTextPositionInternal(0);
		}

		public void BeginReadSession(FileRange.Range? range, long startPosition)
		{
			if (isReading)
				throw new InvalidOperationException("Cannot start more than one reading session for a single stream");

			if (range.HasValue)
			{
				FileRange.Range r = range.Value;
				if (r.End > host.EndPosition
				 || r.Begin < host.BeginPosition)
				{
					throw new ArgumentOutOfRangeException(
						string.Format("Value passed is out of available stream range. value={0}. avaialble={1}",
							r, new FileRange.Range(host.BeginPosition, host.EndPosition)));
				}
			}

			isReading = true;
			currentRange = range;

			SetTextPositionInternal(startPosition);
		}

		public void EndReadSession()
		{
			if (!isReading)
				throw new InvalidOperationException("No reading session is started for the the steam. Nothing to end.");
			isReading = false;
			currentRange = null;
		}

		int MoveBuffer()
		{
			long stmPos = this.Position;

			bufferOrigin = bufString.Length - headerEnd;

			int bytesRead = this.Read(binBuf, 0, BinaryBufferSize);
			int charsDecoded = decoder.GetChars(binBuf, 0, bytesRead, charBuf, 0);

			if (charsDecoded == 0)
				return 0;

			streamPos = stmPos;

			int ret = headerEnd;
			buf.Remove(0, headerEnd);
			buf.Append(charBuf, 0, charsDecoded);
			bufString = buf.ToString();

			headerEnd -= ret;
			headerStart -= ret;
			prevHeaderEnd -= ret;

			return ret;
		}

		void SetTextPositionInternal(long value)
		{
			if (value < 0 || value > host.EndPosition)
				throw new ArgumentOutOfRangeException("value", "Position is out of range BeginPosition-EndPosition");

			TextStreamPosition beginPosObj = new TextStreamPosition(value);
			long posAlignedToBufferSize = beginPosObj.StreamPositionAlignedToBlockSize;

			decoder.Reset();
			if (posAlignedToBufferSize != 0)
			{
				this.Position = posAlignedToBufferSize - maxBytesPerChar;
				this.Read(binBuf, 0, maxBytesPerChar);
				decoder.GetChars(binBuf, 0, maxBytesPerChar, charBuf, 0);
			}
			else
			{
				this.Position = posAlignedToBufferSize;
			}

			headerEnd = 0;

			buf.Length = 0;
			bufString = "";

			MoveBuffer();

			headerEnd = Math.Min(beginPosObj.CharPositionInsideBuffer, bufString.Length);

			FindNextMessageStart();
		}

		Match FindNextMessageStart()
		{
			Match m = headerRe.Match(bufString, headerEnd);
			if (!m.Success)
			{
				if (MoveBuffer() != 0)
				{
					m = headerRe.Match(bufString, headerEnd);
				}
			}
			if (m.Success)
			{
				if (m.Length == 0)
				{
					// This is protection againts header regexps that can match empty strings.
					// Normally, FindNextMessageStart() returns null when it has reached the end of the stream
					// because the regex can't find the next line. The problem is that regex can be composed so
					// that is can match empty strings. In that case without this check we would never 
					// stop parsing the stream producing more and more empty messages.

					throw new Exception("Error in regular expression: empty string matched");
				}

				prevHeaderEnd = headerEnd;

				headerStart = m.Index;
				headerEnd = m.Index + m.Length;

				currMessageStart = m;

			}
			else
			{
				currMessageStart = null;
			}
			return currMessageStart;
		}

		public Encoding TextEncoding
		{
			get { return encoding; }
		}

		public override int Read(byte[] array, int offset, int count)
		{
			count = CheckEndPositionStopCondition(count);

			return base.Read(array, offset, count);
		}

		public override int ReadByte()
		{
			int count = CheckEndPositionStopCondition(1);

			int rv = -1;
			if (count == 1)
				rv = base.ReadByte();

			return rv;
		}

		int CheckEndPositionStopCondition(int count)
		{
			long end;
			if (currentRange.HasValue)
			{
				end = currentRange.Value.End;
			}
			else if (host != null)
			{
				end = host.EndPosition;
			}
			else
			{
				return count;
			}
			if (Position + count > end)
			{
				count = (int)(end - Position);
				if (count < 0)
					count = 0;
			}

			return count;
		}

		TextStreamPosition CharIndexToStreamPosition(int idx)
		{
			return new TextStreamPosition(streamPos, idx - bufferOrigin);
		}

		public class TextMessageCapture
		{
			public readonly string HeadBuffer;
			public readonly Match HeaderMatch;
			public readonly TextStreamPosition BeginStreamPosition;

			public readonly string BodyBuffer;
			public readonly int BodyIndex;
			public readonly int BodyLength;
			public readonly TextStreamPosition EndStreamPosition;

			public readonly bool IsLastMessage;

			public TextMessageCapture(
				string headerBuffer,
				Match headerMatch,
				TextStreamPosition beginPos,
				string bodyBuffer,
				int bodyIdx,
				int bodyLen,
				TextStreamPosition endPos,
				bool isLastMessage)
			{
				HeadBuffer = headerBuffer;
				HeaderMatch = headerMatch;
				BeginStreamPosition = beginPos;

				BodyBuffer = bodyBuffer;
				BodyIndex = bodyIdx;
				BodyLength = bodyLen;

				EndStreamPosition = endPos;
				IsLastMessage = isLastMessage;
			}
		};

		public bool CurrentMessageIsEmpty
		{
			get { return currMessageStart == null; }
		}

		public TextMessageCapture GetCurrentMessageAndMoveToNextOne()
		{
			if (currMessageStart == null)
				return null;

			string headerBuffer = bufString;
			Match headerMatch = currMessageStart;
			TextStreamPosition beginPos = CharIndexToStreamPosition(headerStart);

			if (FindNextMessageStart() != null)
			{
				return new TextMessageCapture(
					headerBuffer, headerMatch, beginPos,
					bufString, prevHeaderEnd, headerStart - prevHeaderEnd, CharIndexToStreamPosition(headerStart),
					false
				);
			}
			else
			{
				return new TextMessageCapture(
					headerBuffer, headerMatch, beginPos,
					bufString, headerEnd, bufString.Length - headerEnd, CharIndexToStreamPosition(bufString.Length),
					true
				);
			}
		}

		public DateTime LastModified
		{
			get { return media.LastModified; }
		}

	};
}
