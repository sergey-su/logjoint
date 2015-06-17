using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;

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

		string IStorageEntry.Id { get { return key; } }

		IXMLStorageSection IStorageEntry.OpenXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
		{
			return new XmlStorageSection(manager, this, sectionKey, additionalNumericKey, openFlags);
		}

		IRawStreamStorageSection IStorageEntry.OpenRawStreamSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
		{
			return new BinaryStorageSection(manager, this, sectionKey, additionalNumericKey, openFlags);
		}

		void IStorageEntry.AllowCleanup()
		{
			if (!cleanupAllowed)
			{
				cleanupAllowed = true;
				WriteCleanupInfoIfCleanupAllowed();
			}
		}

		IEnumerable<SectionInfo> IStorageEntry.EnumSections(CancellationToken cancellation)
		{
			foreach (var sectionFile in manager.Implementation.ListFiles(path, cancellation))
			{
				var sectionInfo = StorageManager.ParseNormalizedSectionKey(System.IO.Path.GetFileName(sectionFile));
				if (sectionInfo == null)
					continue;
				yield return sectionInfo.Value;
			}
		}

		async Task IStorageEntry.TakeSectionSnapshot(string sectionId, Stream targetStream)
		{
			using (var fs = manager.Implementation.OpenFile(Path + System.IO.Path.DirectorySeparatorChar + sectionId, readOnly: true))
			{
				if (fs != null)
					await fs.CopyToAsync(targetStream);
			}
		}

		async Task IStorageEntry.LoadSectionFromSnapshot(string sectionId, Stream sourceStream)
		{
			using (var fs = manager.Implementation.OpenFile(Path + System.IO.Path.DirectorySeparatorChar + sectionId, readOnly: false))
			{
				fs.SetLength(0);
				fs.Position = 0;
				await sourceStream.CopyToAsync(fs);
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
