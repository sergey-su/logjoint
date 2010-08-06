using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Threading;
using System.Text.RegularExpressions;

namespace LogJoint.PlainText
{
	class LogReader: LiveLogReader
	{
		string fileName;

		public LogReader(ILogReaderHost host, string fileName)
			:
			base(host, PlainText.Factory.Instance)
		{
			this.fileName = fileName;
			this.stats.ConnectionParams[PlainText.Factory.Instance.SourcePathParamName] = fileName;
			StartLiveLogThread(string.Format("'{0}' listening thread", fileName));
		}

		class TextFileStreamHost : ITextFileStreamHost
		{
			internal long end;

			public Encoding DetectEncoding(TextFileStreamBase stream)
			{
				return Encoding.ASCII;
			}

			public long BeginPosition
			{
				get { return 0; }
			}

			public long EndPosition
			{
				get { return end; }
			}
		};

		protected override void LiveLogListen(ManualResetEvent stopEvt, LiveLogXMLWriter output)
		{
			TextFileStreamHost streamHost = new TextFileStreamHost();
			using (ILogMedia media = new SimpleFileMedia(
				LogMedia.FileSystemImpl.Instance, 
				SimpleFileMedia.CreateConnectionParamsFromFileName(fileName), 
				new MediaInitParams(trace)))
			using (TextFileStreamBase fs = new TextFileStreamBase(
				media,
				new Regex(@"^(?<body>.+)$", RegexOptions.Compiled | RegexOptions.Multiline | RegexOptions.ExplicitCapture),
				streamHost
			))
			using (FileSystemWatcher watcher = new FileSystemWatcher(Path.GetDirectoryName(fileName), 
				Path.GetFileName(fileName)))
			using (AutoResetEvent fileChangedEvt = new AutoResetEvent(true))
			{
				watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
				watcher.Changed += delegate(object sender, FileSystemEventArgs e)
				{
					fileChangedEvt.Set();
				};
				//watcher.EnableRaisingEvents = true;

				long lastLinePosition = 0;
				WaitHandle[] events = new WaitHandle[] { stopEvt, fileChangedEvt };

				for (; ; )
				{
					if (WaitHandle.WaitAny(events, 200, false) == 0)
						break;

					if (fs.Length == streamHost.end)
						continue;

					streamHost.end = fs.Length;

					DateTime lastModified = fs.LastModified;
					
					fs.BeginReadSession(null, lastLinePosition);
					try
					{
						for (; ; )
						{
							TextFileStreamBase.TextMessageCapture capture = fs.GetCurrentMessageAndMoveToNextOne();
							if (capture == null)
								break;
							lastLinePosition = capture.BeginStreamPosition.Value;

							XmlWriter writer = output.BeginWriteMessage(false);
							writer.WriteStartElement("m");
							writer.WriteAttributeString("d", Listener.FormatDate(lastModified));
							writer.WriteString(capture.HeaderMatch.Groups[1].Value);
							writer.WriteEndElement();
							output.EndWriteMessage();
						}
					}
					finally
					{
						fs.EndReadSession();
					}
				}
			}
		}

	}

	class Factory : IFileReaderFactory
	{
		public static readonly Factory Instance = new Factory();
		public readonly string SourcePathParamName = "sourcePath";

		static Factory()
		{
			LogReaderFactoryRegistry.Instance.Register(Instance);
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

		public ILogReaderFactoryUI CreateUI()
		{
			return new FileLogFactoryUI(this);
		}

		public string GetUserFriendlyConnectionName(IConnectionParams connectParams)
		{
			return connectParams[SourcePathParamName];
		}

		public ILogReader CreateFromConnectionParams(ILogReaderHost host, IConnectionParams connectParams)
		{
			return new LogReader(host, connectParams[SourcePathParamName]);
		}

		#endregion
	};
}
