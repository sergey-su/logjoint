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
		public readonly Type LogMediaType;
		public readonly MessagesReaderExtensions.XmlInitializationParams ExtensionsInitData;

		public StreamBasedFormatInfo(Type logMediaType, MessagesReaderExtensions.XmlInitializationParams extensionsInitData)
		{
			this.LogMediaType = logMediaType;
			this.ExtensionsInitData = extensionsInitData;
		}
	};

	class StreamBasedMediaInitParams : MediaInitParams
	{
		public readonly Type ReaderType;
		public readonly StreamBasedFormatInfo FormatInfo;
		public StreamBasedMediaInitParams(LJTraceSource trace, Type readerType, StreamBasedFormatInfo formatInfo):
			base(trace)
		{
			this.ReaderType = readerType;
			this.FormatInfo = formatInfo;
		}
	};

	public class StreamLogProvider : RangeManagingProvider, ISaveAs
	{
		ILogMedia media;
		IPositionedMessagesReader reader;
		bool isSavableAs;
		string suggestedSaveAsFileName;

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

				media = (ILogMedia)Activator.CreateInstance(
					formatInfo.LogMediaType, connectParams, new StreamBasedMediaInitParams(host.Trace, readerType, formatInfo));

				reader = (IPositionedMessagesReader)Activator.CreateInstance(
					readerType, this.threads, media, formatInfo);

				StartAsyncReader("Reader thread: " + connectParams.ToString());

				InitSavableAsMembers(connectParams);
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

		void InitSavableAsMembers(IConnectionParams connectParams)
		{
			isSavableAs = false;
			string fname = connectParams[ConnectionParamsUtils.PathConnectionParam];
			if (fname != null)
			{
				isSavableAs = TempFilesManager.GetInstance().IsTemporaryFile(fname);
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

	};
}
