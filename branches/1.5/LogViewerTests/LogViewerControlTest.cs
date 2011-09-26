using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Collections.Generic;
using Utils = LogJoint.Utils;

namespace LogViewerTests
{
	/*
	[TestClass()]
	public class IteratorTest
	{
		delegate IEnumerable<IndexedLine> GetEnumeratorDelegate(int start, int end);

		void DoTestIterators2(GetEnumeratorDelegate fw, GetEnumeratorDelegate bk, int count)
		{
			int chunkSize = LinesList.ChunkSize;
			int[] startPositions = new int[]
				{
					-chunkSize,
					-1,
					0,
					chunkSize / 2,
					chunkSize-1,
					chunkSize,
					chunkSize+1,
					count - 1,
					count,
					count + 1,
				};

			int[] endPositions = startPositions;

			int[] deltas = new int[]
				{
					1,
					chunkSize - 1,
					chunkSize,
					chunkSize + 2,
					count,
				};

			foreach (int startPos in startPositions)
				foreach (int endPos in endPositions)
				{

					// check forward iteration
					if (fw != null)
					{
						int pos = Utils.PutInRange(0, count, startPos);
						IEnumerator<IndexedLine> it1 = fw(startPos, endPos).GetEnumerator();
						for (; pos < Utils.PutInRange(0, count, endPos); pos += 1)
						{
							Assert.IsTrue(it1.MoveNext());
							Assert.IsTrue(pos >= 0, "We mustn't enter the loop body for negative positions");
							Assert.IsTrue(pos < count, "We mustn't enter the loop body for the positions more than the list size");
							Assert.AreEqual(it1.Current.Line.Text, pos.ToString()); // check that the object the iterator is pointing to is correct
							Assert.AreEqual(it1.Current.Index, pos); // check the position
						}
						Assert.IsFalse(it1.MoveNext());
					}

					// check reverse iteration
					if (bk != null)
					{
						int pos = Utils.PutInRange(-1, count - 1, startPos);
						IEnumerator<IndexedLine> it2 = bk(startPos, endPos).GetEnumerator();
						for (; pos > Utils.PutInRange(-1, count - 1, endPos); pos -= 1)
						{
							Assert.IsTrue(it2.MoveNext());
							Assert.IsTrue(pos >= 0, "We mustn't enter the loop body for negative positions");
							Assert.IsTrue(pos < count, "We mustn't enter the loop body for the positions more than the list size");
							Assert.AreEqual(it2.Current.Line.Text, pos.ToString()); // check that the object the iterator is pointing to is correct
							Assert.AreEqual(it2.Current.Index, pos); // check the position
						}
						Assert.IsFalse(it2.MoveNext());
					}
				}
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void IteratorsTest()
		{
			LinesList lst1 = new LinesList();
			LinesList lst2 = new LinesList();
			LinesList lst3 = new LinesList();

			for (int i = 0; i < LinesList.ChunkSize * 2 + 2; ++i)
			{
				lst1.Add(new Messsage(null, new DateTime(), i.ToString(), SeverityFlag.Info));
			}
			for (int i = 0; i < LinesList.ChunkSize - 4; ++i)
			{
				lst2.Add(new Messsage(null, new DateTime(), i.ToString(), SeverityFlag.Info));
			}

			LinesList[] lists = new LinesList[] 
			{
				lst1,
				lst2,
				lst3
			};

			foreach (LinesList lst in lists)
			{
				DoTestIterators2(lst.Forward, lst.Reverse, lst.Count);
			}

		}

		void DoTestMultilist1(params int[] indexes)
		{
			List<LinesList> lists = new List<LinesList>();
			for (int i = 0; i < 3; ++i)
				lists.Add(new LinesList());
			int pos = 0;
			DateTime d = new DateTime();
			foreach (int i in indexes)
			{
				lists[i].Add(new Messsage(
					null, d, pos.ToString(), SeverityFlag.Info
				));
				pos++;
				d = d.AddSeconds(1);
			}

			DoTestIterators2(
				delegate(int s, int e)
				{
					return Iterators.MergingForward(lists, s, e);
				},
				delegate(int s, int e)
				{
					return Iterators.MergingReverse(lists, s, e);
				},
				indexes.Length
			);
		}

		void DoTestMultilist2(params int[] counts)
		{
			List<LinesList> lists = new List<LinesList>(counts.Length);
			int pos = 0;
			int total = 0;
			foreach (int c in counts)
			{
				LinesList list = new LinesList();
				lists.Add(list);
				total += c;
				for (int i = 0; i < c; ++i)
				{
					list.Add(new Messsage(
						null, new DateTime(), pos.ToString(), SeverityFlag.Info
					));
					pos++;
				}
			}

			DoTestIterators2(
				delegate(int s, int e)
				{
					return Iterators.ConcatForward(lists, s, e);
				},
				null,
				total
			);
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void MultiIteratorsTest()
		{
			DoTestMultilist1();

			DoTestMultilist1(0,1,0,1,0,1,0);

			DoTestMultilist1(0);

			DoTestMultilist1(1, 1, 1);

			DoTestMultilist1(2, 0, 2, 1, 1, 1, 1, 1, 1, 0, 2, 0, 1, 0, 1, 2, 0);

			List<int> p1 = new List<int>();
			for (int i = 0; i < 123; ++i)
				p1.Add(1);
			for (int i = 0; i < LinesList.ChunkSize + 12; ++i)
			{
				p1.Add(0);
				p1.Add(1);
			}
			DoTestMultilist1(p1.ToArray());
		}

		[DeploymentItem("LogJoint.exe")]
		[TestMethod()]
		public void MultiIteratorsTest2()
		{
			DoTestMultilist2();
			DoTestMultilist2(0);
			DoTestMultilist2(0,0);
			DoTestMultilist2(1, 1);
			DoTestMultilist2(33, 3);
			DoTestMultilist2(0, 11, 22, 0, 77, 0);
		}

	}
*/

}
