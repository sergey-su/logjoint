using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using LogJoint.StreamSearch;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	// todo: return valid TextStreamPosition, not Stream.Position
	public abstract class BoundFinder
	{
		public abstract TextStreamPosition? Find(Stream stream, Encoding encoding, bool findEnd, TextStreamPositioningParams positioningParams);

		public static BoundFinder CreateBoundFinder(XElement node)
		{
			if (node == null)
				return null;
			var finderNode = node.Elements().FirstOrDefault();
			if (finderNode == null)
				return null;
			switch (finderNode.Name.LocalName)
			{
				case "trie-search":
					return new TrieBoundFinder(finderNode);
			}
			return null;
		}
	};

	public class TrieBoundFinder: BoundFinder
	{
		TrieNode trieNode;
		List<string> textTests;

		public TrieBoundFinder(XElement node)
		{
			foreach (XElement n in node.Elements("text"))
			{
				if (textTests == null)
					textTests = new List<string>();
				textTests.Add(n.Value);
			}
		}

		public override TextStreamPosition? Find(Stream stream, Encoding encoding, bool findEnd, TextStreamPositioningParams positioningParams)
		{
			EnsureInitialized(encoding);
			var bufSize = 1 * 1024 * 1024;
			if (findEnd)
				stream.Position = Math.Max(0, stream.Length - bufSize);
			else
				stream.Position = 0;
			var tmp = trieNode.Find(stream, bufSize);
			if (tmp == null)
				return null;
			return new TextStreamPosition(tmp.Value, positioningParams);
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
