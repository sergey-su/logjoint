using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.Analytics
{
	public interface IHeaderMatch
	{
		int Index { get; }
		int Length { get; }
	};

	public interface IHeaderMatcher
	{
		unsafe IHeaderMatch Match(char* pBuffer, int length, int startFrom, string buffer);
	};

	public class TextLogParser
	{
		public static async Task ParseStream(
			Stream inputStream,
			IHeaderMatcher headerMatcher,
			Func<List<MessageInfo>, Task<bool>> messagesSink,
			Action<double> progressHandler = null,
			Flags flags = Flags.None,
			int rawBufferSize = 1024 * 512
		)
		{
			inputStream.Position = 0;
			var totalLen = inputStream.Length;
			if (totalLen == 0)
				progressHandler = null;
			int bufferUnderflowThreshold = 1024 * 4;
			byte[] rawBytesBuffer = new byte[rawBufferSize];
			char[] rawCharsBuffer = new char[rawBufferSize];
			var messages = new List<MessageInfo>(5000);
			var buffer = new SlidingBuffer();
			int currentMessageIndex = 0;
			if ((flags & Flags.SkipDoubleBytePeamble) != 0)
				await inputStream.ReadAsync(rawBytesBuffer, 0, 2);
			for (; ; )
			{
				int bytesRead = await inputStream.ReadAsync(rawBytesBuffer, 0, rawBufferSize);
				if (bytesRead == 0)
					break;
				int charsCount = ((flags & Flags.UCS2) == 0) ?
					GetChars(rawBytesBuffer, bytesRead, rawCharsBuffer) :
					Encoding.Unicode.GetChars(rawBytesBuffer, 0, bytesRead, rawCharsBuffer, 0);
				buffer.Push(rawCharsBuffer, charsCount);
				int endOfProcessedTextPosition = HandleBufferContent(headerMatcher, buffer, messages,
					bufferUnderflowThreshold, ref currentMessageIndex, flags);
				buffer.Pop(endOfProcessedTextPosition);
				if (!await messagesSink(messages))
					break;
				messages.Clear();
				progressHandler?.Invoke((double)inputStream.Position / (double)totalLen);
			}
			buffer.Pop(HandleBufferContent(headerMatcher, buffer, messages, 0, ref currentMessageIndex, flags));
			YieldMessage(buffer, buffer.CurrentContent.Length, messages, ref currentMessageIndex);
			if (messages.Count != 0)
				await messagesSink(messages);
		}

		[Flags]
		public enum Flags
		{
			None = 0,
			UCS2 = 1,
			SkipDoubleBytePeamble = 2
		};

		public struct MessageInfo
		{
			public IHeaderMatch HeaderMatch;
			public int MessageIndex;
			public long StreamPosition;
			public string MessageBoby;
			public string Buffer;
		};

		static unsafe int GetChars(byte[] bytes, int byteCount, char[] chars)
		{
			fixed (byte* pBytes = bytes)
			fixed (char* pChars = chars)
			{
				byte* b = pBytes;
				char* c = pChars;
				for (int i = byteCount; i > 0; --i)
				{
					*c = unchecked((char)*b);
					++c;
					++b;
				}
			}
			return byteCount;
		}

		unsafe static int HandleBufferContent(IHeaderMatcher headerMatcher, SlidingBuffer buffer,
			List<MessageInfo> messages, int bufferUnderflowThreshold, ref int currentMessageIndex,
			Flags flags)
		{
			string content = buffer.CurrentContent;
			int contentLen = content.Length;
			fixed (char* contentPtr = content)
			{
				for (int currentPosition = 0; ; )
				{
					if (contentLen - currentPosition < bufferUnderflowThreshold)
						return currentPosition;
					var startIdx = currentMessageIndex > 0 ? 
						FindNewLine(contentPtr, contentLen, currentPosition) : currentPosition;
					var match = headerMatcher.Match(contentPtr, contentLen, startIdx, content);
					if (match == null)
						return currentPosition;

					YieldMessage(buffer, contentPtr, match.Index, messages, ref currentMessageIndex);

					var h = buffer.AllocatedMatchHeader;
					h.StreamPosition = buffer.ContentStreamPosition + match.Index;
					h.EndOfHeaderPosition = match.Index + match.Length;
					h.Match2 = match;
					h.Buffer = content;
					buffer.CurrentMessageHeader = h;

					if ((flags & Flags.UCS2) != 0)
					{
						int DefaultAlignmentBlockSize = 32 * 1024;
						var realStreamPos = h.StreamPosition * 2;
						if ((flags & Flags.SkipDoubleBytePeamble) != 0)
							realStreamPos += 2;
						h.StreamPosition =
							  (realStreamPos / DefaultAlignmentBlockSize) * DefaultAlignmentBlockSize
							+ (realStreamPos % DefaultAlignmentBlockSize) / 2;
					}

					currentPosition = match.Index + match.Length;
				}
			}
		}

		unsafe static int FindNewLine(char* p, int len, int startIndex)
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

		unsafe static int TrimBodyRight(char* p, int endOfBodyPosition, int endOfHeaderPosition)
		{
			int idx = endOfBodyPosition;
			while (idx > endOfHeaderPosition && char.IsWhiteSpace(p[idx - 1]))
				--idx;
			return idx;
		}

		unsafe static bool YieldMessage(SlidingBuffer buffer, int endOfBodyPosition, List<MessageInfo> messagesList, ref int currentMessageIndex)
		{
			fixed (char* contentPtr = buffer.CurrentContent)
				return YieldMessage(buffer, contentPtr, endOfBodyPosition, messagesList, ref currentMessageIndex);
		}

		unsafe static bool YieldMessage(SlidingBuffer buffer, char* contentPtr, int endOfBodyPosition, List<MessageInfo> messagesList, ref int currentMessageIndex)
		{
			if (buffer.CurrentMessageHeader != null)
			{
				var endOfHeaderPos = buffer.CurrentMessageHeader.EndOfHeaderPosition;
				var bodyLen = TrimBodyRight(contentPtr, endOfBodyPosition, endOfHeaderPos) - endOfHeaderPos;
				string messageBody = new string(contentPtr,
					buffer.CurrentMessageHeader.EndOfHeaderPosition,
					bodyLen
				);

				var h = buffer.CurrentMessageHeader;
				messagesList.Add(new MessageInfo()
				{
					HeaderMatch = h.Match2,
					MessageIndex = currentMessageIndex++,
					StreamPosition = h.StreamPosition,
					MessageBoby = messageBody,
					Buffer = h.Buffer
				});

				buffer.CurrentMessageHeader = null;
			}
			return true;
		}

		class MessageHeader
		{
			public long StreamPosition;
			public int EndOfHeaderPosition;
			public IHeaderMatch Match2;
			public string Buffer;
		};

		class SlidingBuffer
		{
			public string CurrentContent = "";
			public MessageHeader CurrentMessageHeader;
			public long ContentStreamPosition = 0;
			public MessageHeader AllocatedMatchHeader = new MessageHeader();

			public void Push(char[] chars, int count)
			{
				var sb = new StringBuilder(CurrentContent.Length + count + 1);
				sb.Append(CurrentContent);
				sb.Append(chars, 0, count);
				CurrentContent = sb.ToString();
			}

			public void Pop(int numberOfCharacters)
			{
				CurrentContent = CurrentContent.Substring(numberOfCharacters);
				if (CurrentMessageHeader != null)
					CurrentMessageHeader.EndOfHeaderPosition -= numberOfCharacters;
				ContentStreamPosition += numberOfCharacters;
			}
		};
	}

	public class RegexHeaderMatcher : IHeaderMatcher
	{
		readonly Regex re;

		public RegexHeaderMatcher(Regex re)
		{
			this.re = re;
		}

		unsafe IHeaderMatch IHeaderMatcher.Match(char* pBuffer, int length, int startFrom, string buffer)
		{
			var m = re.Match(buffer, startFrom);
			if (!m.Success)
				return null;
			return new RegexHeaderMatch(m);
		}
	};

	public class RegexHeaderMatch : IHeaderMatch
	{
		readonly Match m;

		public RegexHeaderMatch(Match m)
		{
			this.m = m;
		}

		public Match Match { get { return m; } }

		int IHeaderMatch.Index
		{
			get { return m.Index; }
		}

		int IHeaderMatch.Length
		{
			get { return m.Length; }
		}

		public override string ToString()
		{
			return m.ToString();
		}
	};
}
