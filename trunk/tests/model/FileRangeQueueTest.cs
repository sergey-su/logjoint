using System;
using System.Text;
using System.Collections.Generic;
using R = LogJoint.FileRange.Range;
using S = LogJoint.FileRange.IntersectStruct;
using Q = LogJoint.FileRange.RangeQueue;
using NUnit.Framework;

namespace LogJoint.Tests
{
    [TestFixture]
    public class FileRangeQueueTest
    {
        static public void AssertEqual(R exp, R act)
        {
            Assert.That(exp.IsEmpty, Is.EqualTo(act.IsEmpty));
            if (exp.IsEmpty)
                return;
            Assert.That(exp.Begin, Is.EqualTo(act.Begin));
            Assert.That(exp.End, Is.EqualTo(act.End));
            Assert.That(exp.Priority, Is.EqualTo(act.Priority));
        }

        void DoTestIntersect(R r1, R r2, int pos, R r1left, R r1right, R common, R r2left, R r2right)
        {
            S r = R.Intersect(r1, r2);
            Assert.That(pos, Is.EqualTo(r.RelativePosition));
            AssertEqual(r1left, r.Leftover1Left);
            AssertEqual(r1right, r.Leftover1Right);
            AssertEqual(common, r.Common);
            AssertEqual(r2left, r.Leftover2Left);
            AssertEqual(r2right, r.Leftover2Right);
        }

        [Test]
        public void IntersectTest()
        {
            // |----|
            //           |----|
            DoTestIntersect(new R(10, 20, 1), new R(30, 40, 2), -1, new R(), new R(), new R(), new R(), new R());
            // |----|
            //       |----|
            DoTestIntersect(new R(10, 20, 1), new R(20, 30, 2), -1, new R(), new R(), new R(), new R(), new R());
            // |--------|
            //   |----|
            DoTestIntersect(new R(10, 40, 1), new R(20, 30, 2), 0, new R(10, 20, 1), new R(30, 40, 1), new R(20, 30, 2), new R(), new R());
            // |--------|
            // |----|
            DoTestIntersect(new R(10, 40, 1), new R(10, 30, 2), 0, new R(), new R(30, 40, 1), new R(10, 30, 2), new R(), new R());
            // |--------|
            // |--------|
            DoTestIntersect(new R(10, 40, 1), new R(10, 40, 2), 0, new R(), new R(), new R(10, 40, 2), new R(), new R());
            // |--------|
            //   |--------|
            DoTestIntersect(new R(10, 30, 1), new R(20, 40, 2), 0, new R(10, 20, 1), new R(), new R(20, 30, 2), new R(), new R(30, 40, 2));
            //          |----|
            //  |----|
            DoTestIntersect(new R(30, 40, 1), new R(10, 20, 2), 1, new R(), new R(), new R(), new R(), new R());
            //        |----|
            //  |----|
            DoTestIntersect(new R(30, 40, 1), new R(10, 30, 2), 1, new R(), new R(), new R(), new R(), new R());
            //    |----|
            //  |--------|
            DoTestIntersect(new R(20, 30, 1), new R(10, 40, 2), 0, new R(), new R(), new R(20, 30, 2), new R(10, 20, 2), new R(30, 40, 2));
            //     |----|
            //  |----|
            DoTestIntersect(new R(20, 40, 1), new R(10, 30, 2), 0, new R(), new R(30, 40, 1), new R(20, 30, 2), new R(10, 20, 2), new R());
        }

        [Test]
        public void FileRangeQueueTest1()
        {
            Q q = new Q();

            q.Add(new R(10, 40, 1));

            AssertEqual(new R(10, 40, 1), q.GetNext().Value);

            q.Add(new R(20, 30, 2));

            AssertEqual(new R(20, 30, 2), q.GetNext().Value);

            q.Remove(new R(20, 25));

            AssertEqual(new R(25, 30, 2), q.GetNext().Value);

            q.Remove(new R(20, 30));

            AssertEqual(new R(10, 20, 1), q.GetNext().Value);

            q.Remove(new R(long.MinValue, 30));
            q.Remove(new R(60, long.MaxValue));
            q.Add(new R(30, 60, 1));

            AssertEqual(new R(30, 60, 1), q.GetNext().Value);
            q.Remove(new R(30, 40));
            AssertEqual(new R(40, 60, 1), q.GetNext().Value);

            q.Remove(new R(40, 60));
            Assert.That(q.GetNext().HasValue, Is.False);
        }

        [Test]
        public void QueueInvertTest()
        {
            Q q = new Q();

            q.Add(new R(10, 20, 1));

            Q q2 = Q.Invert(new R(long.MinValue, long.MaxValue, 2), q);

            AssertEqual(new R(long.MinValue, 10, 2), q2.GetNext().Value);

            q2.Remove(new R(long.MinValue, 10));

            AssertEqual(new R(20, long.MaxValue, 2), q2.GetNext().Value);

            q2.Remove(new R(20, long.MaxValue));

            Assert.That(q2.GetNext().HasValue, Is.False);
        }
    }

}
