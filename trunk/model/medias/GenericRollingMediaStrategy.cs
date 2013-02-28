using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogJoint
{
	class GenericRollingMediaStrategy : IRollingFilesMediaStrategy
	{
		readonly string baseDirectory;

		public GenericRollingMediaStrategy(string baseDirectory)
		{
			if (baseDirectory == null)
				throw new ArgumentNullException("baseDirectory");
			this.baseDirectory = baseDirectory;
		}

		string IRollingFilesMediaStrategy.BaseDirectory
		{
			get { return baseDirectory; }
		}

		string IRollingFilesMediaStrategy.InitialSearchFilter
		{
			get { return "*.*"; }
		}

		bool IRollingFilesMediaStrategy.IsFileARolledLog(string fileNameToTest)
		{
			return true;
		}
	};
}