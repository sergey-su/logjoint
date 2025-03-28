using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace LogJoint.Persistence.Implementation
{
    public class StorageManagerImplementation : IStorageManagerImplementation, IDisposable
    {
        public StorageManagerImplementation()
        {
            this.trace = LJTraceSource.EmptyTracer;
            this.ready = DoCleanupIfItIsTimeTo();
        }

        void IDisposable.Dispose()
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

        void IStorageManagerImplementation.SetTrace(LJTraceSource trace)
        {
            this.trace = trace;
        }

        void IStorageManagerImplementation.Init(ITimingAndThreading timingThreading, IFileSystemAccess fs, IStorageConfigAccess config)
        {
            this.env = timingThreading;
            this.fs = fs;
            this.config = config;
            this.fs.SetTrace(trace);
            inited.SetResult(1);
        }

        async Task<IStorageEntry> IStorageManagerImplementation.GetEntry(string entryKey, ulong additionalNumericKey)
        {
            if (string.IsNullOrWhiteSpace(entryKey))
                throw new ArgumentException("Wrong entryKey");
            await ready;
            string id = NormalizeKey(entryKey, additionalNumericKey, entryKeyPrefix);
            return await GetEntryById(id);
        }


        Task<IStorageEntry> IStorageManagerImplementation.GetEntryById(string id)
        {
            if (!ValidateNormalizedEntryKey(id))
                throw new ArgumentException("id");
            return GetEntryById(id);
        }

        ulong IStorageManagerImplementation.MakeNumericKey(string stringToBeHashed)
        {
            return GetStringHash(stringToBeHashed);
        }


        #region Implementation

        internal IFileSystemAccess FileSystem
        {
            get { return fs; }
        }

        private async Task<IStorageEntry> GetEntryById(string id)
        {
            StorageEntry entry;
            using (await SemaphoreSlimLock.Create(sync))
            {
                if (!entriesCache.TryGetValue(id, out entry))
                {
                    trace.Info("Entry with key {0} does not exist in the cache. Creating.", id);
                    entry = new StorageEntry(this, id);
                    entriesCache.Add(id, entry);
                }
                await entry.EnsureCreated();
                await entry.ReadCleanupInfo();
                await entry.WriteCleanupInfoIfCleanupAllowed();
            }
            return entry;
        }

        internal static string NormalizeKey(string key, ulong additionalNumericKey, string keyPrefix)
        {
            var maxKeyTailLength = 120;
            var tail = key.Length < maxKeyTailLength ? key : key.Substring(key.Length - maxKeyTailLength, maxKeyTailLength);
            return string.Format("{0}-{1:X}-{2}", keyPrefix, GetStringHash(key) ^ additionalNumericKey, MakeValidFileName(tail));
        }

        internal static SectionInfo? ParseNormalizedSectionKey(string key)
        {
            var m = Regex.Match(key, @"^(\w)\-\w+\-(.+)$");
            if (!m.Success)
                return null;
            SectionInfo info = new SectionInfo()
            {
                Id = key,
                Key = m.Groups[2].Value
            };
            switch (m.Groups[1].Value)
            {
                case XmlStorageSection.KeyPrefix:
                    info.Type = SectionType.Xml;
                    break;
                case BinaryStorageSection.KeyPrefix:
                    info.Type = SectionType.Raw;
                    break;
                default:
                    return null;
            }
            return info;
        }

        internal static bool ValidateNormalizedEntryKey(string key)
        {
            var m = Regex.Match(key, @"^e\-\w+\-.+$");
            return m.Success;
        }

        /// <summary>
        /// Caclulates string hash. Algorithms doesn't use string.GetHashCode() to make sure 
        /// the value doesn't depend on the framework version.
        /// </summary>
        static ulong GetStringHash(string str)
        {
            if (IsBrowser.Value)
            {
                return (ulong)Hashing.GetStableHashCode(str);
            }
            else
            {
                using SHA1 sha1 = SHA1.Create();
                var longHash = sha1.ComputeHash(Encoding.Unicode.GetBytes(str));
                var shortHash = new byte[8];
                for (int i = 0; i < longHash.Length; ++i)
                    shortHash[i % shortHash.Length] ^= longHash[i];
                return BitConverter.ToUInt64(shortHash, 0);
            }
        }

        /// <summary>
        /// Converts string to valid file name by replacing invalid filename characters with _
        /// </summary>
        static string MakeValidFileName(string str)
        {
            return new string(str.Select(c => invalidKeyChars.IndexOf(c) < 0 ? c : '_').ToArray());
        }

        async Task DoCleanupIfItIsTimeTo()
        {
            await inited.Task;
            bool timeToCleanup = false;
            using (var cleanupInfoStream = await FileSystem.OpenFile("cleanup.info", readOnly: false))
            {
                cleanupInfoStream.Position = 0;
                var cleanupInfoContent = await (new StreamReader(cleanupInfoStream, Encoding.ASCII)).ReadToEndAsync();
                string lastCleanupFormat = "LC=yyyy/MM/dd HH:mm:ss";
                var dateFmtProvider = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat;
                if (!DateTime.TryParseExact(cleanupInfoContent, lastCleanupFormat, dateFmtProvider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out DateTime lastCleanupDate))
                    lastCleanupDate = new DateTime();
                var now = env.Now;
                var elapsedSinceLastCleanup = now - lastCleanupDate;
                if (elapsedSinceLastCleanup > TimeSpan.FromHours(Settings.StorageSizes.MinCleanupPeriod)
                 && elapsedSinceLastCleanup > TimeSpan.FromHours(config.CleanupPeriod))
                {
                    trace.Info("Time to cleanup! Last cleanup time: {0}", lastCleanupDate);
                    timeToCleanup = true;
                    cleanupInfoStream.SetLength(0);
                    using (var w = new StreamWriter(cleanupInfoStream, Encoding.ASCII, 1024, leaveOpen: true))
                    {
                        await w.WriteAsync(now.ToString(lastCleanupFormat, dateFmtProvider));
                        await w.FlushAsync();
                    }
                    await cleanupInfoStream.FlushAsync();
                }
            }
            if (timeToCleanup)
            {
                cleanupCancellation = new CancellationTokenSource();
                cleanupTask = env.StartTask(CleanupWorker);
            }
        }

        internal async Task CleanupWorker()
        {
            using (trace.NewFrame)
                try
                {
                    var cancellationToken = cleanupCancellation.Token;
                    long sz = await FileSystem.CalcStorageSize(cancellationToken);
                    trace.Info("Storage size: {0}", sz);
                    int meg = 1024 * 1024;
                    if (sz < Settings.StorageSizes.MinStoreSizeLimit * meg
                     || sz < config.SizeLimit * meg)
                    {
                        trace.Info("Storage size has not exceeded the capacity");
                        return;
                    }
                    var dateFmtProvider = System.Globalization.CultureInfo.InvariantCulture.DateTimeFormat;
                    var dirs = await Task.WhenAll((await FileSystem.ListDirectories("", cancellationToken)).Select(async dir =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        using var s = await FileSystem.OpenFile(dir + Path.DirectorySeparatorChar + StorageEntry.cleanupInfoFileName, readOnly: true);
                        trace.Info("Handling '{0}'", dir);
                        if (s == null)
                        {
                            trace.Info("No {0}", StorageEntry.cleanupInfoFileName);
                            return null;
                        }
                        var cleanupInfoContent = await (new StreamReader(s, Encoding.ASCII)).ReadToEndAsync();
                        if (!DateTime.TryParseExact(cleanupInfoContent, StorageEntry.cleanupInfoLastAccessFormat,
                                dateFmtProvider, System.Globalization.DateTimeStyles.AllowWhiteSpaces, out DateTime lastAccessed))
                        {
                            trace.Warning("Could not parse '{0}'; assuming it's very old and therefore first to cleanup", cleanupInfoContent);
                            lastAccessed = new DateTime(2000, 1, 1);
                        }
                        else
                        {
                            trace.Info("Last accessed on {0}", lastAccessed);
                        }
                        return new { RelativeDirPath = dir, LastAccess = lastAccessed };
                    }));
                    dirs = dirs.Where(dir => dir != null).OrderBy(dir => dir.LastAccess).ToArray();
                    var dirsToDelete = Math.Max(1, dirs.Length / 3);
                    trace.Info("Found {0} deletable dirs. Deleting top {1}", dirs.Length, dirsToDelete);
                    foreach (var dir in dirs.Take(dirsToDelete))
                    {
                        trace.Info("Deleting '{0}'", dir.RelativeDirPath);
                        cancellationToken.ThrowIfCancellationRequested();
                        await FileSystem.DeleteDirectory(dir.RelativeDirPath);
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
        LJTraceSource trace;
        ITimingAndThreading env;
        IFileSystemAccess fs;
        IStorageConfigAccess config;
        readonly TaskCompletionSource<int> inited = new TaskCompletionSource<int>();
        readonly Task ready;
        readonly SemaphoreSlim sync = new SemaphoreSlim(1, 1);
        readonly Dictionary<string, StorageEntry> entriesCache = new Dictionary<string, StorageEntry>();
        CancellationTokenSource cleanupCancellation;
        Task cleanupTask;

        #endregion
    };
}
