using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml;
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
		readonly IPositionedMessagesReader reader;
		bool isSavableAs;
		string suggestedSaveAsFileName;
		string taskbarFileName;

		public StreamLogProvider(
			ILogProviderHost host, 
			ILogProviderFactory factory,
			IConnectionParams connectParams,
			StreamBasedFormatInfo formatInfo,
			Type readerType
		):
			base (host, factory, connectParams)
		{
			using (host.Trace.NewFrame)
			{
				host.Trace.Info("readerType={0}", readerType);

				if (connectionParams[ConnectionParamsUtils.RotatedLogFolderPathConnectionParam] != null)
					media = new RollingFilesMedia(
						LogMedia.FileSystemImpl.Instance,
						readerType, 
						formatInfo,
						host.Trace,
						new GenericRollingMediaStrategy(connectionParams[ConnectionParamsUtils.RotatedLogFolderPathConnectionParam]),
						host.TempFilesManager
					);
				else
					media = new SimpleFileMedia(connectParams);

				reader = (IPositionedMessagesReader)Activator.CreateInstance(
					readerType, new MediaBasedReaderParams(this.threads, media, host.TempFilesManager, settingsAccessor: host.GlobalSettings), formatInfo);

				ITimeOffsets initialTimeOffset;
				if (LogJoint.TimeOffsets.TryParse(
					connectionParams[ConnectionParamsUtils.TimeOffsetConnectionParam] ?? "", out initialTimeOffset))
				{
					reader.TimeOffsets = initialTimeOffset;
				}

				StartAsyncReader("Reader thread: " + connectParams.ToString(), reader);

				InitPathDependentMembers(connectParams);
			}
		}

		public override async Task Dispose()
		{
			if (IsDisposed)
				return;
			string tmpFileName = connectionParamsReadonlyView[ConnectionParamsUtils.PathConnectionParam];
			if (tmpFileName != null && !host.TempFilesManager.IsTemporaryFile(tmpFileName))
				tmpFileName = null;
			await base.Dispose();
			if (media != null)
			{
				media.Dispose();
			}
			if (reader != null)
			{
				reader.Dispose();
			}
			if (tmpFileName != null)
			{
				File.Delete(tmpFileName);
			}
		}

		public bool IsSavableAs
		{
			get { return isSavableAs; }
		}

		public string SuggestedFileName
		{
			get { return suggestedSaveAsFileName; }
		}

		public void SaveAs(string fileName)
		{
			CheckDisposed();
			string srcFileName = connectionParamsReadonlyView[ConnectionParamsUtils.PathConnectionParam];
			if (srcFileName == null)
				return;
			System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fileName));
			System.IO.File.Copy(srcFileName, fileName, true);
		}

		void InitPathDependentMembers(IConnectionParams connectParams)
		{
			isSavableAs = false;
			taskbarFileName = null;
			bool isTempFile = false;
			string guessedFileName = null;

			string fname = connectParams[ConnectionParamsUtils.PathConnectionParam];
			if (fname != null)
			{
				isTempFile = host.TempFilesManager.IsTemporaryFile(fname);
				isSavableAs = isTempFile;
			}
			string connectionIdentity = connectParams[ConnectionParamsUtils.IdentityConnectionParam];
			if (connectionIdentity != null)
				guessedFileName = ConnectionParamsUtils.GuessFileNameFromConnectionIdentity(connectionIdentity);
			if (isSavableAs)
			{
				suggestedSaveAsFileName = guessedFileName;
			}
			taskbarFileName = guessedFileName;
		}

		public override string GetTaskbarLogName()
		{
			return taskbarFileName;
		}
	};
}
