using LogJoint.Preprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Symphony.SpringServiceLog
{
	public class DownloadStep : IPreprocessingStep, IDownloadPreprocessingStep
	{
		internal static readonly string stepName = "sym.cloudwatch.download";
		readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		readonly WebViewTools.IWebViewTools webViewTools;

		internal DownloadStep(
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			WebViewTools.IWebViewTools webViewTools
		)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.webViewTools = webViewTools;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			var logs = await ExecuteInternal(callback);
			foreach (var l in logs)
				callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(l));
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			return (await ExecuteInternal(callback)).FirstOrDefault();
		}


		async Task<List<PreprocessingStepParams>> ExecuteInternal(IPreprocessingStepCallback callback)
		{
			await callback.BecomeLongRunning();
			callback.SetStepDescription("Downloading...");

			var result = new List<PreprocessingStepParams>();
			var request = new CloudWatchDownloader.DownloadRequest(
				CloudWatchDownloader.Environment.QA5,
				new [] { "1a641c4d-d564-4f16-8c0d-1d51eb8a5e26" },
				DateTime.Parse("2019-09-17T11:33:08.240Z")
			);
			var logs = await CloudWatchDownloader.Download(
				webViewTools, request, callback.SetStepDescription);
			foreach (var l in logs)
			{
				string tmpFileName = callback.TempFilesManager.GenerateNewName();
				File.WriteAllText(tmpFileName, l.Value);
				result.Add(new PreprocessingStepParams(
					tmpFileName,
					//$"{sourceFile.FullPath}\\as_pdml",
					$"CloudWatchLogs\\{l.Key}"
					//sourceFile.PreprocessingHistory.Add(new PreprocessingHistoryItem(stepName, StepArgument.ToString(keyFiles))),
					//$"{sourceFile.FullPath} (converted to PDML)"
				));
			}
			return result;
		}
	};
}
