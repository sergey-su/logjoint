using System;
using System.Diagnostics;
using System.Xml.Linq;

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
	/// Log line was found at character 6000 within the string. Problem is: what number can be
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

		[DebuggerStepThrough]
		public TextStreamPosition(long streamPositionAlignedToBufferSize, int textPositionInsideBuffer) :
			this(streamPositionAlignedToBufferSize, textPositionInsideBuffer, TextStreamPositioningParams.Default)
		{
		}

		[DebuggerStepThrough]
		public TextStreamPosition(long streamPositionAlignedToBufferSize, int textPositionInsideBuffer, TextStreamPositioningParams positioningParams)
		{
			if ((streamPositionAlignedToBufferSize & positioningParams.TextPosMask) != 0)
				throw new ArgumentException("Stream position must be aligned to buffer boundaries");
			if (streamPositionAlignedToBufferSize < 0)
				throw new ArgumentOutOfRangeException("streamPositionAlignedToBufferSize", "position cannot be negative");
			if (textPositionInsideBuffer < 0)
				throw new ArgumentOutOfRangeException("textPositionInsideBuffer", "text position cannot be negative");

			this.positioningParams = positioningParams;
			this.data = streamPositionAlignedToBufferSize + textPositionInsideBuffer;
		}

		public enum AlignMode
		{
			BeginningOfContainingBlock,
			NextBlock
		}

		[DebuggerStepThrough]
		public TextStreamPosition(long unalignedStreamPosition, AlignMode mode)
			: this(unalignedStreamPosition, mode, TextStreamPositioningParams.Default)
		{ }

		[DebuggerStepThrough]
		public TextStreamPosition(long unalignedStreamPosition, AlignMode mode, TextStreamPositioningParams positioningParams)
		{
			this.positioningParams = positioningParams;
			long containingBlockBegin = unalignedStreamPosition & positioningParams.StreamPosMask;
			if (mode == AlignMode.BeginningOfContainingBlock)
				data = containingBlockBegin;
			else
				data = containingBlockBegin + positioningParams.AlignmentBlockSize;
		}

		[DebuggerStepThrough]
		public TextStreamPosition(long positionValue) :
			this(positionValue, TextStreamPositioningParams.Default)
		{ }

		[DebuggerStepThrough]
		public TextStreamPosition(long positionValue, TextStreamPositioningParams positioningParams)
		{
			if (positionValue < 0)
				throw new ArgumentOutOfRangeException("positionValue", "position cannot be negative");
			this.positioningParams = positioningParams;
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
			get { return data & positioningParams.StreamPosMask; }
		}
		public int CharPositionInsideBuffer
		{
			[DebuggerStepThrough]
			get { return unchecked((int)(data & positioningParams.TextPosMask)); }
		}
		public long Value
		{
			[DebuggerStepThrough]
			get { return data; }
		}

		readonly TextStreamPositioningParams positioningParams;
		readonly long data;
	};

	public class TextStreamPositioningParams
	{
		public const int MinimumAlignmentBlockSize = 16 * 1024;
		public const int MaximiumAlignmentBlockSize = 256 * 1024;
		/// <summary>
		/// Value choosen so that (new char[AlignmentBlockSize]) doesn't get to LOH
		/// </summary>
		public const int DefaultAlignmentBlockSize = 32 * 1024;
		
		public readonly int AlignmentBlockSize;
		public readonly long TextPosMask;
		public readonly long StreamPosMask;

		public readonly static TextStreamPositioningParams Default = new TextStreamPositioningParams(DefaultAlignmentBlockSize);

		public TextStreamPositioningParams(int alignmentBlockSize)
		{
			if (!IsValidAlignmentBlockSize(alignmentBlockSize))
				throw new ArgumentException("invalid alignmentBlockSize");

			AlignmentBlockSize = alignmentBlockSize;
			TextPosMask = AlignmentBlockSize - 1;
			StreamPosMask = unchecked((long)0xffffffffffffffff - TextPosMask);
		}

		public static TextStreamPositioningParams FromConfigNode(XElement e)
		{
			int maxMsgSz;
			if (int.TryParse(e.AttributeValue("max-message-size"), out maxMsgSz))
				if (maxMsgSz > 0)
					return new TextStreamPositioningParams(GetNearestValidAlignmentBlockSize(maxMsgSz * 1024));
			return Default;
		}

		static bool IsValidAlignmentBlockSize(int alignmentBlockSize)
		{
			return GetNearestValidAlignmentBlockSize(alignmentBlockSize) == alignmentBlockSize;
		}

		static int GetNearestValidAlignmentBlockSize(int testSizeInBytes)
		{
			for (int i = MinimumAlignmentBlockSize; i <= MaximiumAlignmentBlockSize; i = i * 2)
				if (i >= testSizeInBytes)
					return i;
			return MaximiumAlignmentBlockSize;
		}
	};
}
