using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LogJoint.MRU
{
	public class RecentLogEntry : IRecentlyUsedEntity
	{
		public ILogProviderFactory Factory;
		public IConnectionParams ConnectionParams;
		public readonly string Annotation;
		public DateTime? UseTimestampUtc;

		public class FormatNotRegistedException : Exception
		{
			public FormatNotRegistedException(string company, string name)
				:
				base(string.Format("Format \"{0}\\{1}\" is not registered", company, name))
			{
			}
		};

		public class SerializationException : Exception
		{
			public SerializationException(string value)
				: base("Bad entry format: '" + value + "'")
			{
			}
		};

		public RecentLogEntry(ILogProviderFactory factory, IConnectionParams connectionParams, string annotation, DateTime? useTimestampUtc)
		{
			if (factory == null)
				throw new ArgumentNullException("factory");
			if (connectionParams == null)
				throw new ArgumentNullException("connectionParams");
			this.Factory = factory;
			this.ConnectionParams = connectionParams;
			this.Annotation = annotation;
			this.UseTimestampUtc = useTimestampUtc;
			ConnectionParamsUtils.ValidateConnectionParams(ConnectionParams, Factory);
		}

		public RecentLogEntry(ILogProviderFactoryRegistry registry, string recentLogEntryString, string annotation, DateTime? useTimestampUtc)
		{
			var m = MatchRecentLogEntryString(recentLogEntryString);
			string company = m.Groups["company"].Value;
			string name = m.Groups["name"].Value;
			this.Factory = registry.Find(company, name);
			if (Factory == null)
				throw new FormatNotRegistedException(company, name);
			this.ConnectionParams = new ConnectionParams(m.Groups["connectStr"].Value);
			ConnectionParamsUtils.ValidateConnectionParams(ConnectionParams, Factory);
			this.Annotation = annotation;
			this.UseTimestampUtc = useTimestampUtc;
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

		public static ILogProviderFactory ParseFactoryPart(ILogProviderFactoryRegistry registry, string recentLogEntryString)
		{
			var m = MatchRecentLogEntryString(recentLogEntryString);
			string company = m.Groups["company"].Value;
			string name = m.Groups["name"].Value;
			return registry.Find(company, name);
		}
	
		public static string FactoryPartToString(ILogProviderFactory factory)
		{
			return string.Format("<{0}\\{1}>", factory.CompanyName, factory.FormatName);
		}

		string IRecentlyUsedEntity.UserFriendlyName
		{
			get { return Factory.GetUserFriendlyConnectionName(ConnectionParams); }
		}

		string IRecentlyUsedEntity.Annotation
		{
			get { return Annotation; }
		}

		RecentlyUsedEntityType IRecentlyUsedEntity.Type
		{
			get { return RecentlyUsedEntityType.Log; }
		}

		DateTime? IRecentlyUsedEntity.UseTimestampUtc
		{
			get { return this.UseTimestampUtc; }
		}

		ILogProviderFactory IRecentlyUsedEntity.Factory => Factory;

		IConnectionParams IRecentlyUsedEntity.ConnectionParams
		{
			get { return this.ConnectionParams; }
		}

		private static Match MatchRecentLogEntryString(string recentLogEntryString)
		{
			if (recentLogEntryString == null)
				throw new ArgumentNullException("recentLogEntryString");
			Match m = re.Match(recentLogEntryString);
			if (!m.Success)
				throw new SerializationException(recentLogEntryString);
			return m;
		}

		private static readonly Regex re = new Regex(@"^\<(?<company>[^\\]*)\\(?<name>[^\>]*)\>\ ?(?<connectStr>.*)$", RegexOptions.ExplicitCapture);
	};
}
