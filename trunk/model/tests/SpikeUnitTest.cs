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

		[TestMethod]
		public void SpikeUnitTest1()
		{
			Stopwatch sw = new Stopwatch();
			sw.Start();
			string template = "23lk2lk4asldfkasl";
			using (var fs = new FileStream(@"w:\\Product\\debug-20111202-1634.log", FileMode.Open, FileAccess.Read))
			{
				ITextAccess ta = new StreamTextAccess(fs, Encoding.ASCII);
				using (var tai = ta.OpenIterator(0, TextAccessDirection.Forward))
				{
					for (; ; )
					{
						string buf = tai.CurrentBuffer;
						if (buf.Length < template.Length)
							break;
						buf.IndexOf(template);
						if (!tai.Advance(tai.CurrentBuffer.Length - template.Length))
							break;
					}
				}
			}
			sw.Stop();
			Console.WriteLine(sw.Elapsed);
		}

	}
}
