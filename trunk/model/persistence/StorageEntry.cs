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

		public void ReadCleanupInfo()
		{
			using (Stream s = manager.Implementation.OpenFile(CleanupInfoFilePath, true))
				cleanupAllowed = s != null;
		}

		public void WriteCleanupInfoIfCleanupAllowed()
		{
			if (cleanupAllowed)
				using (Stream s = manager.Implementation.OpenFile(CleanupInfoFilePath, false))
				{
					var cleanupInfoData = Encoding.ASCII.GetBytes(DateTime.Now.ToString(cleanupInfoLastAccessFormat));
					s.Position = 0;
					s.Write(cleanupInfoData, 0, cleanupInfoData.Length);
					s.SetLength(cleanupInfoData.Length);
				}
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

		public void AllowCleanup()
		{
			if (!cleanupAllowed)
			{
				cleanupAllowed = true;
				WriteCleanupInfoIfCleanupAllowed();
			}
		}

		string CleanupInfoFilePath
		{
			get { return Path + System.IO.Path.DirectorySeparatorChar + cleanupInfoFileName; }
		}

		readonly StorageManager manager;
		readonly string key;
		readonly string path;
		bool cleanupAllowed;
		internal static readonly string cleanupInfoFileName = "cleanup.info";
		internal static readonly string cleanupInfoLastAccessFormat = "LA=yyyy/MM/dd HH:mm:ss.fff";
	};
}
