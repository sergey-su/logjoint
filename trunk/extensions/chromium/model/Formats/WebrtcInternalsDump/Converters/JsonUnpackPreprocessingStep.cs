using LogJoint.Preprocessing;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Chromium.WebrtcInternalsDump
{
	public class JsonUnpackPreprocessingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal static readonly string stepName = "webrtc_internals_dump.extract";
		readonly Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory;
		readonly PreprocessingStepParams sourceFile;

		internal JsonUnpackPreprocessingStep(Preprocessing.IPreprocessingStepsFactory preprocessingStepsFactory, PreprocessingStepParams srcFile)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.sourceFile = srcFile;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await ExecuteInternal(callback, p => { callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(p)); });
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
		{
			PreprocessingStepParams ret = null;
			await ExecuteInternal(callback, x => { ret = x; });
			return ret;
		}

		async Task ExecuteInternal(IPreprocessingStepCallback callback, Action<PreprocessingStepParams> onNext)
		{
			await callback.BecomeLongRunning();

			callback.TempFilesCleanupList.Add(sourceFile.Uri);

			string tmpFileName = callback.TempFilesManager.GenerateNewName();

			await Converters.JsonToLog(sourceFile.Uri, tmpFileName);

			onNext(new PreprocessingStepParams(tmpFileName, string.Format("{0}\\converted_to_log", sourceFile.FullPath),
					Utils.Concat(sourceFile.PreprocessingSteps, stepName)));
		}
	};
}
