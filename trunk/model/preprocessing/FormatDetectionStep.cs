using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace LogJoint.Preprocessing
{
	public class FormatDetectionStep: IPreprocessingStep
	{
		internal FormatDetectionStep(PreprocessingStepParams srcFile, IExtensionsRegistry extentions, IStepsFactory preprocessingStepsFactory)
		{
			this.sourceFile = srcFile;
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.extentions = extentions;
		}

		async Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
		{
			var header = new StreamHeader(sourceFile.Location);
			var detectedFormatStep = extentions.Items.Select(d => d.DetectFormat(sourceFile, header)).FirstOrDefault(x => x != null);
			if (detectedFormatStep != null)
				callback.YieldNextStep(detectedFormatStep);
			else if (IsZip(sourceFile, header))
				callback.YieldNextStep(preprocessingStepsFactory.CreateUnpackingStep(sourceFile));
			else if (IsGzip(sourceFile, header))
				callback.YieldNextStep(preprocessingStepsFactory.CreateGunzippingStep(sourceFile));
			else if (IsTar(sourceFile, header))
				callback.YieldNextStep(preprocessingStepsFactory.CreateUntarStep(sourceFile));
			else
				await AutodetectFormatAndYield(sourceFile, callback);
		}

		Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
		{
			throw new NotImplementedException();
		}

		static bool HasZipExtension(string fileName)
		{
			return Path.GetExtension(fileName).ToLower() == ".zip";
		}

		static bool IsZip(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (HasZipExtension(fileInfo.Location) || HasZipExtension(fileInfo.FullPath))
				if (header.Header.Take(4).SequenceEqual(new byte[] { 0x50, 0x4b, 0x03, 0x04 }))
					return true;
			return false;
		}

		private static bool HasGzExtension(string fileName)
		{
			return Path.GetExtension(fileName).ToLower() == ".gz";
		}

		static bool IsGzip(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (HasGzExtension(fileInfo.Location) || HasGzExtension(fileInfo.FullPath))
				if (header.Header.Take(2).SequenceEqual(new byte[] { 0x1f, 0x8b }))
					return IsGzipFile(fileInfo.Location);
			return false;
		}

		static bool IsGzipFile(string filePath)
		{
			using (var fstm  = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 64))
			using (var stm = new ICSharpCode.SharpZipLib.GZip.GZipInputStream(fstm))
			{
				try
				{
					stm.Read(new byte[0], 0, 0);
					return true;
				}
				catch (ICSharpCode.SharpZipLib.GZip.GZipException)
				{
					return false;
				}
			}
		}

		static bool IsTar(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			var tarHeader = new ICSharpCode.SharpZipLib.Tar.TarHeader
			{
				Magic = "xxxxx"
			};
			try
			{
				tarHeader.ParseBuffer(header.Header);
			}
			catch (Exception)
			{
				return false;
			}
			return tarHeader.IsChecksumValid
				|| tarHeader.Magic?.Contains(ICSharpCode.SharpZipLib.Tar.TarHeader.TMAGIC) == true;
		}

		static async Task AutodetectFormatAndYield(PreprocessingStepParams file, IPreprocessingStepCallback callback)
		{
			callback.SetStepDescription(string.Format("Detecting format: {0}", file.FullPath));
			var progressHandler = new ProgressHandler() { callback = callback };
			var detectedFormat = await callback.FormatAutodetect.DetectFormat(file.Location, file.FullPath, progressHandler.cancellation.Token, progressHandler);
			if (detectedFormat != null)
			{
				file.DumpToConnectionParams(detectedFormat.ConnectParams);
				callback.YieldLogProvider(new YieldedProvider()
				{
					Factory = detectedFormat.Factory,
					ConnectionParams = detectedFormat.ConnectParams,
					DisplayName = file.FullPath,
					IsHiddenLog = false
				});
			}
		}

		class ProgressHandler : IFormatAutodetectionProgress
		{
			public int formatsTriedSoFar = 0;
			public CancellationTokenSource cancellation = new CancellationTokenSource();
			public IPreprocessingStepCallback callback;

			void IFormatAutodetectionProgress.Trying(ILogProviderFactory factory)
			{
				formatsTriedSoFar++;
				if (formatsTriedSoFar == 2)
					callback.BecomeLongRunning();
				if (callback.Cancellation.IsCancellationRequested)
					cancellation.Cancel();
			}
		};

		class StreamHeader : IStreamHeader
		{
			string fileName;
			byte[] header;

			public StreamHeader(string fileName)
			{
				this.fileName = fileName;
			}

			byte[] IStreamHeader.Header
			{
				get
				{
					if (header == null)
					{
						var tmp = new byte[1024];
						using (var fstm = new FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.ReadWrite, 256))
						{
							int read = fstm.Read(tmp, 0, tmp.Length);
							if (read < tmp.Length)
								tmp = tmp.Take(read).ToArray();
						}
						header = tmp;
					}
					return header;
				}
			}
		};

		readonly PreprocessingStepParams sourceFile;
		readonly IStepsFactory preprocessingStepsFactory;
		readonly IExtensionsRegistry extentions;
	};
}
