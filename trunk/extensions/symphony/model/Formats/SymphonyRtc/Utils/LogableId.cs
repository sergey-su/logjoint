using System.Text.RegularExpressions;

namespace LogJoint.Symphony.Rtc
{
	public class LogableIdUtils
	{
		static readonly RegexOptions reopts = RegexOptions.Compiled | RegexOptions.ExplicitCapture | RegexOptions.Multiline;
		readonly Regex loggerRegex = new Regex(@"^(?<id>(?<type>[\w\.]+)([\w\-\.]*))$", reopts);

		public bool TryParseLogableId(string logger, out string type, out string id)
		{
			var loggerMatch = loggerRegex.Match(logger);
			if (!loggerMatch.Success)
			{
				type = null;
				id = null;
				return false;
			}
			id = loggerMatch.Groups["id"].Value;
			type = loggerMatch.Groups["type"].Value;
			return true;
		}
	}
}
