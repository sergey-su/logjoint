using System.Collections.Generic;

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
