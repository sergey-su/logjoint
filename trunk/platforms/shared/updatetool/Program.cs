using Microsoft.WindowsAzure.Storage;
using Microsoft.WindowsAzure.Storage.Auth;
using Microsoft.WindowsAzure.Storage.Blob;
using Microsoft.WindowsAzure.Storage.Table;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading;
using System.Xml.Linq;
using Ionic.Zip;
using System.Diagnostics;

namespace LogJoint.UpdateTool
{
	class Program
	{
		static Properties.Settings settings = Properties.Settings.Default;

		static void Main(string[] args)
		{
			if (args.Length == 0)
			{
				Console.WriteLine("logjoint.updatetool.exe <command>");
				Console.WriteLine("commands:");
				Console.WriteLine("  pack [prod]                 collects latest binaries and zips them to an archive.");
				Console.WriteLine("                              If prod option is specified zipped LogJoint's config file is modified to fetch production updates.");
				Console.WriteLine("                              By default staging updates are assumed.");
				Console.WriteLine("  deploy [prod]               deploys zipped binaries to the cloud as staging blob (default) or production blob");
				Console.WriteLine("  init                        (re)initializes storage account by creating required objects and setting proper access policies");
				Console.WriteLine("  telem [months]              downloads and analyzes telemetry for specified nr of recent months");
				Console.WriteLine("  issues                      downloads issues reports");
				Console.WriteLine("  packinst [prod]             builds installation image");
				Console.WriteLine("  deployinst [prod]           deploys installation image to the cloud");
				Console.WriteLine("  encrypt <str>               encrypts a string with configured encryption certificate (thumbprint={0})", settings.StorageAccountKeyEncryptionCertThumbprint);
				return;
			}

			switch (args[0])
			{
				case "pack":
					Pack(args.Skip(1).ToArray());
					break;
				case "deploy":
					Deploy(args.Skip(1).ToArray());
					break;
				case "init":
					InitStorageAccount();
					break;
				case "telem":
					DownloadTelemetry(args.Skip(1).FirstOrDefault());
					break;
				case "issues":
					DownloadIssues(args.Skip(1).FirstOrDefault());
					break;
				case "encrypt":
					EncryptStringsWithStorageCertificate(args.Skip(1).ToArray());
					break;
				case "packinst":
					PackInstaller (args.Skip (1).ToArray ());
					break;
				case "deployinst":
					DeployInstaller (args.Skip (1).ToArray ());
					break;
				default:
					Console.WriteLine("Unknown command");
					break;
			}
		}

		static IEnumerable<string> ResolveMasks(string relativePathWithMask, string root)
		{
			bool needsMaskResolution = relativePathWithMask.Contains ('*');
			if (!needsMaskResolution)
				return Enumerable.Repeat(relativePathWithMask, 1);
			if (!root.EndsWith (new string(Path.DirectorySeparatorChar, 1))) 
				root += Path.DirectorySeparatorChar;
			var rootUrl = new Uri (root);
			return 
				Directory
				.GetFiles (root, relativePathWithMask)
				.Select (resolvedAbsPath => rootUrl.MakeRelativeUri (new Uri (resolvedAbsPath)).ToString());
		}

