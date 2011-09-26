using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{
	public class FormatDetectionStep: IPreprocessingStep
	{
		public FormatDetectionStep(string fileName): this(new PreprocessingStepParams(fileName))
		{
		}

		internal FormatDetectionStep(PreprocessingStepParams srcFile)
		{
			sourceFile = srcFile;
		}

		public IEnumerable<IPreprocessingStep> Execute(IPreprocessingStepCallback callback)
		{
			if (IsZip(sourceFile, callback))
				yield return new UnpackingStep(sourceFile);
			else
				AutodetectFormatAndYield(sourceFile, callback);
		}

		static bool HasZipExtension(string fileName)
		{
			return Path.GetExtension(fileName).ToLower() == ".zip";
		}

		static bool IsZip(PreprocessingStepParams fileInfo, IPreprocessingStepCallback callback)
		{
			if (HasZipExtension(fileInfo.Uri) || HasZipExtension(fileInfo.DisplayName))
				return true;
			return Ionic.Zip.ZipFile.IsZipFile(fileInfo.Uri, false);
		}

		static void AutodetectFormatAndYield(PreprocessingStepParams file, IPreprocessingStepCallback callback)
		{
			callback.SetStepDescription(string.Format("Detecting format: {0}", file.DisplayName));
			var detectedFotmat = callback.FormatAutodetect.DetectFormat(file.Uri);
			if (detectedFotmat != null)
			{
				Utils.DumpPreprocessingParamsToConnectionParams(file, detectedFotmat.ConnectParams);
				callback.YieldLogProvider(detectedFotmat.Factory, detectedFotmat.ConnectParams, file.DisplayName);
			}
		}

		readonly PreprocessingStepParams sourceFile;
	};
}
