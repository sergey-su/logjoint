using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Linq;
using System.IO.IsolatedStorage;
using System.Security.Cryptography;

namespace LogJoint.Persistence
{
	public class StorageManager: IStorageManager, IDisposable
	{
		public StorageManager()
		{
			storageImpl = new DesktopStorageImplementation();
		}
		public void Dispose()
		{
		}
		public IStorageEntry GetEntry(string entryKey)
		{
			if (string.IsNullOrWhiteSpace(entryKey))
				throw new ArgumentException("Wrong entryKey");
			string normalizedKey = NormalizeKey(entryKey, entryKeyPrefix);
			StorageEntry section;
			if (!entriesCache.TryGetValue(normalizedKey, out section))
			{
				section = new StorageEntry(this, normalizedKey);
				entriesCache.Add(normalizedKey, section);
			}
			section.EnsureCreated();
			return section;
		}

		internal IStorageImplementation Implementation
		{
			get { return storageImpl; }
		}

		#region Implementation

		class DesktopStorageImplementation : IStorageImplementation
		{
			public DesktopStorageImplementation()
			{
				rootDirectory = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\LogJoint\\";
				if (!Directory.Exists(rootDirectory))
					Directory.CreateDirectory(rootDirectory);
			}

			public void EnsureDirectoryCreated(string dirName)
			{
				var dir = rootDirectory + dirName;
				if (!Directory.Exists(dir))
					Directory.CreateDirectory(dir);
			}

			public Stream OpenFile(string relativePath, bool readOnly)
			{
				if (readOnly && !File.Exists(rootDirectory + relativePath))
					return null;
				return new FileStream(rootDirectory + relativePath,
					readOnly ? FileMode.Open : FileMode.OpenOrCreate,
					readOnly ? FileAccess.Read : FileAccess.ReadWrite,
					FileShare.ReadWrite | FileShare.Delete);
			}

			string rootDirectory;
		};

		internal static string NormalizeKey(string key, string keyPrefix)
		{
			var maxKeyTailLength = 128;
			var tail = key.Length < maxKeyTailLength ? key : key.Substring(key.Length - maxKeyTailLength, maxKeyTailLength);
			return string.Format("{0}-{1:X}-{2}", keyPrefix, GetStringHash(key), MakeValidFileName(tail));
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

		#endregion

		#region Members

		static readonly string invalidKeyChars = new string(Path.GetInvalidFileNameChars());
		static readonly string entryKeyPrefix = "e";
		static SHA1 sha1 = new SHA1CryptoServiceProvider();
		readonly IStorageImplementation storageImpl;
		readonly Dictionary<string, StorageEntry> entriesCache = new Dictionary<string, StorageEntry>();

		#endregion
	};
}
