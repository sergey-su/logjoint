using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public class RecentLogEntry
	{
		public ILogProviderFactory Factory;
		public IConnectionParams ConnectionParams;

		public class FormatNotRegistedException : Exception
		{
			public FormatNotRegistedException(string company, string name)
				:
				base(string.Format("Format \"{0}\\{1}\" is not registered", company, name))
			{
			}
		};

		public RecentLogEntry(ILogProviderFactory factory, IConnectionParams connectionParams)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			if (connectionParams == null)
				throw new ArgumentNullException("connectionParams");
			Factory = factory;
			ConnectionParams = connectionParams;
			ConnectionParamsUtils.ValidateConnectionParams(ConnectionParams, Factory);
		}
		public RecentLogEntry(string recentLogEntryString)
		{
			var m = MatchRecentLogEntryString(recentLogEntryString);
			string company = m.Groups["company"].Value;
			string name = m.Groups["name"].Value;
			Factory = LogProviderFactoryRegistry.DefaultInstance.Find(company, name);
			if (Factory == null)
				throw new FormatNotRegistedException(company, name);
			ConnectionParams = new ConnectionParams(m.Groups["connectStr"].Value);
			ConnectionParamsUtils.ValidateConnectionParams(ConnectionParams, Factory);
		}
		public override string ToString()
		{
			string paramsStr = ConnectionParams.ToString();
			return string.Format("<{0}\\{1}>{2}{3}",
				Factory.CompanyName, Factory.FormatName, string.IsNullOrEmpty(paramsStr) ? "" : " ", paramsStr);
		}
		public string FactoryPartToString()
		{
			return FactoryPartToString(Factory);
		}

		public static RecentLogEntry Parse(string recentLogEntryString)
		{
			return new RecentLogEntry(recentLogEntryString);
		}
		public static ILogProviderFactory ParseFactoryPart(string recentLogEntryString)
		{
			var m = MatchRecentLogEntryString(recentLogEntryString);
			string company = m.Groups["company"].Value;
			string name = m.Groups["name"].Value;
			return LogProviderFactoryRegistry.DefaultInstance.Find(company, name);
		}
	
		public static string FactoryPartToString(ILogProviderFactory factory)
		{
			return string.Format("<{0}\\{1}>", factory.CompanyName, factory.FormatName);
		}

		private static Match MatchRecentLogEntryString(string recentLogEntryString)
		{
			if (recentLogEntryString == null)
				throw new ArgumentNullException("recentLogEntryString");
			Match m = re.Match(recentLogEntryString);
			if (!m.Success)
				throw new ArgumentException("The string has incorrect format", "recentLogEntryString");
			return m;
		}

		private static readonly Regex re = new Regex(@"^\<(?<company>[^\\]*)\\(?<name>[^\>]*)\>\ ?(?<connectStr>.*)$", RegexOptions.ExplicitCapture);
	};
}
