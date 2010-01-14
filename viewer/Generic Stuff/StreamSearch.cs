using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogJoint.StreamSearch
{
	class TrieNode
	{
		public void Add(byte[] data, int pos)
		{
			if (pos >= data.Length)
			{
				isTerminal = true;
				return;
			}
			byte key = data[pos];
			TrieNode child;
			if (!children.TryGetValue(key, out child))
			{
				child = new TrieNode();
				children.Add(key, child);
			}
			child.Add(data, pos + 1);
		}

		public long? Find(Stream s)
		{
			for (; ; )
			{
				int k = s.ReadByte();
				if (k == -1)
					return null;
				TrieNode n;
				if (!this.children.TryGetValue((byte)k, out n))
					continue;
				long pos = s.Position - 1;
				for (; ; )
				{
					int k2 = s.ReadByte();
					if (k2 == -1)
						return null;
					TrieNode n2;
					if (!n.children.TryGetValue((byte)k2, out n2))
						break;
					if (n2.isTerminal)
						return pos;
					n = n2;
				}
				s.Position = pos + 1;
			}
		}

		Dictionary<byte, TrieNode> children = new Dictionary<byte, TrieNode>();
		bool isTerminal;

	};
}
