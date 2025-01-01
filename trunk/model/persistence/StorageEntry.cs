using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using System.Runtime.CompilerServices;

namespace LogJoint.Persistence.Implementation
{
    internal class StorageEntry : IStorageEntry, IStorageEntryInternal
    {
        public StorageEntry(StorageManagerImplementation owner, string path)
        {
            this.path = path;
            this.key = path;
            this.manager = owner;
        }

        public Task EnsureCreated()
        {
            return manager.FileSystem.EnsureDirectoryCreated(path);
        }

        public async ValueTask ReadCleanupInfo()
        {
            using Stream s = await manager.FileSystem.OpenFile(CleanupInfoFilePath, true);
            cleanupAllowed = s != null;
        }

        public async ValueTask WriteCleanupInfoIfCleanupAllowed()
        {
            if (cleanupAllowed)
                using (Stream s = await manager.FileSystem.OpenFile(CleanupInfoFilePath, false))
                {
                    var cleanupInfoData = Encoding.ASCII.GetBytes(DateTime.Now.ToString(cleanupInfoLastAccessFormat,
                            System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat));
                    s.Position = 0;
                    await s.WriteAsync(cleanupInfoData, 0, cleanupInfoData.Length);
                    s.SetLength(cleanupInfoData.Length);
                    await s.FlushAsync();
                }
        }

        public string Path { get { return path; } }
        public string Key { get { return key; } }

        string IStorageEntry.Id { get { return key; } }

        Task<IXMLStorageSection> IStorageEntry.OpenXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
        {
            return XmlStorageSection.Create(manager, this, sectionKey, additionalNumericKey, openFlags);
        }

        Task<ISaxXMLStorageSection> IStorageEntry.OpenSaxXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
        {
            return SaxXmlStorageSection.Create(manager, this, sectionKey, additionalNumericKey, openFlags);
        }

        Task<IRawStreamStorageSection> IStorageEntry.OpenRawStreamSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
        {
            return BinaryStorageSection.Create(manager, this, sectionKey, additionalNumericKey, openFlags);
        }

        Task<IRawStreamStorageSection> IStorageEntryInternal.OpenRawXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey)
        {
            return BinaryStorageSection.Create(manager, this, sectionKey, additionalNumericKey, openFlags, XmlStorageSection.KeyPrefix);
        }

        async Task IStorageEntry.AllowCleanup()
        {
            if (!cleanupAllowed)
            {
                cleanupAllowed = true;
                await WriteCleanupInfoIfCleanupAllowed();
            }
        }

        async IAsyncEnumerable<SectionInfo> IStorageEntry.EnumSections([EnumeratorCancellation] CancellationToken cancellation)
        {
            foreach (var sectionFile in await manager.FileSystem.ListFiles(path, cancellation))
            {
                var sectionInfo = StorageManagerImplementation.ParseNormalizedSectionKey(System.IO.Path.GetFileName(sectionFile));
                if (sectionInfo == null)
                    continue;
                yield return sectionInfo.Value;
            }
        }

        async Task IStorageEntry.TakeSectionSnapshot(string sectionId, Stream targetStream)
        {
            using var fs = await manager.FileSystem.OpenFile(Path + System.IO.Path.DirectorySeparatorChar + sectionId, readOnly: true);
            if (fs != null)
                await fs.CopyToAsync(targetStream);
        }

        async Task IStorageEntry.LoadSectionFromSnapshot(string sectionId, Stream sourceStream, CancellationToken cancellation)
        {
            using var fs = await manager.FileSystem.OpenFile(Path + System.IO.Path.DirectorySeparatorChar + sectionId, readOnly: false);
            fs.SetLength(0);
            fs.Position = 0;
            await sourceStream.CopyToAsync(fs, 4000, cancellation);
            await fs.FlushAsync();
        }

        string CleanupInfoFilePath
        {
            get { return Path + System.IO.Path.DirectorySeparatorChar + cleanupInfoFileName; }
        }

        readonly StorageManagerImplementation manager;
        readonly string key;
        readonly string path;
        bool cleanupAllowed;
        internal static readonly string cleanupInfoFileName = "cleanup.info";
        internal static readonly string cleanupInfoLastAccessFormat = "LA=yyyy/MM/dd HH:mm:ss.fff";
    };
}
