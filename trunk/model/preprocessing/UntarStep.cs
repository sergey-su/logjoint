using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;
using ICSharpCode.SharpZipLib.Tar;

namespace LogJoint.Preprocessing
{
	public class UntarStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal UntarStep(
			PreprocessingStepParams srcFile,
			Progress.IProgressAggregator progressAggregator,
			IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.@params = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			PreprocessingStepParams ret = null;
			await ExecuteInternal(callback, x => { ret = x; });
			return ret;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await ExecuteInternal(callback,
				r => callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(r)));
		}

		async Task ExecuteInternal(
			IPreprocessingStepCallback callback,
			Action<PreprocessingStepParams> yieldOutput)
		{
			await callback.BecomeLongRunning();

			string filter = @params.Argument;
			callback.TempFilesCleanupList.Add(@params.Location);

			string tmpDirectory = callback.TempFilesManager.GenerateNewName();

			var sourceFileInfo = new FileInfo(@params.Location);

			using (var inFileStream = sourceFileInfo.OpenRead())
			// using (var progress = sourceFileInfo.Length != 0 ? progressAggregator.CreateProgressSink() : (Progress.IProgressEventsSink)null)
			{
				using (var tarArchive = TarArchive.CreateInputTarArchive(inFileStream))
				{
					tarArchive.ExtractContents(tmpDirectory);
				}

				void traverseFolder(string relativePath)
				{
					var dirInfo = new DirectoryInfo(Path.Combine(tmpDirectory, relativePath));
					foreach (var f in dirInfo.EnumerateFiles())
					{
						var fileNameInArchive =
							relativePath != "" ? $"{relativePath}{Path.DirectorySeparatorChar}{f.Name}" : f.Name;
						if (filter == null || filter == fileNameInArchive)
						{
							yieldOutput(new PreprocessingStepParams(f.FullName,
								$"{@params.FullPath}{Path.DirectorySeparatorChar}{fileNameInArchive}",
								@params.PreprocessingHistory.Add(new PreprocessingHistoryItem(name, fileNameInArchive))));
						}
					}
					foreach (var f in dirInfo.EnumerateDirectories())
					{
						traverseFolder(Path.Combine(relativePath, f.Name));
					}
				}

				traverseFolder("");
			}
		}

		readonly PreprocessingStepParams @params;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		internal const string name = "untar";
	};
}
