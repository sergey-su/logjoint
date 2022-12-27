using System;
using System.Collections.Generic;
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
			var configFileName = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "logjoint.dll.config");
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

			var cmdLineTraceListenerConfig =
				GetCommandLineArgumentParams("--logging")
				.FirstOrDefault();
			if (cmdLineTraceListenerConfig != null)
				TraceListenerConfig = cmdLineTraceListenerConfig;

			var cmdLineLocalPluginsConfig = GetCommandLineArgumentParams("--plugin").ToArray();
			LocalPlugins = string.Join(";", new [] { LocalPlugins }.Union(cmdLineLocalPluginsConfig));
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


		private static IEnumerable<string> GetCommandLineArgumentParams(string paramName)
		{
			bool nextIsValue = false;
			foreach (var arg in Environment.GetCommandLineArgs())
			{
				if (arg == paramName)
				{
					nextIsValue = true;
				}
				else
				{
					if (nextIsValue)
					{
						yield return arg;
					}
					nextIsValue = false;
				}
			}
		}
	}
}