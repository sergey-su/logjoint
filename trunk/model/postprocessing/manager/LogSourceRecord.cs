using System;
using System.IO;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
	class LogSourceRecord
	{
		private readonly LogMedia.IFileSystem fileSystem;
		public readonly LogSourceMetadata metadata;
		public readonly ILogSource logSource;
		public readonly string logFileName;
		// triggered when log source is closed
		public readonly CancellationTokenSource cancellation;

		public bool logSourceIsAlive;

		public List<PostprocessorOutputRecord> PostprocessorsOutputs = new List<PostprocessorOutputRecord>();

		public LogSourceRecord(ILogSource logSource, LogSourceMetadata metadata, LogMedia.IFileSystem fileSystem)
		{
			this.fileSystem = fileSystem;
			this.logSource = logSource;
			this.metadata = metadata;
			this.logFileName = logSource.Provider.ConnectionParams[ConnectionParamsKeys.PathConnectionParam];
			this.cancellation = new CancellationTokenSource();
		}

		public LogSourcePostprocessorInput ToPostprocessorInput(
			Func<Task<Stream>> openOutputStream, string inputContentsEtag, object customData)
		{
			return new LogSourcePostprocessorInput()
			{
				LogFileName = logFileName,
				openLogFile = () => Task.FromResult(fileSystem.OpenFile(logFileName)),
				LogSource = logSource,
				openOutputFile = openOutputStream,
				CancellationToken = cancellation.Token,
				InputContentsEtag = inputContentsEtag,
				CustomData = customData
			};
		}
	};
}
