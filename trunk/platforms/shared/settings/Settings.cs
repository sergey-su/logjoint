using System;
using System.Configuration;

namespace LogJoint.Properties
{
	public class Settings: ApplicationSettingsBase
	{
		private static Settings defaultInstance = ((Settings)(ApplicationSettingsBase.Synchronized(new Settings())));

		public static Settings Default 
		{
			get
			{
				return defaultInstance;
			}
		}

		[ApplicationScopedSetting]
		public string AutoUpdateUrl
		{
			get 
			{
				return ((string)(this["AutoUpdateUrl"]));
			}
		}

		[ApplicationScopedSetting]
		public string TelemetryUrl
		{
			get
			{
				return ((string)(this["TelemetryUrl"]));
			}
		}

		[ApplicationScopedSetting]
		public string IssuesUrl
		{
			get
			{
				return ((string)(this["IssuesUrl"]));
			}
		}

		[ApplicationScopedSetting]
		public string WorkspacesUrl 
		{
			get
			{
				return ((string)(this["WorkspacesUrl"]));
			}
		}

		[ApplicationScopedSetting]
		public string ForceWebContentCachingFor
		{
			get
			{
				return ((string)(this["ForceWebContentCachingFor"]));
			}
		}

		[ApplicationScopedSetting]
		public string LogDownloaderConfig
		{
			get
			{
				return ((string)(this["LogDownloaderConfig"]));
			}
		}

		[ApplicationScopedSetting]
		public string WinInstallerUrl
		{
			get 
			{
				return ((string)(this["WinInstallerUrl"]));
			}
		}

		[ApplicationScopedSetting]
		public string MacInstallerUrl
		{
			get 
			{
				return ((string)(this["MacInstallerUrl"]));
			}
		}

		[ApplicationScopedSetting]
		public string FeedbackUrl
		{
			get 
			{
				return ((string)(this["FeedbackUrl"]));
			}
		}

		[ApplicationScopedSetting]
		public string MonospaceBookmarks
		{
			get
			{
				return ((string)(this["MonospaceBookmarks"]));
			}
		}
	}
}