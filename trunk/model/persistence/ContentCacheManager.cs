using System;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint.Persistence
{
	public class ContentCacheManager: IContentCache, IDisposable
	{
		public ContentCacheManager(ITraceSourceFactory traceSourceFactory, Implementation.IStorageManagerImplementation impl)
		{
			this.trace = traceSourceFactory.CreateTraceSource("ContentCache", "cache");
			this.impl = impl;
			this.impl.SetTrace(trace);
		}

		void IDisposable.Dispose()
		{
			impl.Dispose();
		}

		async Task<Stream> IContentCache.GetValue(string key)
		{
			var entry = await GetEntry(key);
			var section = await entry .OpenRawStreamSection(dataSectionName, StorageSectionOpenFlag.ReadOnly);
			if (!await ((Implementation.IStorageSectionInternal)section).ExistsInFileSystem())
			{
				section.Dispose();
				return null;
			}
			return new CachedStream(section);
		}

		async Task IContentCache.SetValue(string key, Stream data)
		{
			var entry = await GetEntry(key);
			using (var section = await entry .OpenRawStreamSection(dataSectionName, StorageSectionOpenFlag.ReadWrite))
			{
				await data.CopyToAsync(section.Data);
			}
		}

		private async Task<IStorageEntry> GetEntry(string key)
		{
			var entry = await impl.GetEntry(key, 0);
			await entry.AllowCleanup();
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
