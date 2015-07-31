using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Persistence
{
	public class ContentCacheManager: IContentCache, IDisposable
	{
		public ContentCacheManager(Implementation.IStorageManagerImplementation impl)
		{
			this.trace = new LJTraceSource("ContentCache", "cache");
			this.impl = impl;
			this.impl.SetTrace(trace);
		}

		void IDisposable.Dispose()
		{
			impl.Dispose();
		}

		Stream IContentCache.GetValue(string key)
		{
			var entry = GetEntry(key);
			var section = entry.OpenRawStreamSection(dataSectionName, StorageSectionOpenFlag.ReadOnly);
			if (!((Implementation.IStorageSectionInternal)section).ExistsInFileSystem)
			{
				section.Dispose();
				return null;
			}
			return new CachedStream(section);
		}

		async Task IContentCache.SetValue(string key, Stream data)
		{
			var entry = GetEntry(key);
			using (var section = entry.OpenRawStreamSection(dataSectionName, StorageSectionOpenFlag.ReadWrite))
			{
				await data.CopyToAsync(section.Data);
			}
		}

		private IStorageEntry GetEntry(string key)
		{
			var entry = impl.GetEntry(key, 0);
			entry.AllowCleanup();
			return entry;
		}

		#region Members

		readonly LJTraceSource trace;
		readonly Implementation.IStorageManagerImplementation impl;
		readonly string dataSectionName = "data";

		#endregion

		public class ConfigAccess : Implementation.IStorageConfigAccess
		{
			readonly Settings.IGlobalSettingsAccessor settings;

			public ConfigAccess(Settings.IGlobalSettingsAccessor settings)
			{
				this.settings = settings;
			}

			long Implementation.IStorageConfigAccess.SizeLimit
			{
				get { return settings.ContentStorageSizes.StoreSizeLimit; }
			}

			int Implementation.IStorageConfigAccess.CleanupPeriod
			{
				get { return settings.ContentStorageSizes.CleanupPeriod; }
			}
		};

		class CachedStream : DelegatingStream
		{
			readonly IRawStreamStorageSection section;

			public CachedStream(IRawStreamStorageSection section)
				: base(section.Data)
			{
				this.section = section;
			}

			protected override void Dispose(bool disposing)
			{
				if (disposing)
					section.Dispose();
				base.Dispose(disposing);
			}
		};
	};
}
