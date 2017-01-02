using System;
using System.Collections.Generic;
using System.Linq;
using MonoMac.Foundation;
using MonoMac.AppKit;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint.UI
{
	public static class NSLinkLabelExtensions
	{
		readonly static Regex linkRe = new Regex(@"\*(\w+)\ ([^\*]+)\*");

		public static void SetAttributedContents(this NSLinkLabel lbl, string value)
		{
			var text = new StringBuilder(value ?? "");
			var links = new List<NSLinkLabel.Link>();
			for (; ; )
			{
				var m = linkRe.Match(text.ToString());
				if (!m.Success)
					break;
				var g = m.Groups[2];
				text.Remove(m.Index + m.Length - 1, 1); // remove trailing '*'
				text.Remove(m.Index, 1 + m.Groups[1].Length); // remove leading '*' and action id
				links.Add(new NSLinkLabel.Link(g.Index - 1 - m.Groups[1].Length, g.Length, m.Groups[1].Value));
			}

			lbl.StringValue = text.ToString();
			lbl.Links = links;
		}
	}
}