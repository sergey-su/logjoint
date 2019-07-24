using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using System.Xml;
using System.Collections.Immutable;

namespace LogJoint.Extensibility
{
	public class PluginsIndex: IPluginsIndex
	{
		readonly IReadOnlyList<IPluginIndexItem> items;
		readonly string etag;

		public class Factory : IPluginsIndexFactory
		{
			readonly Telemetry.ITelemetryCollector telemetryCollector;

			public Factory(Telemetry.ITelemetryCollector telemetryCollector)
			{
				this.telemetryCollector = telemetryCollector;
			}

			IPluginsIndex IPluginsIndexFactory.Create(Stream stream, string etag)
			{
				return new PluginsIndex(stream, etag, telemetryCollector);
			}
		};

		private PluginsIndex(Stream stream, string etag, Telemetry.ITelemetryCollector telemetryCollector)
			: this(XDocument.Load(stream), etag, telemetryCollector)
		{
		}

		private PluginsIndex(XDocument doc, string etag, Telemetry.ITelemetryCollector telemetryCollector)
		{
			this.etag = etag;

			var tmp = new Dictionary<string, Item>();
			foreach (var pluginNode in doc.Elements("plugins").Elements("plugin"))
			{
				var id = pluginNode.Element("id")?.Value;
				var versionStr = pluginNode.Element("version")?.Value;
				var name = pluginNode.Element("name")?.Value;
				var platform = pluginNode.Element("platform")?.Value;
				var locationStr = pluginNode.Element("location")?.Value;
				var description = pluginNode.Element("description")?.Value;
				if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(name) || string.IsNullOrEmpty(platform)
					|| !Uri.TryCreate(locationStr ?? "", UriKind.Absolute, out var location)
					|| !Version.TryParse(versionStr, out var version))
				{
					telemetryCollector.ReportException(new Exception("Bad entry in plug-ins index"), pluginNode.ToString());
					continue;
				}
				string thisPlatform =
#if WIN
					"win";
#elif MONOMAC
					"mac";
#else
					"<unk>";
#endif
				if (thisPlatform != platform)
				{
					continue;
				}
				tmp[id] = new Item
				{
					id = id,
					version = version,
					name = name,
					description = description ?? "",
					location = location,
					dependencies = ImmutableArray.CreateRange(pluginNode.Elements("dependency").Select(d => d.Value))
				};
			}

			this.items = ImmutableArray.CreateRange(tmp.Values);
		}

		IReadOnlyList<IPluginIndexItem> IPluginsIndex.Plugins => items;
		string IPluginsIndex.ETag => etag;

		class Item : IPluginIndexItem
		{
			public string id, name, description;
			public Uri location;
			public Version version;
			public ImmutableArray<string> dependencies;

			string IPluginIndexItem.Id => id;
			Version IPluginIndexItem.Version => version;
			string IPluginIndexItem.Name => name;
			string IPluginIndexItem.Description => description;
			Uri IPluginIndexItem.Location => location;
			IReadOnlyList<string> IPluginIndexItem.Dependencies => dependencies;
		};
	}
}
