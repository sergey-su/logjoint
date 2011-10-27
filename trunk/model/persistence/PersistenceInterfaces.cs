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

	public enum StorageSectionAccess
	{
		Read,
		ReadWrite
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
		IStorageSection OpenSection(string sectionKey, StorageSectionType type, StorageSectionAccess accessType);
	};

	public interface IStorageManager
	{
		IStorageEntry GetEntry(string entryKey);
	};
}
