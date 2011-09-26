using System;
using System.Diagnostics;

namespace LogJoint
{
	/// <summary>
	/// <para>
	/// Represents a character position in a text stream. It helps work around
	/// 'char-position-to-stream-position' problem. 'char-position-to-stream-position' problem 
	/// has to do with the fact that generally it is impossible to effectively map a character position to 
	/// its appropriate byte position within text stream due to encodings with variable char length (like utf-8).
	/// Log viewer needs this mapping to be able to store absolute log line position (MessageBase.Position). 
	/// Message's absolute position is used to locate the message again in the stream later.
	/// </para>
	/// 
	/// <para>
	/// In order to illustrate 'char-position-to-stream-position' problem 
	/// suppose you have read a 10000-chars string from the middle of a text stream. 
	/// Suppose you are using UTF-8 and you have consumed 10500 bytes from the stream. 
	/// Log line was found at character 6000 within the string.Problem is: what number can 
	/// stored as this message's position? Note that byte position of 6000th character cannot 
	/// be calculated effectively.
	/// </para>
	/// 
	/// <para>
	/// TextStreamPosition implements the following approach: text stream must be read
	/// and decoded to text by blocks of fixed size in bytes (TextStreamPosition.AlignmentBlockSize). 
	/// Stream position must be aligned to block size before reading the block.
	/// Character position is a combination of the absolute block index and character position inside the
	/// text that was decoded from this block. 
	/// </para>
	/// 
	/// <para>
	/// Block may start at the middle of multi-byte character.
	/// This character must be considered a part of the block. See the implementation of StreamTextAccess
	/// to see how this character is read.
	/// </para>
	/// </summary>
	public struct TextStreamPosition
	{
		public const int AlignmentBlockSize = 32 * 1024; // value choosen so that (new char[AlignmentBlockSize]) doesn't get to LOH
		const long TextPosMask = AlignmentBlockSize - 1;
		const long StreamPosMask = unchecked((long)(0xffffffffffffffff - TextPosMask));

		[DebuggerStepThrough]
		public TextStreamPosition(long streamPositionAlignedToBufferSize, int textPositionInsideBuffer)
		{
			if ((streamPositionAlignedToBufferSize & TextPosMask) != 0)
				throw new ArgumentException("Stream position must be aligned to buffer boundaries");
			if (streamPositionAlignedToBufferSize < 0)
				throw new ArgumentOutOfRangeException("streamPositionAlignedToBufferSize", "position cannot be negative");
			if (textPositionInsideBuffer < 0)
				throw new ArgumentOutOfRangeException("textPositionInsideBuffer", "text position cannot be negative");

			data = streamPositionAlignedToBufferSize + textPositionInsideBuffer;
		}

		public enum AlignMode
		{
			BeginningOfContainingBlock,
			NextBlock
		}

		[DebuggerStepThrough]
		public TextStreamPosition(long unalignedStreamPosition, AlignMode mode)
		{
			long containingBlockBegin = unalignedStreamPosition & StreamPosMask;
			if (mode == AlignMode.BeginningOfContainingBlock)
				data = containingBlockBegin;
			else
				data = containingBlockBegin + AlignmentBlockSize;
		}

		[DebuggerStepThrough]
		public TextStreamPosition(long positionValue)
		{
			if (positionValue < 0)
				throw new ArgumentOutOfRangeException("positionValue", "position cannot be negative");
			data = positionValue;
		}
		/// <summary>
		/// Part of the text position that is in fact the stream position of 
		/// a block that contains the character this position points to.
		/// The value is a valid "stream position" and a valid "text stream position"
		/// at the same time.
		/// </summary>
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
}
