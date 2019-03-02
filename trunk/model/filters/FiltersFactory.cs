using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint
{
	public class FiltersFactory: IFiltersFactory
	{
		readonly IChangeNotification changeNotification;

		public static readonly IFilterScope DefaultScope = new FilterScope();

		public FiltersFactory(IChangeNotification changeNotification)
		{
			this.changeNotification = changeNotification;
		}

		IFilterScope IFiltersFactory.CreateScope()
		{
			return DefaultScope; // targets are immutable, ok to reuse the same object
		}

		IFilterScope IFiltersFactory.CreateScope(IEnumerable<ILogSource> sources, IEnumerable<IThread> threads)
		{
			return new FilterScope(sources, threads);
		}

		IFilter IFiltersFactory.CreateFilter(FilterAction type, string initialName, bool enabled, Search.Options searchOptions)
		{
			return new Filter(type, initialName, enabled, searchOptions, this);
		}

		IFilter IFiltersFactory.CreateFilter(XElement e)
		{
			return new Filter(e, this);
		}

		IFiltersList IFiltersFactory.CreateFiltersList(FilterAction actionWhenEmptyOrDisabled, FiltersListPurpose purpose)
		{
			return new FiltersList(actionWhenEmptyOrDisabled, purpose, changeNotification);
		}

		IFiltersList IFiltersFactory.CreateFiltersList(XElement e, FiltersListPurpose purpose)
		{
			return new FiltersList(e, purpose, this, changeNotification);
		}
	};
}
