using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public class LogReaderFactoryRegistry: ILogReaderFactoryRegistry
	{
		static public LogReaderFactoryRegistry Instance
		{
			get { return instance; }
		}

		static public string ToString(ILogReaderFactory factory)
		{
			if (!string.IsNullOrEmpty(factory.CompanyName))
				return factory.CompanyName + "\\" + factory.FormatName;
			if (!string.IsNullOrEmpty(factory.FormatName))
				return factory.FormatName;
			return factory.ToString();
		}

		#region ILogReaderFactoryRegistry Members

		public void Register(ILogReaderFactory fact)
		{
			if (items.IndexOf(fact) < 0)
				items.Add(fact);
		}

		public void Unregister(ILogReaderFactory fact)
		{
			if (!items.Remove(fact))
				throw new InvalidOperationException("Cannot unregister the factory that was not registered");
		}

		public IEnumerable<ILogReaderFactory> Items
		{
			get { return items; }
		}

		public ILogReaderFactory Find(string companyName, string formatName)
		{
			foreach (ILogReaderFactory fact in Items)
			{
				if (fact.CompanyName == companyName && fact.FormatName == formatName)
				{
					return fact;
				}
			}
			return null;
		}

		#endregion

		List<ILogReaderFactory> items = new List<ILogReaderFactory>();

		static LogReaderFactoryRegistry instance = new LogReaderFactoryRegistry();

	};
}
