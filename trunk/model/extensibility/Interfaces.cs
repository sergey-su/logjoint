using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Extensibility
{
	public interface IPluginsManagerInternal: IPluginsManager
	{
		bool IsConfigured { get; }
		void LoadPlugins(object appEntryPoint, string localPluginsList);
		IReadOnlyList<IPluginManifest> InstalledPlugins { get; }
		Task<IReadOnlyList<IPluginInfo>> FetchAllPlugins(CancellationToken cancellation);
		/// <summary>
		/// Maps plug-in id to requested installation state, true - request for installation,
		/// false - request for uninstallation.
		/// </summary>
		IReadOnlyDictionary<string, bool> InstallationRequests { get; }
		IPluginInstallationRequestsBuilder CreatePluginInstallationRequestsBuilder();
		IEnumerable<Assembly> PluginAssemblies { get; }
	};

	public interface IPluginInfo
	{
		string Id { get; }
		Version Version { get; }
		string Name { get; }
		string Description { get; }
		IPluginIndexItem IndexItem { get; }
		IReadOnlyList<IPluginInfo> Dependencies { get; }
		IReadOnlyList<IPluginInfo> Dependants { get; }
		IPluginManifest InstalledPluginManifest { get; }
	};

	public interface IPluginInstallationRequestsBuilder
	{
		void RequestInstallationState(IPluginInfo plugin, bool desiredState);
		IReadOnlyDictionary<string, bool> InstallationRequests { get; }
		void ApplyRequests();
	};

	public enum PluginFileType
	{
		Unspecified,
		Entry,
		Library,
		FormatDefinition,
		Nib,
		SDK
	};

	public interface IPluginFile
	{
		IPluginManifest Manifest { get; }
		PluginFileType Type { get; }
		string RelativePath { get; }
		string AbsolulePath { get; }
	};

	public interface IPluginManifest
	{
		string PluginDirectory { get; }
		string AbsolulePath { get; }
		string Id { get; }
		string Name { get; }
		string Description { get; }
		Version Version { get; }
		IReadOnlyList<IPluginFile> Files { get; }
		IPluginFile Entry { get; }
		IReadOnlyList<string> Dependencies { get; }
	};



	public class BadManifestException: Exception
	{
		public BadManifestException(string message, Exception inner = null) : base(message, inner) { }
	};

	public interface IPluginsIndex
	{
		string ETag { get; }
		IReadOnlyList<IPluginIndexItem> Plugins { get; }
	};

	public interface IPluginIndexItem
	{
		string Id { get; }
		Version Version { get; }
		string Name { get; }
		string Description { get; }
		Uri Location { get; }
		string ETag { get; }
		IReadOnlyList<string> Dependencies { get; }
	};

	public interface IPluginsIndexFactory
	{
		IPluginsIndex Create(Stream stream, string etag);
	};
}
