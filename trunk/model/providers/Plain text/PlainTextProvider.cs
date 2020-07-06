using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading;
using LogJoint.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.PlainText
{
	class LogProvider: LiveLogProvider
	{
		readonly string fileName;
		long sizeInBytesStat;

		public LogProvider(ILogProviderHost host, IConnectionParams connectParams, ILogProviderFactory factory)
			:
			base(host, factory, connectParams)
		{
			this.fileName = connectParams[ConnectionParamsKeys.PathConnectionParam];
			StartLiveLogThread(string.Format("'{0}' listening thread", fileName));
		}

		public override string GetTaskbarLogName()
		{
			return ConnectionParamsUtils.GuessFileNameFromConnectionIdentity(fileName);
		}

		protected override long CalcTotalBytesStats(IPositionedMessagesReader reader)
		{
			return sizeInBytesStat;
		}

		protected override async Task LiveLogListen(CancellationToken stopEvt, LiveLogXMLWriter output)
		{
			using (ILogMedia media = await SimpleFileMedia.Create(
				LogMedia.FileSystemImpl.Instance, 
				SimpleFileMedia.CreateConnectionParamsFromFileName(fileName)))
			using (FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(fileName), 
				Path.GetFileName(fileName)))
			using (AutoResetEvent fileChangedEvt = new AutoResetEvent(true))
			{
				IMessagesSplitter splitter = new MessagesSplitter(
					new StreamTextAccess(media.DataStream, Encoding.ASCII, TextStreamPositioningParams.Default),
					host.RegexFactory.Create(@"^(?<body>.+)$", ReOptions.Multiline)
				);

				watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
				watcher.Changed += delegate(object sender, FileSystemEventArgs e)
				{
					fileChangedEvt.Set();
				};
				//watcher.EnableRaisingEvents = true;

				long lastLinePosition = 0;
				long lastStreamLength = 0;
				WaitHandle[] events = new WaitHandle[] { stopEvt.WaitHandle, fileChangedEvt };

				var capture = new TextMessageCapture();

				for (; ; )
				{
					if (WaitHandle.WaitAny(events, 250, false) == 0)
						break;

					await media.Update();

					if (media.Size == lastStreamLength)
						continue;

					lastStreamLength = media.Size;
					sizeInBytesStat = lastStreamLength;

					DateTime lastModified = media.LastModified;

					await splitter.BeginSplittingSession(new FileRange.Range(0, lastStreamLength), lastLinePosition, MessagesParserDirection.Forward);
					try
					{
						for (; ; )
						{
							if (!await splitter.GetCurrentMessageAndMoveToNextOne(capture))
								break;
							lastLinePosition = capture.BeginPosition;

							XmlWriter writer = output.BeginWriteMessage(false);
							writer.WriteStartElement("m");
							writer.WriteAttributeString("d", Listener.FormatDate(lastModified));
							writer.WriteString(XmlUtils.RemoveInvalidXMLChars(capture.MessageHeader));
							writer.WriteEndElement();
							output.EndWriteMessage();
						}
					}
					finally
					{
						splitter.EndSplittingSession();
					}
				}
			}
		}
	}

	public class Factory : IFileBasedLogProviderFactory
	{
		readonly ITempFilesManager tempFiles;

		public Factory(ITempFilesManager tempFiles)
		{
			this.tempFiles = tempFiles;
		}

		public static string CompanyName { get { return "LogJoint"; } }

		public static string FormatName { get { return "Text file"; } }


		IEnumerable<string> IFileBasedLogProviderFactory.SupportedPatterns { get { yield break; } }

		IConnectionParams IFileBasedLogProviderFactory.CreateParams(string fileName)
		{
			return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(fileName);
		}

		IConnectionParams IFileBasedLogProviderFactory.CreateRotatedLogParams(string folder)
		{
			throw new NotImplementedException();
		}

		string ILogProviderFactory.CompanyName
		{
			get { return Factory.CompanyName; }
		}

		string ILogProviderFactory.FormatName
		{
			get { return Factory.FormatName; }
		}

		string ILogProviderFactory.FormatDescription
		{
			get { return "Reads all the lines from any text file without any additional parsing. The messages get the timestamp equal to the file modification date. When tracking live file this timestamp may change."; }
		}

		string ILogProviderFactory.UITypeKey { get { return StdProviderFactoryUIs.FileBasedProviderUIKey; } }

		string ILogProviderFactory.GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetFileOrFolderBasedUserFriendlyConnectionName(connectParams);
		}

		string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams)
		{
			return ConnectionParamsUtils.GetConnectionIdentity(connectParams);
		}

		IConnectionParams ILogProviderFactory.GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return ConnectionParamsUtils.RemoveNonPersistentParams(originalConnectionParams.Clone(true), tempFiles);
		}

		ILogProvider ILogProviderFactory.CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host, connectParams, this);
		}

		IFormatViewOptions ILogProviderFactory.ViewOptions { get { return FormatViewOptions.NoRawView; } }

		LogProviderFactoryFlag ILogProviderFactory.Flags
		{
			get { return LogProviderFactoryFlag.None; }
		}
	};
}
