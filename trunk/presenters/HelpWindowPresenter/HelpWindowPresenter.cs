using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace LogJoint.UI.Presenters.Help
{
    public class Presenter : IPresenter
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
            {
                fullUrl = topicUrl;
            }
            else
            {
                var fragmentMatch = Regex.Match(topicUrl, @"^(.+)(\#\w+)$");
                if (fragmentMatch.Success)
                    topicUrl = fragmentMatch.Groups[1].Value;
                fullUrl = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "help", topicUrl);
                if (fragmentMatch.Success)
                    fullUrl = new Uri(fullUrl) + fragmentMatch.Groups[2].Value;
            }
            shellOpen.OpenInWebBrowser(new Uri(fullUrl));
        }
    };
};