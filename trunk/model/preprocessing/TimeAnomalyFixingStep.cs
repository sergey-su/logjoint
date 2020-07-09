using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class TimeAnomalyFixingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal TimeAnomalyFixingStep(
			PreprocessingStepParams srcFile,
			Progress.IProgressAggregator progressAggregator,
			ILogProviderFactoryRegistry logProviderFactoryRegistry,
			IStepsFactory preprocessingStepsFactory,
			RegularExpressions.IRegexFactory regexFactory,
			LogMedia.IFileSystem fileSystem)
		{
			this.@params = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
			this.regexFactory = regexFactory;
			this.fileSystem = fileSystem;
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			return ExecuteInternal(callback);
		}

		Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			// todo: what to do here?
			return Task.FromResult (0);
		}

		async Task<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback)
		{
			await callback.BecomeLongRunning();

			string factoryName = @params.Argument;

			callback.TempFilesCleanupList.Add(@params.Location);
			Action<double?> setStepDescription = prctComplete =>
			{
				var str = new StringBuilder();
				str.Append(@params.FullPath);
				str.Append(": fixing timestamp anomalies...");
				if (prctComplete != null)
					str.AppendFormat(" {0}%", (int)(prctComplete.Value * 100));
				callback.SetStepDescription(str.ToString());
			};
			setStepDescription(null);

			string tmpFileName = callback.TempFilesManager.GenerateNewName();

			var factoryNameSplit = factoryName.Split('\\');
			if (factoryNameSplit.Length != 2)
				throw new InvalidFormatException();
			var factory = logProviderFactoryRegistry.Find(factoryNameSplit[0], factoryNameSplit[1]);
			if (factory == null)
				throw new InvalidDataException("factory not found: " + factoryName);
			var readerFactory = factory as IMediaBasedReaderFactory;
			if (readerFactory == null)
				throw new InvalidDataException("bad factory: " + factoryName);
			using (ILogMedia fileMedia = await SimpleFileMedia.Create(fileSystem,
				SimpleFileMedia.CreateConnectionParamsFromFileName(@params.Location)))
			using (ILogSourceThreadsInternal threads = new LogSourceThreads())
			using (var reader = readerFactory.CreateMessagesReader(
				new MediaBasedReaderParams(threads, fileMedia)))
			{
				var readerImpl = reader as MediaBasedPositionedMessagesReader; // todo: do not use real classes; have stream encoding in an interface.
				if (readerImpl == null)
					throw new InvalidDataException("bad reader was made by factory " + factoryName);
				await reader.UpdateAvailableBounds(false);
				var range = new FileRange.Range(reader.BeginPosition, reader.EndPosition);
				double rangeLen = range.Length;
				using (var progress = progressAggregator.CreateProgressSink())
				using (var writer = new StreamWriter(tmpFileName, false, readerImpl.StreamEncoding))
				await DisposableAsync.Using(await reader.CreateParser(new CreateParserParams(reader.BeginPosition,
					flags: MessagesParserFlag.DisableDejitter | MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading)), async parser =>
				{
					var queue = new VCSKicksCollection.PriorityQueue<IMessage>(
						new MessagesComparer(ignoreConnectionIds: true));
					Action dequeue = () => writer.WriteLine(queue.Dequeue().RawText.ToString());
					double lastPrctComplete = 0;
					var cancellation = callback.Cancellation;
					for (long msgIdx = 0;; ++msgIdx)
					{
						if (cancellation.IsCancellationRequested)
							break;
						var msg = await parser.ReadNext();
						if (msg == null)
							break;
						if ((msgIdx % progressUpdateThreshold) == 0 && rangeLen > 0)
						{
							var prctComplete = (double)(msg.Position - range.Begin) / rangeLen;
							progress.SetValue(prctComplete);
							if (prctComplete - lastPrctComplete > 0.05)
							{
								setStepDescription(prctComplete);
								lastPrctComplete = prctComplete;
							}
						}
						queue.Enqueue(msg);
						if (queue.Count > queueSize)
							dequeue();
					}
					while (queue.Count > 0)
						dequeue();
				});
			}

			return new PreprocessingStepParams(
				tmpFileName, 
				@params.FullPath + " (reordered)",
				@params.PreprocessingHistory.Add(new PreprocessingHistoryItem(name, factoryName))
			);
		}

		readonly PreprocessingStepParams @params;
		readonly IStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
		readonly RegularExpressions.IRegexFactory regexFactory;
		readonly LogMedia.IFileSystem fileSystem;
		internal const string name = "reorder";
		const int progressUpdateThreshold = 1024;
		const int queueSize = 1024 * 128;
	};
}
