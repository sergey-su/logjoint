using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogJoint
{
	/// <summary>
	/// Implements buffered reading from text stream and provides mapping from character index
	/// in the buffer to TextStreamPosition. TextStreamBuffer solves 'char-position-to-stream-position'
	/// problem (see TextStreamPosition for details).
	/// </summary>
	class TextStreamBuffer
	{
		public TextStreamBuffer(Stream stream, Encoding streamEncoding, TextStreamPosition initialPosition)
		{
			if (stream == null)
				throw new ArgumentNullException("stream");
			if (streamEncoding == null)
				throw new ArgumentNullException("streamEncoding");

			this.stream = stream;
			this.encoding = streamEncoding;
			this.decoder = this.encoding.GetDecoder();
			this.maxBytesPerChar = this.encoding.GetMaxByteCount(1);
			this.binaryBuffer = new byte[binaryBufferSize];
			this.charBuffer = new char[binaryBufferSize];
			this.textBuffer = new StringBuilder(textBufferCapacity);
			LoadBufferByPosition(initialPosition);
		}

		public Stream UnderlyingStream
		{
			get { return stream; }
		}

		public Encoding StreamEncoding
		{
			get { return encoding; }
		}

		/// <summary>
		/// Loads the buffer with peice of text that contains the character pointer by <paramref name="position"/>.
		/// To access the character in question use <code>TextBuffer[position.CharPositionInsideBuffer]</code>.
		/// Data is loaded from the underlying stream.
		/// </summary>
		/// <param name="position"></param>
		public void LoadBufferByPosition(TextStreamPosition position)
		{
			TextStreamPosition beginPosObj = position;
			long posAlignedToBufferSize = beginPosObj.StreamPositionAlignedToBlockSize;

			decoder.Reset();
			if (posAlignedToBufferSize != 0)
			{
				stream.Position = posAlignedToBufferSize - maxBytesPerChar;
				stream.Read(binaryBuffer, 0, maxBytesPerChar);
				decoder.GetChars(binaryBuffer, 0, maxBytesPerChar, charBuffer, 0);
			}
			else
			{
				stream.Position = posAlignedToBufferSize;
			}

			textBuffer.Length = 0;
			textBufferAsString = "";

			AdvanceBuffer(0);
		}

		/// <summary>
		/// Shifts the buffer by reading the next block from the underlying stream.
		/// <paramref name="charsToDiscard"/> characters from the beginning of the current buffer 
		/// will be discarded.
		/// </summary>
		/// <param name="charsToDiscard">Amount of already laoded characters to be discarder</param>
		/// <returns>true if buffer successfully adnvanced, false if there is no more data in the stream.</returns>
		public bool AdvanceBuffer(int charsToDiscard)
		{
			if (charsToDiscard < 0)
				throw new InvalidOperationException("Buffer cannot be moved back");
			if (charsToDiscard > textBufferAsString.Length)
				throw new InvalidOperationException("Buffer cannot ne moved by the distance greater than current buffer size");

			long stmPos = stream.Position;

			int bytesRead = stream.Read(binaryBuffer, 0, binaryBufferSize);
			int charsDecoded = decoder.GetChars(binaryBuffer, 0, bytesRead, charBuffer, 0);

			if (charsDecoded == 0)
				return false;

			if (textBuffer.Length - charsToDiscard + charsDecoded > textBufferCapacity)
			{
				// Rollback the change in position
				stream.Position = stmPos;
				// And fail
				throw new InvalidOperationException("Distance to move buffer is too small that caused buffer overflow");
			}

			// Current block becomes 'previous'
			totalCharactersInPrevBlock = textBufferAsString.Length - charactersLeftFromPrevBlock;
			charactersLeftFromPrevBlock = textBufferAsString.Length - charsToDiscard;

			// Remove unneded chars of prev block
			textBuffer.Remove(0, charsToDiscard);
			// Appending chars of new current block
			textBuffer.Append(charBuffer, 0, charsDecoded);
			// Capturing current text buffer
			textBufferAsString = textBuffer.ToString();

			// Assigning stream position of current block
			streamPositionAlignedToBufferSize = stmPos;

			return true;
		}

		/// <summary>
		/// Returns current text buffer
		/// </summary>
		public string TextBuffer
		{
			get { return textBufferAsString; }
		}

		/// <summary>
		/// Returns TextStreamPosition that represents absolute position of <paramref name="idx"/>th char in the buffer.
		/// </summary>
		public TextStreamPosition CharIndexToStreamPosition(int idx)
		{
			if (idx < charactersLeftFromPrevBlock)
				return new TextStreamPosition(streamPositionAlignedToBufferSize - TextStreamPosition.AlignmentBlockSize, totalCharactersInPrevBlock - charactersLeftFromPrevBlock + idx);
			else
				return new TextStreamPosition(streamPositionAlignedToBufferSize, idx - charactersLeftFromPrevBlock);
		}

		static public int MaxTextBufferSize
		{
			get { return textBufferCapacity; }
		}

		#region Implementation

		const int binaryBufferSize = TextStreamPosition.AlignmentBlockSize;
		const int textBufferCapacity = 2 * binaryBufferSize;

		readonly Stream stream; // underling stream
		readonly Encoding encoding; // text in the stream encoding
		readonly Decoder decoder; // converts byte array to characters array
		readonly int maxBytesPerChar; // encoding specific max char size in bytes
		readonly byte[] binaryBuffer; // raw bytes are read here
		readonly char[] charBuffer; // decoder converts raw bytes to chars and stores here
		readonly StringBuilder textBuffer; /* converts char array to string, stores current text buffer exposed publically
		                                      textBuffer contain characters from the current (latest read) block 
		                                      preceeded by characters that are left from previously read block. */
		string textBufferAsString = ""; /* caches string returned by textBuffer.ToString()
		                                   note each time StringBuilder.ToString() is called it generates 
		                                   a new string (checked in .net 2) which is unefficient. */
		long streamPositionAlignedToBufferSize; // stream position where current stream block has been read from
		int charactersLeftFromPrevBlock; // first charactersLeftFromPrevBlock characters in textBuffer are from previously block
		int totalCharactersInPrevBlock; // how many сharacters were in previous block

		#endregion
	}
}
