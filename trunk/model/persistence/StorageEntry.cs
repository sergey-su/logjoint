using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace LogJoint.Persistence
{
	internal class StorageEntry: IStorageEntry
	{
		public StorageEntry(StorageManager owner, string path)
		{
			this.path = path;
			this.key = path;
			this.manager = owner;
		}

		public void EnsureCreated()
		{
			manager.Implementation.EnsureDirectoryCreated(path);
		}

		public string Path { get { return path; } }
		public string Key { get { return key; } }

		public IXMLStorageSection OpenXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
		{
			return new XmlStorageSection(manager, this, sectionKey, additionalNumericKey, openFlags);
		}

		public IRawStreamStorageSection OpenRawStreamSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
		{
			return new BinaryStorageSection(manager, this, sectionKey, additionalNumericKey, openFlags);
		}

		readonly StorageManager manager;
		readonly string key;
		readonly string path;
	};
}