		static XDocument MakeAppConfig(string configTemplateAbsPath, bool prod)
		{
			var configDoc = XDocument.Load(configTemplateAbsPath);
			Func<string, XElement> getSettingNode = settingName =>
			{
				var n = configDoc
					.Descendants()
					.Where(e => e.Name == "setting" && e.Attribute("name") != null && e.Attribute("name").Value == settingName)
					.Select(e => e.Element("value"))
					.FirstOrDefault();
				if (n == null)
					throw new Exception("Bad LogJoint config: no placeholder for " + settingName + " setting found");
				return n;
			};

			Action<string> removeAppSettingNode = settingName =>
			{
				var n = configDoc
					.Descendants()
					.Where(e => e.Name == "appSettings")
					.Select(e => e.Elements("add").Where(a => a.Attribute("key")?.Value == settingName))
					.FirstOrDefault();
				if (n != null)
					n.Remove();
			};

			var configNode = getSettingNode("AutoUpdateUrl");
			var blob = CreateUpdateBlob(prod, false);
			configNode.Value = blob.Uri.ToString();

			configNode = getSettingNode("TelemetryUrl");
			var table = CreateTelemetryTable();
			configNode.Value = TransformUri(table.Uri,
				table.GetSharedAccessSignature(new SharedAccessTablePolicy(), settings.StoredClientsAccessPolicyName, null, null, null, null));

			configNode = getSettingNode("IssuesUrl");
			var issuesContainer = CreateIssuesContainer();
			configNode.Value = TransformUri(issuesContainer.Uri,
				issuesContainer.GetSharedAccessSignature(null, settings.StoredClientsAccessPolicyName));

			configNode = getSettingNode("WorkspacesUrl");
			configNode.Value = settings.WorkspacesUrl;

			configNode = getSettingNode("ForceWebContentCachingFor");
			configNode.Value = settings.ForceWebContentCachingFor;

			configNode = getSettingNode("LogDownloaderConfig");
			configNode.Value = settings.LogDownloaderConfig;

			configNode = getSettingNode("WinInstallerUrl");
			var winInstallerBlob = CreateInstallerBlob (prod: prod, backup: false, win: true);
			configNode.Value = winInstallerBlob.Uri.ToString();

			configNode = getSettingNode("MacInstallerUrl");
			var macInstallerBlob = CreateInstallerBlob (prod: prod, backup: false, win: false);
			configNode.Value = macInstallerBlob.Uri.ToString();

			configNode = getSettingNode("FeedbackEmail");
			configNode.Value = settings.FeedbackEmail;


			// configure trace listener according to build flavor
			configNode = configDoc.Descendants()
				.Elements("sharedListeners").Elements("add")
				.Where(e => e.Attribute("name")?.Value == "file")
				.Single();
#if MONOMAC
			var localLogLocation = "%HOME%/local-lj-debug.log";
#else
			var localLogLocation = "lj-debug.log";
#endif
			configNode.SetAttributeValue("initializeData", 
				(prod ? "" : localLogLocation) + ";membuf=1");


			// remove local debug configs
			removeAppSettingNode("localCosmosLocation");
			removeAppSettingNode("useCallDbAwareCosmosReader");
			removeAppSettingNode("callDbViewerApp");

			return configDoc;
		}

		static IEnumerable<string> GetFilesList(string sourceFilesLocation)
		{
			return settings.FilesList
				.Split ('\n')
				.Select (s => s.Trim ())
				.SelectMany (s => ResolveMasks (s, sourceFilesLocation));
		}
		
		static void Pack(string[] args)
		{
			var prod = args.FirstOrDefault() == "prod";
			var sourceFilesLocation = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), settings.DefaultFilesLocation));
			var destArchiveFileName = GetIntermediateArchiveFullFileName(prod);

			if (File.Exists(destArchiveFileName))
			{
				File.Delete(destArchiveFileName);
				Console.WriteLine("Deleting old {0}", destArchiveFileName);
			}

			Console.WriteLine("Creating {0}", destArchiveFileName);
			using (var zip = new ZipFile(destArchiveFileName))
			{
				zip.ParallelDeflateThreshold = -1; // http://dotnetzip.codeplex.com/workitem/14087
				Console.WriteLine("Reading files from {0}", sourceFilesLocation);
				var filesList = GetFilesList(sourceFilesLocation);
				if (!prod)
				{
#if !MONOMAC
					filesList = filesList.Union(ResolveMasks(@"Formats\LogJoint - LogJoint debug trace.format.xml", sourceFilesLocation));
#endif
				}
				foreach (var relativeFilePath in filesList)
				{
					var sourceFileAbsolutePath = Path.Combine(sourceFilesLocation, relativeFilePath);
					var relativePathInArchive = Path.GetDirectoryName(relativeFilePath);
					if (string.Compare(relativeFilePath, settings.ConfigFileName, true) == 0)
					{
						var configDoc = MakeAppConfig (sourceFileAbsolutePath, prod);
						Console.WriteLine("Adding to archive:   {0}   (config modified to fetch {1} updates, {2} logging)", 
							relativeFilePath, prod ? "prod" : "staging", prod ? "no" : "full");
						zip.AddEntry(relativeFilePath, configDoc.ToString());
					}
					else
					{
						Console.WriteLine("Adding to archive:   {0}", relativeFilePath);
						zip.AddFile(sourceFileAbsolutePath, relativePathInArchive);
					}
				}
				zip.Save();
			}
			Console.WriteLine("Successfully created: {0} ", destArchiveFileName);
		}

