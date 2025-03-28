﻿using System;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Reflection;
using LogJoint;
using LogJoint.FileRange;
using Range = LogJoint.FileRange.Range;
using NUnit.Framework;
using System.Threading.Tasks;

namespace LogJoint.Tests
{
    [TestFixture]
    public class JitterSupportTest
    {
        [DebuggerDisplay("{Time} -> {Msg}")]
        class LogEntry
        {
            public LogEntry(int t, string msg)
            {
                Time = t;
                Msg = msg;
            }
            public int Time;
            public string Msg;
        };

        static IAsyncEnumerable<PostprocessedMessage> UnderlyingParser(LogEntry[] logContent, ReadMessagesParams parserParams)
        {
            Range effectiveRange;
            if (parserParams.Range.HasValue)
            {
                effectiveRange = parserParams.Range.Value;
                if (effectiveRange.Begin < 0)
                    effectiveRange = new Range(0, effectiveRange.End);
                if (effectiveRange.End > logContent.Length)
                    effectiveRange = new Range(effectiveRange.Begin, logContent.Length);
            }
            else
            {
                effectiveRange = new Range(0, logContent.Length);
            }

            bool reverse = parserParams.Direction == ReadMessagesDirection.Backward;
            long pos;
            if (!reverse)
            {
                pos = Math.Max(parserParams.StartPosition, effectiveRange.Begin);
            }
            else
            {
                pos = Math.Min(parserParams.StartPosition - 1, effectiveRange.End);
            }

            var result = new List<PostprocessedMessage>();
            for (; ; )
            {
                if (!reverse)
                {
                    if (pos >= effectiveRange.End)
                        break;
                }
                else
                {
                    if (pos < effectiveRange.Begin)
                        break;
                }
                LogEntry l = logContent[pos];
                IMessage m = new Message(pos, pos + 1, null, new MessageTimestamp(new DateTime(l.Time)), new StringSlice(l.Msg), SeverityFlag.Info);
                if (reverse)
                    pos--;
                else
                    pos++;
                result.Add(new PostprocessedMessage(m, null));
            }
            return result.ToAsyncEnumerable();
        }


        static async Task DoTest(LogEntry[] logContent, ReadMessagesParams originalParams, int jitterBufferSize, LogEntry[] expectedParsedMessages)
        {
            if (originalParams.Range == null)
            {
                originalParams.Range = new Range(0, logContent.Length);
            }
            ReadMessagesParams validatedParams = originalParams;
            validatedParams.EnsureStartPositionIsInRange();
            await using (var jitter = StreamReordering.Reorder(
                p => UnderlyingParser(logContent, p), originalParams, jitterBufferSize).GetAsyncEnumerator())
            {
                int messageIdx;
                int idxStep;
                if (originalParams.Direction == ReadMessagesDirection.Forward)
                {
                    messageIdx = 0;
                    idxStep = 1;
                }
                else
                {
                    messageIdx = -1;
                    idxStep = -1;
                }
                foreach (LogEntry expectedMessage in expectedParsedMessages)
                {
                    Assert.That(await jitter.MoveNextAsync(), Is.True);
                    IMessage actualMessage = jitter.Current.Message;
                    Assert.That(actualMessage, Is.Not.Null);
                    Assert.That((long)expectedMessage.Time, Is.EqualTo(actualMessage.Time.ToLocalDateTime().Ticks));
                    Assert.That(expectedMessage.Msg, Is.EqualTo(actualMessage.Text.Value));
                    Assert.That(validatedParams.StartPosition + messageIdx, Is.EqualTo(actualMessage.Position));
                    messageIdx += idxStep;
                }
                Assert.That(await jitter.MoveNextAsync(), Is.False);
            }
        }

        static LogEntry[] ParseTestLog(string str)
        {
            List<LogEntry> ret = new List<LogEntry>();
            foreach (string entryString in str.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
            {
                string[] parsedEntry = entryString.Split(':');
                ret.Add(new LogEntry(int.Parse(parsedEntry[0]), parsedEntry[1]));
            }
            return ret.ToArray();
        }

        static Task DoTest(string logContent, ReadMessagesParams originalParams, int jitterBufferSize, string expectedParsedMessages)
        {
            return DoTest(ParseTestLog(logContent), originalParams, jitterBufferSize, ParseTestLog(expectedParsedMessages));
        }

        [Test]
        public Task JitterSupport_StartFromFirstMessageWithoutDefects()
        {
            return DoTest("1:a 2:b 3:c 4:d 5:e 6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(0), 2, "1:a 2:b 3:c 4:d 5:e 6:f 7:g 8:h 9:i 10:j");
        }

        [Test]
        public Task JitterSupport_StartFromLastMessageWithoutDefects_Bwd()
        {
            return DoTest("1:a 2:b 3:c 4:d 5:e 6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(10) { Direction = ReadMessagesDirection.Backward }, 2, "10:j 9:i 8:h 7:g 6:f 5:e 4:d 3:c 2:b 1:a");
        }

        [Test]
        public Task JitterSupport_StartFromFirstDefectiveMessage()
        {
            return DoTest("2:a 1:b 3:c 4:d 5:e 6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(0), 2, "1:b 2:a 3:c 4:d 5:e 6:f 7:g 8:h 9:i 10:j");
        }

        [Test]
        public Task JitterSupport_StartFromFirstDefectiveMessage_Bwd()
        {
            return DoTest("1:a 2:b 3:c 4:d 5:e 6:f 7:g 8:h 10:j 9:i", new ReadMessagesParams(10) { Direction = ReadMessagesDirection.Backward }, 2, "10:j 9:i 8:h 7:g 6:f 5:e 4:d 3:c 2:b 1:a");
        }

        [Test]
        public Task JitterSupport_StartFromBeforeDefectiveMessage()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(2), 2, "3:c 4:e 5:d 6:f 7:g 8:h 9:i 10:j");
        }

        [Test]
        public Task JitterSupport_StartFromBeforeDefectiveMessage_Bwd()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(6) { Direction = ReadMessagesDirection.Backward }, 2, "6:f   5:d 4:e   3:c 2:b 1:a");
        }

