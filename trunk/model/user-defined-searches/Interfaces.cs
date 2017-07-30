using System;
using System.Collections.Generic;
using System.Xml.Linq;

namespace LogJoint
{
	public interface IUserDefinedSearches
	{
		IEnumerable<IUserDefinedSearch> Items { get; }
		bool ContainsItem(string name);
	};

	public interface IUserDefinedSearch
	{
		string Name { get; set; }
		IFiltersList Filters { get; set; }
	};

	public class NameDuplicateException: Exception
	{
	};

	internal interface IUserDefinedSearchesInternal: IUserDefinedSearches
	{
		void OnNameChanged(IUserDefinedSearch sender, string oldName);
		void OnFiltersChanged(IUserDefinedSearch sender);
	};
}
