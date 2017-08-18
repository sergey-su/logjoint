using System.Configuration;

namespace LogJoint.UpdateTool.Properties {
	
	internal sealed class Settings : global::System.Configuration.ApplicationSettingsBase {
		
		private static Settings defaultInstance = ((Settings)(global::System.Configuration.ApplicationSettingsBase.Synchronized(new Settings())));
		
		public static Settings Default {
			get {
				return defaultInstance;
			}
		}
		
		[ApplicationScopedSetting]
		public string FilesList {
			get {
				return ((string)(this["FilesList"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string StorageAccountName {
			get {
				return ((string)(this["StorageAccountName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string EncryptedStorageAccountKey {
			get {
				return ((string)(this["EncryptedStorageAccountKey"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string DefaultFilesLocation {
			get {
				return ((string)(this["DefaultFilesLocation"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string ConfigFileName {
			get {
				return ((string)(this["ConfigFileName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string ProdBlobName {
			get {
				return ((string)(this["ProdBlobName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string StagingBlobName {
			get {
				return ((string)(this["StagingBlobName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string UpdatesBlobContainerName {
			get {
				return ((string)(this["UpdatesBlobContainerName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string StoredClientsAccessPolicyName {
			get {
				return ((string)(this["StoredClientsAccessPolicyName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string TelemetryTableName {
			get {
				return ((string)(this["TelemetryTableName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string IssuesBlobContainerName {
			get {
				return ((string)(this["IssuesBlobContainerName"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string WorkspacesUrl {
			get {
				return ((string)(this["WorkspacesUrl"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string ForceWebContentCachingFor {
			get {
				return ((string)(this["ForceWebContentCachingFor"]));
			}
		}

		[ApplicationScopedSetting]
		public string LogDownloaderConfig {
			get {
				return ((string)(this["LogDownloaderConfig"]));
			}
		}

		[ApplicationScopedSetting]
		public string StorageAccountKeyEncryptionCertThumbprint {
			get {
				return ((string)(this["StorageAccountKeyEncryptionCertThumbprint"]));
			}
		}
		
		[ApplicationScopedSetting]
		public string StagingMacInstallerBlobName {
			get {
				return ((string)(this["StagingMacInstallerBlobName"]));
			}
		}

		[ApplicationScopedSetting]
		public string ProdMacInstallerBlobName {
			get {
				return ((string)(this["ProdMacInstallerBlobName"]));
			}
		}

		[ApplicationScopedSetting]
		public string StagingWinInstallerBlobName {
			get {
				return ((string)(this["StagingWinInstallerBlobName"]));
			}
		}

		[ApplicationScopedSetting]
		public string ProdWinInstallerBlobName {
			get {
				return ((string)(this["ProdWinInstallerBlobName"]));
			}
		}

		[ApplicationScopedSetting]
		public string FeedbackEmail {
			get {
				return ((string)(this["FeedbackEmail"]));
			}
		}
	}
}