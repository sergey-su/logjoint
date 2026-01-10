using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System;

namespace LogJoint.Preprocessing
{
    public class GunzippingStep : IPreprocessingStep, IUnpackPreprocessingStep
    {
        internal GunzippingStep(
            PreprocessingStepParams srcFile,
            Progress.IProgressAggregator progressAggregator,
            IStepsFactory preprocessingStepsFactory)
        {
            this.sourceFile = srcFile;
            this.preprocessingStepsFactory = preprocessingStepsFactory;
            this.progressAggregator = progressAggregator;
        }

        async Task<PreprocessingStepParams?> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
        {
            return await ExecuteInternal(callback);
        }

        async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
        {
            callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(await ExecuteInternal(callback)));
        }

        async Task<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback)
        {
            await callback.BecomeLongRunning();

            callback.TempFilesCleanupList.Add(sourceFile.Location);

            string tmpFileName = callback.TempFilesManager.GenerateNewName();

            var sourceFileInfo = new FileInfo(sourceFile.Location);

            using (var inFileStream = sourceFileInfo.OpenRead())
            using (var outFileStream = new FileStream(tmpFileName, FileMode.CreateNew))
            using (var progress = sourceFileInfo.Length != 0 ? progressAggregator.CreateProgressSink() : (Progress.IProgressEventsSink)null)
            {
                using (var gzipStream = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(inFileStream))
                {
                    IOUtils.CopyStreamWithProgress(gzipStream, outFileStream, bytes =>
                    {
                        callback.SetStepDescription(string.Format("{1} {0}: Gunzipping...",
                                IOUtils.FileSizeToString(bytes), sourceFile.FullPath));
                        if (progress != null)
                            progress.SetValue((double)inFileStream.Position / (double)sourceFileInfo.Length);
                    }, callback.Cancellation);

                    return
                        new PreprocessingStepParams(tmpFileName, sourceFile.FullPath,
                            sourceFile.PreprocessingHistory.Add(new PreprocessingHistoryItem(name)));
                }
            }
        }

        readonly PreprocessingStepParams sourceFile;
        readonly IStepsFactory preprocessingStepsFactory;
        readonly Progress.IProgressAggregator progressAggregator;
        internal const string name = "gunzip";
    };
}
