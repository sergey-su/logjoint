using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class UnpackingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal UnpackingStep(
			PreprocessingStepParams srcFile,
			Progress.IProgressAggregator progressAggregator,
			ICredentialsCache credCache,
			IStepsFactory preprocessingStepsFactory)
		{
			this.@params = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
			this.credCache = credCache;
		}

		async Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			PreprocessingStepParams ret = null;
			await ExecuteInternal(callback, x => { ret = x; return false; });
			return ret;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			await ExecuteInternal(callback, p =>
			{
				callback.YieldNextStep(preprocessingStepsFactory.CreateFormatDetectionStep(p));
				return true;
			});
		}

		async Task ExecuteInternal(IPreprocessingStepCallback callback, Func<PreprocessingStepParams, bool> onNext)
		{
			await callback.BecomeLongRunning();

			string specificFileToExtract = @params.Argument;
			callback.TempFilesCleanupList.Add(@params.Location);

			for (string password = null;;)
			{
				try
				{
					DoExtract(callback, specificFileToExtract, onNext, password);
					break;
				}
				catch (PasswordException)
				{
					var uri = new Uri(@params.Location);
					var authMethod = "protected-archive";
					if (password != null)
					{
						credCache.InvalidateCredentialsCache(uri, authMethod);
					}
					var cred = credCache.QueryCredentials(uri, authMethod);
					if (cred == null)
					{
						break;
					}
					password = cred.Password;
				}
			}
		}

		class PasswordException : Exception { };

		private void DoExtract(
			IPreprocessingStepCallback callback,
			string specificFileToExtract,
			Func<PreprocessingStepParams, bool> onNext,
			string password)
		{
			using (var zipFile = new ICSharpCode.SharpZipLib.Zip.ZipFile(@params.Location))
			{
				if (password != null)
					zipFile.Password = password;
				var entriesToEnum = specificFileToExtract != null ?
					Enumerable.Repeat(zipFile.GetEntry(specificFileToExtract), 1) : zipFile.OfType<ICSharpCode.SharpZipLib.Zip.ZipEntry>();
				foreach (var entry in entriesToEnum.Where(e => e != null))
				{
					if (entry.IsDirectory)
						continue;

					if (entry.IsCrypted && password == null)
						throw new PasswordException();

					string entryFullPath = @params.FullPath + "\\" + entry.Name;
					string tmpFileName = callback.TempFilesManager.GenerateNewName();

					callback.SetStepDescription("Unpacking " + entryFullPath);
					using (FileStream tmpFs = new FileStream(tmpFileName, FileMode.CreateNew))
					using (var entryProgress = progressAggregator.CreateProgressSink())
					{
						using (var entryStream = zipFile.GetInputStream(entry))
						{
							var totalLen = entry.Size;
							IOUtils.CopyStreamWithProgress(entryStream, tmpFs, pos =>
							{
								if (totalLen > 0)
								{
									callback.SetStepDescription($"Unpacking {pos * 100 / totalLen}%: {entryFullPath}");
									entryProgress.SetValue((double)pos / totalLen);
								}
							}, callback.Cancellation);
						}
					}

					if (!onNext(new PreprocessingStepParams(tmpFileName, entryFullPath,
							@params.PreprocessingHistory.Add(new PreprocessingHistoryItem(name, entry.Name)))))
					{
						break;
					}
				}
			}
		}

		readonly PreprocessingStepParams @params;
		readonly IStepsFactory preprocessingStepsFactory;
		readonly Progress.IProgressAggregator progressAggregator;
		readonly ICredentialsCache credCache;
		internal const string name = "unzip";
	};
}
