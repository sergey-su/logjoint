using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint
{
	public class FiltersFactory: IFiltersFactory
	{
		public static readonly IFilterScope DefaultScope = new FilterScope();

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

		IFiltersList IFiltersFactory.CreateFiltersList(FilterAction actionWhenEmptyOrDisabled)
		{
			return new FiltersList(actionWhenEmptyOrDisabled);
		}

		IFiltersList IFiltersFactory.CreateFiltersList(XElement e)
		{
			return new FiltersList(e, this);
		}
	};
}
