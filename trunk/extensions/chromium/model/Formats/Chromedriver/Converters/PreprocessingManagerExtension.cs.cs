using LogJoint.Preprocessing;
using System.Linq;
using System;
using LogJoint.Postprocessing;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Chromium.ChromeDriver
{
	public class PreprocessingManagerExtension : IPreprocessingManagerExtension
	{
		readonly IStepsFactory preprocessingStepsFactory;
		readonly ILogProviderFactory chromeDriverLogsFactory;
		readonly ITextLogParser textLogParser;

		public PreprocessingManagerExtension(
			IStepsFactory preprocessingStepsFactory,
			ILogProviderFactory chromeDriverLogsFactory,
			ITextLogParser textLogParser)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.chromeDriverLogsFactory = chromeDriverLogsFactory;
			this.textLogParser = textLogParser;
		}

		IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (IsChromeDriverFormat(header))
				return new TimeFixerPreprocessingStep(preprocessingStepsFactory, chromeDriverLogsFactory, fileInfo, textLogParser);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
		{
			if (stepName == TimeFixerPreprocessingStep.stepName)
				return new TimeFixerPreprocessingStep(preprocessingStepsFactory, chromeDriverLogsFactory, stepParams, textLogParser);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
		{
			return null;
		}

		Task IPreprocessingManagerExtension.FinalizePreprocessing(IPreprocessingStepCallback callback)
		{
			return Task.FromResult(0);
		}

		bool IsChromeDriverFormat(IStreamHeader header)
		{
			if (header.Header.Length == 0)
				return false;
			if ((char)header.Header[0] != '[')
				return false;
			var headerAsStr = new string(header.Header.Select(b => (char)b).ToArray());
			return (new Reader(textLogParser, CancellationToken.None)).TestFormat(headerAsStr);
		}
	};
}
