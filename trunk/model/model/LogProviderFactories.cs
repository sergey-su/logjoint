using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public class LogProviderFactoryRegistry: ILogProviderFactoryRegistry
	{
		static public LogProviderFactoryRegistry DefaultInstance
		{
			get { return instance; }
		}

		static public string ToString(ILogProviderFactory factory)
		{
			if (!string.IsNullOrEmpty(factory.CompanyName))
				return factory.CompanyName + "\\" + factory.FormatName;
			if (!string.IsNullOrEmpty(factory.FormatName))
				return factory.FormatName;
			return factory.ToString();
		}

		#region ILogReaderFactoryRegistry Members

		public void Register(ILogProviderFactory fact)
		{
			if (items.IndexOf(fact) < 0)
				items.Add(fact);
		}

		public void Unregister(ILogProviderFactory fact)
		{
			if (!items.Remove(fact))
				throw new InvalidOperationException("Cannot unregister the factory that was not registered");
		}

		public IEnumerable<ILogProviderFactory> Items
		{
			get { return items; }
		}

		public ILogProviderFactory Find(string companyName, string formatName)
		{
			foreach (ILogProviderFactory fact in Items)
			{
				if (fact.CompanyName == companyName && fact.FormatName == formatName)
				{
					return fact;
				}
			}
			return null;
		}

		#endregion

		readonly List<ILogProviderFactory> items = new List<ILogProviderFactory>();
		static readonly LogProviderFactoryRegistry instance = new LogProviderFactoryRegistry();
	};
}