        [Test]
        public Task JitterSupport_StartOnFirstDefectiveMessage()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(3), 2, "4:e 5:d 6:f 7:g 8:h 9:i 10:j");
        }

        [Test]
        public Task JitterSupport_StartOnFirstDefectiveMessage_Bwd()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(5) { Direction = ReadMessagesDirection.Backward }, 2, "5:d 4:e   3:c 2:b 1:a");
        }

        [Test]
        public Task JitterSupport_StartOnSecondDefectiveMessage()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(4), 2, "5:d 6:f 7:g 8:h 9:i 10:j");
        }

        [Test]
        public Task JitterSupport_StartOnSecondDefectiveMessage_Bwd()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(4) { Direction = ReadMessagesDirection.Backward }, 2, "4:e   3:c 2:b 1:a");
        }

        [Test]
        public Task JitterSupport_StartAfterDefectiveMessage()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(5), 2, "6:f 7:g 8:h 9:i 10:j");
        }

        [Test]
        public Task JitterSupport_StartAfterDefectiveMessage_Bwd()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(3) { Direction = ReadMessagesDirection.Backward }, 2, "3:c 2:b 1:a");
        }

        [Test]
        public Task JitterSupport_StartOnLastMessage()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(9), 2, "10:j");
        }

        [Test]
        public Task JitterSupport_StartOnLastMessage_Bwd()
        {
            return DoTest("1:a 2:b 3:c   5:d 4:e   6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(1) { Direction = ReadMessagesDirection.Backward }, 2, "1:a");
        }

        [Test]
        public Task JitterSupport_StartOnLastDefectiveMessage()
        {
            return DoTest("1:a 2:b 3:c 4:d 5:e 6:f 7:g 8:h 10:i 9:j", new ReadMessagesParams(9), 2, "10:i");
        }

        [Test]
        public Task JitterSupport_StartOnLastDefectiveMessage_Bwd()
        {
            return DoTest("2:b 1:a 3:c 5:d 4:e 6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(1) { Direction = ReadMessagesDirection.Backward }, 2, "1:a");
        }

        [Test]
        public Task JitterSupport_StartOnDefectiveMessageBeforeLast()
        {
            return DoTest("1:a 2:b 3:c 4:d 5:e 6:f 7:g 8:h 10:i 9:j", new ReadMessagesParams(8), 2, "9:j 10:i");
        }

        [Test]
        public Task JitterSupport_StartOnDefectiveMessageBeforeLast_Bwd()
        {
            return DoTest("2:b 1:a 3:c 5:d 4:e 6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(2) { Direction = ReadMessagesDirection.Backward }, 2, "2:b 1:a");
        }



        [Test]
        public Task JitterSupport_DefectBeforeRangeBeginning()
        {
            return DoTest(
                "1:a   3:b 2:c   4:d 5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(4, new Range(4, 10)), 2,
                "5:e 6:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectBeforeRangeBeginning_Bwd()
        {
            return DoTest(
                "1:a   3:b 2:c   4:d 5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(10, new Range(4, 10)) { Direction = ReadMessagesDirection.Backward }, 2,
                "10:j 9:i 8:h 7:g 6:f 5:e"
            );
        }

        [Test]
        public Task JitterSupport_DefectAtRangeBeginning()
        {
            return DoTest(
                "1:a 2:b   4:c 3:d   5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(2, new Range(2, 10)), 2,
                "3:d 4:c 5:e 6:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectAtRangeBeginning_Bwd()
        {
            return DoTest(
                "1:a 2:b   4:c 3:d   5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(4, new Range(0, 4)) { Direction = ReadMessagesDirection.Backward }, 2,
                "4:c 3:d 2:b 1:a"
            );
        }

        [Test]
        public Task JitterSupport_DefectMiddleAtRangeBeginning()
        {
            return DoTest(
                "1:a 2:b   4:c 3:d   5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(3, new Range(3, 10)), 2,
                "4:c 5:e 6:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectMiddleAtRangeBeginning_Bwd()
        {
            return DoTest(
                "1:a 2:b   4:c 3:d   5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(3, new Range(0, 3)) { Direction = ReadMessagesDirection.Backward }, 2,
                "3:d 2:b 1:a"
            );
        }

        [Test]
        public Task JitterSupport_DefectRightBeforeRangeBeginning()
        {
            return DoTest(
                "1:a 2:b   4:c 3:d   5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(4, new Range(4, 10)), 2,
                "5:e 6:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectRightAfterRangeBeginning()
        {
            return DoTest(
                "1:a 2:b   4:c 3:d   5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(2, new Range(2, 10)), 2,
                "3:d 4:c 5:e 6:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectRightAfterRangeEnd()
        {
            return DoTest(
                "1:a 2:b 3:c 4:d 5:e 6:f    8:g 7:h   9:i 10:j",
                new ReadMessagesParams(1, new Range(1, 6)), 2,
                "2:b 3:c 4:d 5:e 6:f"
            );
        }

        [Test]
        public Task JitterSupport_DefectMiddleAtRangeEnd()
        {
            return DoTest(
                "1:a 2:b 3:c 4:d 5:e 6:f    8:g 7:h   9:i 10:j",
                new ReadMessagesParams(1, new Range(1, 7)), 2,
                "2:b 3:c 4:d 5:e 6:f 7:h"
            );
        }

        [Test]
        public Task JitterSupport_DefectRightBeforeRangeEnd()
        {
            return DoTest(
                "1:a 2:b 3:c 4:d 5:e 6:f    8:g 7:h   9:i 10:j",
                new ReadMessagesParams(1, new Range(1, 8)), 2,
                "2:b 3:c 4:d 5:e 6:f 7:h 8:g"
            );
        }

        [Test]
        public Task JitterSupport_DefectSomewhereInTheMiddleOfRange()
        {
            return DoTest(
                "1:a 2:b 3:c 4:d 5:e 6:f    8:g 7:h   9:i 10:j",
                new ReadMessagesParams(3, new Range(3, 10)), 2,
                "4:d 5:e 6:f    7:h 8:g    9:i 10:j"
            );
        }


        [Test]
        public Task JitterSupport_DefectFarBeforeJitterBuffer()
        {
            return DoTest(
                "1:a   3:b 2:c   4:d 5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(7, new Range(7, 10)), 3,
                "8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectRightBeforeJitterBuffer()
        {
            return DoTest(
                "1:a   3:b 2:c   4:d 5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(6, new Range(6, 10)), 3,
                "7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectIsAtBeginningOfJitterBuffer()
        {
            return DoTest(
                "1:a   3:b 2:c   4:d 5:e 6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(5, new Range(5, 10)), 3,
                "6:f 7:g 8:h 9:i 10:j"
            );
        }


        [Test]
        public Task JitterSupport_DefectIsAtEndOfJitterBuffer()
        {
            return DoTest(
                "1:a 2:b 3:c 4:d 5:e 6:f 7:g 8:h   10:i 9:j",
                new ReadMessagesParams(3, new Range(3, 6)), 3,
                "4:d 5:e 6:f"
            );
        }


        [Test]
        public Task JitterSupport_DefectOfSize3_JitterBufferOfSize3()
        {
            return DoTest(
                "1:a 2:b   5:c 4:d 3:e   6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(0, new Range(0, 10)), 3,
                "1:a 2:b 3:e 4:d 5:c 6:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectOfSize3_JitterBufferOfSize4()
        {
            return DoTest(
                "1:a 2:b   5:c 4:d 3:e   6:f 7:g 8:h 9:i 10:j",
                new ReadMessagesParams(0, new Range(0, 10)), 4,
                "1:a 2:b 3:e 4:d 5:c 6:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_DefectOfSize4_JitterBufferOfSize2()
        {
            return DoTest(
                "1:a 2:b   6:c 4:d 5:e 3:f   7:g 8:h 9:i 10:j",
                new ReadMessagesParams(0, new Range(0, 10)), 2,
                "1:a 2:b 4:d 5:e 6:c 3:f 7:g 8:h 9:i 10:j"
            );
        }

        [Test]
        public Task JitterSupport_OrderOfEqualMessagesIsPreserved()
        {
            return DoTest("1:a 2:b 3:c 2:d 2:e 6:f 7:g 8:h 9:i 10:j", new ReadMessagesParams(0), 5,
                "1:a 2:b 2:d 2:e 3:c 6:f 7:g 8:h 9:i 10:j");
        }


        [Test]
        public Task JitterSupport_TheOnlyInputMessage()
        {
            return DoTest("1:a", new ReadMessagesParams(0), 3, "1:a");
        }
    }
}
