using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace LogJoint.Postprocessing
{
	public static class UsersNamingExtensions
	{
		static readonly Regex re = new Regex(@"\<uh\>(?<val>[\w]+)\<\/uh\>", RegexOptions.ExplicitCapture);


		public static string ResolveShortNamesMurkup(this IUserNamesProvider shortNames, string str)
		{
			if (str == null)
				return null;
			for (; ; )
			{
				var m = re.Match(str);
				if (!m.Success)
					break;
				str =
					str.Substring(0, m.Index)
					+ shortNames.GetShortNameForUserHash(m.Groups[1].Value)
					+ str.Substring(m.Index + m.Length, str.Length - m.Index - m.Length);
			}
			return str;
		}
	};

	public class CodenameUserNamesProvider : IUserNamesProvider
	{
		readonly Dictionary<string, string> userNames = new Dictionary<string, string>();
		int usedUserNamesCount = 0;
		readonly static string[] codeNames = new[]
		{
			"Alice",
			"Bob",
			"Charlie",
			"Dan",
			"Eric",
			"Fred",
			"Gabriel",
			"Helen",
			"John",
			"Karl",
			"Lisa",
			"Marie",
			"Nicole",
			"Oscar",
			"Carol",
			"Victor",
			"Arthur",
			"Chuck",
			"Raja",
		};

		public CodenameUserNamesProvider(ILogSourcesManager logSources)
		{
			logSources.OnLogSourceRemoved += (s, e) =>
			{
				if (!logSources.Items.Any())
					Reset();
			};
		}

		string IUserNamesProvider.ResolveObfuscatedUserName(string hash)
		{
			if (string.IsNullOrWhiteSpace(hash))
				return null;
			string value;
			if (userNames.TryGetValue(hash, out value))
				return value;
			if (usedUserNamesCount < codeNames.Length)
				value = codeNames[usedUserNamesCount];
			else
				value = string.Format("User#{0}", usedUserNamesCount + 1);
			++usedUserNamesCount;
			userNames.Add(hash, value);
			return value;
		}

		void Reset()
		{
			userNames.Clear();
			usedUserNamesCount = 0;
		}
	};
}
