using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
    public class LogProviderFactoryRegistry : ILogProviderFactoryRegistry
    {
        static public string ToString(ILogProviderFactory factory)
        {
            if (!string.IsNullOrEmpty(factory.CompanyName))
                return factory.CompanyName + "\\" + factory.FormatName;
            if (!string.IsNullOrEmpty(factory.FormatName))
                return factory.FormatName;
            return factory.ToString();
        }


        void ILogProviderFactoryRegistry.Register(ILogProviderFactory fact)
        {
            if (items.IndexOf(fact) < 0)
                items.Add(fact);
        }

        void ILogProviderFactoryRegistry.Unregister(ILogProviderFactory fact)
        {
            if (!items.Remove(fact))
                throw new InvalidOperationException("Cannot unregister the factory that was not registered");
        }

        IEnumerable<ILogProviderFactory> ILogProviderFactoryRegistry.Items
        {
            get { return items; }
        }

        ILogProviderFactory ILogProviderFactoryRegistry.Find(string companyName, string formatName)
        {
            foreach (ILogProviderFactory fact in items)
            {
                if (fact.CompanyName == companyName && fact.FormatName == formatName)
                {
                    return fact;
                }
            }
            return null;
        }


        readonly List<ILogProviderFactory> items = new List<ILogProviderFactory>();
    };
}
