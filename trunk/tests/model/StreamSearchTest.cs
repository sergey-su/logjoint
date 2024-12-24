﻿using System.Text;
using System.IO;
using LogJoint.StreamSearch;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class TrieNodeTest
	{
		void DoTest(TrieNode n, string streamStr, long initialPos, long? expected, long limit = long.MaxValue)
		{
			Stream s = new MemoryStream(Encoding.UTF8.GetBytes(streamStr));
			s.Position = initialPos;
			Assert.That(expected, Is.EqualTo(n.Find(s, limit)));
		}

		[Test]
		public void FindTest()
		{
			TrieNode n = new TrieNode();

			DoTest(n, "", 0, null);
			DoTest(n, "qwe", 0, null);
			DoTest(n, "qwe", 1, null);
			DoTest(n, "йцу", 0, null);
			DoTest(n, "йцу", 1, null);
			DoTest(n, "йцу", 2, null);

			n.Add(Encoding.UTF8.GetBytes("bar"), 0);
			n.Add(Encoding.UTF8.GetBytes("ba"), 0);
			DoTest(n, "bar", 0, 0);
			DoTest(n, "bba", 0, 1);
			DoTest(n, "bbaarr", 0, 1);
			DoTest(n, "йцbaук", 1, 4);
			DoTest(n, "йцbукar", 1, null);
		}

		[Test]
		public void FindTestAtEndOfStream()
		{
			TrieNode n = new TrieNode();

			n.Add(Encoding.UTF8.GetBytes("</pdml>"), 0);
			string testInput = "</packet> </pdml>";
			DoTest(n, testInput, 0, 10, limit: testInput.Length);
		}

	}


}
