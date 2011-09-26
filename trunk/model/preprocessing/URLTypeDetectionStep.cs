using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{
	public class URLTypeDetectionStep : IPreprocessingStep
	{
		public URLTypeDetectionStep(string url)
			: this(new PreprocessingStepParams(url))
		{
		}

		internal URLTypeDetectionStep(PreprocessingStepParams srcFile)
		{
			sourceFile = srcFile;
		}

		public IEnumerable<IPreprocessingStep> Execute(IPreprocessingStepCallback callback)
		{
			// todo: check file URLs


			yield return new DownloadingStep(sourceFile);
		}

		readonly PreprocessingStepParams sourceFile;
	};
}
