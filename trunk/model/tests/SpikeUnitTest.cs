using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Reflection;
using System.Diagnostics;
using System.Threading;
using System.Net;
using System.IO;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using LogJoint;

namespace logjoint.model.tests
{
	[TestClass]
	public class SpikeUnitTest
	{
		IEnumerable<string> IterateBuffers(ITextAccessIterator tai, string template)
		{
			for (; ; )
			{
				string buf = tai.CurrentBuffer;
				if (buf.Length < template.Length)
					break;
				yield return buf;
				if (!tai.Advance(tai.CurrentBuffer.Length - template.Length))
					break;
			}
		}

		IEnumerable<int> IterateMatches(string buf, string template)
		{
			for (int startIdx = 0; ; )
			{
				int charIdx = buf.IndexOf(template, startIdx);
				if (charIdx < 0)
					break;
				yield return charIdx;
				startIdx = charIdx + template.Length;
			}
		}

		[TestMethod]
		public void SpikeUnitTest1()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			string template = "CMD_CALL_INCOMING";
			long matches = 0;
			using (var fs = new FileStream(@"w:\\Product\\debug-20111202-1634.log", FileMode.Open, FileAccess.Read))
			{
				ITextAccess ta = new StreamTextAccess(fs, Encoding.ASCII);
				using (var tai = ta.OpenIterator(0, TextAccessDirection.Forward))
				{
					foreach (var buf in IterateBuffers(tai, template))
					{
						foreach (var m in IterateMatches(buf, template))
						{
							++matches;
							var pos = tai.CharIndexToPosition(m);
						}
					}
				}
			}
			sw.Stop();
			Console.WriteLine("{0}. Matches={1}", sw.Elapsed, matches);
		}

	}
}
