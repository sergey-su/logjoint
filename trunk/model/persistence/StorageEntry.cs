using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace LogJoint.Persistence
{
	internal class StorageEntry: IStorageEntry
	{
		public IStorageSection OpenSection(string sectionKey, StorageSectionType type, StorageSectionAccess accessType)
		{
			return null;
		}
	};
}
