using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public class SemicolonSeparatedMap
	{
		public SemicolonSeparatedMap(string str)
		{
			for (int idx = 0;;)
			{
				Match m = re.Match(str, idx, str.Length - idx);
				if (!m.Success)
					break;
				if (m.Groups["sep"].Value != "=")
					break;
				string name = GetCapturedToken(m);
				idx = m.Index + m.Length;
				m = re.Match(str, idx, str.Length - idx);
				if (!m.Success)
					break;
				string value = GetCapturedToken(m);
				values[name] = value;
				if (m.Groups["sep"].Value != ";")
					break;
				idx = m.Index + m.Length;
			}
		}

		public int Count
		{
			get { return values.Count; }
		}

		public void AssignFrom(SemicolonSeparatedMap other)
		{
			values.Clear();
			foreach (KeyValuePair<string, string> i in other.values)
				values[i.Key] = i.Value;
		}

		public bool AreEqual(SemicolonSeparatedMap other)
		{
			if (values.Count != other.values.Count)
				return false;
			foreach (KeyValuePair<string, string> i in values)
			{
				string v;
				if (!other.values.TryGetValue(i.Key, out v))
					return false;
				if (v != i.Value)
					return false;
			}
			return true;
		}

		public string this[string name]
		{
			get
			{
				string ret;
				values.TryGetValue(name, out ret);
				return ret;
			}
			set 
			{
				if (value == null)
					values.Remove(name);
				else
					values[name] = value; 
			}
		}

		public override string ToString()
		{
			StringBuilder buf = new StringBuilder();
			foreach (KeyValuePair<string, string> v in values.OrderBy(pair => pair.Key))
			{
				buf.AppendFormat("{0}{1} = {2}", buf.Length == 0 ? "" : "; ",
					EscapeToken(v.Key), EscapeToken(v.Value));
			}
			return buf.ToString();
		}

		private static Regex re = new Regex(@"
			^ # stick to the beginning
			\s* # there might be leading space before token
			(
			    (\'
			       (?<token1> # a token inside single quotes
			          ([^\'] | \'{2})*
			       )
			    \')
			  | (?<token2> # or a token without quotes
			       [^\'\=\;\ \t]+
			    ) 
			)
			\s* # there might trailing space
			(?<sep> # separator
			   [\=\;]?
			)
			",
			RegexOptions.IgnorePatternWhitespace | RegexOptions.ExplicitCapture);

		string GetCapturedToken(Match m)
		{
			string token = m.Groups["token1"].Value;
			if (token == "")
				token = m.Groups["token2"].Value;
			token = token.Replace("''", "'");
			return token;
		}

		string EscapeToken(string token)
		{
			token = token.Replace("'", "''");
			if (token.IndexOfAny(new char[] { '=', ';', ' ', '\t', '\'' }) >= 0)
				token = "'" + token + "'";
			return token;
		}

		Dictionary<string, string> values = new Dictionary<string, string>();
	}
}
