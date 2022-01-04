using System;
using System.Linq;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint
{
	public class StreamBasedFormatInfo
	{
		public readonly MessagesReaderExtensions.XmlInitializationParams ExtensionsInitData;

		public StreamBasedFormatInfo(MessagesReaderExtensions.XmlInitializationParams extensionsInitData)
		{
			this.ExtensionsInitData = extensionsInitData;
		}
	};

	public class StreamLogProvider : AsyncLogProvider, ISaveAs
	{
		ILogMedia media;
		IPositionedMessagesReader reader;
		readonly ITempFilesManager tempFilesManager;
		bool isSavableAs;
		string suggestedSaveAsFileName;
		string taskbarName;

		public StreamLogProvider(
			ILogProviderHost host,
			ILogProviderFactory factory,
			IConnectionParams connectParams,
			Func<MediaBasedReaderParams, IPositionedMessagesReader> readerCreator,
			ITempFilesManager tempFilesManager,
			ITraceSourceFactory traceSourceFactory,
			ISynchronizationContext modelSynchronizationContext,
			Settings.IGlobalSettingsAccessor globalSettings,
			LogMedia.IFileSystem fileSystem
		) :
			base (host, factory, connectParams, traceSourceFactory, modelSynchronizationContext, globalSettings)
		{
			this.tempFilesManager = tempFilesManager;
			StartAsyncReader(async () => {
				if (connectionParams[ConnectionParamsKeys.RotatedLogFolderPathConnectionParam] != null)
				{
					media = new RollingFilesMedia(
						fileSystem,
						readerCreator,
						tracer,
						new GenericRollingMediaStrategy(
							connectionParams[ConnectionParamsKeys.RotatedLogFolderPathConnectionParam],
							ConnectionParamsUtils.GetRotatedLogPatterns(connectParams)
						)
					);
				}
				else
				{
					media = await SimpleFileMedia.Create(fileSystem, connectParams);
				}

				reader = readerCreator(new MediaBasedReaderParams(this.threads, media,
						parentLoggingPrefix: tracer.Prefix));

				if (TimeOffsets.TryParse(
					connectionParams[ConnectionParamsKeys.TimeOffsetConnectionParam] ?? "", out ITimeOffsets initialTimeOffset))
				{
					reader.TimeOffsets = initialTimeOffset;
				}

				return reader;
			});

			InitPathDependentMembers(connectParams);
		}

		public override async Task Dispose()
		{
			if (IsDisposed)
				return;
			string tmpFileName = connectionParamsReadonlyView[ConnectionParamsKeys.PathConnectionParam];
			if (tmpFileName != null && !tempFilesManager.IsTemporaryFile(tmpFileName))
				tmpFileName = null;
			await base.Dispose();
			media?.Dispose();
			reader?.Dispose();
			if (tmpFileName != null)
			{
				File.Delete(tmpFileName);
			}
		}

		bool ISaveAs.IsSavableAs
		{
			get { return isSavableAs; }
		}

		string ISaveAs.SuggestedFileName
		{
			get { return suggestedSaveAsFileName; }
		}

		async Task ISaveAs.SaveAs(Stream outStream)
		{
			CheckDisposed();
			string srcFileName = connectionParamsReadonlyView[ConnectionParamsKeys.PathConnectionParam];
			if (srcFileName == null)
				return;
			using var inStream = new FileStream(srcFileName, FileMode.Open);
			await IOUtils.CopyStreamWithProgressAsync(inStream, outStream, _ => {}, 
				System.Threading.CancellationToken.None);
		}

		void InitPathDependentMembers(IConnectionParams connectParams)
		{
			isSavableAs = false;
			taskbarName = null;
			string guessedFileName = null;

			string fname = connectParams[ConnectionParamsKeys.PathConnectionParam];
			if (fname != null)
			{
				bool isTempFile = tempFilesManager.IsTemporaryFile(fname);
				isSavableAs = isTempFile;
			}
			string connectionIdentity = connectParams[ConnectionParamsKeys.IdentityConnectionParam];
			if (connectionIdentity != null)
				guessedFileName = ConnectionParamsUtils.GuessFileNameFromConnectionIdentity(connectionIdentity);
			string displayName = connectParams[ConnectionParamsKeys.DisplayNameConnectionParam];
			if (isSavableAs)
			{
				if (!string.IsNullOrEmpty(displayName))
					suggestedSaveAsFileName = SanitizeSuggestedFileName(displayName);
				else
					suggestedSaveAsFileName = SanitizeSuggestedFileName(guessedFileName);
			}
			taskbarName = !string.IsNullOrEmpty(displayName) ? displayName : guessedFileName;
		}

		public override string GetTaskbarLogName()
		{
			return taskbarName;
		}

		static string SanitizeSuggestedFileName(string str)
		{
			var invalidChars = Path.GetInvalidFileNameChars().ToHashSet();
			return new string(str.Select(c => invalidChars.Contains(c) ? '_' : c).Take(250).ToArray());
		}
	};
}
