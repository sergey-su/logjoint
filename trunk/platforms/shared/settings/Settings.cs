using System;
using System.Configuration;

namespace LogJoint.Properties
{
	// todo
	public class Settings//: ApplicationSettingsBase
	{
		// private static Settings defaultInstance = ((Settings)(ApplicationSettingsBase.Synchronized(new Settings())));
		private static Settings settings = new  Settings();

		public static Settings Default 
		{
			get
			{
				return settings;
			}
		}

		// [ApplicationScopedSetting]
		public string AutoUpdateUrl
		{
			get 
			{
				return "";
			}
		}

		
		public string TelemetryUrl
		{
			get
			{
				return "";
			}
		}

		public string IssuesUrl
		{
			get
			{
				return "";
			}
		}

		// [ApplicationScopedSetting]
		public string WorkspacesUrl 
		{
			get
			{
				return "";
				// return ((string)(this["WorkspacesUrl"]));
			}
		}

		//[ApplicationScopedSetting]
		public string ForceWebContentCachingFor
		{
			get
			{
				return "";
				// return ((string)(this["ForceWebContentCachingFor"]));
			}
		}

		// [ApplicationScopedSetting]
		public string LogDownloaderConfig
		{
			get
			{
				return "";
				// return ((string)(this["LogDownloaderConfig"]));
			}
		}

		// [ApplicationScopedSetting]
		public string WinInstallerUrl
		{
			get 
			{
				return "";
				// return ((string)(this["WinInstallerUrl"]));
			}
		}

		// [ApplicationScopedSetting]
		public string MacInstallerUrl
		{
			get 
			{
				return "";
				// return ((string)(this["MacInstallerUrl"]));
			}
		}

		// [ApplicationScopedSetting]
		public string FeedbackUrl
		{
			get 
			{
				return"";
				// return ((string)(this["FeedbackUrl"]));
			}
		}

		// [ApplicationScopedSetting]
		public string MonospaceBookmarks
		{
			get
			{
				return "";
				// return ((string)(this["MonospaceBookmarks"]));
			}
		}

		// [ApplicationScopedSetting]
		public string PluginsUrl
		{
			get
			{
				return "";
				// return ((string)(this["PluginsUrl"]));
			}
		}
	}
}