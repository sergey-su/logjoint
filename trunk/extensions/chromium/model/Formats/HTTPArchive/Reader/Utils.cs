using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace LogJoint.Chromium.HttpArchive
{
    public class Parser
    {
        public struct Start
        {
            public string Method;
            public string Url;
            public string Protocol;
        };

        public bool TryParseStart(string messageText, out Start start)
        {
            start = new Start();
            var match = startRegex.Match(messageText);
            if (!match.Success)
                return false;
            start.Method = match.Groups["method"].Value;
            start.Url = match.Groups["url"].Value;
            start.Protocol = match.Groups["protocol"].Value;
            return true;
        }

        readonly Regex startRegex = new Regex(@"^(?<method>\w+) (?<url>\S+)( (?<protocol>[\w\/\d\.]+$))?", RegexOptions.Compiled | RegexOptions.ExplicitCapture);

        public struct Receive
        {
            public string Protocol;
            public int Status;
            public string StatusText;
        };

        public bool TryParseReceive(string messageText, out Receive start)
        {
            start = new Receive();
            var match = receiveRegex.Match(messageText);
            if (!match.Success)
                return false;
            start.Protocol = match.Groups["protocol"].Value;
            int.TryParse(match.Groups["status"].Value, out start.Status);
            start.StatusText = match.Groups["statusText"].Value;
            if (start.StatusText == "")
                start.StatusText = null;
            return true;
        }

        readonly Regex receiveRegex = new Regex(@"^(?<protocol>\S*) (?<status>\d+)( (?<statusText>.+))?$", RegexOptions.Compiled | RegexOptions.ExplicitCapture);
    };
}
