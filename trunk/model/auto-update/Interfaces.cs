using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using System.Threading;

namespace LogJoint.AutoUpdate
{
    public interface IAutoUpdater : IDisposable
    {
        AutoUpdateState State { get; }
        LastUpdateCheckInfo LastUpdateCheckResult { get; }
        void CheckNow();
        bool TrySetRestartAfterUpdateFlag();

        event EventHandler Changed;
    };

    public enum AutoUpdateState
    {
        Unknown,
        Disabled,
        Inactive,
        Idle,
        Checking,
        WaitingRestart,
        Failed,
        FailedDueToBadInstallationDirectory
    };

    public class LastUpdateCheckInfo
    {
        public DateTime When { get; private set; }
        public string ErrorMessage { get; private set; }

        public LastUpdateCheckInfo(DateTime when, string errorMessage)
        {
            When = when;
            ErrorMessage = errorMessage;
        }
    };

    public interface IUpdateDownloader
    {
        bool IsDownloaderConfigured { get; }
        Task<DownloadUpdateResult> DownloadUpdate(string etag, Stream targetStream, CancellationToken cancellation);
        Task<DownloadUpdateResult> CheckUpdate(string etag, CancellationToken cancellation);
    };

    public struct DownloadUpdateResult
    {
        public enum StatusCode
        {
            Success,
            NotModified,
            Failure
        };
        public StatusCode Status;
        public string ETag;
        public DateTime LastModifiedUtc;
        public string ErrorMessage;
    };

    public interface IPendingUpdate
    {
        IUpdateKey Key { get; }
        bool TrySetRestartAfterUpdateFlag();
        Task Dispose();
    };

    public interface IUpdateKey
    {
        bool Equals(IUpdateKey other);
    };

    public interface IFactory
    {
        IUpdateDownloader CreateAppUpdateDownloader();
        IUpdateDownloader CreatePluginsIndexUpdateDownloader();
        IUpdateDownloader CreatePluginUpdateDownloader(Extensibility.IPluginInfo pluginInfo);
        IUpdateKey CreateUpdateKey(string appEtag, IReadOnlyDictionary<string, string> pluginsEtags);
        IUpdateKey CreateNullUpdateKey();
        Task<IPendingUpdate> CreatePendingUpdate(
            IReadOnlyList<Extensibility.IPluginInfo> requiredPlugins,
            string managedAssembliesPath,
            string updateLogFileName,
            CancellationToken cancellation
        );
        IAutoUpdater CreateAutoUpdater(Extensibility.IPluginsManagerInternal pluginsManager);
    };

    class BadInstallationDirException : Exception
    {
        public BadInstallationDirException(Exception e)
            : base("bad installation directory: unable to create pending update directory", e)
        {
        }
    };

    class PastUpdateFailedException : Exception
    {
        public PastUpdateFailedException(string updateLogName, string updateLogContents)
            : base(string.Format("update failed. Log {0}: {1}", updateLogName, updateLogContents))
        {
        }
    };
}