#if MONOMAC
		static void PackInstaller(string[] args)
		{
			var pwd = Directory.GetCurrentDirectory();
			var prod = args.FirstOrDefault() == "prod";
			var sourceFilesLocation = Path.GetFullPath(Path.Combine(pwd, settings.DefaultFilesLocation));
			var dmgToolWorkdir = Path.GetFullPath(Path.Combine(pwd, "dmg"));
			var tempDirName = Path.Combine(dmgToolWorkdir, "logjoint.app");
			var outputDmgPath = Path.Combine (pwd, 
				(prod ? settings.ProdMacInstallerBlobName : settings.StagingMacInstallerBlobName));

			if (Directory.Exists(tempDirName))
			{
				Directory.Delete (tempDirName, true);
				Console.WriteLine("Deleted old temporary dir {0}", tempDirName);
			}

			Console.WriteLine("Creating new {0}", tempDirName);
			Console.WriteLine("Reading files from {0}", sourceFilesLocation);
			foreach (var relativeFilePath in GetFilesList(sourceFilesLocation))
			{
				var sourceFileAbsolutePath = Path.Combine(sourceFilesLocation, relativeFilePath);
				var destFileAbsolutePath = Path.Combine(tempDirName, "Contents", relativeFilePath);
				Directory.CreateDirectory (Path.GetDirectoryName (destFileAbsolutePath));
				Console.WriteLine("Copying:   {0}", relativeFilePath);
				if (string.Compare(relativeFilePath, settings.ConfigFileName, true) == 0)
					MakeAppConfig (sourceFileAbsolutePath, prod).Save (destFileAbsolutePath);
				else
					File.Copy (sourceFileAbsolutePath, destFileAbsolutePath);
			}
			Console.WriteLine("Successfully created temp dir: {0} ", tempDirName);

			if (File.Exists(outputDmgPath))
			{
				File.Delete(Path.Combine(pwd, outputDmgPath));
				Console.WriteLine("Deleted old image: {0}", outputDmgPath);
			}

			Console.WriteLine("Building new dmg...");
			var pi = new ProcessStartInfo ()
			{
				WorkingDirectory = dmgToolWorkdir,
				FileName = "appdmg",
				Arguments = "imagespec.json " + outputDmgPath
			};
			var proc = Process.Start (pi);
			if (proc == null)
				throw new Exception ("Failed to start appdmg");
			proc.WaitForExit();
			Console.WriteLine("New image created: {0}", outputDmgPath);
		}

		static void DeployInstaller(string[] args)
		{
			var prod = args.FirstOrDefault() == "prod";
			var settings = Properties.Settings.Default;
			var blob = CreateInstallerBlob(prod: prod, backup: false, win: false);
			if (blob.Exists())
				BackupBlob(blob, CreateInstallerBlob(prod: prod, backup: true, win: false));
			Console.Write("Deleting exising {0} ...   ", blob.Uri);
			if (blob.DeleteIfExists())
				Console.WriteLine("Deleted");
			else
				Console.WriteLine("Nothing to delete");
			var installerFileName = Path.Combine (Directory.GetCurrentDirectory (), 
				prod ? settings.ProdMacInstallerBlobName : settings.StagingMacInstallerBlobName);
			Console.WriteLine("Uploading new bits from {0}", installerFileName);
			blob.UploadFromFile(installerFileName);
			Console.WriteLine("Successfully uploaded {0}. New etag: {1}", blob.Uri, blob.Properties.ETag);
		}

#else
		static void PackInstaller(string[] args)
		{
			throw new Exception ("Not supprted");
		}

		static void DeployInstaller(string[] args)
		{
			throw new Exception ("Not supprted");
		}
