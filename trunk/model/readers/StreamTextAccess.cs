using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogJoint
{
	/// <summary>
	/// Implements buffered reading from text stream and provides mapping from character index
	/// in the buffer to TextStreamPosition. StreamTextAccess solves 'char-position-to-stream-position'
	/// problem (see TextStreamPosition for details).
	/// </summary>
	/// <remarks>
	/// Usually StreamTextAccess reads data from the stream at positions aligned to TextStreamPosition.AlignmentBlockSize  
	/// by blocks of size TextStreamPosition.AlignmentBlockSize. However if the encoding requires more than 1 byte per 
	/// character extra bytes are read from the beginning of a block. By that StreamTextAccess implements the following rule:
	///    If a (multibyte) characher starts at block i and has at least one byte at block block i+1 - the character belongs to block i+1.
	/// </remarks>
	public class StreamTextAccess: ITextAccess
	{
		public StreamTextAccess(Stream stream, Encoding streamEncoding, TextStreamPositioningParams textStreamPositioningParams)
		{
			if (stream == null)
				throw new ArgumentNullException(nameof (stream));
			if (streamEncoding == null)
				throw new ArgumentNullException(nameof (streamEncoding));

			this.textStreamPositioningParams = textStreamPositioningParams;
			this.binaryBufferSize = textStreamPositioningParams.AlignmentBlockSize;
			this.maximumSequentialAdvancesAllowed = 4;
			this.textBufferCapacity = maximumSequentialAdvancesAllowed * binaryBufferSize;

			this.stream = stream;
			this.encoding = streamEncoding;
			this.decoder = this.encoding.GetDecoder();
			this.maxBytesPerChar = GetMaxBytesPerChar(this.encoding);
			this.binaryBuffer = new byte[binaryBufferSize];
			this.charBuffer = new char[binaryBufferSize];
			this.textBuffer = new char[textBufferCapacity];
			this.iterator = new TextAccessIterator(this);
		}

		public StreamTextAccess(Stream stream, Encoding streamEncoding) :
			this(stream, streamEncoding, TextStreamPositioningParams.Default)
		{
		}

		public Stream UnderlyingStream
		{
			get { return stream; }
		}

		public Encoding StreamEncoding
		{
			get { return encoding; }
		}

		public bool ReadingStarted
		{
			get { return readingStarted; }
		}

		/// <summary>
		/// Creates valid TextStreamPosition object that points to the charachter that starts at or contains 
		/// the byte defined by <paramref name="streamPosition"/>
		/// </summary>
		/// <param name="streamPosition">Stream position. In other words 0-based byte index in stream's data.</param>
		/// <param name="streamEncoding">Manadatory encoding information of the stream</param>
		/// <param name="stream"></param>
		/// <returns>Valid TextStreamPosition object</returns>
		public static async Task<TextStreamPosition> StreamPositionToTextStreamPosition(long streamPosition, Encoding streamEncoding, Stream stream, TextStreamPositioningParams textStreamPositioningParams)
		{
			if (streamEncoding == null)
				throw new ArgumentNullException("streamEncoding");

			TextStreamPosition tmp = new TextStreamPosition(streamPosition, textStreamPositioningParams);

#if !SILVERLIGHT
			if (streamEncoding.IsSingleByte)
				return tmp;
#endif
			if (streamEncoding == Encoding.Unicode || streamEncoding == Encoding.BigEndianUnicode)
				return new TextStreamPosition(tmp.StreamPositionAlignedToBlockSize, tmp.CharPositionInsideBuffer / 2, textStreamPositioningParams);
#if !SILVERLIGHT
			if (streamEncoding == Encoding.UTF32)
				return new TextStreamPosition(tmp.StreamPositionAlignedToBlockSize, tmp.CharPositionInsideBuffer / 4, textStreamPositioningParams);
#endif

			if (stream == null)
				throw new ArgumentNullException("stream object is required to determine text stream position with given encoding", "stream");

			var boundedStream = new BoundedStream();
			boundedStream.SetStream(stream, false);
			boundedStream.SetBounds(null, streamPosition);
			StreamTextAccess tmpTextAccess = new StreamTextAccess(boundedStream, streamEncoding, textStreamPositioningParams);
			await tmpTextAccess.BeginReading(tmp.StreamPositionAlignedToBlockSize, TextAccessDirection.Forward);
			tmp = tmpTextAccess.CharIndexToPosition(tmpTextAccess.BufferString.Length);
			tmpTextAccess.EndReading();

			return tmp;
		}

		/// <summary>
		/// Prepares object for sequential reading from the stream.
		/// The object is loaded with peice of text that contains the character pointer by <paramref name="initialPosition"/>.
		/// Data is loaded from the underlying stream.
		/// </summary>
		public async Task BeginReading(long initialPosition, TextAccessDirection direction)
		{
			if (readingStarted)
				throw new InvalidOperationException("Cannot start reading session. Another reading session has already been started");
			if (initialPosition < 0)
				throw new ArgumentException("Negative positions are not allowed", "initialPosition");

			readingStarted = true;

			this.direction = direction;
			this.startPosition = new TextStreamPosition(initialPosition, textStreamPositioningParams);

			streamPositionToReadFromNextTime = new TextStreamPosition(initialPosition, textStreamPositioningParams).StreamPositionAlignedToBlockSize;
			streamPositionAlignedToBufferSize = streamPositionToReadFromNextTime;
			decoderNeedsReloading = EncodingNeedsReloading();

			textBufferLength = 0;
			textBufferAsString = "";

			charsCutFromBeginningOfTextBuffer = 0;
			charsCutFromEndOfTextBuffer = 0;

			await AdvanceBufferInternal(0);
		}

		/// <summary>
		/// End reading session previously started with BeginReadingSession()
		/// </summary>
		public void EndReading()
		{
			if (!readingStarted)
				throw new InvalidOperationException("Cannot end reading session. No reading session has been started");
			readingStarted = false;
		}

		/// <summary>
		/// Shifts the buffer by reading the next block from the underlying stream.
		/// <paramref name="charsToDiscard"/> characters from the beginning of the current buffer 
		/// will be discarded.
		/// </summary>
		/// <param name="charsToDiscard">Amount of already laoded characters to be discarder</param>
		/// <returns>true if buffer successfully adnvanced, false if there is no more data in the stream.</returns>
		public ValueTask<bool> Advance(int charsToDiscard)
		{
			return AdvanceBufferInternal(charsToDiscard);
		}

		/// <summary>
		/// Returns current text buffer
		/// </summary>
		public string BufferString
		{
			get { return textBufferAsString; }
		}

		public TextAccessDirection AdvanceDirection
		{
			get { CheckIsReading(); return direction; }
		}

		/// <summary>
		/// Returns TextStreamPosition that represents absolute position of <paramref name="idx"/>th char in the buffer.
		/// </summary>
		public TextStreamPosition CharIndexToPosition(int idx)
		{
			CheckIsReading();

			if (idx < 0 || idx > textBufferAsString.Length)
				throw new ArgumentOutOfRangeException("char index is out of range");

			idx += charsCutFromBeginningOfTextBuffer;

			if (direction == TextAccessDirection.Backward)
			{
				int tmp = textBufferLength - charactersLeftFromPrevBlock;
				if (idx > tmp)
					return new TextStreamPosition(streamPositionAlignedToBufferSize + textStreamPositioningParams.AlignmentBlockSize, idx - tmp, textStreamPositioningParams);
				else
					return new TextStreamPosition(streamPositionAlignedToBufferSize, idx, textStreamPositioningParams);
			}
			else
			{
				if (idx < charactersLeftFromPrevBlock)
					return new TextStreamPosition(streamPositionAlignedToBufferSize - textStreamPositioningParams.AlignmentBlockSize, totalCharactersInPrevBlock - charactersLeftFromPrevBlock + idx, textStreamPositioningParams);
				else
					return new TextStreamPosition(streamPositionAlignedToBufferSize, idx - charactersLeftFromPrevBlock, textStreamPositioningParams);
			}
		}

		/// <summary>
		/// Returns char index in BufferString string that has absolute position <paramref name="pos"/>
		/// </summary>
		public int PositionToCharIndex(TextStreamPosition pos)
		{
			int? tmp = null;
			if (direction == TextAccessDirection.Backward)
			{
				if (pos.StreamPositionAlignedToBlockSize == streamPositionAlignedToBufferSize)
					tmp = pos.CharPositionInsideBuffer;
				else if (pos.StreamPositionAlignedToBlockSize == streamPositionAlignedToBufferSize + textStreamPositioningParams.AlignmentBlockSize)
					tmp = pos.CharPositionInsideBuffer + textBufferLength - charactersLeftFromPrevBlock;
			}
			else
			{
				if (pos.StreamPositionAlignedToBlockSize == streamPositionAlignedToBufferSize)
					tmp = pos.CharPositionInsideBuffer + charactersLeftFromPrevBlock;
				else if (pos.StreamPositionAlignedToBlockSize == streamPositionAlignedToBufferSize - textStreamPositioningParams.AlignmentBlockSize)
					tmp = pos.CharPositionInsideBuffer - totalCharactersInPrevBlock + charactersLeftFromPrevBlock;
			}
			if (tmp != null)
			{
				int ret = tmp.Value - charsCutFromBeginningOfTextBuffer;
				if (ret > textBufferAsString.Length)
					return textBufferAsString.Length;
				if (ret >= 0)
					return ret;
			}
			throw new ArgumentOutOfRangeException("position maps to the character that doesn't belong to current buffer");
		}

		public int MaxTextBufferSize
		{
			get { return textBufferCapacity; }
		}

		#region ITextAccess members

		public async Task<ITextAccessIterator> OpenIterator(long initialPosition, TextAccessDirection direction)
		{
			if (readingStarted)
				throw new InvalidOperationException("Another iterator already exists. Dispose it first.");
			await BeginReading(initialPosition, direction);
			iterator.Open();
			return iterator;
		}

		public int AverageBufferLength 
		{
			get { return textStreamPositioningParams.AlignmentBlockSize / 4; }
		}

		public int MaximumSequentialAdvancesAllowed
		{
			get { return maximumSequentialAdvancesAllowed; }
		}

		#endregion

		#region Implementation

		async ValueTask<bool> AdvanceBufferInternal(int charsToDiscard)
		{
			CheckIsReading();

			if (charsToDiscard < 0)
				throw new ArgumentOutOfRangeException(nameof (charsToDiscard), "charsToDiscard must be greater or equal to zero");
			if (charsToDiscard > textBufferAsString.Length)
				throw new ArgumentOutOfRangeException(nameof (charsToDiscard), "Buffer cannot be moved by the distance greater than current buffer size");

			if (!CheckPreconditionsToMoveBuffer())
				return false;
			await PositionateStreamAndReloadDecoderIfNeeded();
			int charsDecoded = await ReadAndDecodeNextBinaryBlock();
			if (charsDecoded == 0)
			{
				AdvanceStreamPositionToReadFromNextTime();
				return false;
			}
			DetectOverflow(charsToDiscard, charsDecoded);
			MoveBufferInternal(charsToDiscard, charsDecoded);
			CaptureTextBufferAsString();

			return true;
		}

		bool CheckPreconditionsToMoveBuffer()
		{
			if (direction == TextAccessDirection.Backward)
				return streamPositionToReadFromNextTime >= 0;
			return true;
		}

		bool EncodingNeedsReloading()
		{
#if !SILVERLIGHT
			return !encoding.IsSingleByte;
#else
			return true;
#endif
		}

		async ValueTask PositionateStreamAndReloadDecoderIfNeeded()
		{
			if (decoderNeedsReloading)
			{
				decoder.Reset();
				if (streamPositionToReadFromNextTime != 0 && maxBytesPerChar > 1)
				{
					stream.Position = streamPositionToReadFromNextTime - maxBytesPerChar;
					await stream.ReadAsync(binaryBuffer, 0, maxBytesPerChar);
					decoder.GetChars(binaryBuffer, 0, maxBytesPerChar, charBuffer, 0);
					// Decoder reloaded and stream has right position. Leaving.
					return;
				}
			}

			// Stream.Position may have unifficient implementation. Check to avoid unneded work.
			// Note: normally when reading forward the position is updated automatically to correct value by Stream.Read()
			if (stream.Position != streamPositionToReadFromNextTime)
				stream.Position = streamPositionToReadFromNextTime;
		}

		async ValueTask<int> ReadAndDecodeNextBinaryBlock()
		{
			int bytesRead = await stream.ReadAsync(binaryBuffer, 0, binaryBufferSize);
			int charsDecoded = decoder.GetChars(binaryBuffer, 0, bytesRead, charBuffer, 0);

			return charsDecoded;
		}

		void RemoveFromTextBuffer(int startIndex, int length)
		{
			Array.Copy(textBuffer, startIndex + length, textBuffer, startIndex, textBufferLength - (startIndex + length));
			textBufferLength -= length;
		}

		void InsertToBeginninOfTextBuffer(char[] value, int charCount)
		{
			Array.Copy(textBuffer, 0, textBuffer, charCount, textBufferLength);
			Array.Copy(value, textBuffer, charCount);
			textBufferLength += charCount;
			//textBuffer.Insert(0, charBuffer, 0, charsDecoded);
		}

		void AppendToTextBuffer(char[] value, int charCount)
		{
			Array.Copy(value, 0, textBuffer, textBufferLength, charCount);
			textBufferLength += charCount;
			//textBuffer.Append(charBuffer, 0, charsDecoded);
		}

		void MoveBufferInternal(int charsToDiscard, int charsDecoded)
		{
			// Current block becomes 'previous'
			totalCharactersInPrevBlock = textBufferLength - charactersLeftFromPrevBlock;

			if (direction == TextAccessDirection.Backward)
			{
				int charsToRemoveFromTheEnd = charsToDiscard + charsCutFromEndOfTextBuffer;
				charactersLeftFromPrevBlock = textBufferLength - charsToRemoveFromTheEnd;
				// Remove unneded chars of prev block
				RemoveFromTextBuffer(textBufferLength - charsToRemoveFromTheEnd, charsToRemoveFromTheEnd);
				// Insert chars of new current block
				InsertToBeginninOfTextBuffer(charBuffer, charsDecoded);
			}
			else
			{
				int charsToRemoveFromTheBeginning = charsToDiscard + charsCutFromBeginningOfTextBuffer;
				charactersLeftFromPrevBlock = textBufferLength - charsToRemoveFromTheBeginning;
				// Remove unneded chars of prev block
				RemoveFromTextBuffer(0, charsToRemoveFromTheBeginning);
				// Appending chars of new current block
				AppendToTextBuffer(charBuffer, charsDecoded);
			}
			
			// Assigning stream position of current block
			streamPositionAlignedToBufferSize = streamPositionToReadFromNextTime;

			// Update stream position for the next block
			AdvanceStreamPositionToReadFromNextTime();

			decoderNeedsReloading = EncodingNeedsReloading() && (direction == TextAccessDirection.Backward);
		}

		private void AdvanceStreamPositionToReadFromNextTime()
		{
			if (direction == TextAccessDirection.Backward)
				streamPositionToReadFromNextTime -= binaryBufferSize;
			else
				streamPositionToReadFromNextTime += binaryBufferSize;
		}

		private void CaptureTextBufferAsString()
		{
			int len = textBufferLength;
			int beginIdx = 0;
			int endIdx = len;
			bool subStringMode = false;
			if (startPosition.StreamPositionAlignedToBlockSize == streamPositionAlignedToBufferSize)
			{
				subStringMode = true;
				Debug.Assert(charactersLeftFromPrevBlock == 0);
				if (direction == TextAccessDirection.Forward)
				{
					beginIdx = Math.Min(startPosition.CharPositionInsideBuffer, endIdx);
					charsCutFromBeginningOfTextBuffer = beginIdx;
				}
				else
				{
					endIdx = Math.Min(startPosition.CharPositionInsideBuffer, endIdx);
					charsCutFromEndOfTextBuffer = len - endIdx;
				}
			}
			if (subStringMode)
			{
				textBufferAsString = new string(textBuffer, beginIdx, endIdx - beginIdx);
				//textBuffer.ToString(beginIdx, endIdx - beginIdx);
			}
			else
			{
				textBufferAsString = new string(textBuffer, 0, textBufferLength);
				charsCutFromBeginningOfTextBuffer = 0;
				charsCutFromEndOfTextBuffer = 0;
			}
		}

		void DetectOverflow(int charsToDiscard, int charsDecoded)
		{
			if (textBufferLength - charsToDiscard + charsDecoded > textBufferCapacity)
			{
				// Rollback the change of position
				stream.Position = streamPositionToReadFromNextTime;
				// And fail
				throw new OverflowException("Distance to move buffer is too small that caused buffer overflow");
			}
		}

		void CheckIsReading()
		{
			if (!readingStarted)
				throw new InvalidOperationException("TextAccess object has no reading session open. "+
					"Call BeginReadingSession() first");
		}

		static int GetMaxBytesPerChar(Encoding encoding)
		{
#if !SILVERLIGHT
			if (encoding.IsSingleByte)
				return 1;
#endif
			return encoding.GetMaxByteCount(1) - encoding.GetPreamble().Length;
		}

		class TextAccessIterator : ITextAccessIterator
		{
			public TextAccessIterator(StreamTextAccess impl)
			{
				this.impl = impl;
			}

			public void Open()
			{
				disposed = false;
			}

			#region ITextAccessIterator Members

			public string CurrentBuffer
			{
				get { CheckDisposed(); return impl.BufferString; }
			}

			public long CharIndexToPosition(int idx)
			{
				CheckDisposed();
				return impl.CharIndexToPosition(idx).Value;
			}

			public int PositionToCharIndex(long position)
			{
				CheckDisposed();
				return impl.PositionToCharIndex(new TextStreamPosition(position, impl.textStreamPositioningParams));
			}

			public TextAccessDirection AdvanceDirection
			{
				get { CheckDisposed(); return impl.AdvanceDirection; }
			}

			public ValueTask<bool> Advance(int charsToDiscard)
			{
				CheckDisposed();
				return impl.Advance(charsToDiscard);
			}

			#endregion

			#region IDisposable Members

			public void Dispose()
			{
				if (!disposed)
				{
					disposed = true;
					impl.EndReading();
				}
			}

			#endregion

			void CheckDisposed()
			{
				if (disposed)
					throw new ObjectDisposedException("ITextAccessIterator");
			}

			StreamTextAccess impl;
			bool disposed;
		};

		readonly TextStreamPositioningParams textStreamPositioningParams;
		readonly int binaryBufferSize;
		readonly int maximumSequentialAdvancesAllowed;
		readonly int textBufferCapacity;

		readonly Stream stream; // underling stream
		readonly Encoding encoding; // stream encoding
		readonly Decoder decoder; // converts byte array to characters array
		readonly int maxBytesPerChar; // encoding specific max char size in bytes
		readonly byte[] binaryBuffer; // raw bytes are read here
		readonly char[] charBuffer; // decoder converts raw bytes to chars and stores here
		char[] textBuffer; /* stores current text buffer.
		                            textBuffer contain characters from the current (latest read) block 
		                            preceeded (or followed in reverse mode) by characters 
									that are left from previously read block. */
		int textBufferLength;
		string textBufferAsString; /* caches string (or a substring) returned by textBuffer.ToString().
		                                   note each time StringBuilder.ToString() is called it generates 
		                                   a new string (checked in .net 2) which is unefficient. */
		TextAccessDirection direction; // direction to advance the buffer
		TextStreamPosition startPosition; // TextStreamPosition that reading started from
		bool readingStarted; // buffer was loaded at least once
		long streamPositionToReadFromNextTime; // stores stream position internally that allows external code to change underlying stream's position
		bool decoderNeedsReloading; // when reading next block - decoder needs to be reset and reloaded
		long streamPositionAlignedToBufferSize; // stream position where current stream block has been read from
		int charactersLeftFromPrevBlock; /* first (forward mode) or last (backward mode) charactersLeftFromPrevBlock characters in textBuffer 
		                                    are from previous block */
		int totalCharactersInPrevBlock; // how many сharacters were in the previous block
		int charsCutFromBeginningOfTextBuffer; // how many сharacters at the beginning of textBuffer are not included to textBufferAsString because of startPosition limitation
		int charsCutFromEndOfTextBuffer; // how many сharacters at the end of textBuffer are not included to textBufferAsString because of startPosition limitation
		readonly TextAccessIterator iterator; // iterator object to be returned by OpenIterator

		#endregion
	}
}
