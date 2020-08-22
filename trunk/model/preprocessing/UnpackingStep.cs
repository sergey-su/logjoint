using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;
using System.Threading.Tasks;
using LogJoint.LogMedia;

namespace LogJoint.Preprocessing
{
	public class UnpackingStep : IPreprocessingStep, IUnpackPreprocessingStep
	{
		internal UnpackingStep(
			PreprocessingStepParams srcFile,
			Progress.IProgressAggregator progressAggregator,
			ICredentialsCache credCache,
			IStepsFactory preprocessingStepsFactory,
			IFileSystem fileSystem)
		{
			this.@params = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.progressAggregator = progressAggregator;
			this.credCache = credCache;
			this.fileSystem = fileSystem;
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
					await DoExtract(callback, specificFileToExtract, onNext, password, fileSystem);
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

		private async Task DoExtract(
			IPreprocessingStepCallback callback,
			string specificFileToExtract,
			Func<PreprocessingStepParams, bool> onNext,
			string password,
			IFileSystem fileSystem)
		{
			async Task<ICSharpCode.SharpZipLib.Zip.ZipFile> CreateZipFile()
			{
				if (!IsBrowser.Value)
				{
					return new ICSharpCode.SharpZipLib.Zip.ZipFile(@params.Location);
				}
				else
				{
					var ms = new MemoryStream();
					using (var fileStream = fileSystem.OpenFile(@params.Location))
						await IOUtils.CopyStreamWithProgressAsync(fileStream, ms, _ => { }, callback.Cancellation);
					return new ICSharpCode.SharpZipLib.Zip.ZipFile(ms, leaveOpen: false);
				}
			}
			using (var zipFile = await CreateZipFile())
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
		readonly IFileSystem fileSystem;
		internal const string name = "unzip";
	};
}
