using System;
using System.IO;
using System.Reflection;

namespace LogJoint.UI.Presenters.Help
{
	public class Presenter: IPresenter
	{
		readonly IShellOpen shellOpen;

		public Presenter(IShellOpen shellOpen)
		{
			this.shellOpen = shellOpen;
		}

		void IPresenter.ShowHelp(string topicUrl)
		{
			string fullUrl;
			if (topicUrl.StartsWith("http", StringComparison.OrdinalIgnoreCase))
				fullUrl = topicUrl;
			else
				fullUrl = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "help", topicUrl);
			shellOpen.OpenInWebBrowser(new Uri(fullUrl));
		}
	};
};