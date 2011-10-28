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

		public IStorageSection OpenSection(string sectionKey, StorageSectionType type, StorageSectionOpenFlag openFlags)
		{
			return CreateSectionImpl(type, sectionKey, openFlags);
		}

		StorageSectionBase CreateSectionImpl(StorageSectionType type, string sectionKey, StorageSectionOpenFlag openFlags)
		{
			switch (type)
			{
				case StorageSectionType.XML:
					return new XmlStorageSection(manager, this, sectionKey, openFlags);
				case StorageSectionType.RawStream:
					return new BinaryStorageSection(manager, this, sectionKey, openFlags);
				default:
					throw new ArgumentException("wrong type: " + type.ToString());
			}
		}

		readonly StorageManager manager;
		readonly string key;
		readonly string path;
	};
}
