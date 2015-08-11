using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System;

namespace LogJointTests
{
	[TestClass()]
	public class StreamTextAccessTest
	{
		class Str
		{
			StringBuilder buf = new StringBuilder();

			public Str Add(char c, int count)
			{
				buf.Append(c, count);
				return this;
			}

			public Str Add(char c)
			{
				return Add(c, 1);
			}

			public Stream ToStream(Encoding encoding)
			{
				Stream stream = new MemoryStream();
				byte[] b = encoding.GetBytes(buf.ToString());
				stream.Write(b, 0, b.Length);
				return stream;
			}

			public override string ToString()
			{
				return buf.ToString();
			}
		};

		static Str S()
		{
			return new Str();
		}

		static readonly int blockSz = TextStreamPositioningParams.Default.AlignmentBlockSize;

		static void TestCharPosMapping(StreamTextAccess sut, TextStreamPosition pos, int charIdx)
		{
			Assert.AreEqual(pos.Value, sut.CharIndexToPosition(charIdx).Value);
			Assert.AreEqual(charIdx, sut.PositionToCharIndex(pos));
		}

		[TestMethod()]
		public void AdvanceBufferTest_ASCII()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz).Add('b', blockSz).ToStream(Encoding.ASCII),
				Encoding.ASCII
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			Assert.AreEqual(new Str().Add('a', blockSz).ToString(), buf.BufferString);
			buf.Advance(blockSz - 5);
			Assert.AreEqual("aaaaabbbbb", buf.BufferString.Substring(0, 10));
		}

		[TestMethod()]
		public void AdvanceBufferTest_ReverseDirection_StreamLenIsMultipleOfAlignmentSize()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('1', blockSz).Add('2', blockSz).ToStream(Encoding.ASCII),
				Encoding.ASCII
			);
			buf.BeginReading(blockSz * 2, TextAccessDirection.Backward);
			Assert.AreEqual("", buf.BufferString);
			buf.Advance(0);
			Assert.AreEqual(new Str().Add('2', blockSz).ToString(), buf.BufferString);
			buf.Advance(blockSz);
			Assert.AreEqual(new Str().Add('1', blockSz).ToString(), buf.BufferString);
		}

		[TestMethod()]
		public void AdvanceBufferTest_ReverseDirection_StartFromBlockBoundary()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('1', blockSz).Add('2', blockSz).ToStream(Encoding.ASCII),
				Encoding.ASCII
			);
			buf.BeginReading(blockSz, TextAccessDirection.Backward);
			Assert.AreEqual("", buf.BufferString);
			buf.Advance(0);
			Assert.AreEqual(new Str().Add('1', blockSz).ToString(), buf.BufferString);
			Assert.IsFalse(buf.Advance(blockSz));
		}


		[TestMethod()]
		public void AdvanceBufferTest_UTF16()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz / 2).Add('b', blockSz / 2).ToStream(Encoding.Unicode),
				Encoding.Unicode
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			Assert.AreEqual(new Str().Add('a', blockSz/2).ToString(), buf.BufferString);
			buf.Advance(blockSz/2 - 5);
			Assert.AreEqual("aaaaabbbbb", buf.BufferString.Substring(0, 10));
		}

		[TestMethod()]
		public void AdvanceBufferTest_BufferEndsAtTheMiddleOfUTF8Char()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz - 1).Add('Θ').Add('b', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			Assert.AreEqual(new Str().Add('a', blockSz - 1).ToString(), buf.BufferString);
			buf.Advance(blockSz - 5);
			Assert.AreEqual("aaaaΘbbbbb", buf.BufferString.Substring(0, 10));
		}

		[TestMethod()]
		[ExpectedException(typeof(OverflowException))]
		public void AdvanceBufferTest_DetectOverflow()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz).Add('b', blockSz).Add('c', blockSz).Add('d', blockSz).Add('e', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			buf.Advance(0);
			buf.Advance(0);
			buf.Advance(0);
			buf.Advance(0);
			buf.Advance(0);
		}

		[TestMethod()]
		[ExpectedException(typeof(OverflowException))]
		public void AdvanceBufferTest_DetectOverflowReverse()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz).Add('b', blockSz).Add('c', blockSz).Add('d', blockSz).Add('e', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(blockSz * 4 + 123, TextAccessDirection.Backward);
			buf.Advance(0);
			buf.Advance(0);
			buf.Advance(0);
			buf.Advance(0);
			buf.Advance(0);
		}

		[TestMethod()]
		[ExpectedException(typeof(InvalidOperationException))]
		public void AdvanceBufferTest_ReadSessionMustBeStartedToAdvance()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.Advance(1);
		}

		[TestMethod()]
		public void LoadBufferTest_UTF8CharAtBlockBoundaryBelongsToNextBlock_Forward()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz - 1).Add('Θ').Add('b', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(blockSz, TextAccessDirection.Forward);
			Assert.AreEqual("Θbbbbbbbbb", buf.BufferString.Substring(0, 10));
		}

		[TestMethod()]
		public void LoadBufferTest_UTF8CharAtBlockBoundaryBelongsToNextBlock_Reversed()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz - 1).Add('Θ').Add('b', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(new TextStreamPosition(blockSz, 0).Value, TextAccessDirection.Backward);
			Assert.AreEqual("", buf.BufferString);
			buf.Advance(0);
			Assert.AreEqual(new Str().Add('a', blockSz - 1).ToString(), buf.BufferString);
		}

		[TestMethod()]
		public void LoadBufferTest_UTF8CharAtBlockBoundaryBelongsToNextBlock_Reversed2()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz - 1).Add('Θ').Add('b', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(new TextStreamPosition(blockSz, 1).Value, TextAccessDirection.Backward);
			Assert.AreEqual("Θ", buf.BufferString);
			buf.Advance(0);
			Assert.AreEqual(new Str().Add('a', blockSz - 1).Add('Θ').ToString(), buf.BufferString);
		}

		[TestMethod()]
		public void LoadBufferTest_UTF8CharAtBlockBoundaryBelongsToNextBlock_Reversed3()
		{
			StreamTextAccess buf = new StreamTextAccess(
				new Str().Add('a', blockSz - 1).Add('Θ').Add('b', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(new TextStreamPosition(blockSz, 1).Value, TextAccessDirection.Backward);
			Assert.AreEqual("Θ", buf.BufferString);
			buf.Advance(1);
			Assert.AreEqual(new Str().Add('a', blockSz - 1).ToString(), buf.BufferString);
		}

		[TestMethod()]
		public void LoadBufferTest_EndReached()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz).Add('b', 100).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			Assert.AreEqual(S().Add('a', blockSz).ToString(), buf.BufferString);
			Assert.IsTrue(buf.Advance(20));
			Assert.AreEqual(S().Add('a', blockSz-20).Add('b', 100).ToString(), buf.BufferString);
			Assert.IsFalse(buf.Advance(20));
		}

		[TestMethod()]
		public void LoadBufferTest_Reverse_StartReadingFromBeginning()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', 100).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Backward);
			Assert.AreEqual("", buf.BufferString);
			buf.Advance(0);
			Assert.AreEqual("", buf.BufferString);
		}

		[TestMethod()]
		public void LoadBufferTest_Reverse_StartReadingFromMiddle_BeginReached()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz).Add('b', 200).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(blockSz + 100, TextAccessDirection.Backward);
			Assert.AreEqual(S().Add('b', 100).ToString(), buf.BufferString);
			Assert.IsTrue(buf.Advance(10));
			Assert.AreEqual(S().Add('a', blockSz).Add('b', 90).ToString(), buf.BufferString);
			Assert.IsFalse(buf.Advance(90));
		}


		[TestMethod()]
		public void CharIndexToStreamPositionTest()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz).Add('b', 200).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			TestCharPosMapping(buf, new TextStreamPosition(0, 20), 20);
			Assert.IsTrue(buf.Advance(10));
			Assert.AreEqual(S().Add('a', blockSz-10).Add('b', 200).ToString(), buf.BufferString);
			TestCharPosMapping(buf, new TextStreamPosition(0, 20), 10);
			TestCharPosMapping(buf, new TextStreamPosition(blockSz, 20), blockSz+10);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CharIndexToStreamPositionTest_NegativeIdx()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			buf.CharIndexToPosition(-1);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void CharIndexToStreamPositionTest_TooBigIdx()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			buf.CharIndexToPosition(blockSz + 10);
		}

		[TestMethod()]
		[ExpectedException(typeof(ArgumentOutOfRangeException))]
		public void StreamPositionToCharIndexTest_IdxFromPrevBlock()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz*3).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			buf.Advance(100);
			buf.PositionToCharIndex(new TextStreamPosition(50));
		}

		[TestMethod()]
		public void StreamPositionToCharIndexTest_InvalidBigTextStreamPositionIsMappedToPastTheEndCharIndex()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz * 3).ToStream(Encoding.Unicode),
				Encoding.Unicode
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			// valid Unicode text stream positions are from 0 to blockSz/2. below is invalid position.
			var invalidTextStreamPosition = new TextStreamPosition(blockSz - 10);
			Assert.AreEqual(blockSz/2, buf.PositionToCharIndex(invalidTextStreamPosition));
		}

		[TestMethod()]
		public void CharIndexToStreamPositionTest_Reversed()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz).Add('b', 200).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(blockSz + 200, TextAccessDirection.Backward);
			TestCharPosMapping(buf, new TextStreamPosition(blockSz, 20), 20);
			Assert.IsTrue(buf.Advance(10));
			TestCharPosMapping(buf, new TextStreamPosition(blockSz, 20), blockSz + 20);
			TestCharPosMapping(buf, new TextStreamPosition(20), 20);
		}

		[TestMethod()]
		public void ChangeDirectionTest()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz+100).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(20, TextAccessDirection.Forward);
			buf.Advance(10);
			Assert.AreEqual(buf.AdvanceDirection, TextAccessDirection.Forward);
			buf.EndReading();

			buf.BeginReading(20, TextAccessDirection.Backward);
			buf.Advance(10);
			Assert.AreEqual(buf.AdvanceDirection, TextAccessDirection.Backward);
			buf.EndReading();
		}

		[TestMethod()]
		[ExpectedException(typeof(InvalidOperationException))]
		public void NestedBeginReadingSessionNotAllowed()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz + 100).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(2, TextAccessDirection.Forward);
			buf.BeginReading(2, TextAccessDirection.Forward);
		}

		[TestMethod()]
		[ExpectedException(typeof(InvalidOperationException))]
		public void NotPairedEndReadSessionMustFail()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz + 100).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.EndReading();
		}

		[TestMethod()]
		public void StartPositionAtFirstBlock()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', 100).Add('b', 100).Add('c', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(50, TextAccessDirection.Forward);
			Assert.AreEqual(S().Add('a', 50).Add('b', 100).Add('c', blockSz - 200).ToString(), buf.BufferString);
			TestCharPosMapping(buf, new TextStreamPosition(50), 0);
			TestCharPosMapping(buf, new TextStreamPosition(100), 50);
		}


		[TestMethod()]
		public void EndStreamAtFirstBlock()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', 100).Add('b', 100).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(10, TextAccessDirection.Forward);
			Assert.AreEqual(S().Add('a', 90).Add('b', 100).ToString(), buf.BufferString);
			TestCharPosMapping(buf, new TextStreamPosition(10), 0);
			TestCharPosMapping(buf, new TextStreamPosition(60), 50);
			TestCharPosMapping(buf, new TextStreamPosition(200), 190);
		}

		[TestMethod()]
		public void GettingPositionOfPastTheEndCharIsAllowed()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', 100).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			TestCharPosMapping(buf, new TextStreamPosition(100), 100);
		}

		[TestMethod()]
		public void GettingPositionOfPastTheEndCharIsAllowed_ZeroLengthBuffer()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', 0).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(0, TextAccessDirection.Forward);
			TestCharPosMapping(buf, new TextStreamPosition(0), 0);
		}

		[TestMethod()]
		public void StartPositionPointsToNonExistingCharachter_Forward()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', 10).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(11, TextAccessDirection.Forward);
			Assert.AreEqual("", buf.BufferString);
		}

		[TestMethod()]
		public void StartPositionPointsToNonExistingCharachter_Backward()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', 10).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(11, TextAccessDirection.Backward);
			Assert.AreEqual(S().Add('a', 10).ToString(), buf.BufferString);
			int idx = buf.PositionToCharIndex(new TextStreamPosition(11));
		}

		[TestMethod()]
		public void StartPositionPointsToNonExistingCharachter_Backward_SecondBlock()
		{
			StreamTextAccess buf = new StreamTextAccess(
				S().Add('a', blockSz).Add('b', blockSz).ToStream(Encoding.UTF8),
				Encoding.UTF8
			);
			buf.BeginReading(blockSz*2, TextAccessDirection.Backward);
			Assert.AreEqual("", buf.BufferString);
			int idx = buf.PositionToCharIndex(new TextStreamPosition(blockSz*2));
			Assert.AreEqual(0, idx);
		}
	}
}
