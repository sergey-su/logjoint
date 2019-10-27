using System.Collections.Generic;
using System.IO;
using System.Text;

namespace LogJoint.Tests.Integration.Chromium
{
	class Helpers
	{
		public static IEnumerable<string> SplitTextStream(Stream stm, Encoding encoding = null)
		{
			stm.Position = 0;
			using (var reader = new StreamReader(stm, encoding ?? Encoding.ASCII, false, 10000, true))
				for (var l = reader.ReadLine(); l != null; l = reader.ReadLine())
					yield return l.TrimEnd();
		}
	}
}
