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
	class UserDefinedSearch : IUserDefinedSearch
	{
		string name;
		IFiltersList filters;

		public UserDefinedSearch(
			string name,
			IFiltersList filtersList
		)
		{
			this.name = name;
			this.filters = filtersList;
		}

		string IUserDefinedSearch.Name => name;
		IFiltersList IUserDefinedSearch.Filters => filters;
	};
}
