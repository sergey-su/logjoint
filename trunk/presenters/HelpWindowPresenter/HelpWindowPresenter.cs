using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

namespace LogJoint.UI.Presenters.Help
{
	public class Presenter: IPresenter
	{
		public Presenter()
		{
		}

		void IPresenter.ShowHelp(string topicUrl)
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
	};
};