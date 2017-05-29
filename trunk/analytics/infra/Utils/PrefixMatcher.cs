using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Analytics
{
	public interface IPrefixMatcher
	{
		int RegisterPrefix(string prefix);
		void Freeze();
		IMatchedPrefixesCollection Match(string str);
	};


	public interface IMatchedPrefixesCollection: IEnumerable<int>
	{
		bool Contains(int prefixId);
	};

	public class PrefixMatcher : IPrefixMatcher
	{
		public int RegisterPrefix(string prefix)
		{
			if (isFrozen)
				throw new InvalidOperationException("Can not register a prefix in the frozen prefix matcher");
			TrieNode node = root;
			foreach (var c in prefix)
			{
				TrieNode tmp;
				if (c == '?')
				{
					if (node.any == null)
						node.any = new TrieNode();
					tmp = node.any;
				}
				else
				{
					if (!node.children.TryGetValue(c, out tmp))
						node.children.Add(c, tmp = new TrieNode());
				}
				node = tmp;
			}
			if (node.prefixId == 0)
				node.prefixId = ++maxPrefixId;
			return node.prefixId;
		}

		public void Freeze()
		{
			isFrozen = true;
		}

		public IMatchedPrefixesCollection Match(string str)
		{
			HashSet<int> matchedIds = null;
			TrieNode node = root;
			foreach (char c in str)
			{
				node = node.Match(c);
				if (node == null)
					break;
				if (node.prefixId != 0)
				{
					if (matchedIds == null)
						matchedIds = new HashSet<int>();
					matchedIds.Add(node.prefixId);
				}
			}
			return matchedIds != null ? new Collection(matchedIds) : empty;
		}

		public IMatchedPrefixesCollection MatchSubstrings(string inputString)
		{
			HashSet<int> matchedIds = null;
			var matchStates = new List<PrefixMatcher.MatchState>(inputString.Length);

			int matchedId = 0;
			foreach (var c in inputString)
			{
				matchStates.Add(BeginMatch());
				for (var i = 0; i < matchStates.Count; ++i)
				{
					var state = matchStates[i];
					if (state != null)
					{
						if (!MatchNext(state, c, out matchedId))
							matchStates[i] = null;
						if (matchedId != 0)
						{
							if (matchedIds == null)
								matchedIds = new HashSet<int>();
							matchedIds.Add(matchedId);
						}
					}
				}
			}
			return matchedIds != null ? new Collection(matchedIds) : empty;
		}

		MatchState BeginMatch()
		{
			return new MatchState() { node = root };
		}

		bool MatchNext(MatchState state, char c, out int matchedId)
		{
			matchedId = 0;
			TrieNode node = state.node.Match(c);
			if (node == null)
				return false;
			if (node.prefixId != 0)
				matchedId = node.prefixId;
			state.node = node;
			return true;
		}

		class MatchState
		{
			public TrieNode node;
		};

		class TrieNode
		{
			public Dictionary<char, TrieNode> children = new Dictionary<char, TrieNode>();
			public TrieNode any;
			public int prefixId;

			public TrieNode Match(char c)
			{
				TrieNode node;
				if (children.TryGetValue(c, out node))
					return node;
				else if (any != null)
					return any;
				return null;
			}
		};

		class Collection : IMatchedPrefixesCollection
		{
			readonly HashSet<int> set;

			public Collection(HashSet<int> set)
			{
				this.set = set;
			}

			bool IMatchedPrefixesCollection.Contains(int prefixId) { return set.Contains(prefixId); }
			IEnumerator<int> IEnumerable<int>.GetEnumerator() { return set.GetEnumerator();  }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { return set.GetEnumerator(); }
		};

		class EmptyCollection : IMatchedPrefixesCollection
		{
			bool IMatchedPrefixesCollection.Contains(int prefixId) { return false; }
			IEnumerator<int> IEnumerable<int>.GetEnumerator() { yield break; }
			System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() { yield break; }
		};

		readonly TrieNode root = new TrieNode();
		readonly IMatchedPrefixesCollection empty = new EmptyCollection();
		int maxPrefixId;
		bool isFrozen;
	};
}
