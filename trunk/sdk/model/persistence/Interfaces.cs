using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LogJoint.Persistence
{
	[Flags]
	public enum StorageSectionOpenFlag
	{
		None = 0,
		ReadOnly = 1,
		ReadWrite = 3,
		AccessMask = ReadOnly | ReadWrite,
		ClearOnOpen = 8,
		IgnoreStorageExceptions = 16
	};

	public interface IStorageSection: IDisposable
	{
		StorageSectionOpenFlag OpenFlags { get; }
		string AbsolutePath { get; }
	};

	public interface IXMLStorageSection : IStorageSection
	{
		XDocument Data { get; }
	};

	public interface ISaxXMLStorageSection : IStorageSection
	{
		/// <summary>
		/// SAX reader to read session data.
		/// null id xml section is empty.
		/// </summary>
		XmlReader Reader { get; }
		/// <summary>
		/// Returns the fraction on input the <see cref="Reader"/> has read so far
		/// </summary>
		double ReadProgress { get; }
	};

	public interface IRawStreamStorageSection : IStorageSection
	{
		Stream Data { get; }
	};

	public interface IStorageEntry
	{
		string Id { get; }

		/// <summary>
		/// Returns an XML section in persistent storage.
		/// When open with ReadWrite flag the returned IStorageSection's Dispose may throw StorageException.
		/// </summary>
		IXMLStorageSection OpenXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey = 0);
		/// <summary>
		/// Returns an XML section in persistent storage that is accessible via simple API (SAX).
		/// </summary>
		ISaxXMLStorageSection OpenSaxXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey = 0);
		/// <summary>
		/// Returns a raw data section in persistent storage.
		/// When open with ReadWrite flag the returned IStorageSection's Dispose may throw StorageException
		/// </summary>
		IRawStreamStorageSection OpenRawStreamSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey = 0);
		void AllowCleanup();

		IEnumerable<SectionInfo> EnumSections(CancellationToken cancellation);
		Task TakeSectionSnapshot(string sectionId, Stream targetStream);
		Task LoadSectionFromSnapshot(string sectionId, Stream sourceStream, CancellationToken cancellation);
	};

	public enum SectionType
	{
		Xml,
		Raw
	};

	public struct SectionInfo
	{
		public string Key;
		public SectionType Type;
		public string Id;
	};

	public interface IStorageManager: IDisposable
	{
		Task<IStorageEntry> GetEntry(string entryKey, ulong additionalNumericKey = 0);
		Task<IStorageEntry> GetEntryById(string id);
		ulong MakeNumericKey(string stringToBeHashed);

		IStorageEntry GlobalSettingsEntry { get; }
	};

	public interface IContentCache
	{
		Task<Stream> GetValue(string key);
		Task SetValue(string key, Stream data);
	};

	public interface IFirstStartDetector
	{
		bool IsFirstStartDetected { get; }
	};

	public interface IWebContentCache
	{
		Task<Stream> GetValue(Uri uri);
		Task SetValue(Uri key, Stream data);
	};

	public interface IWebContentCacheConfig
	{
		bool IsCachingForcedForHost(string hostName);
	};

	public interface ICredentialsProtection
	{
		byte[] Protect(byte[] userData);
		byte[] Unprotect(byte[] encryptedData);
	};

	public class StorageException : Exception
	{
		public StorageException(Exception inner) : base(inner.Message, inner) { }
	};

	public class StorageFullException : StorageException
	{
		public StorageFullException(Exception inner) : base(inner) { }
	};
}
