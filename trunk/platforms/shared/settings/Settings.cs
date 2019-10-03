using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;

namespace LogJoint.Properties
{
	public class Settings
	{
		private static Settings settings = new  Settings();

		Settings()
		{
			var configFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetEntryAssembly().Location), "logjoint.exe.config");
			ReadFromFile(configFileName);
		}

		void ReadFromFile(string fileName)
		{
			var config = XDocument.Load(fileName);
			foreach (var settingElement in config
				.Elements("configuration")
				.Elements("applicationSettings")
				.Elements("LogJoint.Properties.Settings")
				.Elements("setting"))
			{
				var name = settingElement.Attribute("name")?.Value;
				var value = settingElement.Element("value")?.Value;
				if (string.IsNullOrEmpty(name) || value == null)
					throw new Exception($"bad config in element {settingElement}");
				this.GetType().InvokeMember(name, BindingFlags.SetProperty, null, this, new [] { value });
			}

			TraceListenerConfig = config
				.Elements("configuration")
				.Elements("system.diagnostics")
				.Elements("sharedListeners")
				.Elements("add")
				.Where(e => e.Attribute("type")?.Value == "LogJoint.TraceListener, logjoint.model")
				.Attributes("initializeData")
				.FirstOrDefault()
				?.Value;
		}

		public static Settings Default => settings;

		public string TraceListenerConfig { get; set; }

		public string AutoUpdateUrl { get; set; } = "";
		public string TelemetryUrl { get;  set; } = "";
		public string IssuesUrl { get; set; } = "";
		public string WorkspacesUrl { get; set; } = "";
		public string ForceWebContentCachingFor { get; set; } = "";
		public string LogDownloaderConfig { get; set; } = "";
		public string WinInstallerUrl { get; set; } = "";
		public string MacInstallerUrl { get; set; } = "";
		public string FeedbackUrl { get; set; } = "";
		public string MonospaceBookmarks { get; set; } = "";
		public string PluginsUrl { get; set; } = "";
		public string LocalPlugins { get; set; } = "";
	}
}