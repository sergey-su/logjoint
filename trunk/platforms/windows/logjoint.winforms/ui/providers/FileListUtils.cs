using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint
{
	static class FileListUtils
	{
		public static IEnumerable<string> ParseFileList(string str)
		{
			int idx = 0;
			Match m = fileListRe.Match(str, idx);
			if (m.Success)
			{
				do
				{
					yield return m.Groups[1].Value;
					idx += m.Length + 1;
					if (idx >= str.Length)
						break;
					m = fileListRe.Match(str, idx, str.Length - idx);
				} while (m.Success);
			}
			else
			{
				yield return str;
			}
		}

		public static string MakeFileList(string[] fnames)
		{
			StringBuilder buf = new StringBuilder();
			if (fnames.Length == 1)
			{
				buf.Append(fnames[0]);
			}
			else
			{
				foreach (string n in fnames)
				{
					buf.AppendFormat("{0}\"{1}\"", buf.Length == 0 ? "" : " ", n);
				}
			}
			return buf.ToString();
		}

		static readonly Regex fileListRe = new Regex(@"^\s*\""\s*([^\""]+)\s*\""");
	}
}
