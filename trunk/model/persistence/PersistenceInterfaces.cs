using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;

namespace LogJoint.Persistence
{
	public enum StorageSectionType
	{
		Invalid,
		RawStream,
		XML
	};

	[Flags]
	public enum StorageSectionOpenFlag
	{
		None = 0,
		ReadOnly = 1,
		ReadWrite = 3,
		AccessMask = ReadOnly | ReadWrite
	};

	public interface IStorageSection: IDisposable
	{
		StorageSectionType Type { get; }
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
		IStorageSection OpenSection(string sectionKey, StorageSectionType type, StorageSectionOpenFlag accessType);
	};

	public interface IStorageManager
	{
		IStorageEntry GetEntry(string entryKey);
	};

	internal interface IStorageImplementation
	{
		void EnsureDirectoryCreated(string relativePath);
		Stream OpenFile(string relativePath, bool readOnly);
	};

}
