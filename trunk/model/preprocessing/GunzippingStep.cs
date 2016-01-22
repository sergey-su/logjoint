using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zlib;
using System.Threading.Tasks;
using System;

namespace LogJoint.Preprocessing
{
	public class GunzippingStep : IPreprocessingStep
	{
		internal GunzippingStep(
			PreprocessingStepParams srcFile,
			Progress.IProgressAggregator progressAggregator,
			IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			return ExecuteInternal(callback);
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(await ExecuteInternal(callback)));
		}

		async Task<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback)
		{
			await callback.BecomeLongRunning();

			string tmpFileName = callback.TempFilesManager.GenerateNewName();

			var sourceFileInfo = new FileInfo(sourceFile.Uri);

			using (var inFileStream = sourceFileInfo.OpenRead())
			using (var outFileStream = new FileStream(tmpFileName, FileMode.CreateNew))
			using (var progress = sourceFileInfo.Length != 0 ? progressAggregator.CreateProgressSink() : (Progress.IProgressEventsSink)null)
			{
				using (var gzipStream = new GZipStream(inFileStream, CompressionMode.Decompress, true))
				{
					DownloadingStep.CopyStreamWithProgress(gzipStream, outFileStream, downloadedBytes =>
					{
						callback.SetStepDescription(string.Format("{1} {0}: Gunzipping...",
								DownloadingStep.FileSizeToString(downloadedBytes), sourceFile.FullPath));
						if (progress != null)
							progress.SetValue((double)downloadedBytes / (double)sourceFileInfo.Length);
					});

					return
						new PreprocessingStepParams(tmpFileName, sourceFile.FullPath,
							Utils.Concat(sourceFile.PreprocessingSteps, name));
				}
			}
		}

		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		internal const string name = "gunzip";
	};
}
