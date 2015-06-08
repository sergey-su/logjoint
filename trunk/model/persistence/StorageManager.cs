using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Persistence
{
	public class StorageManager: IStorageManager, IDisposable
	{
		public StorageManager(IEnvironment env, IStorageImplementation impl)
		{
			this.trace = new LJTraceSource("Storage");
			this.env = env;
			this.storageImpl = impl;
			impl.SetTrace(trace);
			this.globalSettingsEntry = new Lazy<IStorageEntry>(() => GetEntry("global"));
			this.globalSettingsAccessor = new Lazy<Settings.IGlobalSettingsAccessor>(() => env.CreateSettingsAccessor(this));
			DoCleanupIfItIsTimeTo();
		}

		public void Dispose()
		{
			if (cleanupTask != null)
			{
				cleanupCancellation.Cancel();
				cleanupTask.Wait();
				cleanupTask.Dispose();
				cleanupCancellation.Dispose();
				cleanupTask = null;
				cleanupCancellation = null;
			}
		}

		public IStorageEntry GetEntry(string entryKey)
		{
			return GetEntry(entryKey, 0);
		}

		public IStorageEntry GetEntry(string entryKey, ulong additionalNumericKey)
		{
			if (string.IsNullOrWhiteSpace(entryKey))
				throw new ArgumentException("Wrong entryKey");
			string normalizedKey = NormalizeKey(entryKey, additionalNumericKey, entryKeyPrefix);
			StorageEntry entry;
			if (!entriesCache.TryGetValue(normalizedKey, out entry))
			{
				trace.Info("Entry with key {0} does not exist in the cache. Creating.", normalizedKey);
				entry = new StorageEntry(this, normalizedKey);
				entriesCache.Add(normalizedKey, entry);
			}
			entry.EnsureCreated();
			entry.ReadCleanupInfo();
			entry.WriteCleanupInfoIfCleanupAllowed();
			return entry;
		}

		IStorageEntry IStorageManager.GlobalSettingsEntry
		{
			get { return globalSettingsEntry.Value; }
		}

		Settings.IGlobalSettingsAccessor IStorageManager.GlobalSettingsAccessor
		{
			get { return globalSettingsAccessor.Value; }
		}

		public ulong MakeNumericKey(string stringToBeHashed)
		{
			return GetStringHash(stringToBeHashed);
		}

		internal IStorageImplementation Implementation
		{
			get { return storageImpl; }
		}

		#region Implementation

		internal static string NormalizeKey(string key, ulong additionalNumericKey, string keyPrefix)
		{
			var maxKeyTailLength = 120;
			var tail = key.Length < maxKeyTailLength ? key : key.Substring(key.Length - maxKeyTailLength, maxKeyTailLength);
			return string.Format("{0}-{1:X}-{2}", keyPrefix, GetStringHash(key) ^ additionalNumericKey, MakeValidFileName(tail));
		}

		/// <summary>
		/// Caclulates string hash. Algorithms doesn't use string.GetHashCode() to make sure 
		/// the value doesn't depend on the framework version.
		/// </summary>
		static ulong GetStringHash(string str)
		{
			var longHash = sha1.ComputeHash(Encoding.Unicode.GetBytes(str));
			var shortHash = new byte[8];
			for (int i = 0; i < longHash.Length; ++i)
				shortHash[i % shortHash.Length] ^= longHash[i];
			return BitConverter.ToUInt64(shortHash, 0);
		}

		/// <summary>
		/// Converts string to valid file name by replacing invalid filename characters with _
		/// </summary>
		static string MakeValidFileName(string str)
		{
			return new string(str.Select(c => invalidKeyChars.IndexOf(c) < 0 ? c : '_').ToArray());
		}

		void DoCleanupIfItIsTimeTo()
		{
			bool timeToCleanup = false;
			using (var cleanupInfoStream = Implementation.OpenFile("cleanup.info", false))
			{
				cleanupInfoStream.Position = 0;
				var cleanupInfoContent = (new StreamReader(cleanupInfoStream, Encoding.ASCII)).ReadToEnd();
				string lastCleanupFormat = "LC=yyyy/MM/dd HH:mm:ss";
				var dateFmtProvider = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat;
				DateTime lastCleanupDate;
				if (!DateTime.TryParseExact(cleanupInfoContent, lastCleanupFormat, dateFmtProvider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out lastCleanupDate))
					lastCleanupDate = new DateTime();
				var now = env.Now;
				var elapsedSinceLastCleanup = now - lastCleanupDate;
				if (elapsedSinceLastCleanup > TimeSpan.FromHours(Settings.StorageSizes.MinCleanupPeriod)
				 && elapsedSinceLastCleanup > TimeSpan.FromHours(globalSettingsAccessor.Value.StorageSizes.CleanupPeriod))
				{
					trace.Info("Time to cleanup! Last cleanup time: {0}", lastCleanupDate);
					timeToCleanup = true;
					cleanupInfoStream.SetLength(0);
					var w = new StreamWriter(cleanupInfoStream, Encoding.ASCII);
					w.Write(now.ToString(lastCleanupFormat, dateFmtProvider));
					w.Flush();
				}
			}
			if (timeToCleanup)
			{
				cleanupCancellation = new CancellationTokenSource();
				cleanupTask = env.StartCleanupWorker(CleanupWorker);
			}
		}

		internal void CleanupWorker()
		{
			using (trace.NewFrame)
			try
			{
				var cancellationToken = cleanupCancellation.Token;
				long sz = Implementation.CalcStorageSize(cancellationToken);
				trace.Info("Storage size: {0}", sz);
				int meg = 1024 * 1024;
				if (sz < Settings.StorageSizes.MinStoreSizeLimit * meg
				 || sz < globalSettingsAccessor.Value.StorageSizes.StoreSizeLimit * meg)
				{
					trace.Info("Storage size has not exceeded the capacity");
					return;
				}
				var dateFmtProvider = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat;
				var dirs = Implementation.ListDirectories("", cancellationToken).Select(dir =>
				{
					cancellationToken.ThrowIfCancellationRequested();
					using (var s = Implementation.OpenFile(dir + Path.DirectorySeparatorChar + StorageEntry.cleanupInfoFileName, true))
					{
						trace.Info("Handling '{0}'", dir);
						if (s == null)
						{
							trace.Info("No {0}", StorageEntry.cleanupInfoFileName);
							return null;
						}
						var cleanupInfoContent = (new StreamReader(s, Encoding.ASCII)).ReadToEnd();
						DateTime lastAccessed;
						if (!DateTime.TryParseExact(cleanupInfoContent, StorageEntry.cleanupInfoLastAccessFormat,
								dateFmtProvider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out lastAccessed))
						{
							trace.Warning("Could not parse '{0}'", cleanupInfoContent);
							return null;
						}
						trace.Info("Last accessed on {0}", lastAccessed);
						return new { RelativeDirPath = dir, LastAccess = lastAccessed };
					}
				}).Where(dir => dir != null).OrderBy(dir => dir.LastAccess).ToArray();
				var dirsToDelete = Math.Max(1, dirs.Length / 3);
				trace.Info("Found {0} deletable dirs. Deleting top {1}", dirs.Length, dirsToDelete);
				foreach (var dir in dirs.Take(dirsToDelete))
				{
					trace.Info("Deleting '{0}'", dir.RelativeDirPath);
					cancellationToken.ThrowIfCancellationRequested();
					Implementation.DeleteDirectory(dir.RelativeDirPath);
				}
			}
			catch (OperationCanceledException)
			{
				trace.Warning("Operation cancelled");
			}
			catch (Exception e)
			{
				trace.Error(e, "Cleanup failed");
			}
		}

		#endregion

		#region Members

		static readonly string invalidKeyChars = new string(Path.GetInvalidFileNameChars());
		static readonly string entryKeyPrefix = "e";
		static SHA1 sha1 = new SHA1CryptoServiceProvider();
		readonly LJTraceSource trace;
		readonly IEnvironment env;
		readonly IStorageImplementation storageImpl;
		readonly Dictionary<string, StorageEntry> entriesCache = new Dictionary<string, StorageEntry>();
		readonly Lazy<IStorageEntry> globalSettingsEntry;
		readonly Lazy<Settings.IGlobalSettingsAccessor> globalSettingsAccessor;
		CancellationTokenSource cleanupCancellation;
		Task cleanupTask;

		#endregion
	};
}
