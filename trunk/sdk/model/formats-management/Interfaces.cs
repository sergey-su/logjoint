using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;

namespace LogJoint
{
	/// <summary>
	/// Registry of all log providers' factories.
	/// It holds factories for built-in providers (windows event log, OutputDebugString, ect)
	/// as well as factories for all user-defined providers.
	/// </summary>
	public interface ILogProviderFactoryRegistry
	{
		void Register(ILogProviderFactory fact);
		void Unregister(ILogProviderFactory fact);
		IEnumerable<ILogProviderFactory> Items { get; }
		ILogProviderFactory Find(string companyName, string formatName);
	};

	/// <summary>
	/// A repository of XML documents that describe user-defined format.
	/// One implementation enumerates XML files in a directory (<seealso cref="LogJoint.DirectoryFormatsRepository"/>).
	/// Other implementation enumerates embedded resource streams (<seealso cref="LogJoint.ResourcesFormatsRepository"/>).
	/// </summary>
	public interface IFormatDefinitionsRepository
	{
		IEnumerable<IFormatDefinitionRepositoryEntry> Entries { get; }
	};

	public interface IFormatDefinitionRepositoryEntry
	{
		string Location { get; }
		DateTime LastModified { get; }
		XElement LoadFormatDescription();
	};

	public interface IUserDefinedFactory : ILogProviderFactory, IDisposable
	{
		bool IsDisposed { get; }
		/// <summary>
		/// Location of format definition document (file or resource)
		/// </summary>
		string Location { get; }
	};

	public interface IUserDefinedFormatsManager
	{
		int ReloadFactories();
		IEnumerable<IUserDefinedFactory> Items { get; }
		void RegisterFormatType(string configNodeName, Type formatType);
		IFormatDefinitionsRepository Repository { get; }
	};
}
