using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading;

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
		IXMLStorageSection OpenXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey = 0);
		IRawStreamStorageSection OpenRawStreamSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey = 0);
		void AllowCleanup();
	};

	public interface IStorageManager
	{
		IStorageEntry GetEntry(string entryKey, ulong additionalNumericKey = 0);
		ulong MakeNumericKey(string stringToBeHashed);
	};

	internal interface IStorageImplementation
	{
		void EnsureDirectoryCreated(string relativePath);
		/// <summary>
		/// Opens file stream specified by its relative path.
		/// OpenFile may fail because of for example lack of space or
		/// concurrent access to the file by another instance of LogJoint.
		/// In case of failre the method may returns null if file is being open 
		/// for readin or it throws an exception if file is being open for writing.
		/// OpenFile does the best to handle concurrent access and fails only
		/// if something really bad happens.
		/// </summary>
		Stream OpenFile(string relativePath, bool readOnly);
		/// <summary>
		/// Returns relative paths of subdirectories
		/// </summary>
		string[] ListDirectories(string rootRelativePath, CancellationToken cancellation);
		void DeleteDirectory(string relativePath);
		string AbsoluteRootPath { get; }
		long CalcStorageSize(CancellationToken cancellation);
	};

	internal interface IEnvironment
	{
		DateTime Now { get; }
	};
}
