using System;

namespace LogJoint.Persistence
{
	public class PersistentUserDataManager: IStorageManager, IDisposable
	{
		public PersistentUserDataManager(Implementation.IStorageManagerImplementation impl)
		{
			this.trace = new LJTraceSource("Storage", "storage");
			this.impl = impl; 
			this.impl.SetTrace(trace);
			this.globalSettingsEntry = new Lazy<IStorageEntry>(() => impl.GetEntry("global", 0));
		}

		void IDisposable.Dispose()
		{
			impl.Dispose();
		}

		IStorageEntry IStorageManager.GetEntry(string entryKey, ulong additionalNumericKey)
		{
			return impl.GetEntry(entryKey, additionalNumericKey);
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
				get { return settings.StorageSizes.StoreSizeLimit; }
			}

			int Implementation.IStorageConfigAccess.CleanupPeriod
			{
				get { return settings.StorageSizes.CleanupPeriod; }
			}
		};
	};
}
