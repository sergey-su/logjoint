using System;
using System.Collections.Generic;
using LogJoint;
using LogJoint.MessagesContainers;
using NUnit.Framework;
using Msg = LogJoint.IMessage;
using Range = LogJoint.FileRange.Range;

namespace LogJoint.Tests
{
    [TestFixture]
    public class LinesTest
    {
        public void AssertEqual(Range exp, Range act)
        {
            FileRangeQueueTest.AssertEqual(exp, act);
        }

        Msg NewMsg()
        {
            return NewMsg(0, "msg", new DateTime());
        }

        Msg NewMsg(int num)
        {
            return NewMsg(num, num.ToString(), (new DateTime(2000, 1, 1)) + TimeSpan.FromSeconds(num));
        }

        Msg NewMsg(long pos, string msg, DateTime d)
        {
            return new LogJoint.Message(pos, pos + 1, null, new LogJoint.MessageTimestamp(d), new StringSlice(msg), SeverityFlag.Info);
        }

        void CheckLines(RangesManagingCollection lines, params string[] ranges)
        {
            int i = 0;
            foreach (MessagesRange r in lines.Ranges)
            {
                Assert.That(ranges[i], Is.EqualTo(r.ToString()));
                ++i;
            }
            Assert.That(ranges.Length, Is.EqualTo(i));
        }

        void CheckCollection(IMessagesCollection coll, params int[] exp)
        {
            Assert.That(exp.Length, Is.EqualTo(coll.Count));
            int idx = 0;
            foreach (IndexedMessage m in coll.Forward(0, int.MaxValue))
            {
                Assert.That(idx, Is.EqualTo(m.Index));
                Assert.That(exp[idx].ToString(), Is.EqualTo(m.Message.Text.ToString()));
                ++idx;
            }
            IEnumerable<IndexedMessage> e2 = coll.Reverse(int.MaxValue, -1);
            if (e2 != null)
            {
                idx = coll.Count - 1;
                foreach (IndexedMessage m in e2)
                {
                    Assert.That(idx, Is.EqualTo(m.Index));
                    Assert.That(exp[idx].ToString(), Is.EqualTo(m.Message.Text.ToString()));
                    --idx;
                }
            }
        }

        [Test]
        public void EmptyLinesTest()
        {
            RangesManagingCollection lines = new RangesManagingCollection();

            lines.SetActiveRange(new Range(10, 40, 1));

            CheckLines(lines, "(10-10-40,1)");

            lines.SetActiveRange(new Range(0, 30, 1));

            CheckLines(lines, "(0-0-30,1)");

            lines.SetActiveRange(new Range(20, 50, 1));

            CheckLines(lines, "(20-20-50,1)");

            lines.SetActiveRange(new Range(30, 40, 1));

            CheckLines(lines, "(30-30-40,1)");

            lines.SetActiveRange(new Range(0, 50, 1));

            CheckLines(lines, "(0-0-50,1)");

            lines.SetActiveRange(new Range(100, 200, 1));

            CheckLines(lines, "(100-100-200,1)");

            lines.SetActiveRange(new Range(0, 20, 1));

            CheckLines(lines, "(0-0-20,1)");
        }

        [Test]
        public void NormalScenarioLinesTest()
        {
            RangesManagingCollection lines = new RangesManagingCollection();

            lines.SetActiveRange(new Range(10, 40, 1));

            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                CheckLines(lines, "(10-10-40,1) open");
                r.Add(NewMsg(10), false);
                CheckLines(lines, "(10-10-40,1) open");
                r.Add(NewMsg(20), false);
                r.Add(NewMsg(30), false);
                r.Add(NewMsg(40), false);
                r.Complete();
                CheckLines(lines, "(10-40-40,1) open");
            }

            CheckCollection(lines, 10, 20, 30, 40);
            CheckLines(lines, "(10-40-40,1)");

            lines.SetActiveRange(new Range(0, 30, 1));

            CheckLines(lines, "(0-0-10,1)", "(10-20-30,1)");
            CheckCollection(lines, 10, 20);

            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                CheckLines(lines, "(0-0-10,1) open", "(10-20-30,1)");
                r.Add(NewMsg(0), false);
                r.Add(NewMsg(10), false);
                r.Complete();
                CheckLines(lines, "(0-10-10,1) open", "(10-20-30,1)");
            }

            CheckLines(lines, "(0-20-30,1)");
            CheckCollection(lines, 0, 10, 10, 20);

            lines.SetActiveRange(new Range(20, 60, 1));

            CheckLines(lines, "(20-20-60,1)");
            CheckCollection(lines, 20);

            lines.SetActiveRange(new Range(0, 50, 1));

