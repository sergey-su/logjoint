using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading;
using LogJoint.RegularExpressions;

namespace LogJoint.PlainText
{
	class LogProvider: LiveLogProvider
	{
		string fileName;

		public LogProvider(ILogProviderHost host, string fileName)
			:
			base(host, PlainText.Factory.Instance)
		{
			this.fileName = fileName;
			this.stats.ConnectionParams[PlainText.Factory.Instance.SourcePathParamName] = fileName;
			StartLiveLogThread(string.Format("'{0}' listening thread", fileName));
		}

		protected override void LiveLogListen(ManualResetEvent stopEvt, LiveLogXMLWriter output)
		{
			using (ILogMedia media = new SimpleFileMedia(
				LogMedia.FileSystemImpl.Instance, 
				SimpleFileMedia.CreateConnectionParamsFromFileName(fileName), 
				new MediaInitParams(trace)))
			using (FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(fileName), 
				Path.GetFileName(fileName)))
			using (AutoResetEvent fileChangedEvt = new AutoResetEvent(true))
			{
				IMessagesSplitter splitter = new MessagesSplitter(
					new StreamTextAccess(media.DataStream, Encoding.ASCII, TextStreamPositioningParams.Default),
					RegexFactory.Instance.Create(@"^(?<body>.+)$", ReOptions.Multiline)
				);

				watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
				watcher.Changed += delegate(object sender, FileSystemEventArgs e)
				{
					fileChangedEvt.Set();
				};
				//watcher.EnableRaisingEvents = true;

				long lastLinePosition = 0;
				long lastStreamLength = 0;
				WaitHandle[] events = new WaitHandle[] { stopEvt, fileChangedEvt };

				var capture = new TextMessageCapture();

				for (; ; )
				{
					if (WaitHandle.WaitAny(events, 200, false) == 0)
						break;

					if (media.DataStream.Length == lastStreamLength)
						continue;

					lastStreamLength = media.DataStream.Length;

					DateTime lastModified = media.LastModified;

					splitter.BeginSplittingSession(new FileRange.Range(0, lastStreamLength), lastLinePosition, MessagesParserDirection.Forward);
					try
					{
						for (; ; )
						{
							if (!splitter.GetCurrentMessageAndMoveToNextOne(capture))
								break;
							lastLinePosition = capture.BeginPosition;

							XmlWriter writer = output.BeginWriteMessage(false);
							writer.WriteStartElement("m");
							writer.WriteAttributeString("d", Listener.FormatDate(lastModified));
							//writer.WriteString(capture.HeaderMatch.Groups[1].Value); todo
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

	class Factory : IFileBasedLogProviderFactory
	{
		public static readonly Factory Instance = new Factory();
		public readonly string SourcePathParamName = "sourcePath";

		static Factory()
		{
			LogProviderFactoryRegistry.DefaultInstance.Register(Instance);
		}

		#region IFileReaderFactory

		public IEnumerable<string> SupportedPatterns { get { yield break; } }

		public IConnectionParams CreateParams(string fileName)
		{
			ConnectionParams p = new ConnectionParams();
			p[SourcePathParamName] = fileName;
			return p;
		}

		#endregion

		#region ILogReaderFactory Members

		public string CompanyName
		{
			get { return "LogJoint"; }
		}

		public string FormatName
		{
			get { return "Text file"; }
		}

		public string FormatDescription
		{
			get { return "Reads all the lines from any text file without any additional parsing. The messages get the timestamp equal to the file modification date. When tracking live file this timestamp may change."; }
		}

		public ILogProviderFactoryUI CreateUI(IFactoryUIFactory factory)
		{
			return factory.CreateFileProviderFactoryUI(this);
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return connectParams[SourcePathParamName];
		}

		public IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams)
		{
			return originalConnectionParams;
		}

		public ILogProvider CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams)
		{
			return new LogProvider(host, connectParams[SourcePathParamName]);
		}

		#endregion
	};
}
