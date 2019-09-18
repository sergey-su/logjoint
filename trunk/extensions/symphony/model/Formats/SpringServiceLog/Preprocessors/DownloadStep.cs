using LogJoint.Preprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace LogJoint.Symphony.SpringServiceLog
{
	public class DownloadStep : IPreprocessingStep, IDownloadPreprocessingStep
	{
		internal static readonly string stepName = "sym.backendlogs.download";
		internal static readonly string urlProtocol = "logjoint";
		internal static readonly string urlHost = "rtc-backend-logs";
		readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		readonly WebViewTools.IWebViewTools webViewTools;
		readonly PreprocessingStepParams source;

		internal DownloadStep(
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			WebViewTools.IWebViewTools webViewTools,
			PreprocessingStepParams source
		): this(preprocessingStepsFactory, webViewTools)
		{
			this.source = source;
		}

		internal DownloadStep(
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			WebViewTools.IWebViewTools webViewTools,
			IReadOnlyCollection<string> ids,
			DateTime referenceTime,
			string env
		): this(preprocessingStepsFactory, webViewTools)
		{
			source = new PreprocessingStepParams(MakeUrl(
				new CloudWatchDownloader.DownloadRequest(env, ids, referenceTime)
			));
		}

		private DownloadStep(
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			WebViewTools.IWebViewTools webViewTools
		)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.webViewTools = webViewTools;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			var l = await ExecuteInternal(callback);
			callback.YieldNextStep(preprocessingStepsFactory.CreateUnpackingStep(l));
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			return await ExecuteInternal(callback);
		}


		async Task<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback)
		{
			await callback.BecomeLongRunning();

			if (!TryParseUrl(source.Location, out var request))
			{
				throw new ArgumentException($"Can not parse URL {source.Location}");
			}

			using (var sharedDownloadTask = callback.GetOrAddSharedValue($"{stepName}:{source.Location}", async () =>
			{
				var logs = await CloudWatchDownloader.Download(
					webViewTools, request, callback.SetStepDescription);
				string zipTmpFileName = callback.TempFilesManager.GenerateNewName();
				using (var zipToOpen = new FileStream(zipTmpFileName, FileMode.CreateNew))
				using (var archive = new ZipArchive(zipToOpen, ZipArchiveMode.Create))
				{
					foreach (var l in logs)
					{
						string tmpFile = callback.TempFilesManager.GenerateNewName();
						File.WriteAllText(tmpFile, l.Value);
						archive.CreateEntryFromFile(tmpFile, l.Key);
						File.Delete(tmpFile);
					}
				}
				return zipTmpFileName;
			}))
			{
				if (!sharedDownloadTask.IsValueCreator)
					callback.SetStepDescription("Waiting for downloaded data...");
				var tmpFileName = await sharedDownloadTask.Value;
				return new PreprocessingStepParams(
					tmpFileName,
					source.FullPath,
					source.PreprocessingHistory.Add(new PreprocessingHistoryItem(stepName))
				);
			}
			// todo: cache
		}

		static string MakeUrl(CloudWatchDownloader.DownloadRequest request)
		{
			return $"{urlProtocol}://{urlHost}?ids={string.Join(",", request.Ids.Select(Uri.EscapeDataString))}&t={request.ReferenceTime.ToUnixTimestampMillis()}&env={request.Env.Name}";
		}

		static bool TryParseUrl(string url,
			out CloudWatchDownloader.DownloadRequest request)
		{
			request = null;
			if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
				return false;
			if (uri.Scheme != urlProtocol)
				return false;
			if (uri.Host != urlHost)
				return false;
			var ids = new string[0];
			long t = 0;
			string env = "?";
			foreach (var q in uri.Query.Split(new [] { '&', '?' }, StringSplitOptions.RemoveEmptyEntries))
			{
				var kv = q.Split('=');
				if (kv.Length != 2)
					continue;
				if (kv[0] == "ids")
					ids = kv[1].Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries).Select(Uri.UnescapeDataString).ToArray();
				else if (kv[0] == "t")
					long.TryParse(kv[1], out t);
				else if (kv[0] == "env")
					env = kv[1];
			}
			request = new CloudWatchDownloader.DownloadRequest(
				env, ids, t.UnixTimestampMillisToDateTime());
			return true;
		}
	};
}
