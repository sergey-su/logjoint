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
		ClearOnOpen = 8
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

		IXMLStorageSection OpenXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey = 0);
		IRawStreamStorageSection OpenRawStreamSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey = 0);
		void AllowCleanup();

		IEnumerable<SectionInfo> EnumSections(CancellationToken cancellation);
		Task TakeSectionSnapshot(string sectionId, Stream targetStream);
		Task LoadSectionFromSnapshot(string sectionId, Stream sourceStream);
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
		ulong MakeNumericKey(string stringToBeHashed);
		IStorageEntry GlobalSettingsEntry { get; }
		Settings.IGlobalSettingsAccessor GlobalSettingsAccessor { get; }
		IStorageEntry GetEntryById(string id);
	};

	/// <summary>
	/// Implements actual platform-dependent data access for StorageManager.
	/// </summary>
	public interface IStorageImplementation
	{
		void SetTrace(LJTraceSource trace);
		void EnsureDirectoryCreated(string relativePath);
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
	};

	public interface IEnvironment
	{
		DateTime Now { get; }
		TimeSpan MinimumTimeBetweenCleanups { get; }
		long MaximumStorageSize { get; }
		Task StartCleanupWorker(Action cleanupRoutine);
		Settings.IGlobalSettingsAccessor CreateSettingsAccessor(IStorageManager storageManager);
	};

	public interface IFirstStartDetector
	{
		bool IsFirstStartDetected { get; }
	};
}
