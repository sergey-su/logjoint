using System;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System.Reflection;
using System.IO;
using System.Xml.Linq;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO.Compression;
using LogJoint.Persistence;
using System.Collections.Immutable;
using System.Runtime.InteropServices;

namespace LogJoint.AutoUpdate
{
    class PendingUpdate : IPendingUpdate
    {
        readonly string tempInstallationDir;
        readonly IUpdateKey key;
        readonly LJTraceSource trace;
        readonly Process updaterProcess;
        readonly string autoRestartFlagFileName;
        static int pendingUpdateIdx;

        public static async Task<IPendingUpdate> Create(
            IFactory factory,
            ITempFilesManager tempFiles,
            ITraceSourceFactory traceSourceFactory,
            MultiInstance.IInstancesCounter mutualExecutionCounter,
            IReadOnlyList<Extensibility.IPluginInfo> requiredPlugins,
            string managedAssembliesPath,
            string updateLogFileName,
            CancellationToken cancellation
        )
        {
            LJTraceSource trace = traceSourceFactory.CreateTraceSource("AutoUpdater", $"pupd-{Interlocked.Increment(ref pendingUpdateIdx)}");

            string installationDir = Path.GetFullPath(
                Path.Combine(managedAssembliesPath, Constants.installationPathRootRelativeToManagedAssembliesLocation));
            string tempInstallationDir = GetTempInstallationDir(installationDir, tempFiles);

            async Task<(string tempZipFile, DownloadUpdateResult result)> Download(IUpdateDownloader updateDownloader, string name)
            {
                var tempFileName = tempFiles.GenerateNewName();
                using (var tempFileStream = new FileStream(tempFileName, FileMode.Create, FileAccess.Write))
                {
                    trace.Info("downloading update for '{0}' to '{1}'", name, tempFileName);
                    var downloadResult = await updateDownloader.DownloadUpdate(null, tempFileStream, cancellation);
                    cancellation.ThrowIfCancellationRequested();
                    if (downloadResult.Status == DownloadUpdateResult.StatusCode.Failure)
                        throw new Exception($"Failed to download update for {name}: {downloadResult.ErrorMessage}");
                    return (tempFileName, downloadResult);
                }
            }

            var downloadResults = await Task.WhenAll(
                new[] { Download(factory.CreateAppUpdateDownloader(), "app") }
                .Union(requiredPlugins.Select(plugin => Download(factory.CreatePluginUpdateDownloader(plugin), plugin.Name)))
            );

            void UnzipDownloadedUpdate(string zipFileName, string targetDir)
            {
                using (var fs = new FileStream(zipFileName, FileMode.Open))
                using (var zipFile = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    try
                    {
                        zipFile.ExtractToDirectory(targetDir);
                    }
                    catch (UnauthorizedAccessException e)
                    {
                        throw new BadInstallationDirException(e);
                    }
                }
                cancellation.ThrowIfCancellationRequested();
            }

            trace.Info("unzipping downloaded update to {0}", tempInstallationDir);
            UnzipDownloadedUpdate(downloadResults[0].tempZipFile, tempInstallationDir);

            var newUpdateInfoPath = Path.Combine(tempInstallationDir,
                Constants.managedAssembliesLocationRelativeToInstallationRoot, Constants.updateInfoFileName);
            new UpdateInfoFileContent(downloadResults[0].result.ETag, DateTime.UtcNow, null).Write(newUpdateInfoPath);

            UpdatePermissions(tempInstallationDir);

            trace.Info("starting updater");

            async Task<(Process process, string autoRestartFlagFileName)> StartUpdater()
            {
                var tempUpdaterExePath = tempFiles.GenerateNewName() + ".lj.updater.exe";
                string updaterExePath;
                string programToStart;
                string firstArg;
                string autoRestartCommandLine;
                string autoRestartIPCKey;
                string restartFlagFileName;

                if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
                {
                    updaterExePath = Path.Combine(installationDir, Constants.managedAssembliesLocationRelativeToInstallationRoot, "logjoint.updater.exe");
                    var monoPath = @"/Library/Frameworks/Mono.framework/Versions/Current/bin/mono";
                    programToStart = monoPath;
                    firstArg = string.Format("\"{0}\" ", tempUpdaterExePath);
                    restartFlagFileName = tempFiles.GenerateNewName() + ".autorestart";
                    autoRestartIPCKey = restartFlagFileName;
                    autoRestartCommandLine = Path.GetFullPath(Path.Combine(installationDir, ".."));
                }
                else
                {
                    updaterExePath = Path.Combine(installationDir, "updater", "logjoint.updater.exe");
                    programToStart = tempUpdaterExePath;
                    firstArg = "";
                    autoRestartIPCKey = Constants.startAfterUpdateEventName;
                    autoRestartCommandLine = Path.Combine(installationDir, "logjoint.exe");
                    restartFlagFileName = null;
                }

                File.Copy(updaterExePath, tempUpdaterExePath);

                trace.Info("updater executable copied to '{0}'", tempUpdaterExePath);

                trace.Info("this update's log is '{0}'", updateLogFileName);

                var updaterExeProcessParams = new ProcessStartInfo()
                {
                    UseShellExecute = false,
                    FileName = programToStart,
                    Arguments = string.Format("{0}\"{1}\" \"{2}\" \"{3}\" \"{4}\" \"{5}\" \"{6}\"",
                        firstArg,
                        installationDir,
                        tempInstallationDir,
                        mutualExecutionCounter.MutualExecutionKey,
                        updateLogFileName,
                        autoRestartIPCKey,
                        autoRestartCommandLine
                    ),
                    WorkingDirectory = Path.GetDirectoryName(tempUpdaterExePath)
                };

                trace.Info("starting updater executable '{0}' with args '{1}'",
                    updaterExeProcessParams.FileName,
                    updaterExeProcessParams.Arguments);

                Environment.SetEnvironmentVariable("MONO_ENV_OPTIONS", ""); // todo
                var process = Process.Start(updaterExeProcessParams);
                // wait a bit to catch and log immediate updater's failure
                for (int i = 0; i < 10 && !cancellation.IsCancellationRequested; ++i)
                {
                    if (process.HasExited && process.ExitCode != 0)
                    {
                        trace.Error("updater process exited abnormally with code {0}", process.ExitCode);
                        break;
                    }
                    await Task.Delay(100);
                }
                return (process, restartFlagFileName);
            }

            var (updater, autoRestartFlagFileName) = await StartUpdater();
            var key = factory.CreateUpdateKey(
                downloadResults[0].result.ETag,
                ImmutableDictionary.CreateRange(
                    downloadResults.Skip(1).Select(r => r.result.ETag).Zip(requiredPlugins, (etag, plugin) => new KeyValuePair<string, string>(plugin.Id, etag))
                )
            );

            var pluginsFolder = Path.Combine(tempInstallationDir, Constants.managedAssembliesLocationRelativeToInstallationRoot, "Plugins");
            if (Directory.Exists(pluginsFolder))
                Directory.Delete(pluginsFolder, true);
            Directory.CreateDirectory(pluginsFolder);


            var pluginFormats = new HashSet<string>();
            foreach (var plugin in downloadResults.Skip(1).Zip(requiredPlugins, (downloadResult, plugin) => (plugin, downloadResult)))
            {
                var pluginFolder = Path.Combine(pluginsFolder, plugin.plugin.Id);
                UnzipDownloadedUpdate(plugin.downloadResult.tempZipFile, pluginFolder);
                new UpdateInfoFileContent(plugin.downloadResult.result.ETag, plugin.downloadResult.result.LastModifiedUtc, null).Write(
                    Path.Combine(pluginFolder, Constants.updateInfoFileName));

                try
                {
                    Extensibility.IPluginManifest manifest = new Extensibility.PluginManifest(pluginFolder);
                    pluginFormats.UnionWith(manifest.Files
                        .Where(f => f.Type == Extensibility.PluginFileType.FormatDefinition)
                        .Select(f => Path.GetFileName(f.AbsolutePath).ToLower()));
                }
                catch (Extensibility.BadManifestException)
                {
                    continue;
                }
            }

            CopyCustomFormats(
                managedAssembliesPath,
                Path.Combine(tempInstallationDir, Constants.managedAssembliesLocationRelativeToInstallationRoot),
                pluginFormats, // Temporary measure: plugin formats used to be copied to root Formats folder. Ignore them on update.
                trace
            );

            return new PendingUpdate(tempInstallationDir, key, trace, updater, autoRestartFlagFileName);
        }

