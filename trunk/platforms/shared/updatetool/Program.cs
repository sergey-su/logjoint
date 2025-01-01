﻿using Microsoft.WindowsAzure.Storage;
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
using System.Diagnostics;
using System.IO.Compression;

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
                Console.WriteLine("  test [prod]                 takes binaries collected by last pack command and runs integration tests against them.");
                Console.WriteLine("  deploy [prod]               deploys zipped binaries to the cloud as staging blob (default) or production blob");
                Console.WriteLine("  init                        (re)initializes storage account by creating required objects and setting proper access policies");
                Console.WriteLine("  telem [months]              downloads and analyzes telemetry for specified nr of recent months");
                Console.WriteLine("  issues                      downloads issues reports");
                Console.WriteLine("  packinst [prod]             builds installation image");
                Console.WriteLine("  deployinst [prod]           deploys installation image to the cloud");
                Console.WriteLine("  encrypt <str>               encrypts a string with configured encryption certificate (thumbprint={0})", settings.StorageAccountKeyEncryptionCertThumbprint);
                Console.WriteLine("  plugin alloc [id]           allocates new plugin id, or if id is specified, only allocates inbox url");
                Console.WriteLine("  plugin index [prod]         updates plugins index blob");
                Console.WriteLine("  plugin test [prod]          run integration tests for all plugins from inbox against prod or staging host app");
                return;
            }

            switch (args[0])
            {
                case "pack":
                    Pack(args.Skip(1).ToArray());
                    break;
                case "test":
                    Test(args.Skip(1).ToArray());
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
                    PackInstaller(args.Skip(1).ToArray());
                    break;
                case "deployinst":
                    DeployInstaller(args.Skip(1).ToArray());
                    break;
                case "plugin":
                    switch (args.ElementAtOrDefault(1) ?? "")
                    {
                        case "alloc":
                            AllocatePlugin(args.Skip(2).ToArray());
                            break;
                        case "index":
                            UpdatePluginsIndex(args.Skip(2).ToArray());
                            break;
                        case "test":
                            TestPlugins(args.Skip(2).ToArray());
                            break;
                        default:
                            Console.WriteLine("Unknown plugin command");
                            break;
                    }
                    break;
                default:
                    Console.WriteLine("Unknown command");
                    break;
            }
        }

        static IEnumerable<string> ResolveMasks(string relativePathWithMask, string root)
        {
            bool needsMaskResolution = relativePathWithMask.Contains('*');
            if (!needsMaskResolution)
                return Enumerable.Repeat(relativePathWithMask, 1);
            if (!root.EndsWith(new string(Path.DirectorySeparatorChar, 1)))
                root += Path.DirectorySeparatorChar;
            var rootUrl = new Uri(root);
            return
                Directory
                .GetFiles(root, relativePathWithMask)
                .Select(resolvedAbsPath => rootUrl.MakeRelativeUri(new Uri(resolvedAbsPath)).ToString());
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
            var winInstallerBlob = CreateInstallerBlob(prod: prod, backup: false, win: true);
            configNode.Value = winInstallerBlob.Uri.ToString();

            configNode = getSettingNode("MacInstallerUrl");
            var macInstallerBlob = CreateInstallerBlob(prod: prod, backup: false, win: false);
            configNode.Value = macInstallerBlob.Uri.ToString();

            configNode = getSettingNode("FeedbackUrl");
            configNode.Value = settings.FeedbackUrl;

            configNode = getSettingNode("PluginsUrl");
            configNode.Value = CreatePluginsBlob(prod).Uri.ToString();

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


            var localPluginsNode = getSettingNode("LocalPlugins");
            localPluginsNode.Value = "";

            // remove local debug configs
            removeAppSettingNode("localCosmosLocation");
            removeAppSettingNode("useCallDbAwareCosmosReader");
            removeAppSettingNode("callDbViewerApp");

            return configDoc;
        }

        static IEnumerable<string> GetFilesList(string sourceFilesLocation)
        {
            return settings.FilesList
                .Split('\r', '\n')
                .Select(s => s.Trim())
                .Where(s => s.Length > 0)
                .SelectMany(s => ResolveMasks(s, sourceFilesLocation));
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
            using (var zip = ZipFile.Open(destArchiveFileName, ZipArchiveMode.Create))
            {
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
                        var configDoc = MakeAppConfig(sourceFileAbsolutePath, prod);
                        Console.WriteLine("Adding to archive:   {0}   (config modified to fetch {1} updates, {2} logging)",
                            relativeFilePath, prod ? "prod" : "staging", prod ? "no" : "full");
                        using (var configStream = zip.CreateEntry(relativeFilePath).Open())
                            configDoc.Save(configStream);
                    }
                    else
                    {
                        Console.WriteLine("Adding to archive:   {0}", relativeFilePath);
                        using (var entryStream = zip.CreateEntry(relativeFilePath).Open())
                        using (var sourceFileStream = new FileStream(sourceFileAbsolutePath, FileMode.Open))
                            sourceFileStream.CopyTo(entryStream);
                    }
                }
            }
            Console.WriteLine("Successfully created: {0} ", destArchiveFileName);
        }

        static void Test(string[] args)
        {
            var testsFileRelative = settings.FilesList
                .Split('\r', '\n')
                .Where(f => f.Contains("logjoint.integration.tests.dll"))
                .FirstOrDefault();
            if (testsFileRelative == null)
                throw new Exception("Cannot find tests file location");
            var prod = args.FirstOrDefault() == "prod";
            var archiveFileName = GetIntermediateArchiveFullFileName(prod);
            Console.WriteLine("Will test packed binaries from {0}", archiveFileName);
            var tempFolder = Path.Combine(Path.GetTempPath(), $"logjoint.updatetool.test.{(prod ? "prod" : "staging")}");
            Console.WriteLine("Will test in temp folder {0}", tempFolder);
            if (Directory.Exists(tempFolder))
            {
                Console.WriteLine("Deleting existing temp folder {0}", tempFolder);
                Directory.Delete(tempFolder, true);
            }
            ZipFile.ExtractToDirectory(archiveFileName, tempFolder);
            ProcessStartInfo runnerStartInfo = new ProcessStartInfo
            {
                FileName = "dotnet",
                WorkingDirectory = tempFolder,
                Arguments = $"\"{testsFileRelative}\"",
                UseShellExecute = false
            };
            Console.WriteLine("Running {0} in {1} with arguments {2}",
                runnerStartInfo.FileName, runnerStartInfo.WorkingDirectory, runnerStartInfo.Arguments);
            using (var runnerProcess = Process.Start(runnerStartInfo))
            {
                runnerProcess.WaitForExit();
            }
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
			Console.Write("Deleting existing {0} ...   ", blob.Uri);
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
            throw new Exception("Not supprted");
        }

        static void DeployInstaller(string[] args)
        {
            throw new Exception("Not supprted");
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
            if (win == null)
            {
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

        private static CloudBlockBlob CreatePluginsBlob(bool prod, bool backup = false)
        {
            return CreateBlob(prod ? settings.PluginsBlobName : settings.StagingPluginsBlobName, backup: backup);
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
            Console.Write("Deleting existing {0} ...   ", blob.Uri);
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
            Console.Write("Backing up existing {0} to {1} ...   ", blob.Uri, backupBlob.Uri);
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

            Console.Write("Setting permissions on the updates blobs container ... ");
            updatesContainer.SetPermissions(updatesContainerPermissions);
            Console.WriteLine("Done");

            void CreatePluginsIndex(bool prod)
            {
                Console.Write($"Creating {(prod ? "PROD" : "STAGING")} plugins index blob ... ");
                var pluginsIndexBlob = CreatePluginsBlob(prod);
                if (pluginsIndexBlob.Exists())
                {
                    Console.WriteLine("Already exists");
                }
                else
                {
                    var initialContents = Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><plugins></plugins>");
                    pluginsIndexBlob.UploadFromByteArray(initialContents, 0, initialContents.Length);
                    Console.WriteLine("Created empty one");
                }
            }
            CreatePluginsIndex(true);
            CreatePluginsIndex(false);

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
            Console.WriteLine("Done");

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

            CloudBlobContainer pluginsInboxContainer = blobClient.GetContainerReference(settings.PluginsInboxBlobContainerName);
            Console.Write("Creating plug-ins inbox blob container ... ");
            Console.WriteLine(pluginsInboxContainer.CreateIfNotExists() ? "Created" : "Already exists");

            BlobContainerPermissions pluginsContainerPermissions = new BlobContainerPermissions();
            pluginsContainerPermissions.PublicAccess = BlobContainerPublicAccessType.Off;
            pluginsContainerPermissions.SharedAccessPolicies.Add(settings.StoredClientsAccessPolicyName, new SharedAccessBlobPolicy()
            {
                SharedAccessExpiryTime = DateTime.MaxValue,
                Permissions = SharedAccessBlobPermissions.Read | SharedAccessBlobPermissions.Write
            });

            Console.Write("Setting permissions on the plug-ins inbox blobs container ... ");
            pluginsInboxContainer.SetPermissions(pluginsContainerPermissions);
            Console.WriteLine("Done");

            Console.WriteLine("Storage setup finished");
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
            Console.WriteLine("{0} string(s) encrypted successfully", strings.Length);
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


        static void AllocatePlugin(string[] arg)
        {
            CloudBlobContainer pluginsInboxContainer = CreatePluginsInboxBlobContainer();

            var newPluginId = arg.ElementAtOrDefault(0);
            if (newPluginId == null)
            {
                newPluginId = Guid.NewGuid().ToString("N");
                Console.WriteLine("Allocated new plug-in id {0}", newPluginId);
            }
            var blob = pluginsInboxContainer.GetBlockBlobReference(newPluginId);

            Console.Write("Uploading initial empty plug-in content ...");
            blob.UploadFromByteArray(new byte[0], 0, 0);
            Console.WriteLine("Done");

            var pluginWriteOnlyUri = TransformUri(blob.Uri, blob.GetSharedAccessSignature(null, settings.StoredClientsAccessPolicyName));
            Console.WriteLine("Allocated plug-in inbox URI:");
            Console.WriteLine("{0}", pluginWriteOnlyUri);
        }

        private static CloudBlobContainer CreatePluginsInboxBlobContainer()
        {
            CloudStorageAccount storageAccount = CreateStorageAccount();
            var blobClient = storageAccount.CreateCloudBlobClient();

            CloudBlobContainer pluginsInboxContainer = blobClient.GetContainerReference(settings.PluginsInboxBlobContainerName);
            return pluginsInboxContainer;
        }

        static string UnquoteETag(string value)
        {
            if (!(value.Length > 2 && value[0] == '"' && value[value.Length - 1] == '"'))
                throw new Exception("ETag is expected to be quoted");
            return value.Substring(1, value.Length - 2);
        }

        static void UpdatePluginsIndex(string[] args)
        {
            var prod = args.FirstOrDefault() == "prod";
            var packagesSuffix = $"{(int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1)).TotalSeconds}";
            Console.WriteLine("Updating {0} plugins index ... ", prod ? "PRODUCTION" : "STAGING");

            var indexBlob = CreatePluginsBlob(prod);
            XDocument existingIndex;
            if (indexBlob.Exists())
            {
                using (var tmpStream = new MemoryStream())
                {
                    indexBlob.DownloadToStream(tmpStream);
                    tmpStream.Position = 0;
                    existingIndex = XDocument.Load(tmpStream);
                }
            }
            else
            {
                existingIndex = new XDocument();
            }

            var anythingChanged = false;
            var result = new XDocument(new XElement("plugins"));
            var inbox = CreatePluginsInboxBlobContainer();
            foreach (var item in inbox.ListBlobs())
            {
                var pluginBlob = inbox.GetBlobReference(new CloudBlockBlob(item.Uri).Name);
                Console.Write("Reading {0} ... ", pluginBlob.Name);
                try
                {
                    var tempZipFile = Path.GetTempFileName();
                    pluginBlob.DownloadToFile(tempZipFile, FileMode.Create);

                    XElement manifest;

                    using (var zip = ZipFile.OpenRead(tempZipFile))
                    {
                        var manifestEntry = zip.GetEntry("manifest.xml");
                        using (var manifestEntryStream = manifestEntry.Open())
                            manifest = XDocument.Load(manifestEntryStream).Root;
                    }

                    bool productionPackage = manifest.Attribute("production")?.Value == "true";
                    var sourceETag = UnquoteETag(pluginBlob.Properties.ETag);
                    var existingPlugin = existingIndex
                        .Elements("plugins")
                        .Elements("plugin")
                        .Where(p => p.Element("id")?.Value == manifest.Element("id").Value)
                        .FirstOrDefault();
                    var existingSourceETag = existingPlugin?.Element("source-etag")?.Value;

                    Console.Write(" ver={0}, platf={1} src-etag={2} {3}... ",
                        manifest.Element("version").Value,
                        manifest.Element("platform").Value,
                        sourceETag,
                        productionPackage ? "" : "DRY RUN");

                    string status;
                    if (!productionPackage)
                    {
                        if (existingPlugin != null)
                        {
                            status = "UNCHANGED (dry run)";
                            result.Root.Add(existingPlugin);
                        }
                        else
                        {
                            status = "SKIPPED (dry run)";
                        }
                    }
                    else if (sourceETag == existingSourceETag)
                    {
                        status = "UNCHANGED (same etag)";
                        result.Root.Add(existingPlugin);
                    }
                    else
                    {
                        var acceptedPluginBlob = CreateBlob($"{pluginBlob.Name}-{packagesSuffix}", backup: false);
                        acceptedPluginBlob.Properties.ContentType = "application/zip";
                        acceptedPluginBlob.UploadFromFile(tempZipFile);

                        var plugin = new XElement("plugin",
                            manifest.Element("id"),
                            manifest.Element("version"),
                            manifest.Element("name"),
                            manifest.Element("description"),
                            manifest.Element("platform"),
                            new XElement("location", acceptedPluginBlob.Uri),
                            new XElement("etag", UnquoteETag(acceptedPluginBlob.Properties.ETag)),
                            new XElement("source-etag", sourceETag)
                        );
                        foreach (var dep in manifest.Elements("dependency"))
                            plugin.Add(dep);

                        result.Root.Add(plugin);
                        status = "DONE";
                        anythingChanged = true;
                    }

                    File.Delete(tempZipFile);
                    Console.WriteLine(status);
                }
                catch (Exception e)
                {
                    Console.WriteLine("FAILED. {0}", e.Message);
                    throw;
                }
            }

            if (anythingChanged)
            {
                indexBlob.Properties.ContentType = "text/xml";
                if (indexBlob.Exists())
                    BackupBlob(indexBlob, CreateUpdateBlob(prod, backup: true));
                Console.Write("Writing index ... ");
                using (var indexStream = new MemoryStream())
                {
                    result.Save(indexStream);
                    indexStream.Position = 0;
                    indexBlob.UploadFromStream(indexStream);
                }
                Console.WriteLine("DONE");
            }
            else
            {
                Console.WriteLine("No changes needed");
            }
        }

        static void TestPlugins(string[] args)
        {
            var prod = args.FirstOrDefault() == "prod";

            var mainAppTempZipFile = Path.GetTempFileName();
            var mainAppBlob = CreateUpdateBlob(prod, prod);
            mainAppBlob.DownloadToFile(mainAppTempZipFile, FileMode.Create);

            var inbox = CreatePluginsInboxBlobContainer();
            var nrFailed = 0;
            foreach (var item in inbox.ListBlobs())
            {
                var pluginBlob = inbox.GetBlobReference(new CloudBlockBlob(item.Uri).Name);
                Console.WriteLine("----------------------------------------------");
                Console.WriteLine("Handling {0}", pluginBlob.Name);
                try
                {
                    var pluginTempZipFile = Path.GetTempFileName();
                    pluginBlob.DownloadToFile(pluginTempZipFile, FileMode.Create);
                    var pluginToolPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), settings.PluginToolLocation));

                    var pi = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"\"{pluginToolPath}\" test \"{pluginTempZipFile}\" \"{mainAppTempZipFile}\"",
                        UseShellExecute = false,
                    };
                    Console.WriteLine("Running {0} {1}", pi.FileName, pi.Arguments);
                    bool passed;
                    using (var proc = Process.Start(pi))
                    {
                        proc.WaitForExit();
                        passed = proc.ExitCode == 0;
                    }

                    Console.WriteLine("Finished {0} {1}", pluginBlob.Name, passed ? "PASSED" : "FAILED");
                    nrFailed += (passed ? 0 : 1);

                    File.Delete(pluginTempZipFile);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed {0}", e.Message);
                    throw;
                }
            }

            File.Delete(mainAppTempZipFile);

            Console.WriteLine("Done. {0} plugin(s) failed.", nrFailed);
        }
    }
}
