using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Xml;
using System.IO;
using System.Threading.Tasks;

namespace LogJoint
{
	public class LiveLogXMLWriter: IDisposable
	{
		static Encoding unicodeEncodingNoBOM = new UnicodeEncoding(false, false);

		public LiveLogXMLWriter(Stream output, XmlWriterSettings baseSettings, long maxSizeInBytes)
		{
			if (maxSizeInBytes > 0 && maxSizeInBytes % 4 != 0)
				throw new ArgumentException("Max size must be multiple of 4", nameof(maxSizeInBytes));

			this.output = output;
			this.closeOutput = baseSettings.CloseOutput;
			this.settings = baseSettings.Clone();
			this.settings.CloseOutput = false;
			this.settings.Encoding = unicodeEncodingNoBOM;
			this.maxSize = maxSizeInBytes;
		}

		static public Encoding OutputEncoding
		{
			get { return unicodeEncodingNoBOM; }
		}

		public XmlWriter BeginWriteMessage(bool replaceLastMessage)
		{
			CheckDisposed();

			if (messageOpen)
				throw new InvalidOperationException("Cannot open more than one message");
			messageOpen = true;

			if (replaceLastMessage)
			{
				if (writer != null)
				{
					writer.Close();
					writer = null;
				}
				output.SetLength(lastMessagePosition);
				output.Position = lastMessagePosition;
			}
			lastMessagePosition = output.Position;
			if (writer == null)
			{
				writer = XmlWriter.Create(output, settings);
			}
			return writer;
		}

		public void EndWriteMessage()
		{
			CheckDisposed();

			if (!messageOpen)
				throw new InvalidOperationException("There is no open message");
			messageOpen = false;

			writer.Flush();

			if (maxSize > 0)
				DoLimitSize();
		}

		public void Dispose()
		{
			if (isDisposed)
				return;
			isDisposed = true;

			if (writer != null)
			{
				writer.Close();
				writer = null;
			}
			if (closeOutput)
			{
				output.Close();
			}
		}

		void CheckDisposed()
		{
			if (isDisposed)
				throw new ObjectDisposedException(GetType().Name);
		}

		void DoLimitSize()
		{
			if (output.Length < maxSize)
				return;

			long halfMaxSize = maxSize / 2;
			long copyFrom = output.Length - halfMaxSize;
			long copyTo = 0;

			int bufSz = 2048;
			byte[] buf = new byte[bufSz];

			for (long i = 0; i < halfMaxSize; i += bufSz)
			{
				output.Position = copyFrom + i;
				int read = output.Read(buf, 0, bufSz);
				
				output.Position = copyTo + i;
				output.Write(buf, 0, read);
			}

			output.SetLength(halfMaxSize);
		}

		readonly Stream output;
		readonly bool closeOutput;
		readonly XmlWriterSettings settings;
		readonly long maxSize;
		bool isDisposed;
		bool messageOpen;
		long lastMessagePosition;
		XmlWriter writer;
	};

	public abstract class LiveLogProvider : StreamLogProvider, ILogProvider
	{
		protected readonly LJTraceSource trace;
		private IConnectionParams originalConnectionParams;
		CancellationTokenSource stopEvt;
		Task listeningThread;
		LiveLogXMLWriter output;
		readonly long defaultBackupMaxFileSize = 0;//16 * 1024 * 1024;

		static ConnectionParams CreateConnectionParams(IConnectionParams originalConnectionParams, ITempFilesManager tempFilesManager)
		{
			ConnectionParams connectParams = new ConnectionParams();
			connectParams.AssignFrom(originalConnectionParams);
			connectParams[ConnectionParamsKeys.PathConnectionParam] = tempFilesManager.CreateEmptyFile();
			return connectParams;
		}

		public LiveLogProvider(ILogProviderHost host, ILogProviderFactory factory, IConnectionParams originalConnectionParams, DejitteringParams? dejitteringParams = null)
			:
			base(
				host, 
				factory,
				CreateConnectionParams(originalConnectionParams, host.TempFilesManager),
				@params => new XmlFormat.MessagesReader(
					@params,
					XmlFormat.XmlFormatInfo.MakeNativeFormatInfo(LiveLogXMLWriter.OutputEncoding.EncodingName, dejitteringParams, new FormatViewOptions(rawViewAllowed: false), host.RegexFactory),
					host.RegexFactory,
					host.TraceSourceFactory
				)
			)
		{
			this.trace = base.tracer;
			this.originalConnectionParams = new ConnectionParamsReadOnlyView(originalConnectionParams);
			using (trace.NewFrame)
			{
				try
				{
					string fileName = base.connectionParamsReadonlyView[ConnectionParamsKeys.PathConnectionParam];

					XmlWriterSettings xmlSettings = new XmlWriterSettings();
					xmlSettings.CloseOutput = true;
					xmlSettings.ConformanceLevel = ConformanceLevel.Fragment;
					xmlSettings.OmitXmlDeclaration = false;
					xmlSettings.Indent = true;

					output = new LiveLogXMLWriter(
						new FileStream(fileName, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read),
						xmlSettings,
						defaultBackupMaxFileSize
					);
					trace.Info("Output created");

					stopEvt = new CancellationTokenSource();
				}
				catch (Exception e)
				{
					trace.Error(e, "Failed to inistalize live log reader. Disposing what has been created so far.");
					Dispose();
					throw;
				}
			}
		}

		IConnectionParams ILogProvider.ConnectionParams
		{
			get { return originalConnectionParams; }
		}

		protected void StartLiveLogThread(string threadName)
		{
			using (trace.NewFrame)
			{
				listeningThread = TaskUtils.StartInThreadPoolTaskScheduler(ListeningThreadProc);
				trace.Info("Thread started");
			}
		}

		public override async Task Dispose()
		{
			using (trace.NewFrame)
			{
				if (IsDisposed)
				{
					trace.Warning("Already disposed");
					return;
				}

				if (listeningThread != null)
				{
					if (listeningThread.IsCompleted)
					{
						trace.Info("Thread is not alive.");
					}
					else
					{
						trace.Info("Thread has been created. Setting stop event and joining the thread.");
						stopEvt.Cancel();
						await listeningThread;
						trace.Info("Thread finished");
					}
				}

				if (output != null)
				{
					output.Dispose();
				}

				trace.Info("Calling base destructor");
				await base.Dispose();
			}
		}

		abstract protected Task LiveLogListen(CancellationToken stopEvt, LiveLogXMLWriter output);

		protected void ReportBackgroundActivityStatus(bool active)
		{
			var newStatus = active ? LogProviderBackgroundAcivityStatus.Active : LogProviderBackgroundAcivityStatus.Inactive;
			StatsTransaction(stats =>
			{
				if (stats.BackgroundAcivityStatus != newStatus)
				{
					stats.BackgroundAcivityStatus = newStatus;
					return LogProviderStatsFlag.BackgroundAcivityStatus;
				}
				return LogProviderStatsFlag.None;
			});
		}

		async Task ListeningThreadProc()
		{
			using (trace.NewFrame)
			{
				try
				{
					await LiveLogListen(this.stopEvt.Token, this.output);
				}
				catch (Exception e)
				{
					trace.Error(e, "Live log listening thread failed");
				}
				finally
				{
					ReportBackgroundActivityStatus(false);
				}
			}
		}
	}
}