            CheckLines(lines, "(0-0-20,1)", "(20-20-50,1)");
            CheckCollection(lines, 20);

            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                CheckLines(lines, "(0-0-20,1) open", "(20-20-50,1)");
                r.Add(NewMsg(0), false);
                r.Complete();
            }

            CheckLines(lines, "(0-20-50,1)");
            CheckCollection(lines, 0, 20);

            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                CheckLines(lines, "(0-20-50,1) open");
                r.Add(NewMsg(50), false);
                r.Complete();
            }

            CheckLines(lines, "(0-50-50,1)");
            CheckCollection(lines, 0, 20, 50);

            lines.SetActiveRange(new Range(100, 150, 1));

            CheckLines(lines, "(100-100-150,1)");
            CheckCollection(lines);
        }

        [Test]
        public void PrelimimaryStopLinesTest()
        {
            RangesManagingCollection lines = new RangesManagingCollection();

            lines.SetActiveRange(new Range(0, 30, 1));
            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(0), false);
                r.Add(NewMsg(10), false);
            }

            CheckLines(lines, "(0-10-30,1)");
            CheckCollection(lines, 0, 10);

            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(10), false);
            }

            CheckLines(lines, "(0-10-30,1)");
            CheckCollection(lines, 0, 10);

            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(10), false);
                r.Add(NewMsg(15), false);
            }

            CheckLines(lines, "(0-15-30,1)");
            CheckCollection(lines, 0, 10, 15);

            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(10), false);
                r.Add(NewMsg(15), false);
                r.Add(NewMsg(20), false);
                r.Add(NewMsg(30), false);
                r.Complete();
            }

            CheckLines(lines, "(0-30-30,1)");
            CheckCollection(lines, 0, 10, 15, 20, 30);

            lines.SetActiveRange(new Range(100, 300, 1));
            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(100), false);
                r.Add(NewMsg(110), false);
                r.Add(NewMsg(150), false);
                r.Add(NewMsg(160), false);
                r.Add(NewMsg(170), false);
            }

            CheckLines(lines, "(100-170-300,1)");
            CheckCollection(lines, 100, 110, 150, 160, 170);

            lines.SetActiveRange(new Range(100, 150, 1));

            CheckLines(lines, "(100-110-150,1)");
            CheckCollection(lines, 100, 110);
        }

        [Test]
        public void StopReadingTest1()
        {
            Assert.Throws<InvalidOperationException>(() =>
            {
                var lines = new RangesManagingCollection();

                lines.SetActiveRange(new Range(0, 50, 1));

                using (MessagesRange r = lines.GetNextRangeToFill())
                {
                    r.Add(NewMsg(0), false);
                    r.Complete();
                    Assert.That(true, Is.EqualTo(r.IsComplete));
                    r.Add(NewMsg(30), false);
                    Assert.That(false, Is.EqualTo(r.IsComplete));
                }
            });
        }

        [Test]
        public void Bug01Test()
        {
            var lines = new RangesManagingCollection();

            lines.SetActiveRange(new Range(0, 5362));
            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(0x00000000), false);
                r.Add(NewMsg(0x0000037D), false); // 893
                r.Add(NewMsg(0x000005E3), false); // 1507
                r.Add(NewMsg(0x00000BDD), false);
                r.Add(NewMsg(0x00000F01), false);
                r.Add(NewMsg(0x00001121), false);
                r.Complete();
            }


            lines.SetActiveRange(new Range(0, 5362));
            Assert.That(lines.GetNextRangeToFill(), Is.Null);

            lines.SetActiveRange(new Range(/*894*/1507, 5362));

            lines.SetActiveRange(new Range(0, 5362));
            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(0x00000000), false);
                r.Add(NewMsg(0x0000037D), false); // 893
                r.Complete();
            }


            lines.SetActiveRange(new Range(/*894*/1507, 5362));

            lines.SetActiveRange(new Range(0, 5362));
            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(0x00000000), false);
                r.Add(NewMsg(0x0000037D), false); // 893
                r.Complete();
            }

            lines.SetActiveRange(new Range(0, /*894*/1507));

            lines.SetActiveRange(new Range(893/*1*/, 5362));
            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(0x000005E3), false); // 1507
                r.Add(NewMsg(0x00000BDD), false);
                r.Add(NewMsg(0x00000F01), false);
                r.Add(NewMsg(0x00001121), false);
                r.Complete();
            }

            lines.SetActiveRange(new Range(/*894*/1507, 5362));

            lines.SetActiveRange(new Range(0, 5362/*4386*/, 1));
            using (MessagesRange r = lines.GetNextRangeToFill())
            {
                r.Add(NewMsg(0x00000000), false);
                r.Add(NewMsg(0x0000037D), false); // 893
                r.Complete();
            }

            lines.SetActiveRange(new Range(0, /*894*/1507));

            CheckCollection(lines, 0, 893);
        }

        int[] Range(params int[] ranges)
        {
            Assert.That(ranges.Length % 2, Is.EqualTo(0));

            List<int> ret = new List<int>();
            for (int i = 0; i < ranges.Length; i += 2)
            {
                for (int j = ranges[i]; j < ranges[i + 1]; ++j)
                    ret.Add(j);
            }
            return ret.ToArray();
        }
    }

}
