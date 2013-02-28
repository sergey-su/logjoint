using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{
	public class UnpackingStep : IPreprocessingStep
	{
		internal UnpackingStep(PreprocessingStepParams srcFile)
		{
			sourceFile = srcFile;
		}

		internal PreprocessingStepParams ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			return ExecuteInternal(callback, param).FirstOrDefault();
		}

		public IEnumerable<IPreprocessingStep> Execute(IPreprocessingStepCallback callback)
		{
			return ExecuteInternal(callback, null).Select(p => new FormatDetectionStep(p));
			//var tmpParamsArray = ExecuteInternal(callback, null).ToArray();
			//if (tmpParamsArray.Length > 1)
			//{
			//    var userSelection = callback.UserRequests.SelectFilesToProcess(tmpParamsArray.Select(initParams => initParams.DisplayName).ToArray());
			//    return tmpParamsArray.Zip(Enumerable.Range(0, tmpParamsArray.Length),
			//        (initParams, i) => userSelection[i] ? new FormatDetectionStep(initParams) : null).Where(initParams => initParams != null);
			//}
			//else
			//{
			//    return tmpParamsArray.Select(initParams => new FormatDetectionStep(initParams));
			//}
		}

		IEnumerable<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback, string specificFileToExtract)
		{
			callback.BecomeLongRunning();

			using (var zipFile = new Ionic.Zip.ZipFile(sourceFile.Uri))
			{
				string currentEntryBeingExcracted = null;
				zipFile.ExtractProgress += (s, evt) =>
				{
					evt.Cancel = callback.IsCancellationRequested;
					if (currentEntryBeingExcracted != null && evt.TotalBytesToTransfer != 0)
						callback.SetStepDescription(string.Format("Unpacking {1}%: {0}",
							currentEntryBeingExcracted,
							evt.BytesTransferred * (long)100 / evt.TotalBytesToTransfer));
				};
				var entriesToEnum = specificFileToExtract != null ?
					Enumerable.Repeat(zipFile[specificFileToExtract], 1) : zipFile.Entries;
				foreach (var entry in entriesToEnum)
				{
					if (entry.IsDirectory)
						continue;

					string entryFullPath = sourceFile.FullPath + "\\" + entry.FileName;
					string tmpFileName = callback.TempFilesManager.GenerateNewName();

					callback.SetStepDescription("Unpacking " + entryFullPath);
					using (FileStream tmpFs = new FileStream(tmpFileName, FileMode.CreateNew))
					{
						currentEntryBeingExcracted = entryFullPath;
						entry.Extract(tmpFs);
						currentEntryBeingExcracted = null;
					}

					string preprocessingStep = string.Format("unzip {0}", entry.FileName);

					yield return 
						new PreprocessingStepParams(tmpFileName, entryFullPath,
							Utils.Concat(sourceFile.PreprocessingSteps, preprocessingStep));
				}
			}
		}

		readonly PreprocessingStepParams sourceFile;
	};

}
