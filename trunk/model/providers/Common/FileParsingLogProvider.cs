using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml;

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

	public class StreamLogProvider : RangeManagingProvider, ISaveAs, IEnumAllMessages, IOpenContainingFolder
	{
		ILogMedia media;
		readonly IPositionedMessagesReader reader;
		bool isSavableAs;
		string suggestedSaveAsFileName;
		string containingFolderPath;
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
						new GenericRollingMediaStrategy(connectionParams[ConnectionParamsUtils.RotatedLogFolderPathConnectionParam])
					);
				else
					media = new SimpleFileMedia(connectParams);

				reader = (IPositionedMessagesReader)Activator.CreateInstance(
					readerType, new MediaBasedReaderParams(this.threads, media, settingsAccessor: host.GlobalSettings), formatInfo);

				ITimeOffsets initialTimeOffset;
				if (LogJoint.TimeOffsets.TryParse(
					connectionParams[ConnectionParamsUtils.TimeOffsetConnectionParam] ?? "", out initialTimeOffset))
				{
					reader.TimeOffsets = initialTimeOffset;
				}

				StartAsyncReader("Reader thread: " + connectParams.ToString());

				InitPathDependentMembers(connectParams);
			}
		}

		public override void Dispose()
		{
			base.Dispose();
			if (media != null)
			{
				media.Dispose();
			}
			if (reader != null)
			{
				reader.Dispose();
			}
		}

		protected override Algorithm CreateAlgorithm()
		{
			return new RangeManagingAlgorithm(this, reader);
		}

		protected override IPositionedMessagesReader GetReader()
		{
			return reader;
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
			string srcFileName = ConnectionParams[ConnectionParamsUtils.PathConnectionParam];
			if (srcFileName == null)
				return;
			System.IO.Directory.CreateDirectory(Path.GetDirectoryName(fileName));
			System.IO.File.Copy(srcFileName, fileName, true);
		}

		void InitPathDependentMembers(IConnectionParams connectParams)
		{
			isSavableAs = false;
			containingFolderPath = null;
			taskbarFileName = null;
			bool isTempFile = false;
			string guessedFileName = null;

			string fname = connectParams[ConnectionParamsUtils.PathConnectionParam];
			if (fname != null)
			{
				isTempFile = TempFilesManager.GetInstance().IsTemporaryFile(fname);
				isSavableAs = isTempFile;
				if (!isTempFile)
				{
					containingFolderPath = fname;
				}
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

		public IEnumerable<PostprocessedMessage> LockProviderAndEnumAllMessages(Func<IMessage, object> postprocessor)
		{
			LockMessages();
			try
			{
				using (var parser = GetReader().CreateParser(
					new CreateParserParams(0, null, MessagesParserFlag.HintParserWillBeUsedForMassiveSequentialReading, 
						MessagesParserDirection.Forward, postprocessor)))
				{
					for (; ; )
					{
						var msg = parser.ReadNextAndPostprocess();
						if (msg.Message == null)
							break;
						yield return msg;
					}
				}
			}
			finally
			{
				UnlockMessages();
			}
		}

		string IOpenContainingFolder.PathOfFileToShow
		{
			get { return containingFolderPath; }
		}

		public override string GetTaskbarLogName()
		{
			return taskbarFileName;
		}
	};
}
