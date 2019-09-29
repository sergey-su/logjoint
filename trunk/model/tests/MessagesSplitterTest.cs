using System;
using LogJoint;
using LogJoint.RegularExpressions;
using Rhino.Mocks;
using Range = LogJoint.FileRange.Range;
using NUnit.Framework;

namespace LogJoint.Tests
{
	delegate void SimpleDelegate();

	[TestFixture]
	public class MessagesSplitterTest
	{
		static IRegexFactory reFactory = LJRegexFactory.Instance;

		[Test]
		public void GetCurrentMessageAndMoveToNextOne_MainScenario_Forward()
		{
			MockRepository repo = new MockRepository();


			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None));
			repo.VerifyAll();

			
			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(0, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.PositionToCharIndex(0)).Return(0);
			Expect.Call(it.CurrentBuffer).Return(
			  "123456 abc 283147948 abc 3498");
			// |      |  |          |  |   |
			// 0      7  10         21 24  28 - char idx
			// 0      8  16         45 56  67 - positions
			Expect.Call(it.CharIndexToPosition(7)).Return((long)8);
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 100), 0, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(21)).Return((long)45);
			repo.ReplayAll();
			var capt = new TextMessageCapture();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length));
			Assert.AreEqual(" 283147948 ", capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength));
			Assert.AreEqual(8L, capt.BeginPosition);
			Assert.AreEqual(45L, capt.EndPosition);
			Assert.IsFalse(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.Advance(24)).Repeat.Once().Do((Predicate<int>)delegate(int i)
			{
				repo.Verify(it);
				repo.BackToRecord(it);
				Expect.Call(it.CurrentBuffer).Return(
				   " 3498 abc 2626277");
				//  |     |  |          
				//  0     6  9           - char idx
				//  56    72 81          - position
				Expect.Call(it.CharIndexToPosition(6)).Return((long)72);
				repo.Replay(it);
				return true;
			});
			repo.ReplayAll();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length));
			Assert.AreEqual(" 3498 ", capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength));
			Assert.AreEqual(45L, capt.BeginPosition);
			Assert.AreEqual(72L, capt.EndPosition);
			Assert.IsFalse(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.Advance(9)).Repeat.Once().Do((Predicate<int>)delegate(int i)
			{
				repo.Verify(it);
				repo.BackToRecord(it);
				Expect.Call(it.CurrentBuffer).Return(
				  " 2626277");
				// |       | 
				// 0       8 - char idx
				// 81      90  - position
				Expect.Call(it.CharIndexToPosition(8)).Return((long)90);
				repo.Replay(it);
				return true;
			});
			repo.ReplayAll();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length));
			Assert.AreEqual(" 2626277", capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength));
			Assert.AreEqual(72L, capt.BeginPosition);
			Assert.AreEqual(90L, capt.EndPosition);
			Assert.IsTrue(capt.IsLastMessage);
			Assert.IsTrue(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			repo.ReplayAll();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.IsTrue(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			it.Dispose(); Expect.On(it);
			repo.ReplayAll();
			target.EndSplittingSession();
			repo.VerifyAll();
		}

		[Test]
		public void GetCurrentMessageAndMoveToNextOne_MainScenario_Backward()
		{
			MockRepository repo = new MockRepository();


			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();

			repo.BackToRecordAll();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None));
			repo.VerifyAll();


			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(100, TextAccessDirection.Backward)).Return(it);
			Expect.Call(it.PositionToCharIndex(100)).Return(29);
			Expect.Call(it.CurrentBuffer).Return(
			  "123456 abc 283147948 abc 3498");
			// |      |  |          |  |    |   
			// 0      7  10         21 24   29  - char idx
			// 50     61 67         85 87   100 - position
			Expect.Call(it.CharIndexToPosition(21)).Return((long)85);
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 100), 100, MessagesParserDirection.Backward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(7)).Return((long)61);
			Expect.Call(it.CharIndexToPosition(29)).Return((long)100);
			repo.ReplayAll();
			var capt = new TextMessageCapture();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length));
			Assert.AreEqual(" 3498", capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength));
			Assert.AreEqual(85L, capt.BeginPosition);
			Assert.AreEqual(100L, capt.EndPosition);
			Assert.IsTrue(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.Advance(8)).Repeat.Once().Do((Predicate<int>)delegate(int i)
			{
				repo.Verify(it);
				repo.BackToRecord(it);
				Expect.Call(it.CurrentBuffer).Return(
				   "11 abc 123456 abc 283147948 ");
				//  |  |  |       |  |         |   
				//  0  3  6       14 17        27   - char idx
				//  20 33 50      61 67        85   - positions
				Expect.Call(it.CharIndexToPosition(3)).Return((long)33);
				repo.Replay(it);
				return true;
			});
			repo.ReplayAll();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length));
			Assert.AreEqual(" 283147948 ", capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength));
			Assert.AreEqual(61L, capt.BeginPosition);
			Assert.AreEqual(85L, capt.EndPosition);
			Assert.IsFalse(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);



			repo.BackToRecordAll();
			Expect.Call(it.Advance(14)).Repeat.Once().Do((Predicate<int>)delegate(int i)
			{
				repo.Verify(it);
				repo.BackToRecord(it);
				Expect.Call(it.CurrentBuffer).Return(
				   "11 abc 123456 ");
				//  |  |  |       | 
				//  0  3  6       13 - char idx
				//  20 33         61 - pos
				repo.Replay(it);
				return true;
			});
			repo.ReplayAll();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length));
			Assert.AreEqual(" 123456 ", capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength));
			Assert.AreEqual(33L, capt.BeginPosition);
			Assert.AreEqual(61L, capt.EndPosition);
			Assert.IsFalse(capt.IsLastMessage);
			Assert.IsTrue(target.CurrentMessageIsEmpty);



			repo.BackToRecordAll();
			repo.ReplayAll();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.IsTrue(target.CurrentMessageIsEmpty);



			repo.BackToRecordAll();
			it.Dispose(); Expect.On(it);
			repo.ReplayAll();
			target.EndSplittingSession();
			repo.VerifyAll();
		}

		[Test]
		public void BeginSplittingSession_WithStartPositionOutOfRange()
		{
			MockRepository repo = new MockRepository();


			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
			repo.VerifyAll();


			repo.BackToRecordAll();
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 100), 110, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsTrue(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			repo.ReplayAll();
			TextMessageCapture capt = new TextMessageCapture();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.IsTrue(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			repo.ReplayAll();
			target.EndSplittingSession();
			repo.VerifyAll();
		}


		[Test]
		public void BeginSplittingSession_WithStartPositionThatDoesntGetMappedToCharacterByTextAccess()
		{
			MockRepository repo = new MockRepository();


			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
			repo.VerifyAll();


			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(90, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.PositionToCharIndex(90)).Throw(new ArgumentOutOfRangeException());
			it.Dispose(); Expect.On(it);
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 100), 90, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsTrue(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			repo.ReplayAll();
			TextMessageCapture capt = new TextMessageCapture();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.IsTrue(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			repo.ReplayAll();
			target.EndSplittingSession();
			repo.VerifyAll();
		}

		[Test]
		public void BeginSplittingSession_TextIteratorMustBeCleanedUpInCaseOfException()
		{
			MockRepository repo = new MockRepository();


			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
			repo.VerifyAll();


			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(0, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.PositionToCharIndex(0)).Return(0);
			Expect.Call(it.CurrentBuffer).Throw(new System.Security.SecurityException());
			it.Dispose(); Expect.On(it);
			repo.ReplayAll();
			try
			{
				target.BeginSplittingSession(new Range(0, 100), 0, MessagesParserDirection.Forward);
				Assert.IsTrue(false, "We must never get here because of an exception in prev call");
			}
			catch (System.Security.SecurityException)
			{
			}
			repo.VerifyAll();
			Assert.IsTrue(target.CurrentMessageIsEmpty);
		}

		[Test]
		public void BeginSplittingSession_NestedSessionsAreNotAllowed()
		{
			MockRepository repo = new MockRepository();


			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
			repo.VerifyAll();


			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(0, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.PositionToCharIndex(0)).Return(0);
			Expect.Call(it.CurrentBuffer).Return("00 111 222");
			Expect.Call(it.CharIndexToPosition(3)).Return((long)3);
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 100), 0, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			Assert.Throws<InvalidOperationException>(()=>
			{
				target.BeginSplittingSession(new Range(0, 200), 0, MessagesParserDirection.Forward);
			});
		}

		[Test]
		public void HeaderReMustNotBeRightToLeft()
		{
			MockRepository repo = new MockRepository();
			Assert.Throws<ArgumentException>(()=> 
			{
				MessagesSplitter target = new MessagesSplitter(repo.CreateMock<ITextAccess>(), 
					reFactory.Create(@"111", ReOptions.RightToLeft));
			});
		}


		[Test]
		public void NotPairedEndSplittingSession()
		{
			MockRepository repo = new MockRepository();

			var ta = repo.CreateMock<ITextAccess>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
			target.BeginSplittingSession(new Range(0, 100), 200, MessagesParserDirection.Forward);
			target.EndSplittingSession();

			Assert.Throws<InvalidOperationException>(()=>
			{
				target.EndSplittingSession();
			});
		}


		[Test]
		public void GetCurrentMessageAndMoveToNextOneWhenNoOpenSession()
		{
			MockRepository repo = new MockRepository();
			var ta = repo.CreateMock<ITextAccess>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(100);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
			TextMessageCapture capt = new TextMessageCapture();
			Assert.Throws<InvalidOperationException>(()=>
			{
				target.GetCurrentMessageAndMoveToNextOne(capt);
			});
		}

		[Test]
		public void HeaderRegexMatchesPartOfAMessage_Forward()
		{
			MockRepository repo = new MockRepository();

			var capt = new TextMessageCapture();

			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.AverageBufferLength).Return(100);
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"ab(c)?", ReOptions.None), MessagesSplitterFlags.PreventBufferUnderflow);
			repo.VerifyAll();

			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(0, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.PositionToCharIndex(0)).Return(0);
			Expect.Call(it.CurrentBuffer).Return("ab");
			Expect.Call(it.Advance(0)).Repeat.Once().Do((Predicate<int>)delegate(int i)
			{
				repo.Verify(it);
				repo.BackToRecord(it);
				Expect.Call(it.CurrentBuffer).Return("abc_");
				Expect.Call(it.CharIndexToPosition(0)).Return((long)0);
				repo.Replay(it);
				return true;
			});
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 10), 0, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			Expect.Call(it.Advance(3)).Return(false);
			Expect.Call(it.CharIndexToPosition(4)).Return((long)3);
			repo.ReplayAll();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.MessageHeader);
			Assert.AreEqual("_", capt.MessageBody);
			Assert.IsTrue(target.CurrentMessageIsEmpty);
		}

		[Test]
		public void StartBackwardReadingFromAlmostEndPosition()
		{
			MockRepository repo = new MockRepository();


			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None));
			repo.VerifyAll();


			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(99, TextAccessDirection.Backward)).Return(it);
			Expect.Call(it.PositionToCharIndex(99)).Return(28);
			Expect.Call(it.CurrentBuffer).Return(
			  "123456 abc 283147948 abc 3498");
			// |      |  |          |  |   ||   
			// 0      7  10         21 24  28\29  - char idx
			// 50     61 67         85 87  99\100 - position
			Expect.Call(it.CharIndexToPosition(21)).Return((long)85);
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 100), 99, MessagesParserDirection.Backward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(7)).Return((long)61);
			Expect.Call(it.CharIndexToPosition(28)).Return((long)99);
			repo.ReplayAll();
			var capt = new TextMessageCapture();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("abc", capt.MessageHeader);
			Assert.AreEqual(" 349", capt.MessageBody);
			Assert.AreEqual(85L, capt.BeginPosition);
			Assert.AreEqual(99L, capt.EndPosition);
			Assert.IsTrue(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);
		}

		[Test]
		public void FirstTextBufferIsEmpty_Backward()
		{
			MockRepository repo = new MockRepository();

			var capt = new TextMessageCapture();

			int aveBufSize = 100;

			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			Expect.Call(ta.AverageBufferLength).Return(aveBufSize);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None), MessagesSplitterFlags.PreventBufferUnderflow);
			repo.VerifyAll();

			//        _abc
			//        ||  |
			// pos:  11|  15
			//         12
			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(15, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.CurrentBuffer).Return("");
			Expect.Call(it.PositionToCharIndex(15)).Return(0); // querying past-the end position is allowed
			Expect.Call(it.Advance(0)).Repeat.Once().Do((Predicate<int>)delegate(int i)
			{
				repo.Verify(it);
				repo.BackToRecord(it);
				Expect.Call(it.CurrentBuffer).Return("_abc");
				Expect.Call(it.CharIndexToPosition(1)).Return((long)12);
				repo.Replay(it);
				return true;
			});
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(10, 20), 15, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);
		}

		[Test]
		public void MessageIsNotReadBecauseItStartsAtTheEndOfTheRange_Forward()
		{
			MockRepository repo = new MockRepository();

			// reading from position 0 with range 0-6
			//   _msg1_msg2_msg3
			//   |     |
			//   0     6
			// range ends at pos 6 that is past-the-end position. msg2 shouldn't be read.

			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));
			repo.VerifyAll();

			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(0, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.PositionToCharIndex(0)).Return(0);
			Expect.Call(it.CharIndexToPosition(1)).Return((long)1);
			Expect.Call(it.CurrentBuffer).Return("_msg1_msg2_msg3");
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 6), 0, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(6)).Return((long)6);
			repo.ReplayAll();
			var capt = new TextMessageCapture();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("msg", capt.MessageHeader);
			Assert.AreEqual("1_", capt.MessageBody);
			Assert.AreEqual(1L, capt.BeginPosition);
			Assert.AreEqual(6L, capt.EndPosition);
			Assert.IsTrue(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			repo.ReplayAll();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
		}

		[Test]
		public void MessageIsReadBecauseItStartsRightBeforeTheEndOfTheRange_Forward()
		{
			MockRepository repo = new MockRepository();

			// reading from position 0 with range 0-7
			//   _msg1_msg2_msg3
			//   |      |   |
			//   0      7   11
			// range ends at pos 7. msg2 starts at pos 6. msg2 must be read.

			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));
			repo.VerifyAll();

			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(0, TextAccessDirection.Forward)).Return(it);
			Expect.Call(it.PositionToCharIndex(0)).Return(0);
			Expect.Call(it.CharIndexToPosition(1)).Return((long)1);
			Expect.Call(it.CurrentBuffer).Return("_msg1_msg2_msg3");
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(0, 7), 0, MessagesParserDirection.Forward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(6)).Return((long)6);
			repo.ReplayAll();
			var capt = new TextMessageCapture();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("msg", capt.MessageHeader);
			Assert.AreEqual("1_", capt.MessageBody);
			Assert.AreEqual(1L, capt.BeginPosition);
			Assert.AreEqual(6L, capt.EndPosition);
			Assert.IsFalse(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(11)).Return((long)11);
			repo.ReplayAll();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("msg", capt.MessageHeader);
			Assert.AreEqual("2_", capt.MessageBody);
			Assert.AreEqual(6L, capt.BeginPosition);
			Assert.AreEqual(11L, capt.EndPosition);
			Assert.IsTrue(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			repo.ReplayAll();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
		}

		[Test]
		public void MessageIsNotReadBecauseItEndsAtTheBeginningOfTheRange_Backward()
		{
			MockRepository repo = new MockRepository();

			// reading from position 11 with range 6-15
			//   _msg1_msg2_msg3
			//    |    |    |   |
			//    1    6    11  15
			// range begins at pos 6. msg1_ ends at pos 6 (its past-the-end position = 6). msg1_ shouldn't be read.

			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));
			repo.VerifyAll();

			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(11, TextAccessDirection.Backward)).Return(it);
			Expect.Call(it.PositionToCharIndex(11)).Return(11);
			Expect.Call(it.CharIndexToPosition(6)).Return((long)6);
			Expect.Call(it.CurrentBuffer).Return("_msg1_msg2_msg3");
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(6, 15), 11, MessagesParserDirection.Backward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(11)).Return((long)11);
			Expect.Call(it.CharIndexToPosition(1)).Return((long)1);
			repo.ReplayAll();
			var capt = new TextMessageCapture();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("msg", capt.MessageHeader);
			Assert.AreEqual("2_", capt.MessageBody);
			Assert.AreEqual(6L, capt.BeginPosition);
			Assert.AreEqual(11L, capt.EndPosition);
			Assert.IsTrue(capt.IsLastMessage);
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			repo.ReplayAll();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
		}

		[Test]
		public void MessageIsReadBecauseItEndsRightAfterTheBeginningOfTheRange_Backward()
		{
			MockRepository repo = new MockRepository();

			// reading from position 11 with range 5-15
			//   _msg1_msg2_msg3
			//    |   |     |   |
			//    1   5     11  15
			// range begins at pos 5. msg1_ ends at pos 6 (its past-the-end position = 6). msg1_ must be read.

			ITextAccess ta = repo.CreateMock<ITextAccess>();
			ITextAccessIterator it = repo.CreateMock<ITextAccessIterator>();
			Expect.Call(ta.MaximumSequentialAdvancesAllowed).Return(3);
			repo.ReplayAll();
			MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));
			repo.VerifyAll();

			repo.BackToRecordAll();
			Expect.Call(ta.OpenIterator(11, TextAccessDirection.Backward)).Return(it);
			Expect.Call(it.PositionToCharIndex(11)).Return(11);
			Expect.Call(it.CharIndexToPosition(6)).Return((long)6);
			Expect.Call(it.CurrentBuffer).Return("_msg1_msg2_msg3");
			repo.ReplayAll();
			target.BeginSplittingSession(new Range(5, 15), 11, MessagesParserDirection.Backward);
			repo.VerifyAll();
			Assert.IsFalse(target.CurrentMessageIsEmpty);


			repo.BackToRecordAll();
			Expect.Call(it.CharIndexToPosition(11)).Return((long)11);
			Expect.Call(it.CharIndexToPosition(1)).Return((long)1);
			repo.ReplayAll();
			var capt = new TextMessageCapture();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("msg", capt.MessageHeader);
			Assert.AreEqual("2_", capt.MessageBody);
			Assert.AreEqual(6L, capt.BeginPosition);
			Assert.AreEqual(11L, capt.EndPosition);
			Assert.IsTrue(capt.IsLastMessage); // in backward mode the first message that was read is "IsLastMessage"
			Assert.IsFalse(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			Expect.Call(it.Advance(9)).Repeat.Once().Do((Predicate<int>)delegate(int i)
			{
				repo.Verify(it);
				repo.BackToRecord(it);
				Expect.Call(it.CurrentBuffer).Return("_msg1_");
				repo.Replay(it);
				return true;
			});
			repo.ReplayAll();
			Assert.IsTrue(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
			Assert.AreEqual("msg", capt.MessageHeader);
			Assert.AreEqual("1_", capt.MessageBody);
			Assert.AreEqual(1L, capt.BeginPosition);
			Assert.AreEqual(6L, capt.EndPosition);
			Assert.IsFalse(capt.IsLastMessage);
			Assert.IsTrue(target.CurrentMessageIsEmpty);

			repo.BackToRecordAll();
			repo.ReplayAll();
			Assert.IsFalse(target.GetCurrentMessageAndMoveToNextOne(capt));
			repo.VerifyAll();
		}
	}
}
