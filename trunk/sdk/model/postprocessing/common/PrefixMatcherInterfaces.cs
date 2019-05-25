using System.Collections.Generic;

namespace LogJoint.Postprocessing
{
	public interface IPrefixMatcher
	{
		int RegisterPrefix(string prefix);
		void Freeze();
		IMatchedPrefixesCollection Match(string str);
	};


	public interface IMatchedPrefixesCollection: IEnumerable<int>
	{
		bool Contains(int prefixId);
	};
}
