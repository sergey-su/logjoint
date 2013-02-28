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
		string containingFolder;

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
					readerType, new MediaBasedReaderParams(this.threads, media), formatInfo);

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
			System.IO.File.Copy(srcFileName, fileName);
		}

		void InitPathDependentMembers(IConnectionParams connectParams)
		{
			isSavableAs = false;
			containingFolder = null;
			string fname = connectParams[ConnectionParamsUtils.PathConnectionParam];
			if (fname != null)
			{
				var isTempFile = TempFilesManager.GetInstance().IsTemporaryFile(fname);
				isSavableAs = isTempFile;
				if (!isTempFile)
				{
					containingFolder = fname;
				}
			}
			if (isSavableAs)
			{
				string id = connectParams[ConnectionParamsUtils.IdentityConnectionParam];
				if (id != null)
				{
					int idx = id.LastIndexOfAny(new char[] {'\\', '/'});
					if (idx == -1)
						suggestedSaveAsFileName = id;
					else
						suggestedSaveAsFileName = id.Substring(idx + 1, id.Length - idx - 1);
				}
			}
		}

		public IEnumerable<PostprocessedMessage> LockProviderAndEnumAllMessages(Func<MessageBase, object> postprocessor)
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
			get { return containingFolder; }
		}
	};
}
