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
			Factory = factory;
			ConnectionParams = connectionParams;
		}
		public RecentLogEntry(string str)
		{
			Match m = re.Match(str);
			if (!m.Success)
				throw new ArgumentException("The string has incorrect format", "str");
			string company = m.Groups["company"].Value;
			string name = m.Groups["name"].Value;
			Factory = LogProviderFactoryRegistry.DefaultInstance.Find(company, name);
			if (Factory == null)
				throw new FormatNotRegistedException(company, name);
			ConnectionParams = new ConnectionParams(m.Groups["connectStr"].Value);
		}
		public override string ToString()
		{
			string paramsStr = ConnectionParams.ToString();
			return string.Format("<{0}\\{1}>{2}{3}",
				Factory.CompanyName, Factory.FormatName, string.IsNullOrEmpty(paramsStr) ? "" : " ", paramsStr);
		}

		private static readonly Regex re = new Regex(@"^\<(?<company>[^\\]*)\\(?<name>[^\>]*)\>\ ?(?<connectStr>.*)$", RegexOptions.ExplicitCapture);
	};
}
