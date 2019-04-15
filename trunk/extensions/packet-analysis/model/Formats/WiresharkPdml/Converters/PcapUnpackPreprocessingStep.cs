using LogJoint.Preprocessing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Wireshark.Dpml
{
	public class PcapUnpackPreprocessingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal static readonly string stepName = "pcap.extract";
		readonly Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly PreprocessingStepParams sourceFile;
		readonly ITShark tshark;
		readonly string keyFile;

		internal PcapUnpackPreprocessingStep(Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory, ITShark tshark, PreprocessingStepParams srcFile, string keyFile = null)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.sourceFile = srcFile;
			this.tshark = tshark;
			this.keyFile = keyFile;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await ExecuteInternal(callback, keyFile, p => { callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(p)); });
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			PreprocessingStepParams ret = null;
			await ExecuteInternal(callback, string.IsNullOrWhiteSpace(param) ? null : param, x => { ret = x; });
			return ret;
		}

		async Task ExecuteInternal(IPreprocessingStepCallback callback, string keyFile, Action<PreprocessingStepParams> onNext)
		{
			await callback.BecomeLongRunning();

			callback.TempFilesCleanupList.Add(sourceFile.Uri);
			callback.SetStepDescription("scanning...");

			string tmpFileName = callback.TempFilesManager.GenerateNewName();

			await Converters.PcapToPdmp(sourceFile.Uri, keyFile, tmpFileName, tshark, callback.Cancellation, callback.SetStepDescription, callback.Trace);

			onNext(new PreprocessingStepParams(
				tmpFileName,
				$"{sourceFile.FullPath}\\as_pdml",
				Utils.Concat(sourceFile.PreprocessingSteps, string.Format(keyFile != null ? "{0} {1}" : "{0}", stepName, keyFile)),
				$"{sourceFile.FullPath} (converted to PDML)")
			);
		}
	};
}
