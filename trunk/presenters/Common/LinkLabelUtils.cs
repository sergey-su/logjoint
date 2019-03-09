using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Text.RegularExpressions;
using LogJoint.AutoUpdate;

namespace LogJoint.UI.Presenters
{
	public static class LinkLabelUtils
	{
		readonly static Regex linkRe = new Regex(@"\*(\w+)\ ([^\*]+)\*");

		public struct ParsedLinkLabelString
		{
			public string Text;
			public List<(int, int, string)> Links; // position, length, link data
		};

		public static ParsedLinkLabelString ParseLinkLabelString(string value)
		{
			var text = new StringBuilder(value ?? "");
			var links = new List<(int, int, string)> ();
			for (; ; )
			{
				var m = linkRe.Match(text.ToString());
				if (!m.Success)
					break;
				var g = m.Groups[2];
				text.Remove(m.Index + m.Length - 1, 1); // remove trailing '*'
				text.Remove(m.Index, 2 + m.Groups[1].Length); // remove leading '*', action id and space following action id
				links.Add((g.Index - 2 - m.Groups[1].Length, g.Length, m.Groups[1].Value));
			}
			return new ParsedLinkLabelString()
			{
				Text = text.ToString(),
				Links = links 
			};
		}
	};
};