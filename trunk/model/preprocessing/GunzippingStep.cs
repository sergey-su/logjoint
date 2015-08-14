using System.Collections.Generic;
using System.IO;
using System.Linq;
using Ionic.Zlib;

namespace LogJoint.Preprocessing
{
    public class GunzippingStep : IPreprocessingStep
    {
        internal GunzippingStep(
            PreprocessingStepParams srcFile,
            Progress.IProgressAggregator progressAggregator,
            IPreprocessingStepsFactory preprocessingStepsFactory)
        {
            this.sourceFile = srcFile;
            this.preprocessingStepsFactory = preprocessingStepsFactory;
            this.progressAggregator = progressAggregator;
        }

        PreprocessingStepParams IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback, string param)
        {
            return ExecuteInternal(callback, param).FirstOrDefault();
        }

        IEnumerable<IPreprocessingStep> IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
        {
            return ExecuteInternal(callback, null).Select(p => preprocessingStepsFactory.CreateFormatDetectionStep(p));
        }

        IEnumerable<PreprocessingStepParams> ExecuteInternal(IPreprocessingStepCallback callback, string specificFileToExtract)
        {
            callback.BecomeLongRunning();

            string tmpFileName = callback.TempFilesManager.GenerateNewName();

            var sourceFileInfo = new FileInfo(sourceFile.Uri);

            using (var inFileStream = sourceFileInfo.OpenRead())
            using (var outFileStream = new FileStream(tmpFileName, FileMode.CreateNew))
            using (var progress = sourceFileInfo.Length != 0 ? progressAggregator.CreateProgressSink() : (Progress.IProgressEventsSink)null)
            {
                using (var gzipStream = new GZipStream(inFileStream, CompressionMode.Decompress, true))
                {
                    DownloadingStep.CopyStreamWithProgress(gzipStream, outFileStream, downloadedBytes =>
                    {
                        callback.SetStepDescription(string.Format("{1} {0}: Gunzipping...",
                                DownloadingStep.FileSizeToString(downloadedBytes), sourceFile.FullPath));
                        if (progress != null)
                            progress.SetValue((double)downloadedBytes / (double)sourceFileInfo.Length);
                    });

                    string preprocessingStep = string.Format("{0} {1}", name, sourceFile.FullPath);

                    var entryFullPath = Path.ChangeExtension(sourceFile.FullPath, null);

                    yield return
                        new PreprocessingStepParams(tmpFileName, entryFullPath,
                            Utils.Concat(sourceFile.PreprocessingSteps, preprocessingStep));
                }
            }
        }

        readonly PreprocessingStepParams sourceFile;
        readonly IPreprocessingStepsFactory preprocessingStepsFactory;
        readonly Progress.IProgressAggregator progressAggregator;
        internal const string name = "gunzip";
    };
}
