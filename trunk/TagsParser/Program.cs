using System;
using System.Linq;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace TagsParser
{
	class TagsPredicate
	{
		Node root;

		public class SyntaxError: Exception
		{
			public int Position { get; private set; }

			public SyntaxError(string message, int pos): base(message)
			{
				Position = pos;
			}
		};

		public static TagsPredicate Parse(string str)
		{
			return new TagsPredicate() { root = Node.ParseNode(str)?.Simplify() };
		}

		public bool IsMatch(HashSet<string> tags)
		{
			return root == null || root.Eval(new EvalContext() { tags = tags } );
		}

		public override string ToString()
		{
			return ToString('g');
		}

		/// <summary>
		/// Gets string represenation of the predicate formula
		/// </summary>
		/// <param name="format">g - general format, p - with parens</param>
		public string ToString(char format)
		{
			return root == null ? "" : root.ToString(format);
		}

		public HashSet<string> GetUsedTags()
		{
			var set = new HashSet<string>();
			root?.GetUsedTags(set);
			return set;
		}

		public void UnuseTag(string tag)
		{
			root = root?.UnuseTag(tag)?.Simplify();
		}

		public void AddTag(string tag)
		{
			root = Node.AddTag(root, tag);
		}

		struct EvalContext
		{
			public HashSet<string> tags;
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

			Node(Type t, params Node[] children)
			{								
				this.type = t;
				this.children = children.ToList();
			}

			Node(Token tok)
			{
				if (!Regex.IsMatch(tok.value, @"^[\w\.\-_\#\@\/\\]+$"))
					throw new SyntaxError("Bad tag name", tok.pos);
				this.type = Type.Tag;
				this.children = new List<Node>();
				this.tag = tok.value;
			}

			static List<Token> Tokenize(string str)
			{
				var list = new List<Token>();
				var re = new Regex(@"\S+");
				for (int i = 0;;)
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
					if (t?.value == "NOT")
					{
						var t2 = popToken();
						if (t2 == null)
							return new Node(t);
						return new Node(Type.Not, new Node(t2));
					}
					return t != null ? new Node(t) : null;
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

				for (;;)
				{
					var token = popToken();
					if (token == null)
						break;
					if (token.value == "AND")
					{
					}
					else if (token.value == "OR")
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
						break; // todo: throw?
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
					return new Node(Type.Or, children.Union(new [] {tag}).ToArray());
				return new Node(Type.Or, new [] { this, tag });
			}

			public Node Simplify()
			{
				if (type == Type.Tag || type == Type.Not)
					return this;
				if (children.Count == 1 && (type == Type.And || type == Type.Or))
					return children[0].Simplify();
				return new Node(type, children.Select(c => c.Simplify()).ToArray());
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
				case Type.And: return string.Format(
					"{0}{1}{2}", p1, string.Join(" AND ", children.Select(c => c.ToString(format))), p2);
				case Type.Or: return string.Format(
					"{0}{1}{2}", p1, string.Join(" OR ", children.Select(c => c.ToString(format))), p2);
				case Type.Not: return string.Format(
					"{0}NOT {1}{2}", p1, children[0].ToString(format), p2);
				case Type.Tag: return tag;
				}
				return "";
			}

			public void GetUsedTags(HashSet<string> tags)
			{
				if (type == Type.Tag)
					tags.Add(tag);
				else
					children.ForEach(c => c.GetUsedTags(tags));
			}
		};

	}

	class MainClass
	{
		static void TestParse(string str, string parenthesizedExpr, string parentheseslessExpr)
		{
			try
			{
				var p = TagsPredicate.Parse(str);
				Console.WriteLine(string.Format("{0} -> {1} -> {2}", str, p.ToString('p'), p));
			}
			catch (TagsPredicate.SyntaxError e)
			{
				Console.WriteLine(string.Format("{0} -> '{1}' as {2}", str, e.Message, e.Position));
			}
		}

		static void TestMatch(string expr, bool expectMatch, params string[] tags)
		{
			var p = TagsPredicate.Parse(expr);
			var isMatch = p.IsMatch(new HashSet<string>(tags));
			Console.WriteLine(string.Format(
				"'{0}' matches [{1}]? {2} - {3}", 
			    expr, string.Join(" ", tags), isMatch, isMatch == expectMatch ? "PASS" : "FAIL"));
		}

		static void TestUsedTags(string expr, params string[] expectedTags)
		{
			var p = TagsPredicate.Parse(expr);
			var actual = p.GetUsedTags();
			Console.WriteLine(actual.SetEquals(expectedTags) ? "PASS" : "FAIL");
		}

		static void TestUnuseTag(string expr, string tag)
		{
			var p = TagsPredicate.Parse(expr);
			p.UnuseTag(tag);
			Console.WriteLine(string.Format("'{0}' - {2} -> '{1}'", expr, p.ToString('p'), tag));
		}

		static void TestAddTag(string expr, string tag)
		{
			var p = TagsPredicate.Parse(expr);
			p.AddTag(tag);
			Console.WriteLine(string.Format("'{0}' + {2} -> '{1}'", expr, p.ToString('p'), tag));
		}

		public static void Main(string[] args)
		{
			TestParse("a b");
			TestParse("a OR b");
			TestParse("a AND b");
			TestParse("a AND NOT b");
			TestParse("NOT foo");
			TestParse("a OR b AND NOT foo");
			TestParse("a OR b AND NOT foo OR c AND d");
			TestParse("a");
			TestParse("NOT a");
			TestParse("a AND (NOT b)");
			TestParse("");

			TestMatch("a OR b", true, "a", "c");
			TestMatch("a OR b", false, "d", "c");
			TestMatch("foo AND NOT bar", true, "a", "foo", "bazz");
			TestMatch("foo AND NOT bar", false, "a", "foo", "bar");

			TestMatch("a OR b AND c AND NOT d", true, "a", "b", "c", "d");
			TestMatch("a OR b AND c AND NOT d", false, "b", "c", "d");
			TestMatch("a OR b AND c AND NOT d", true, "b", "c", "e");

			TestUsedTags("a OR b", "a", "b");
			TestUsedTags("a AND b", "a", "b");
			TestUsedTags("a AND b OR NOT a", "a", "b");
			TestUsedTags("foo bar zoo", "foo", "bar", "zoo");

			TestUnuseTag("foo bar zoo", "foo");
			TestUnuseTag("foo AND bar", "foo");
			TestUnuseTag("NOT foo", "foo");
			TestUnuseTag("a OR b OR c AND NOT b", "b");

			TestAddTag("a OR b", "c");
			TestAddTag("a", "c");
			TestAddTag("", "c");
			TestAddTag("NOT a", "c");
			TestAddTag("a AND b AND d", "c");
		}
	}
}
