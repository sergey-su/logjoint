using System;
using System.Collections.Generic;

namespace LogJoint
{
	class GenericRollingMediaStrategy : IRollingFilesMediaStrategy
	{
		readonly string baseDirectory;
		readonly IEnumerable<string> searchPatterns;

		public GenericRollingMediaStrategy(string baseDirectory, IEnumerable<string> searchPatterns)
		{
			this.baseDirectory = baseDirectory ?? throw new ArgumentNullException(nameof(baseDirectory));
			var searchPatternsSet = searchPatterns.ToHashSet();
			if (searchPatternsSet.Count == 0)
			{
				searchPatternsSet.Add("*.*");
			}
			this.searchPatterns = searchPatternsSet;
		}

		string IRollingFilesMediaStrategy.BaseDirectory => baseDirectory;

		IEnumerable<string> IRollingFilesMediaStrategy.SearchPatterns => searchPatterns;

		bool IRollingFilesMediaStrategy.IsFileARolledLog(string fileNameToTest)
		{
			return true;
		}
	};
}