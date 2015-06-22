using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace LogJoint.Preprocessing
{
	public class FormatDetectionStep: IPreprocessingStep
	{
		internal FormatDetectionStep(PreprocessingStepParams srcFile, IPreprocessingManagerExtensionsRegistry extentions, IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.extentions = extentions;
		}

		IEnumerable<IPreprocessingStep> IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			var detectedFormatStep = extentions.Items.Select(d => d.DetectFormat(sourceFile)).FirstOrDefault();
			if (detectedFormatStep != null)
				yield return detectedFormatStep;
			else if (IsZip(sourceFile, callback))
				yield return preprocessingStepsFactory.CreateUnpackingStep(sourceFile);
			else
				AutodetectFormatAndYield(sourceFile, callback);
		}

		PreprocessingStepParams IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			throw new NotImplementedException();
		}

		static bool HasZipExtension(string fileName)
		{
			return Path.GetExtension(fileName).ToLower() == ".zip";
		}

		static bool IsZip(PreprocessingStepParams fileInfo, IPreprocessingStepCallback callback)
		{
			if (HasZipExtension(fileInfo.Uri) || HasZipExtension(fileInfo.FullPath))
				return true;
			return Ionic.Zip.ZipFile.IsZipFile(fileInfo.Uri, false);
		}

		static void AutodetectFormatAndYield(PreprocessingStepParams file, IPreprocessingStepCallback callback)
		{
			callback.SetStepDescription(string.Format("Detecting format: {0}", file.FullPath));
			var progressHandler = new ProgressHandler() { callback = callback };
			var detectedFormat = callback.FormatAutodetect.DetectFormat(file.Uri, progressHandler.cancellation.Token, progressHandler);
			if (detectedFormat != null)
			{
				Utils.DumpPreprocessingParamsToConnectionParams(file, detectedFormat.ConnectParams);
				callback.YieldLogProvider(detectedFormat.Factory, detectedFormat.ConnectParams, file.FullPath);
			}
		}

		class ProgressHandler : IFormatAutodetectionProgress
		{
			public int formatsTriedSoFar = 0;
			public CancellationTokenSource cancellation = new CancellationTokenSource();
			public IPreprocessingStepCallback callback;

			void IFormatAutodetectionProgress.Trying(ILogProviderFactory factory)
			{
				formatsTriedSoFar++;
				if (formatsTriedSoFar == 2)
					callback.BecomeLongRunning();
				if (callback.IsCancellationRequested)
					cancellation.Cancel();
			}
		};


		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly IPreprocessingManagerExtensionsRegistry extentions;
	};
}
