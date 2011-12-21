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
			if (HasZipExtension(fileInfo.Uri) || HasZipExtension(fileInfo.FullPath))
				return true;
			return Ionic.Zip.ZipFile.IsZipFile(fileInfo.Uri, false);
		}

		static void AutodetectFormatAndYield(PreprocessingStepParams file, IPreprocessingStepCallback callback)
		{
			callback.SetStepDescription(string.Format("Detecting format: {0}", file.FullPath));
			var detectedFormat = callback.FormatAutodetect.DetectFormat(file.Uri);
			if (detectedFormat != null)
			{
				Utils.DumpPreprocessingParamsToConnectionParams(file, detectedFormat.ConnectParams);
				callback.YieldLogProvider(detectedFormat.Factory, detectedFormat.ConnectParams, file.FullPath);
			}
		}

		readonly PreprocessingStepParams sourceFile;
	};
}
