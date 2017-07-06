using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public class FiltersFactory: IFiltersFactory
	{
		public static readonly IFilterScope defaultTarget = new FilterScope();

		IFilterScope IFiltersFactory.CreateScope()
		{
			return defaultTarget; // targets are immutable, ok to reuse the same object
		}

		IFilterScope IFiltersFactory.CreateScope(IEnumerable<ILogSource> sources, IEnumerable<IThread> threads)
		{
			return new FilterScope(sources, threads);
		}

		IFilter IFiltersFactory.CreateFilter(FilterAction type, string initialName, bool enabled, Search.Options searchOptions)
		{
			return new Filter(type, initialName, enabled, searchOptions, this);
		}


		IFiltersList IFiltersFactory.CreateFiltersList(FilterAction actionWhenEmptyOrDisabled)
		{
			return new FiltersList(actionWhenEmptyOrDisabled);
		}
	};
}
