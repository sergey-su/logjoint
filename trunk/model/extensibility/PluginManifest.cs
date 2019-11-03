using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace LogJoint.Extensibility
{
	public class PluginManifest : IPluginManifest
	{
		readonly string pluginDirectory;
		readonly string absolutePath;
		readonly string id;
		readonly string name;
		readonly string description;
		readonly Version version;
		readonly IReadOnlyList<IPluginFile> files;
		readonly IPluginFile entry;
		readonly IPluginFile test;
		readonly IPluginFile testEntry;
		readonly IReadOnlyList<string> dependencies;

		public PluginManifest(string pluginDirectory)
		{
			this.pluginDirectory = pluginDirectory;
			this.absolutePath = Path.Combine(pluginDirectory, ManifestFileName);
			XDocument doc;
			try
			{
				doc = XDocument.Load(this.absolutePath);
			}
			catch (Exception e)
			{
				throw new BadManifestException($"Failed to load manifest", e);
			}
			var root = doc.Element("manifest") ?? throw new BadManifestException($"Bad manifest root element");
			XElement getMandatory(string name) =>
				root.Element(name) ?? throw new BadManifestException($"Mandatory property {name} is missing");
			this.id = getMandatory("id").Value;
			if (string.IsNullOrWhiteSpace(this.id))
				throw new BadManifestException($"'{id}' is not a valid plugin id");
			var versionStr = getMandatory("version").Value;
			if (!Version.TryParse(versionStr, out this.version))
				throw new BadManifestException($"'{versionStr}' is not a valid plugin version");
			if (version.Build == -1)
				throw new BadManifestException($"Bad version '{versionStr}': version should contain at least 3 components");
			this.name = getMandatory("name").Value;
			if (string.IsNullOrWhiteSpace(this.name))
				throw new BadManifestException($"'{name}' is not a bad name");
			this.description = getMandatory("description").Value;
			this.files = root.Elements("file").Select(fileNode =>
			{
				PluginFileType type;
				var typeStr = fileNode.Attribute("type")?.Value ?? "unspecified";
				switch (typeStr)
				{
					case "entry": type = PluginFileType.Entry; break;
					case "format": type = PluginFileType.FormatDefinition; break;
					case "dll": type = PluginFileType.Library; break;
					case "sdk": type = PluginFileType.SDK; break;
					case "nib": type = PluginFileType.Nib; break;
					case "test": type = PluginFileType.Test; break;
					case "test-entry": type = PluginFileType.TestEntry; break;
					case "test-dll": type = PluginFileType.TestLibrary; break;
					case "unspecified": type = PluginFileType.Unspecified; break;
					default: throw new BadManifestException($"Bad file type {typeStr}");
				}
				return new File
				{
					manifest = this,
					type = type,
					relativePath = fileNode.Value // todo: normalize path separators
				};
			}).ToArray().AsReadOnly();
			this.entry = this.files.FirstOrDefault(f => f.Type == PluginFileType.Entry)
				?? throw new BadManifestException($"Plugin entry is missing from manifest");
			this.testEntry = this.files.FirstOrDefault(f => f.Type == PluginFileType.TestEntry);
			this.test = this.files.FirstOrDefault(f => f.Type == PluginFileType.Test) ?? this.testEntry;
			this.dependencies = root.Elements("dependency").Select(depNode =>
			{
				return !string.IsNullOrWhiteSpace(depNode.Value) ? depNode.Value : throw new BadManifestException($"Bad dependency {depNode}");
			}).ToArray().AsReadOnly();
		}

		public static string ManifestFileName => "manifest.xml";

		string IPluginManifest.PluginDirectory => pluginDirectory;

		string IPluginManifest.AbsolulePath => absolutePath;

		string IPluginManifest.Id => id;
		Version IPluginManifest.Version => version;

		string IPluginManifest.Name => name;
		string IPluginManifest.Description => description;

		IReadOnlyList<IPluginFile> IPluginManifest.Files => files;

		IPluginFile IPluginManifest.Entry => entry;

		IPluginFile IPluginManifest.Test => test;

		IPluginFile IPluginManifest.TestEntry => testEntry;

		IReadOnlyList<string> IPluginManifest.Dependencies => dependencies;

		public override string ToString() => $"{id} {version}";

		class File : IPluginFile
		{
			public PluginManifest manifest;
			public PluginFileType type;
			public string relativePath;

			IPluginManifest IPluginFile.Manifest => manifest;

			PluginFileType IPluginFile.Type => type;

			string IPluginFile.RelativePath => relativePath;

			string IPluginFile.AbsolulePath => Path.Combine(manifest.pluginDirectory, relativePath);

			public override string ToString() => $"{type} {relativePath}";
		};
	}
}
