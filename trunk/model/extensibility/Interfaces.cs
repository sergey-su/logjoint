using System;
using System.Collections.Generic;

namespace LogJoint
{
	public interface IPluginsManagerInternal: IPluginsManager
	{
		void LoadPlugins(object appEntryPoint);
		IPluginManifest LoadManifest(string pluginDirectory);
	}

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

	public interface IPluginDependency
	{
		IPluginManifest Manifest { get; }
		string PluginId { get; }
	};

	public interface IPluginManifest
	{
		string PluginDirectory { get; }
		string AbsolulePath { get; }
		string Id { get; }
		IReadOnlyList<IPluginFile> Files { get; }
		IPluginFile Entry { get; }
		IReadOnlyList<IPluginDependency> Dependencies { get; }
	};

	public class BadManifestException: Exception
	{
		public BadManifestException(string message, Exception inner = null) : base(message, inner) { }
	};
}
