using System.Collections.Generic;
using System.Threading;

namespace LogJoint.Postprocessing
{
	class LogSourceRecord
	{
		public readonly LogSourceMetadata metadata;
		public readonly ILogSource logSource;
		public readonly string logFileName;
		// triggered when log source is closed
		public readonly CancellationTokenSource cancellation;

		public bool logSourceIsAlive;

		public List<PostprocessorOutputRecord> PostprocessorsOutputs = new List<PostprocessorOutputRecord>();

		public LogSourceRecord(ILogSource logSource, LogSourceMetadata metadata)
		{
			this.logSource = logSource;
			this.metadata = metadata;
			this.logFileName = logSource.Provider.ConnectionParams[ConnectionParamsUtils.PathConnectionParam];
			this.cancellation = new CancellationTokenSource();
		}

		public LogSourcePostprocessorInput ToPostprocessorInput(
			string outputFileName, string inputContentsEtag, object customData)
		{
			return new LogSourcePostprocessorInput()
			{
				LogFileName = logFileName,
				LogSource = logSource,
				OutputFileName = outputFileName,
				CancellationToken = cancellation.Token,
				InputContentsEtag = inputContentsEtag,
				CustomData = customData
			};
		}
	};
}
