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
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			PreprocessingStepParams ret = null;
			await ExecuteInternal(callback, param, x => { ret = x; });
			return ret;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await ExecuteInternal(callback, null,
				r => callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(r)));
		}

		async Task ExecuteInternal(
			IPreprocessingStepCallback callback,
			string filter,
			Action<PreprocessingStepParams> yieldOutput)
		{
			await callback.BecomeLongRunning();

			callback.TempFilesCleanupList.Add(sourceFile.Uri);

			string tmpDirectory = callback.TempFilesManager.GenerateNewName();

			var sourceFileInfo = new FileInfo(sourceFile.Uri);

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
								$"{sourceFile.FullPath}{Path.DirectorySeparatorChar}{fileNameInArchive}",
								sourceFile.PreprocessingSteps.Concat(new[] { $"{name} {fileNameInArchive}" })));
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

		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		internal const string name = "untar";
	};
}
