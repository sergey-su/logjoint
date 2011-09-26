using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public static class FormatAutodetect
	{
		public class DetectedFormat
		{
			public readonly ILogReaderFactory Factory;
			public readonly IConnectionParams ConnectParams;
			public DetectedFormat(ILogReaderFactory fact, IConnectionParams cp)
			{
				Factory = fact;
				ConnectParams = cp;
			}
		};

		public static DetectedFormat DetectFormat(string fileName)
		{
			LogReaderFactoryRegistry facRegistry = LogReaderFactoryRegistry.Instance;
			foreach (ILogReaderFactory factory in facRegistry.Items)
			{
				IFileReaderFactory fileFactory = factory as IFileReaderFactory;
				if (fileFactory == null)
					continue;
				IConnectionParams connectParams = fileFactory.CreateParams(fileName);
				using (ILogReader logReader = fileFactory.CreateFromConnectionParams(new FakeHost(), connectParams))
				{
					logReader.NavigateTo(null, NavigateFlag.AlignTop | NavigateFlag.OriginStreamBoundaries);
					if (!logReader.WaitForAnyState(true, true, 5000))
						continue;
					if (logReader.Stats.State == ReaderState.LoadError)
						continue;
					return new DetectedFormat(factory, connectParams);
				}
			}
			return null;
		}


		class FakeHost : ILogReaderHost
		{
			Source tracer = Source.EmptyTracer;
			LogSourceThreads logSourceThreads = new LogSourceThreads(Source.EmptyTracer, new Threads(), null);

			public Source Trace
			{
				get { return tracer; }
			}

			public ITempFilesManager TempFilesManager
			{
				get { return LogJoint.TempFilesManager.GetInstance(tracer); }
			}

			public LogSourceThreads Threads
			{
				get { return logSourceThreads; }
			}

			public void OnAboutToIdle()
			{
			}

			public void OnStatisticsChanged(StatsFlag flags)
			{
			}

			public void OnMessagesChanged()
			{
			}

			public void Dispose()
			{
			}
		};
	}
}
