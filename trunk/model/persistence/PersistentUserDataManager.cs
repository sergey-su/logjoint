using System;
using System.Threading.Tasks;

namespace LogJoint.Persistence
{
	public class PersistentUserDataManager: IStorageManager, IDisposable
	{
		public PersistentUserDataManager(ITraceSourceFactory traceSourceFactory, Implementation.IStorageManagerImplementation impl, IShutdown shutdown)
		{
			this.trace = traceSourceFactory.CreateTraceSource("Storage", "storage");
			this.impl = impl; 
			this.impl.SetTrace(trace);
			this.globalSettingsEntry = new Lazy<IStorageEntry>(() => impl.GetEntry("global", 0));
			shutdown.Cleanup += (sender, e) => impl.Dispose();
		}

		void IDisposable.Dispose()
		{
			impl.Dispose();
		}

		Task<IStorageEntry> IStorageManager.GetEntry(string entryKey, ulong additionalNumericKey)
		{
			return Task.FromResult(impl.GetEntry(entryKey, additionalNumericKey));
		}


		IStorageEntry IStorageManager.GlobalSettingsEntry
		{
			get { return globalSettingsEntry.Value; }
		}

		IStorageEntry IStorageManager.GetEntryById(string id)
		{
			return impl.GetEntryById(id);
		}

		ulong IStorageManager.MakeNumericKey(string stringToBeHashed)
		{
			return impl.MakeNumericKey(stringToBeHashed);
		}

		#region Members

		readonly LJTraceSource trace;
		readonly Implementation.IStorageManagerImplementation impl;
		readonly Lazy<IStorageEntry> globalSettingsEntry;

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
				get { return settings.UserDataStorageSizes.StoreSizeLimit; }
			}

			int Implementation.IStorageConfigAccess.CleanupPeriod
			{
				get { return settings.UserDataStorageSizes.CleanupPeriod; }
			}
		};
	};
}
