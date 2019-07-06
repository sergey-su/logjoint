using LogJoint.Preprocessing;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wireshark.Dpml
{
	public class PcapUnpackPreprocessingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal static readonly string stepName = "pcap.extract";
		readonly Preprocessing.IStepsFactory preprocessingStepsFactory;
		readonly PreprocessingStepParams sourceFile;
		readonly ITShark tshark;
		readonly Func<IPreprocessingStepCallback, Task<PreprocessingStepParams[]>> getKeyFiles;

		internal PcapUnpackPreprocessingStep(
			Preprocessing.IStepsFactory preprocessingStepsFactory,
			ITShark tshark,
			PreprocessingStepParams srcFile,
			PreprocessingStepParams[] keyFiles)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.sourceFile = srcFile;
			this.tshark = tshark;
			if (keyFiles != null)
				this.getKeyFiles = (IPreprocessingStepCallback _) => Task.FromResult(keyFiles);
			else if (srcFile?.Argument != null)
				this.getKeyFiles = (IPreprocessingStepCallback callback) =>
					Task.WhenAll(StepArgument.Parse(srcFile.Argument).Select(history => callback.ReplayHistory(history)));
			else
				this.getKeyFiles = (IPreprocessingStepCallback _) => Task.FromResult(new PreprocessingStepParams[0]);
		}

		private static class StepArgument
		{
			public static string ToString(PreprocessingStepParams[] keyFiles)
			{
				string str = new JArray(keyFiles.Select(
					keyFile => new JArray(keyFile.PreprocessingHistory.Select(item => item.ToString()).ToArray())
				).ToArray()).ToString(Formatting.None);
				return str;
			}

			public static IEnumerable<ImmutableArray<PreprocessingHistoryItem>> Parse(string value)
			{
				JArray jarray;
				try
				{
					jarray = JArray.Parse(value);
				}
				catch (JsonException)
				{
					yield break;
				}
				foreach (var keyHistory in jarray.OfType<JArray>())
				{
					yield return ImmutableArray.CreateRange(
						keyHistory
							.OfType<JValue>()
							.Where(v => v.Type == JTokenType.String)
							.Select(str => PreprocessingHistoryItem.TryParse(str.ToString(), out var historyItem) ? historyItem : null)
							.Where(historyItem => historyItem != null)
					);
				}
		}
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(await ExecuteInternal(callback, await getKeyFiles(callback))));
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			return await ExecuteInternal(callback, await getKeyFiles(callback));
		}


		async Task<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback, PreprocessingStepParams[] keyFiles)
		{
			await callback.BecomeLongRunning();

			callback.TempFilesCleanupList.Add(sourceFile.Location);
			callback.SetStepDescription("scanning...");

			string tmpFileName = callback.TempFilesManager.GenerateNewName();

			await Converters.PcapToPdmp(sourceFile.Location, keyFiles.Select(f => f.Location).ToArray(),
				tmpFileName, tshark, callback.Cancellation, callback.SetStepDescription, callback.Trace);

			return new PreprocessingStepParams(
				tmpFileName,
				$"{sourceFile.FullPath}\\as_pdml",
				sourceFile.PreprocessingHistory.Add(new PreprocessingHistoryItem(stepName, StepArgument.ToString(keyFiles))),
				$"{sourceFile.FullPath} (converted to PDML)"
			);
		}
	};
}
