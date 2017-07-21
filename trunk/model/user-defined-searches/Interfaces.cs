using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace LogJoint
{
	public interface IUserDefinedSearches
	{
		IEnumerable<IUserDefinedSearch> Items { get; }
	};

	public interface IUserDefinedSearch
	{
		string Name { get; }
		IFiltersList Filters { get; }
	};
}
