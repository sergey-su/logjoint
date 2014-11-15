using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
	public class FiltersFactory: IFiltersFactory
	{
		public static readonly IFilterTarget defaultTarget = new FilterTarget();

		IFilterTarget IFiltersFactory.CreateFilterTarget()
		{
			return defaultTarget; // targets are immutable, ok to reuse the same object
		}

		IFilterTarget IFiltersFactory.CreateFilterTarget(IEnumerable<ILogSource> sources, IEnumerable<IThread> threads)
		{
			return new FilterTarget(sources, threads);
		}

		IFilter IFiltersFactory.CreateFilter(FilterAction type, string initialName, bool enabled, string template, bool wholeWord, bool regExp, bool matchCase)
		{
			return new Filter(type, initialName, enabled, template, wholeWord, regExp, matchCase, this);
		}


		IFiltersList IFiltersFactory.CreateFiltersList(FilterAction actionWhenEmptyOrDisabled)
		{
			return new FiltersList(actionWhenEmptyOrDisabled);
		}
	};
}
