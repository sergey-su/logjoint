using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

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
		// Returns a raw data section in persistent storage.
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
		IStorageEntry GetEntry(string entryKey, ulong additionalNumericKey = 0);
		IStorageEntry GetEntryById(string id);
		ulong MakeNumericKey(string stringToBeHashed);

		IStorageEntry GlobalSettingsEntry { get; }
	};

	public interface IContentCache
	{
		Stream GetValue(string key);
		Task SetValue(string key, Stream data);
	};

	public interface IFirstStartDetector
	{
		bool IsFirstStartDetected { get; }
	};

	public interface IWebContentCache
	{
		Stream GetValue(Uri uri);
		Task SetValue(Uri key, Stream data);
	};

	public interface IWebContentCacheConfig
	{
		bool IsCachingForcedForHost(string hostName);
	};

	public class StorageException : Exception
	{
		public StorageException(Exception inner) : base(inner.Message, inner) { }
	};

	public class StorageFullException : StorageException
	{
		public StorageFullException(Exception inner) : base(inner) { }
	};


	namespace Implementation
	{
		public interface IStorageManagerImplementation : IDisposable
		{
			void SetTrace(LJTraceSource trace);
			void Init(ITimingAndThreading timingThreading, IFileSystemAccess fs, IStorageConfigAccess config);
			IStorageEntry GetEntry(string entryKey, ulong additionalNumericKey);
			ulong MakeNumericKey(string stringToBeHashed);
			IStorageEntry GetEntryById(string id);
		};

		public interface ITimingAndThreading
		{
			DateTime Now { get; }
			Task StartTask(Action routine);
		};

		public interface IStorageConfigAccess
		{
			long SizeLimit { get; } // megs
			int CleanupPeriod { get; } // hours
		};

		/// <summary>
		/// Implements actual platform-dependent data access for StorageManager.
		/// </summary>
		public interface IFileSystemAccess
		{
			void SetTrace(LJTraceSource trace);
			void EnsureDirectoryCreated(string relativePath);
			bool DirectoryExists(string relativePath);
			/// <summary>
			/// Opens file stream specified by its relative path.
			/// OpenFile may fail because of for example lack of space or
			/// concurrent access to the file by another instance of LogJoint.
			/// In case of failre the method returns null if file is being open 
			/// for reading. The method throws an exception if file is being open for writing.
			/// OpenFile does the best to handle concurrent access and fails only
			/// if something really bad happens.
			/// </summary>
			Stream OpenFile(string relativePath, bool readOnly);
			/// <summary>
			/// Returns relative paths of subdirectories. It throws OperationCanceledException if cancellation was requested before enumeration is finished.
			/// </summary>
			string[] ListDirectories(string rootRelativePath, CancellationToken cancellation);
			string[] ListFiles(string rootRelativePath, CancellationToken cancellation);
			void DeleteDirectory(string relativePath);
			string AbsoluteRootPath { get; }
			/// <summary>
			/// Returns total storage size in bytes. May take time. It throws OperationCanceledException if cancellation was requested.
			/// </summary>
			long CalcStorageSize(CancellationToken cancellation);
			/// <summary>
			/// Converts implementation specific exception to StorageException
			/// </summary>
			void ConvertException(Exception e);
		};

		public interface IStorageSectionInternal
		{
			bool ExistsInFileSystem { get; }
		};
	}
}
