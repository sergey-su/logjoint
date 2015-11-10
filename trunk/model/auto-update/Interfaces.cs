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
	public interface IAutoUpdater: IDisposable
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
		public DateTime When;
		public string ErrorMessage;
	};

	public interface IUpdateDownloader
	{
		bool IsDownloaderConfigured { get; }
		Task<DownloadUpdateResult> DownloadUpdate(string etag, Stream targetStream, CancellationToken cancellation);
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
}
