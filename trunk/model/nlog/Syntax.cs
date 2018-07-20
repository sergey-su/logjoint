using System;
using System.Collections.Generic;
using System.Text;
using System.Diagnostics;

namespace LogJoint.NLog
{
	static class Syntax
	{
		public enum NodeType
		{
			Layout, // Layout sequence. Data not used. Children contains fixed texts and rederers.
			Text, // Fixed text in the layout. Data field contains text string.
			Renderer, // Renderer. Data field containt renderer name. Children contain rederer params.
			RendererParam // Renderer param. Data is a param name. Children has 1 element that stores param value.
		};
		[DebuggerDisplay("{Type} {Data}")]
		public class Node
		{
			public NodeType Type;
			public string Data;
			public string Description;
			public List<Node> Children;
			public int? NodeStart;
			public int? NodeEnd;
			public Node(NodeType t, string data, string description, params Node[] children)
			{
				Type = t;
				Data = data;
				Description = description;
				Children = new List<Node>(children);
			}
		};

		public static Node MakeLayoutNode(IEnumerator<Parser.Token> toks, bool embeddedLayout, bool firstTokenConsumed = false)
		{
			Node ret = new Node(NodeType.Layout, "", "");

			var text = new StringBuilder();
			int? textStart = 0;
			int? textEnd = 0;
			Action dumpTextNodeIfNotEmpty = () =>
			{
				if (text.Length > 0)
				{
					ret.Children.Add(new Node(NodeType.Text, text.ToString(), "") { NodeStart = textStart, NodeEnd = textEnd });
					text.Clear();
					textStart = null;
					textEnd = null;
				}
			};

			for (int tokIdx = 0;; tokIdx++)
			{
				if (!(tokIdx == 0 && firstTokenConsumed))
					if (!toks.MoveNext())
						break;
				var tok = toks.Current;

				if (ret.NodeStart == null)
					ret.NodeStart = tok.Position;
				ret.NodeEnd = tok.Position;

				if (tok.Type == Parser.TokenType.RendererBegin)
				{
					dumpTextNodeIfNotEmpty();
					ret.Children.Add(MakeRendererNode(toks));
				}
				else if (embeddedLayout && (tok.Type == Parser.TokenType.RendererEnd || tok.Type == Parser.TokenType.ParamColon))
				{
					break;
				}
				else
				{
					text.Append(tok.Value);
					if (textStart == null)
						textStart = tok.Position;
					textEnd = tok.Position + 1;
				}
			}
			dumpTextNodeIfNotEmpty();

			return ret;
		}

		static Node MakeRendererNode(IEnumerator<Parser.Token> toks)
		{
			Node ret = new Node(NodeType.Renderer, "", "");
			ReadName(ret, toks);
			ret.Description = "${" + ret.Data + "}";

			while (toks.Current.Type == Parser.TokenType.ParamColon)
			{
				Node param = MakeParamNode(toks);
				if (param != null)
					ret.Children.Add(param);
			}

			return ret;
		}

		static bool ReadName(Node destinationNode, IEnumerator<Parser.Token> toks)
		{
			int? start = null;
			int? end = null;
			StringBuilder name = new StringBuilder();
			for (; ; )
			{
				if (!toks.MoveNext())
					break;
				end = toks.Current.Position;
				if (toks.Current.Type == Parser.TokenType.Literal || toks.Current.Type == Parser.TokenType.EscapedLiteral)
					name.Append(toks.Current.Value);
				else
					break;
				if (start == null)
					start = toks.Current.Position;
			}
			destinationNode.Data = name.ToString().ToLower().Trim();
			destinationNode.NodeStart = start;
			destinationNode.NodeEnd = end;
			return destinationNode.Data.Length > 0;
		}

		static Node MakeParamNode(IEnumerator<Parser.Token> toks)
		{
			Node ret = new Node(NodeType.RendererParam, "", "");
			if (!ReadName(ret, toks))
				return ReadDefaultParam(toks, ret);
			ret.Description = ret.Data;
			if (toks.Current.Type != Parser.TokenType.ParamEq)
				return null;
			ret.Children.Add(MakeLayoutNode(toks, embeddedLayout: true));
			return ret;
		}

		private static Node ReadDefaultParam(IEnumerator<Parser.Token> toks, Node ret)
		{
			ret.Data = ret.Description = "";
			ret.NodeEnd = ret.NodeStart = toks.Current.Position;
			ret.Children.Add(MakeLayoutNode(toks, embeddedLayout: true, firstTokenConsumed: true));
			return ret;
		}
	}
}
