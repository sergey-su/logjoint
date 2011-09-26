using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Diagnostics;
using System.IO;

namespace LogJoint
{
	class Help
	{
		static public void ShowHelp(string topicUrl)
		{
			string fullUrl;
			if (topicUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				fullUrl = topicUrl;
			else
				fullUrl = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\help\\" + topicUrl;
			ProcessStartInfo ps = new ProcessStartInfo();
			ps.UseShellExecute = true;
			ps.Verb = "open";
			ps.FileName = fullUrl;
			Process proc = Process.Start(ps);
			if (proc != null)
				proc.Close();
		}
	}
}
