using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class UnpackingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal UnpackingStep(
			PreprocessingStepParams srcFile,
			Progress.IProgressAggregator progressAggregator,
			IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			PreprocessingStepParams ret = null;
			await ExecuteInternal(callback, param, x => { ret = x; return false; });
			return ret;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await ExecuteInternal(callback, null, p =>
			{
				callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(p));
				return true;
			});
		}

		async Task ExecuteInternal(IPreprocessingStepCallback callback, string specificFileToExtract, Func<PreprocessingStepParams, bool> onNext)
		{
			await callback.BecomeLongRunning();

			callback.TempFilesCleanupList.Add(sourceFile.Uri);

			using (var zipFile = new Ionic.Zip.ZipFile(sourceFile.Uri))
			{
				string currentEntryBeingExtracted = null;
				Progress.IProgressEventsSink progress = null;
				zipFile.ExtractProgress += (s, evt) =>
				{
					evt.Cancel = callback.Cancellation.IsCancellationRequested;
					if (currentEntryBeingExtracted != null && evt.TotalBytesToTransfer != 0)
					{
						callback.SetStepDescription(string.Format("Unpacking {1}%: {0}",
							currentEntryBeingExtracted,
							evt.BytesTransferred * (long)100 / evt.TotalBytesToTransfer));
						if (progress != null)
							progress.SetValue(
								(double)evt.BytesTransferred / (double)evt.TotalBytesToTransfer);
					}
				};
				var entriesToEnum = specificFileToExtract != null ?
					Enumerable.Repeat(zipFile[specificFileToExtract], 1) : zipFile.Entries;
				foreach (var entry in entriesToEnum.Where(e => e != null))
				{
					if (entry.IsDirectory)
						continue;

					string entryFullPath = sourceFile.FullPath + "\\" + entry.FileName;
					string tmpFileName = callback.TempFilesManager.GenerateNewName();

					callback.SetStepDescription("Unpacking " + entryFullPath);
					using (FileStream tmpFs = new FileStream(tmpFileName, FileMode.CreateNew))
					using (var entryProgress = progressAggregator.CreateProgressSink())
					{
						currentEntryBeingExtracted = entryFullPath;
						progress = entryProgress;
						entry.Extract(tmpFs);
						currentEntryBeingExtracted = null;
						progress = null;
					}

					string preprocessingStep = string.Format("{0} {1}", name, entry.FileName);

					if (!onNext(new PreprocessingStepParams(tmpFileName, entryFullPath,
							Utils.Concat(sourceFile.PreprocessingSteps, preprocessingStep))))
					{
						break;
					}
				}
			}
		}

		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		internal const string name = "unzip";
	};
}