#endif

		private static string TransformUri(Uri resourceUri, string sasToken)
		{
			var creds = new StorageCredentials(sasToken);
			return creds.TransformUri(resourceUri).ToString();
		}

		private static string GetIntermediateArchiveFullFileName(bool prod)
		{
			var destArchiveFileName = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(),
				prod ? settings.ProdBlobName : settings.StagingBlobName));
			return destArchiveFileName;
		}

		private static CloudBlockBlob CreateBlob(string baseName, bool backup)
		{
			CloudStorageAccount storageAccount = CreateStorageAccount();
			var blobClient = storageAccount.CreateCloudBlobClient();
			var blobContainer = blobClient.GetContainerReference(settings.UpdatesBlobContainerName);
			var blobName = baseName;
			if (backup)
				blobName = string.Format("bk-{0:yyyy-MM-dd-HH-mm-ss}-{1}", DateTime.UtcNow, blobName);
			var blob = blobContainer.GetBlockBlobReference(blobName);
			return blob;
		}

		private static CloudBlockBlob CreateUpdateBlob(bool prod, bool backup)
		{
			return CreateBlob(prod ? settings.ProdBlobName : settings.StagingBlobName, backup);
		}

		private static CloudBlockBlob CreateInstallerBlob(bool prod, bool backup, bool? win)
		{
			if (win == null) {
#if MONOMAC
				win = false;
#else
				win = true;
#endif
			}
			if (win.Value)
				return CreateBlob(prod ? settings.ProdWinInstallerBlobName : settings.StagingWinInstallerBlobName, backup);
			else
				return CreateBlob(prod ? settings.ProdMacInstallerBlobName : settings.StagingMacInstallerBlobName, backup);
		}

		private static CloudTable CreateTelemetryTable()
		{
			CloudStorageAccount storageAccount = CreateStorageAccount();
			var tableClient = storageAccount.CreateCloudTableClient();
			var telemetryTable = tableClient.GetTableReference(settings.TelemetryTableName);
			return telemetryTable;
		}

		private static CloudStorageAccount CreateStorageAccount()
		{
			var accountAndKey = new StorageCredentials(settings.StorageAccountName, 
				DecryptStringWithStorageCertificate(settings.EncryptedStorageAccountKey));
			return new CloudStorageAccount(accountAndKey, true);
		}

		private static CloudBlobContainer CreateIssuesContainer()
		{
			CloudStorageAccount storageAccount = CreateStorageAccount();
			var blobClient = storageAccount.CreateCloudBlobClient();
			var issuesContainer = blobClient.GetContainerReference(settings.IssuesBlobContainerName);
			return issuesContainer;
		}

		static void Deploy(string[] args)
		{
			var prod = args.FirstOrDefault() == "prod";
			var settings = Properties.Settings.Default;
			var blob = CreateUpdateBlob(prod, false);
			if (blob.Exists())
				BackupBlob(blob, CreateUpdateBlob(prod, true));
			Console.Write("Deleting exising {0} ...   ", blob.Uri);
			if (blob.DeleteIfExists())
				Console.WriteLine("Deleted");
			else
				Console.WriteLine("Nothing to delete");
			var srcArchiveFileName = GetIntermediateArchiveFullFileName(prod);
			Console.WriteLine("Uploading new bits from {0}", srcArchiveFileName);
			blob.UploadFromFile(srcArchiveFileName);
			Console.WriteLine("Successfully uploaded {0}. New etag: {1}", blob.Uri, blob.Properties.ETag);
		}

		private static void BackupBlob(CloudBlockBlob blob, CloudBlockBlob backupBlob)
		{
			Console.Write("Backing up exising {0} to {1} ...   ", blob.Uri, backupBlob.Uri);
			backupBlob.StartCopy(blob);
			for (int iter = 1; ; ++iter)
			{

				if (backupBlob.CopyState.Status == CopyStatus.Success)
				{
					Console.WriteLine("Done");
					break;
				}
				else if (backupBlob.CopyState.Status == CopyStatus.Pending)
				{
					Thread.Sleep(1000);
					if (iter % 5 == 0)
						Console.Write(".");
					if (iter > 120)
					{
						Console.WriteLine();
						Console.WriteLine("Back up takes too much. Continue w/o backup.");
						break;
					}
				}
				else if (backupBlob.CopyState.Status == CopyStatus.Failed || backupBlob.CopyState.Status == CopyStatus.Aborted || backupBlob.CopyState.Status == CopyStatus.Invalid)
				{
					Console.WriteLine();
					Console.WriteLine("Backup {0} ({1})" + backupBlob.CopyState.Status, backupBlob.CopyState.StatusDescription);
					break;
				}
			}
		}

		static void InitStorageAccount()
		{
			CloudStorageAccount storageAccount = CreateStorageAccount();

			var blobClient = storageAccount.CreateCloudBlobClient();

			CloudBlobContainer updatesContainer = blobClient.GetContainerReference(settings.UpdatesBlobContainerName);
			Console.Write("Creating updates blob container ... ");
			Console.WriteLine(updatesContainer.CreateIfNotExists() ? "Created" : "Already exists");

			BlobContainerPermissions updatesContainerPermissions = new BlobContainerPermissions();
			updatesContainerPermissions.PublicAccess = BlobContainerPublicAccessType.Blob;

			Console.Write("Setting permissions on the blob container ... ");
			updatesContainer.SetPermissions(updatesContainerPermissions);
			Console.WriteLine("Done");
			
			var tableClient = storageAccount.CreateCloudTableClient();
			var telemetryTable = tableClient.GetTableReference(settings.TelemetryTableName);
			Console.Write("Creating telemetry table ... ");
			Console.WriteLine(telemetryTable.CreateIfNotExists() ? "Created" : "Already exists");

			var tablePermissions = new TablePermissions();
			tablePermissions.SharedAccessPolicies.Add(settings.StoredClientsAccessPolicyName, new SharedAccessTablePolicy()
			{
				SharedAccessExpiryTime = DateTime.MaxValue,
				Permissions = SharedAccessTablePermissions.Add
			});

			Console.Write("Setting permissions on telemetry table ... ");
			telemetryTable.SetPermissions(tablePermissions);

			CloudBlobContainer issuesContainer = blobClient.GetContainerReference(settings.IssuesBlobContainerName);
			Console.Write("Creating issues container ... ");
			Console.WriteLine(issuesContainer.CreateIfNotExists() ? "Created" : "Already exists");
			
			BlobContainerPermissions issuesContainerPermissions = new BlobContainerPermissions();
			issuesContainerPermissions.SharedAccessPolicies.Add(settings.StoredClientsAccessPolicyName, new SharedAccessBlobPolicy()
			{
				SharedAccessExpiryTime = DateTime.MaxValue,
				Permissions = SharedAccessBlobPermissions.Create
			});

			Console.Write("Setting permissions on issues container ... ");
			issuesContainer.SetPermissions(issuesContainerPermissions);

			Console.WriteLine("Done");
		}

		static X509Certificate2 FindStorageKeyEncryptionCertificate(bool mustHavePrivateKey)
		{
			var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
			store.Open(OpenFlags.OpenExistingOnly | OpenFlags.ReadOnly);
			var ret = store.Certificates.Cast<X509Certificate2>().FirstOrDefault(cert => 
				cert.Thumbprint != null && 
				cert.Thumbprint.Equals(settings.StorageAccountKeyEncryptionCertThumbprint, StringComparison.InvariantCultureIgnoreCase));
			store.Close();
			if (ret == null)
				throw new SecurityException("Storage key encryption certificate is not found in Personal store");
			if (mustHavePrivateKey && !ret.HasPrivateKey)
				throw new SecurityException("Storage key encryption certificate does not have private key");
			return ret;
		}

		static string DecryptStringWithStorageCertificate(string encryptedData)
		{
			var cert = FindStorageKeyEncryptionCertificate(mustHavePrivateKey: true);
			byte[] encryptedDataBytes = Convert.FromBase64String(encryptedData);
			var rsaProvider = (RSACryptoServiceProvider)cert.PrivateKey;
			var decryptedBytes = rsaProvider.Decrypt(encryptedDataBytes, false);
			return Encoding.UTF8.GetString(decryptedBytes);
		}

		static void EncryptStringsWithStorageCertificate(string[] strings)
		{
			var cert = FindStorageKeyEncryptionCertificate(mustHavePrivateKey: false);
			foreach (var s in strings)
			{
				var bytes = Encoding.UTF8.GetBytes(s);
				var rsaProvider = (RSACryptoServiceProvider)cert.PublicKey.Key;
				var encryptedBytes = rsaProvider.Encrypt(bytes, false);
				Console.WriteLine("--- Encrypting input string:");
				Console.WriteLine(s);
				Console.WriteLine("--- Encrypted output string:");
				Console.WriteLine(Convert.ToBase64String(encryptedBytes));
			}
			Console.WriteLine("{0} string(s) enrypted successfully", strings.Length);
		}

		static void DownloadTelemetry(string monthsBack)
		{
			var table = CreateTelemetryTable();

			int monthsBackNum;
			int.TryParse(monthsBack ?? "", out monthsBackNum);

			var query = new TableQuery<Telemetry.AzureStorageEntry>();
			if (monthsBackNum != 0)
				query = query.Where(TableQuery.GenerateFilterCondition("PartitionKey", QueryComparisons.GreaterThan, DateTime.UtcNow.AddMonths(-monthsBackNum).ToString("yyyy-MM")));

			var dataSet = new Telemetry.RawDataSet();
			int processedEntriesCount = 0;
			Console.WriteLine();
			Console.Write("Processed entries:    0");
			Action printProcessedEntries = () =>
			{
				Console.Write("\b\b\b\b");
				Console.Write("{0,4}", processedEntriesCount);
			};
			foreach (var entry in table.ExecuteQuery(query))
			{
				dataSet.HandleTelemetryEntry(entry);
				processedEntriesCount++;
				if (processedEntriesCount % 100 == 0)
				{
					printProcessedEntries();
				}
			}
			printProcessedEntries();
			Console.WriteLine();
			Telemetry.Analytics.AnalizeFeaturesUse(dataSet);
			Console.WriteLine();
		}
		
		static void DownloadIssues(string arg)
		{
			var container = CreateIssuesContainer();
			int blobsCount = 0;
			foreach (var blobItem in container.ListBlobs())
			{
				++blobsCount;
				var blob = new CloudBlob(blobItem.Uri);
				// todo
			}
			Console.WriteLine("Downloaded {0} issue reports", blobsCount);
		}
	}
}
