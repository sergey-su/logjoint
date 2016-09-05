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
			IPreprocessingStepsFactory preprocessingStepsFactory)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
			this.logProviderFactoryRegistry = logProviderFactoryRegistry;
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			return ExecuteInternal(callback, param);
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			// todo: what to do here?
		}

		async Task<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback, string factoryName)
		{
			await callback.BecomeLongRunning();

			callback.TempFilesCleanupList.Add(sourceFile.Uri);
			callback.SetStepDescription(sourceFile.FullPath + ": fixing timestamp anomalies...");

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
			using (ILogMedia fileMedia = new SimpleFileMedia(
				SimpleFileMedia.CreateConnectionParamsFromFileName(sourceFile.Uri)))
			using (ILogSourceThreads threads = new LogSourceThreads())
			using (var reader = readerFactory.CreateMessagesReader(
				new MediaBasedReaderParams(threads, fileMedia, callback.TempFilesManager)))
			{
				var readerImpl = reader as MediaBasedPositionedMessagesReader; // todo: do not use real classes; have stream endcoding in an interface.
				if (readerImpl == null)
					throw new InvalidDataException("bad reader was made by factory " + factoryName);
				reader.UpdateAvailableBounds(false);
				var range = new FileRange.Range(reader.BeginPosition, reader.EndPosition);
				double rangeLen = range.Length;
				using (var progress = progressAggregator.CreateProgressSink())
				using (var parser = reader.CreateParser(new CreateParserParams(reader.BeginPosition, 
					flags: MessagesParserFlag.DisableDejitter | MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading)))
				using (var writer = new StreamWriter(tmpFileName, false, readerImpl.StreamEncoding))
				{
					var queue = new VCSKicksCollection.PriorityQueue<IMessage>(
						new MessagesComparer(ignoreConnectionIds: true));
					Action dequeue = () => writer.WriteLine(queue.Dequeue().RawText.ToString());
					for (long msgIdx = 0;; ++msgIdx)
					{
						var msg = parser.ReadNext();
						if (msg == null)
							break;
						if ((msgIdx % progressUpdateThreshold) == 0 && rangeLen > 0)
							progress.SetValue((double)(msg.Position - range.Begin) / rangeLen);
						queue.Enqueue(msg);
						if (queue.Count > queueSize)
							dequeue();
					}
					while (queue.Count > 0)
						dequeue();
				}
			}

			return new PreprocessingStepParams(
				tmpFileName, 
				sourceFile.FullPath + " (reordered)",
				Utils.Concat(sourceFile.PreprocessingSteps, string.Format("{0} {1}", name, factoryName))
			);
		}

		readonly PreprocessingStepParams sourceFile;
		readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly ILogProviderFactoryRegistry logProviderFactoryRegistry;
		internal const string name = "reorder";
		const int progressUpdateThreshold = 1024;
		const int queueSize = 1024 * 128;
	};
}
