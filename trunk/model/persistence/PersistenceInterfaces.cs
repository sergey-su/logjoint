using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;

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
	};

	public interface IStorageManager
	{
		IStorageEntry GetEntry(string entryKey, ulong additionalNumericKey = 0);
		ulong MakeNumericKey(string stringToBeHashed);
	};

	internal interface IStorageImplementation
	{
		void EnsureDirectoryCreated(string relativePath);
		Stream OpenFile(string relativePath, bool readOnly);
		string AbsoluteRootPath { get; }
	};

}
