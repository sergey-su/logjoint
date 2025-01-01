using LogJoint.AppLaunch;
using Microsoft.Win32;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint
{
    class PluggableProtocolManager
    {
        public PluggableProtocolManager(
            ITraceSourceFactory traceSourceFactory,
            MultiInstance.IInstancesCounter instancesCounter,
            IShutdown shutdown,
            Telemetry.ITelemetryCollector telemetryCollector,
            Persistence.IFirstStartDetector firstStartDetector,
            ILaunchUrlParser launchUrlParser
        )
        {
            this.tracer = traceSourceFactory.CreateTraceSource("PluggableProtocol");
            this.launchUrlParser = launchUrlParser;

            if (instancesCounter.IsPrimaryInstance)
            {
                this.regUpdater = RegistryUpdater(shutdown.ShutdownToken, telemetryCollector, firstStartDetector.IsFirstStartDetected);
            }

            shutdown.Cleanup += (s, e) =>
            {
                if (regUpdater != null)
                    shutdown.AddCleanupTask(regUpdater.WithTimeout(TimeSpan.FromSeconds(1)));
            };
        }

        async Task RegistryUpdater(CancellationToken cancel, Telemetry.ITelemetryCollector telemetryCollector, bool skipWaiting)
        {
            try
            {
                tracer.Info("pluggable protocol registration updater started");
                if (!skipWaiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(15), cancel).ConfigureAwait(continueOnCapturedContext: false);
                    tracer.Info("waited enough. waking up");
                }

                if (!TestRegEntries())
                {
                    var exePath = Application.ExecutablePath;
                    if (IsGoodExeToRegister(exePath))
                        UpdateRegEntries(exePath);
                }
            }
            catch (TaskCanceledException)
            {
            }
            catch (Exception e)
            {
                telemetryCollector.ReportException(e, "failed to register pluggable protocol");
            }
        }

        bool IsGoodExeToRegister(string exePath)
        {
            try
            {
                tracer.Info("testing if exe '{0}' is good to be registered as pluggable protocol handler", exePath);
                var rootPath = Path.GetPathRoot(exePath);
                if (string.IsNullOrEmpty(rootPath))
                {
                    tracer.Warning("no root path");
                    return false;
                }
                var driveInfo = new DriveInfo(rootPath);
                var driveType = driveInfo.DriveType;
                var isGoodDriveType = driveType == DriveType.Fixed || driveType == DriveType.Ram;
                tracer.Info("exe is on a drive of type {0} which is {1} to contain protocol handler exe",
                    driveType, isGoodDriveType ? "GOOD" : "NOT GOOD");
                return isGoodDriveType;
            }
            catch (ArgumentException e)
            {
                tracer.Error(e, "failed to test exe path");
                return false;
            }
        }

        bool TestRegEntries()
        {
            tracer.Info("testing existing protocol registration");
            Func<string, bool> fail = reason =>
            {
                tracer.Warning("current registration is bad: {0}", reason);
                return false;
            };
            using (var commandKey = Registry.ClassesRoot.OpenSubKey(launchUrlParser.ProtocolName + @"\shell\open\command"))
            {
                if (commandKey == null)
                    return fail("protocol key does not exist");
                var command = commandKey.GetValue("", "") as string;
                if (command == null)
                    return fail("command is not specified");
                string exePath = ParseExePathFromCommand(command);
                if (exePath == null)
                    return fail("can not find exe path in command " + command);
                bool exeExists = File.Exists(exePath);
                if (!exeExists)
                    return fail(string.Format("exe '{0}' specified in command '{1}'  does not exist", exePath, command));
                var configFile = exePath + ".config";
                if (File.Exists(configFile) && File.ReadAllText(configFile).Contains("etRo9N3hAZhGqN0rivOi3orK8MSOBf"))
                    return fail(string.Format("exe '{0}' is known buggy bin", exePath));
                tracer.Info("protocol registration is OK");
                return true;
            }
        }

        void UpdateRegEntries(string exePath)
        {
            tracer.Info("updating registry entries");
            using (var protocolRootKey = Registry.CurrentUser.CreateSubKey(@"Software\Classes\" + launchUrlParser.ProtocolName))
            {
                protocolRootKey.SetValue("", "URL:LogJoint Protocol");
                protocolRootKey.SetValue("URL Protocol", "");
                using (var iconKey = protocolRootKey.CreateSubKey("DefaultIcon"))
                    iconKey.SetValue("", string.Format("{0}{1}{0},0", '"', exePath));
                using (var commandKey = protocolRootKey.CreateSubKey(@"shell\open\command"))
                    commandKey.SetValue("", string.Format("{0}{1}{0} {0}%1{0}", '"', exePath));
            }
            tracer.Info("registry entries updated OK");
        }

        static string ParseExePathFromCommand(string command)
        {
            var split = command.Split(new[] { '"' }, StringSplitOptions.RemoveEmptyEntries);
            if (split.Length < 1)
                return null;
            return split[0];
        }

        readonly LJTraceSource tracer;
        readonly Task regUpdater;
        readonly ILaunchUrlParser launchUrlParser;
    }
}