        PendingUpdate(
            string tempInstallationDir,
            IUpdateKey key,
            LJTraceSource trace,
            Process updaterProcess,
            string autoRestartFlagFileName
        )
        {
            this.tempInstallationDir = tempInstallationDir;
            this.key = key;
            this.trace = trace;
            this.updaterProcess = updaterProcess;
            this.autoRestartFlagFileName = autoRestartFlagFileName;
        }

        IUpdateKey IPendingUpdate.Key => key;

        async Task IPendingUpdate.Dispose()
        {
            trace.Info("Disposing");
            var updaterTask = updaterProcess.GetExitCodeAsync(TimeSpan.FromSeconds(10));
            updaterProcess.Kill();
            await updaterTask;
            if (Directory.Exists(tempInstallationDir))
            {
                trace.Info("Deleting temp update folder");
                try
                {
                    Directory.Delete(tempInstallationDir, true);
                }
                catch (Exception e)
                {
                    trace.Error(e, "Failed to delete temp folder");
                }
            }
            trace.Info("Disposed");
        }

        bool IPendingUpdate.TrySetRestartAfterUpdateFlag()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                if (autoRestartFlagFileName == null)
                    return false;
                if (!File.Exists(autoRestartFlagFileName))
                    return false;
                using (var fs = File.OpenWrite(autoRestartFlagFileName))
                    fs.WriteByte((byte)'1');
                return true;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                EventWaitHandle evt;
                if (!EventWaitHandle.TryOpenExisting(Constants.startAfterUpdateEventName, out evt))
                    return false;
                evt.Set();
                return true;
            }
            else
            {
                return false;
            }
        }

        static string GetTempInstallationDir(string installationDir, ITempFilesManager tempFiles)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                string tempInstallationDir = Path.Combine(
                    tempFiles.GenerateNewName(),
                    "pending-logjoint-update");
                return tempInstallationDir;
            }
            else
            {
                // On windows: download update to a folder next to installation dir.
                // This ensures almost 100% that temp folder and installation dir are on the same HDD partition
                // which ensures speed and success of moving the temp folder in place of installation dir.
                var localUpdateCheckId = Guid.NewGuid().GetHashCode();
                string tempInstallationDir = Path.GetFullPath(string.Format(@"{0}\..\pending-logjoint-update-{1:x}",
                    installationDir, localUpdateCheckId));
                return tempInstallationDir;
            }
        }

        static void UpdatePermissions(string installationDir)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                var executablePath = Path.Combine(installationDir,
                    Constants.nativeExecutableLocationRelativeToInstallationRoot);
                IOUtils.EnsureIsExecutable(executablePath);
            }
        }

        static IEnumerable<KeyValuePair<string, string>> EnumFormatsDefinitions(string formatsDir)
        {
            return (new DirectoryFormatsRepository(formatsDir))
                .Entries
                .Select(e => new KeyValuePair<string, string>(Path.GetFileName(e.Location).ToLower(), e.Location));
        }

        static void CopyCustomFormats(
            string managedAssmebliesLocation,
            string tmpManagedAssmebliesLocation,
            HashSet<string> execludedFormats,
            LJTraceSource trace
        )
        {
            var srcFormatsDir = Path.Combine(managedAssmebliesLocation, DirectoryFormatsRepository.RelativeFormatsLocation);
            var destFormatsDir = Path.Combine(tmpManagedAssmebliesLocation, DirectoryFormatsRepository.RelativeFormatsLocation);
            var destFormats = EnumFormatsDefinitions(destFormatsDir).ToLookup(x => x.Key);
            foreach (var srcFmt in EnumFormatsDefinitions(srcFormatsDir).Where(x => !destFormats.Contains(x.Key)))
            {
                if (execludedFormats.Contains(Path.GetFileName(srcFmt.Value).ToLower()))
                {
                    trace.Info("not copying format excluded format {0}", srcFmt.Key);
                    continue;
                }
                trace.Info("copying user-defined format {0} to {1}", srcFmt.Key, destFormatsDir);
                File.Copy(srcFmt.Value, Path.Combine(destFormatsDir, Path.GetFileName(srcFmt.Value)));
            }
        }
    };
}
