using System;
using LogJoint;
using LogJoint.RegularExpressions;
using Range = LogJoint.FileRange.Range;
using NUnit.Framework;
using NSubstitute;
using System.Threading.Tasks;

namespace LogJoint.Tests
{
    delegate void SimpleDelegate();

    [TestFixture]
    public class MessagesSplitterTest
    {
        static readonly IRegexFactory reFactory = RegularExpressions.FCLRegexFactory.Instance;
        static readonly ValueTask<bool> trueTask = new ValueTask<bool>(Task.FromResult(true));
        static readonly ValueTask<bool> falseTask = new ValueTask<bool>(Task.FromResult(false));

        [Test]
        public async Task GetCurrentMessageAndMoveToNextOne_MainScenario_Forward()
        {
            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None));


            ta.OpenIterator(0, TextAccessDirection.Forward).Returns(it);
            it.PositionToCharIndex(0).Returns(0);
            it.CurrentBuffer.Returns(
              "123456 abc 283147948 abc 3498");
            // |      |  |          |  |   |
            // 0      7  10         21 24  28 - char idx
            // 0      8  16         45 56  67 - positions
            it.CharIndexToPosition(7).Returns((long)8);
            await target.BeginSplittingSession(new Range(0, 100), 0, ReadMessagesDirection.Forward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.CharIndexToPosition(21).Returns((long)45);
            var capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length)));
            Assert.That(" 283147948 ", Is.EqualTo(capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength)));
            Assert.That(8L, Is.EqualTo(capt.BeginPosition));
            Assert.That(45L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            it.Advance(24).Returns(_ =>
            {
                it.CurrentBuffer.Returns(
                   " 3498 abc 2626277");
                //  |     |  |          
                //  0     6  9           - char idx
                //  56    72 81          - position
                it.CharIndexToPosition(6).Returns((long)72);
                return trueTask;
            });
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length)));
            Assert.That(" 3498 ", Is.EqualTo(capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength)));
            Assert.That(45L, Is.EqualTo(capt.BeginPosition));
            Assert.That(72L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.Advance(9).Returns(_ =>
            {
                it.CurrentBuffer.Returns(
                  " 2626277");
                // |       | 
                // 0       8 - char idx
                // 81      90  - position
                it.CharIndexToPosition(8).Returns((long)90);
                return trueTask;
            });
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length)));
            Assert.That(" 2626277", Is.EqualTo(capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength)));
            Assert.That(72L, Is.EqualTo(capt.BeginPosition));
            Assert.That(90L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.True);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);


            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);


            target.EndSplittingSession();
            it.Received(1).Dispose();
        }

        [Test]
        public async Task GetCurrentMessageAndMoveToNextOne_MainScenario_Backward()
        {
            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();

            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None));


            ta.OpenIterator(100, TextAccessDirection.Backward).Returns(it);
            it.PositionToCharIndex(100).Returns(29);
            it.CurrentBuffer.Returns(
              "123456 abc 283147948 abc 3498");
            // |      |  |          |  |    |   
            // 0      7  10         21 24   29  - char idx
            // 50     61 67         85 87   100 - position
            it.CharIndexToPosition(21).Returns((long)85);
            await target.BeginSplittingSession(new Range(0, 100), 100, ReadMessagesDirection.Backward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            it.CharIndexToPosition(7).Returns((long)61);
            it.CharIndexToPosition(29).Returns((long)100);
            var capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length)));
            Assert.That(" 3498", Is.EqualTo(capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength)));
            Assert.That(85L, Is.EqualTo(capt.BeginPosition));
            Assert.That(100L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.True);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.Advance(8).Returns(_ =>
            {
                it.CurrentBuffer.Returns(
                   "11 abc 123456 abc 283147948 ");
                //  |  |  |       |  |         |   
                //  0  3  6       14 17        27   - char idx
                //  20 33 50      61 67        85   - positions
                it.CharIndexToPosition(3).Returns((long)33);
                return trueTask;
            });
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length)));
            Assert.That(" 283147948 ", Is.EqualTo(capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength)));
            Assert.That(61L, Is.EqualTo(capt.BeginPosition));
            Assert.That(85L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.Advance(14).Returns(_ =>
            {
                it.CurrentBuffer.Returns(
                   "11 abc 123456 ");
                //  |  |  |       | 
                //  0  3  6       13 - char idx
                //  20 33         61 - pos
                return trueTask;
            });
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.HeaderBuffer.Substring(capt.HeaderMatch.Index, capt.HeaderMatch.Length)));
            Assert.That(" 123456 ", Is.EqualTo(capt.BodyBuffer.Substring(capt.BodyIndex, capt.BodyLength)));
            Assert.That(33L, Is.EqualTo(capt.BeginPosition));
            Assert.That(61L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);



            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);


            target.EndSplittingSession();
            it.Received(1).Dispose();
        }

        [Test]
        public async Task BeginSplittingSession_WithStartPositionOutOfRange()
        {
            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));


            await target.BeginSplittingSession(new Range(0, 100), 110, ReadMessagesDirection.Forward);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);


            TextMessageCapture capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);

            target.EndSplittingSession();
        }

        [Test]
        public async Task BeginSplittingSession_WithStartPositionThatDoesntGetMappedToCharacterByTextAccess()
        {
            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));

            ta.OpenIterator(90, TextAccessDirection.Forward).Returns(it);
            it.PositionToCharIndex(90).Returns(_ => throw new ArgumentOutOfRangeException());
            await target.BeginSplittingSession(new Range(0, 100), 90, ReadMessagesDirection.Forward);
            it.Received(1).Dispose();
            Assert.That(target.CurrentMessageIsEmpty, Is.True);


            TextMessageCapture capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);

            target.EndSplittingSession();
        }

        [Test]
        public async Task BeginSplittingSession_TextIteratorMustBeCleanedUpInCaseOfException()
        {
            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));


            ta.OpenIterator(0, TextAccessDirection.Forward).Returns(it);
            it.PositionToCharIndex(0).Returns(0);
            it.CurrentBuffer.Returns(_ => throw new System.Security.SecurityException());

            try
            {
                await target.BeginSplittingSession(new Range(0, 100), 0, ReadMessagesDirection.Forward);
                Assert.Fail("We must never get here because of an exception in prev call");
            }
            catch (System.Security.SecurityException)
            {
            }
            it.Received(1).Dispose();
            Assert.That(target.CurrentMessageIsEmpty, Is.True);
        }

        [Test]
        public async Task BeginSplittingSession_NestedSessionsAreNotAllowed()
        {
            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));


            ta.OpenIterator(0, TextAccessDirection.Forward).Returns(it);
            it.PositionToCharIndex(0).Returns(0);
            it.CurrentBuffer.Returns("00 111 222");
            it.CharIndexToPosition(3).Returns((long)3);
            await target.BeginSplittingSession(new Range(0, 100), 0, ReadMessagesDirection.Forward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await target.BeginSplittingSession(new Range(0, 200), 0, ReadMessagesDirection.Forward);
            });
        }

        [Test]
        public void HeaderReMustNotBeRightToLeft()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                MessagesSplitter target = new MessagesSplitter(Substitute.For<ITextAccess>(),
                    reFactory.Create(@"111", ReOptions.RightToLeft));
            });
        }


        [Test]
        public async Task NotPairedEndSplittingSession()
        {
            var ta = Substitute.For<ITextAccess>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
            await target.BeginSplittingSession(new Range(0, 100), 200, ReadMessagesDirection.Forward);
            target.EndSplittingSession();

            Assert.Throws<InvalidOperationException>(() =>
            {
                target.EndSplittingSession();
            });
        }

        [Test]
        public void GetCurrentMessageAndMoveToNextOneWhenNoOpenSession()
        {
            var ta = Substitute.For<ITextAccess>();
            ta.MaximumSequentialAdvancesAllowed.Returns(100);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"111", ReOptions.None));
            TextMessageCapture capt = new TextMessageCapture();
            Assert.ThrowsAsync<InvalidOperationException>(async () =>
            {
                await target.GetCurrentMessageAndMoveToNextOne(capt);
            });
        }

        [Test]
        public async Task HeaderRegexMatchesPartOfAMessage_Forward()
        {
            var capt = new TextMessageCapture();

            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.AverageBufferLength.Returns(100);
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"ab(c)?", ReOptions.None), MessagesSplitterFlags.PreventBufferUnderflow);

            ta.OpenIterator(0, TextAccessDirection.Forward).Returns(it);
            it.PositionToCharIndex(0).Returns(0);
            it.CurrentBuffer.Returns("ab");
            it.Advance(0).Returns(_ =>
            {
                it.CurrentBuffer.Returns("abc_");
                it.CharIndexToPosition(0).Returns((long)0);
                return trueTask;
            });
            await target.BeginSplittingSession(new Range(0, 10), 0, ReadMessagesDirection.Forward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            it.Advance(3).Returns(falseTask);
            it.CharIndexToPosition(4).Returns((long)3);
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.MessageHeader));
            Assert.That("_", Is.EqualTo(capt.MessageBody));
            Assert.That(target.CurrentMessageIsEmpty, Is.True);
        }

        [Test]
        public async Task StartBackwardReadingFromAlmostEndPosition()
        {
            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None));


            ta.OpenIterator(99, TextAccessDirection.Backward).Returns(it);
            it.PositionToCharIndex(99).Returns(28);
            it.CurrentBuffer.Returns(
              "123456 abc 283147948 abc 3498");
            // |      |  |          |  |   ||   
            // 0      7  10         21 24  28\29  - char idx
            // 50     61 67         85 87  99\100 - position
            it.CharIndexToPosition(21).Returns((long)85);
            await target.BeginSplittingSession(new Range(0, 100), 99, ReadMessagesDirection.Backward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            it.CharIndexToPosition(7).Returns((long)61);
            it.CharIndexToPosition(28).Returns((long)99);
            var capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("abc", Is.EqualTo(capt.MessageHeader));
            Assert.That(" 349", Is.EqualTo(capt.MessageBody));
            Assert.That(85L, Is.EqualTo(capt.BeginPosition));
            Assert.That(99L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.True);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);
        }

        [Test]
        public async Task FirstTextBufferIsEmpty_Backward()
        {
            var capt = new TextMessageCapture();

            int aveBufSize = 100;

            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            ta.AverageBufferLength.Returns(aveBufSize);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"abc", ReOptions.None), MessagesSplitterFlags.PreventBufferUnderflow);

            //        _abc
            //        ||  |
            // pos:  11|  15
            //         12
            ta.OpenIterator(15, TextAccessDirection.Forward).Returns(it);
            it.CurrentBuffer.Returns("");
            it.PositionToCharIndex(15).Returns(0); // querying past-the end position is allowed
            it.Advance(0).Returns(_ =>
            {
                it.CurrentBuffer.Returns("_abc");
                it.CharIndexToPosition(1).Returns((long)12);
                return trueTask;
            });
            await target.BeginSplittingSession(new Range(10, 20), 15, ReadMessagesDirection.Forward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);
        }

        [Test]
        public async Task MessageIsNotReadBecauseItStartsAtTheEndOfTheRange_Forward()
        {
            // reading from position 0 with range 0-6
            //   _msg1_msg2_msg3
            //   |     |
            //   0     6
            // range ends at pos 6 that is past-the-end position. msg2 shouldn't be read.

            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));

            ta.OpenIterator(0, TextAccessDirection.Forward).Returns(it);
            it.PositionToCharIndex(0).Returns(0);
            it.CharIndexToPosition(1).Returns((long)1);
            it.CurrentBuffer.Returns("_msg1_msg2_msg3");
            await target.BeginSplittingSession(new Range(0, 6), 0, ReadMessagesDirection.Forward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.CharIndexToPosition(6).Returns((long)6);
            var capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("msg", Is.EqualTo(capt.MessageHeader));
            Assert.That("1_", Is.EqualTo(capt.MessageBody));
            Assert.That(1L, Is.EqualTo(capt.BeginPosition));
            Assert.That(6L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.True);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
        }

        [Test]
        public async Task MessageIsReadBecauseItStartsRightBeforeTheEndOfTheRange_Forward()
        {
            // reading from position 0 with range 0-7
            //   _msg1_msg2_msg3
            //   |      |   |
            //   0      7   11
            // range ends at pos 7. msg2 starts at pos 6. msg2 must be read.

            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));

            ta.OpenIterator(0, TextAccessDirection.Forward).Returns(it);
            it.PositionToCharIndex(0).Returns(0);
            it.CharIndexToPosition(1).Returns((long)1);
            it.CurrentBuffer.Returns("_msg1_msg2_msg3");
            await target.BeginSplittingSession(new Range(0, 7), 0, ReadMessagesDirection.Forward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.CharIndexToPosition(6).Returns((long)6);
            var capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("msg", Is.EqualTo(capt.MessageHeader));
            Assert.That("1_", Is.EqualTo(capt.MessageBody));
            Assert.That(1L, Is.EqualTo(capt.BeginPosition));
            Assert.That(6L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            it.CharIndexToPosition(11).Returns((long)11);
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("msg", Is.EqualTo(capt.MessageHeader));
            Assert.That("2_", Is.EqualTo(capt.MessageBody));
            Assert.That(6L, Is.EqualTo(capt.BeginPosition));
            Assert.That(11L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.True);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
        }

        [Test]
        public async Task MessageIsNotReadBecauseItEndsAtTheBeginningOfTheRange_Backward()
        {
            // reading from position 11 with range 6-15
            //   _msg1_msg2_msg3
            //    |    |    |   |
            //    1    6    11  15
            // range begins at pos 6. msg1_ ends at pos 6 (its past-the-end position = 6). msg1_ shouldn't be read.

            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));

            ta.OpenIterator(11, TextAccessDirection.Backward).Returns(it);
            it.PositionToCharIndex(11).Returns(11);
            it.CharIndexToPosition(6).Returns((long)6);
            it.CurrentBuffer.Returns("_msg1_msg2_msg3");
            await target.BeginSplittingSession(new Range(6, 15), 11, ReadMessagesDirection.Backward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.CharIndexToPosition(11).Returns((long)11);
            it.CharIndexToPosition(1).Returns((long)1);
            var capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("msg", Is.EqualTo(capt.MessageHeader));
            Assert.That("2_", Is.EqualTo(capt.MessageBody));
            Assert.That(6L, Is.EqualTo(capt.BeginPosition));
            Assert.That(11L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.True);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
        }

        [Test]
        public async Task MessageIsReadBecauseItEndsRightAfterTheBeginningOfTheRange_Backward()
        {
            // reading from position 11 with range 5-15
            //   _msg1_msg2_msg3
            //    |   |     |   |
            //    1   5     11  15
            // range begins at pos 5. msg1_ ends at pos 6 (its past-the-end position = 6). msg1_ must be read.

            ITextAccess ta = Substitute.For<ITextAccess>();
            ITextAccessIterator it = Substitute.For<ITextAccessIterator>();
            ta.MaximumSequentialAdvancesAllowed.Returns(3);
            MessagesSplitter target = new MessagesSplitter(ta, reFactory.Create(@"msg", ReOptions.None));

            ta.OpenIterator(11, TextAccessDirection.Backward).Returns(it);
            it.PositionToCharIndex(11).Returns(11);
            it.CharIndexToPosition(6).Returns((long)6);
            it.CurrentBuffer.Returns("_msg1_msg2_msg3");
            await target.BeginSplittingSession(new Range(5, 15), 11, ReadMessagesDirection.Backward);
            Assert.That(target.CurrentMessageIsEmpty, Is.False);


            it.CharIndexToPosition(11).Returns((long)11);
            it.CharIndexToPosition(1).Returns((long)1);
            var capt = new TextMessageCapture();
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("msg", Is.EqualTo(capt.MessageHeader));
            Assert.That("2_", Is.EqualTo(capt.MessageBody));
            Assert.That(6L, Is.EqualTo(capt.BeginPosition));
            Assert.That(11L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.True); // in backward mode the first message that was read is "IsLastMessage"
            Assert.That(target.CurrentMessageIsEmpty, Is.False);

            it.Advance(9).Returns(_ =>
            {
                it.CurrentBuffer.Returns("_msg1_");
                return trueTask;
            });
            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.True);
            Assert.That("msg", Is.EqualTo(capt.MessageHeader));
            Assert.That("1_", Is.EqualTo(capt.MessageBody));
            Assert.That(1L, Is.EqualTo(capt.BeginPosition));
            Assert.That(6L, Is.EqualTo(capt.EndPosition));
            Assert.That(capt.IsLastMessage, Is.False);
            Assert.That(target.CurrentMessageIsEmpty, Is.True);

            Assert.That(await target.GetCurrentMessageAndMoveToNextOne(capt), Is.False);
        }
    }
}
