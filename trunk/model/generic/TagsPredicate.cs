using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public class TagsPredicate
	{
		Node root;
		ImmutableHashSet<(string, int)> usedTags;
		string gString, pString;

		public class SyntaxError : Exception
		{
			public int Position { get; private set; }

			public SyntaxError(string message, int pos) : base(message)
			{
				Position = pos;
			}

			internal static readonly string ExpectedTagName = "Expected tag name.";
			internal static readonly string BadTagName = "Bad tag name.";
		};

		public static TagsPredicate Parse(string str)
		{
			return new TagsPredicate() { root = Node.ParseNode(str)?.Simplify() };
		}

		public static bool TryParse(string str, out TagsPredicate predicate)
		{
			try
			{
				predicate = Parse(str);
				return true;
			}
			catch
			{
				predicate = null;
				return false;
			}
		}

		public static bool IsKeyword(string str)
		{
			return ParseKeyword(str, out var _);
		}

		static bool ParseKeyword(string str, out string keyword)
		{
			var s = str.ToUpper();
			if (s == "NOT" || s == "OR" || s == "AND")
				keyword = s;
			else
				keyword = null;
			return keyword != null;
		}

		/// <summary>
		/// Creates predicate that matches all tag sets that any input predicates would match.
		/// Resulting predicate may match more than any individual input predicate.
		/// </summary>
		public static TagsPredicate Combine(IEnumerable<TagsPredicate> predicates)
		{
			return new TagsPredicate()
			{
				root = Node.Combine(predicates.Select(p => p.root)).Simplify()
			};
		}

		public static TagsPredicate MakeMatchAnyPredicate(IEnumerable<string> tags)
		{
			var ret = new TagsPredicate();
			foreach (var t in tags)
				ret = ret.Add(t);
			return ret;
		}

		public static readonly TagsPredicate Empty = new TagsPredicate();

		public bool IsMatch(ISet<string> tags)
		{
			return root == null || root.Eval(new EvalContext() { tags = tags });
		}

		public override string ToString()
		{
			return ToString('g');
		}

		/// <summary>
		/// Gets string representation of the predicate formula
		/// </summary>
		/// <param name="format">g - general format, p - with parens</param>
		/// <remarks>The function returns the same string reference for same input argument</remarks>
		public string ToString(char format)
		{
			if (format == 'p' && pString != null)
				return pString;
			if (format == 'g' && gString != null)
				return gString;
			var ret = root == null ? "" : root.ToString(format);
			if (format == 'p')
				pString = ret;
			else if (format == 'g')
				return gString = ret;
			return ret;
		}

		public ImmutableHashSet<(string, int)> UsedTags
		{
			get
			{
				if (usedTags == null)
				{
					var builder = ImmutableHashSet.CreateBuilder<(string, int)>();
					root?.GetUsedTags(builder);
					usedTags = builder.ToImmutable();
				}
				return usedTags;
			}
		}

		public TagsPredicate Remove(string tag)
		{
			return new TagsPredicate()
			{
				root = root?.Clone()?.UnuseTag(tag)?.Simplify()
			};
		}

		public TagsPredicate Add(string tag)
		{
			return new TagsPredicate()
			{
				root = Node.AddTag(root?.Clone(), tag)
			};
		}

		struct EvalContext
		{
			public ISet<string> tags;
		};

		class Node
		{
			public enum Type
			{
				Or,
				And,
				Not,
				Tag
			};

			class Token
			{
				public string value;
				public int pos;
			};

			public readonly Type type;
			public readonly List<Node> children;
			public readonly string tag;
			public readonly int tagPosition;

			Node(Type t, params Node[] children)
			{
				this.type = t;
				this.children = children.ToList();
			}

			Node(Token tok)
			{
				if (!Regex.IsMatch(tok.value, @"^[\w\.\-_\#\@\/\\\:]+$"))
					throw new SyntaxError(SyntaxError.BadTagName, tok.pos);
				this.type = Type.Tag;
				this.children = new List<Node>();
				this.tag = tok.value;
				this.tagPosition = tok.pos;
			}

			Node(Node other)
			{
				this.type = other.type;
				this.children = other.children.Select(c => c.Clone()).ToList();
				this.tag = other.tag;
			}

			public Node Clone()
			{
				return new Node(this);
			}

			static List<Token> Tokenize(string str)
			{
				var list = new List<Token>();
				var re = new Regex(@"\S+");
				for (int i = 0; ;)
				{
					var m = re.Match(str, i);
					if (!m.Success)
						break;
					list.Add(new Token()
					{
						value = m.Value,
						pos = m.Index
					});
					i = m.Index + m.Length;
				}
				return list;
			}

			static bool RecognizeKeyword(Token token, out string keyword)
			{
				return TagsPredicate.ParseKeyword(token.value, out keyword);
			}

			public static Node Combine(IEnumerable<Node> nodes)
			{
				var terms = new Dictionary<string, Node>();
				Action<Node> addTerm = node =>
				{
					var simple = node.Simplify();
					var key = simple.Normalize().ToString('p');
					if (!terms.ContainsKey(key))
						terms.Add(key, simple);
				};
				foreach (var n in nodes)
				{
					if (n != null)
					{
						if (n.type == Type.Or)
							n.children.ForEach(addTerm);
						else
							addTerm(n);
					}
				}
				return new Node(Type.Or, terms.Values.ToArray()).Simplify();
			}

			// supports only 3 levels: top level OR, second level - ANDs, tag or NOT tag at level 3.
			public static Node ParseNode(string str)
			{
				var tokens = Tokenize(str);
				var tokenIdx = 0;
				Func<Token> popToken = () => tokenIdx < tokens.Count ? tokens[tokenIdx++] : null;
				Action pushToken = () => --tokenIdx;
				Func<Node> popTag = () => // gets next tag or negated tag
				{
					var t = popToken();
					if (t == null)
						return null;
					if (RecognizeKeyword(t, out var kw))
					{
						if (kw == "NOT")
						{
							var t2 = popToken();
							if (t2 == null)
								throw new SyntaxError(SyntaxError.ExpectedTagName, t.pos + "NOT".Length);
							else if (RecognizeKeyword(t2, out kw))
								throw new SyntaxError(SyntaxError.BadTagName, t2.pos);
							return new Node(Type.Not, new Node(t2));
						}
						else
						{
							throw new SyntaxError(SyntaxError.BadTagName, t.pos);
						}
					}
					return new Node(t);
				};

				Node topLevelOr = new Node(Type.Or);
				Node currentAnd = null;

				Action startNewAnd = () => topLevelOr.children.Add(currentAnd = new Node(Type.And));

				startNewAnd();

				Node firstNode = popTag();
				if (firstNode != null)
					currentAnd.children.Add(firstNode);
				else
					return null;

				for (; ; )
				{
					var token = popToken();
					if (token == null)
						break;
					RecognizeKeyword(token, out var kw);
					if (kw == "AND")
					{
					}
					else if (kw == "OR")
					{
						startNewAnd();
					}
					else // a tag without operator, assume "OR tag"
					{
						startNewAnd();
						pushToken();
					}
					Node tag = popTag();
					if (tag != null)
						currentAnd.children.Add(tag);
					else
						throw new SyntaxError(SyntaxError.ExpectedTagName, str.Length);
				}

				return topLevelOr;
			}


			public static Node AddTag(Node to, string tag)
			{
				var tagObj = new Node(new Token() { value = tag, pos = 0 });
				if (to == null)
					return tagObj;
				return to.AddTag(tagObj);
			}

			public bool Eval(EvalContext ctx)
			{
				switch (type)
				{
					case Type.And: return children.All(t => t.Eval(ctx));
					case Type.Or: return children.Any(t => t.Eval(ctx));
					case Type.Not: return !children[0].Eval(ctx);
					case Type.Tag: return ctx.tags.Contains(tag);
				}
				return false;
			}

			public Node UnuseTag(string tag)
			{
				if (type == Type.Tag)
					return this.tag == tag ? null : this;
				var n = new Node(type, children.Select(c => c.UnuseTag(tag)).Where(c => c != null).ToArray());
				if (n.children.Count == 0)
					return null;
				return n;
			}

			public Node AddTag(Node tag)
			{
				if (type == Type.Or)
					return new Node(Type.Or, children.Union(new[] { tag }).ToArray());
				return new Node(Type.Or, new[] { this, tag });
			}

			public Node Simplify()
			{
				if (type == Type.Tag || type == Type.Not)
					return this;
				if (children.Count == 1 && (type == Type.And || type == Type.Or))
					return children[0].Simplify();
				return new Node(type, children.Select(c => c.Simplify()).ToArray());
			}

			public Node Normalize()
			{
				if (type == Type.And || type == Type.Or)
				{
					return new Node(type,
						children
							.Select(c => c.Normalize())
							.Select(c => (c.ToString('p'), c))
							.OrderBy(p => p.Item1)
							.Select(p => p.c)
							.ToArray()
					);
				}
				return this;
			}

			public override string ToString()
			{
				return ToString('p');
			}

			public string ToString(char format)
			{
				string p1 = format == 'p' ? "(" : "";
				string p2 = format == 'p' ? ")" : "";
				switch (type)
				{
					case Type.And:
						return string.Format(
						 "{0}{1}{2}", p1, string.Join(" AND ", children.Select(c => c.ToString(format))), p2);
					case Type.Or:
						return string.Format(
						  "{0}{1}{2}", p1, string.Join(" OR ", children.Select(c => c.ToString(format))), p2);
					case Type.Not:
						return string.Format(
						  "{0}NOT {1}{2}", p1, children[0].ToString(format), p2);
					case Type.Tag: return tag;
				}
				return "";
			}

			public void GetUsedTags(ImmutableHashSet<(string, int)>.Builder tags)
			{
				if (type == Type.Tag)
					tags.Add((tag, tagPosition));
				else
					children.ForEach(c => c.GetUsedTags(tags));
			}
		};
	}
}
