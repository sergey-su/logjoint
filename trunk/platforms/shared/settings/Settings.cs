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

		public string TraceListenerConfig { get; private set; }

		public string AutoUpdateUrl { get; private set; }	 = "";
		public string TelemetryUrl { get; private set; } = "";
		public string IssuesUrl { get; private set; } = "";
		public string WorkspacesUrl { get; private set; } = "";
		public string ForceWebContentCachingFor { get; private set; } = "";
		public string LogDownloaderConfig { get; private set; } = "";
		public string WinInstallerUrl { get; private set; } = "";
		public string MacInstallerUrl { get; private set; } = "";
		public string FeedbackUrl { get; private set; } = "";
		public string MonospaceBookmarks { get; private set; } = "";
		public string PluginsUrl { get; private set; } = "";
		public string LocalPlugins { get; private set; } = "";
	}
}