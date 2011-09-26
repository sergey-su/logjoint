using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using LogJoint.StreamSearch;

namespace LogJoint
{
	public abstract class BoundFinder
	{
		public abstract long? Find(Stream stream, Encoding encoding);

		public static BoundFinder CreateBoundFinder(XmlNode node)
		{
			if (node == null)
				throw new ArgumentNullException("node");
			XmlNode finderNode = node.SelectSingleNode("*");
			switch (finderNode != null ? finderNode.Name : "")
			{
				case "trie-search":
					return new TrieBoundFinder(finderNode);
				default:
					throw new Exception("Cannot load bound finder");
			}
		}
	};

	public class TrieBoundFinder: BoundFinder
	{
		TrieNode trieNode;
		List<string> textTests;

		public TrieBoundFinder(XmlNode node)
		{
			foreach (XmlNode n in node.SelectNodes("text"))
			{
				if (textTests == null)
					textTests = new List<string>();
				textTests.Add(n.InnerText);
			}
		}

		public override long? Find(Stream stream, Encoding encoding)
		{
			EnsureInitialized(encoding);
			stream.Position = 0;
			return trieNode.Find(stream);
		}

		void EnsureInitialized(Encoding encoding)
		{
			if (trieNode != null)
				return;
			TrieNode tmp = new TrieNode();
			if (textTests != null)
			{
				if (encoding == null)
					throw new ArgumentException("<trie-search> cannot be loaded. Text encoding is not specified.");
				foreach (string text in textTests)
				{
					tmp.Add(encoding.GetBytes(text), 0);
				}
			}
			if (tmp.IsEmpty)
			{
				throw new ArgumentException("<trie-search> doesn't contain enough information");
			}

			trieNode = tmp;
		}
	};
}
