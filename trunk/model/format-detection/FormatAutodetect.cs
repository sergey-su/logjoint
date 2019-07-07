using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using LogJoint.MRU;

namespace LogJoint
{
	public class FormatAutodetect : IFormatAutodetect
	{
		static int lastPerfOp;

		public FormatAutodetect(IRecentlyUsedEntities recentlyUsedLogs, ILogProviderFactoryRegistry factoriesRegistry, ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory) :
			this(recentlyUsedLogs.MakeFactoryMRUIndexGetter(), factoriesRegistry, tempFilesManager, traceSourceFactory)
		{
		}

		public FormatAutodetect(Func<ILogProviderFactory, int> mruIndexGetter, ILogProviderFactoryRegistry factoriesRegistry, ITempFilesManager tempFilesManager, ITraceSourceFactory traceSourceFactory)
		{
			this.mruIndexGetter = mruIndexGetter;
			this.factoriesRegistry = factoriesRegistry;
			this.tempFilesManager = tempFilesManager;
			this.traceSourceFactory = traceSourceFactory;
		}

		DetectedFormat IFormatAutodetect.DetectFormat(string fileName, string loggableName, CancellationToken cancellation, IFormatAutodetectionProgress progress)
		{
			return DetectFormat(fileName, loggableName, mruIndexGetter, factoriesRegistry, cancellation, progress, tempFilesManager, traceSourceFactory);
		}

		IFormatAutodetect IFormatAutodetect.Clone()
		{
			return new FormatAutodetect(mruIndexGetter, factoriesRegistry, tempFilesManager, traceSourceFactory);
		}

		static DetectedFormat DetectFormat(
			string fileName,
			string loggableName,
			Func<ILogProviderFactory, int> mruIndexGetter,
			ILogProviderFactoryRegistry factoriesRegistry,
			CancellationToken cancellation,
			IFormatAutodetectionProgress progress,
			ITempFilesManager tempFilesManager,
			ITraceSourceFactory traceSourceFactory)
		{
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentException("fileName");
			if (mruIndexGetter == null)
				throw new ArgumentNullException("mru");
			Func<SimpleFileMedia> createFileMedia = () => new SimpleFileMedia(SimpleFileMedia.CreateConnectionParamsFromFileName(fileName));
			var log = traceSourceFactory.CreateTraceSource("App", string.Format("fdtc.{0}", Interlocked.Increment(ref lastPerfOp)));
			using (new Profiling.Operation(log, string.Format("format detection of {0}", loggableName)))
			using (ILogSourceThreadsInternal threads = new LogSourceThreads())
			using (var localCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
			{
				var ret = GetOrderedListOfRelevantFactories(fileName, mruIndexGetter, factoriesRegistry).AsParallel().Select(factory =>
				{
					try
					{
						using (var perfOp = new Profiling.Operation(log, factory.ToString()))
						using (var fileMedia = createFileMedia())
						using (var reader = ((IMediaBasedReaderFactory)factory).CreateMessagesReader(
							new MediaBasedReaderParams(threads, fileMedia, tempFilesManager, traceSourceFactory,
								MessagesReaderFlags.QuickFormatDetectionMode, parentLoggingPrefix: log.Prefix)))
						{
							if (progress != null)
								progress.Trying(factory);
							if (localCancellation.IsCancellationRequested)
							{
								perfOp.Milestone("cancelled");
								return null;
							}
							reader.UpdateAvailableBounds(false);
							perfOp.Milestone("bounds detected");
							using (var parser = reader.CreateParser(new CreateParserParams(0, null, 
								MessagesParserFlag.DisableMultithreading| MessagesParserFlag.DisableDejitter, MessagesParserDirection.Forward)))
							{
								if (parser.ReadNext() != null)
								{
									log.Info("Autodetected format of {0}: {1}", fileName, factory);
									localCancellation.Cancel();
									return new DetectedFormat(factory, ((IFileBasedLogProviderFactory)factory).CreateParams(fileName));
								}
							}
						}
					}
					catch (Exception e)
					{
						log.Error(e, "Failed to load '{0}' as {1}", fileName, factory);
					}
					return null;
				}).FirstOrDefault(x => x != null);
				if (ret != null)
					return ret;
				using (var fileMedia = createFileMedia())
				{
					if (!IOUtils.IsBinaryFile(fileMedia.DataStream))
					{
						log.Info("File does not look binary");
						var factory = factoriesRegistry.Find(
							PlainText.Factory.CompanyName, PlainText.Factory.FormatName) as IFileBasedLogProviderFactory;
						if (factory != null)
						{
							log.Info("Fall back to plaintext format");
							return new DetectedFormat(factory, factory.CreateParams(fileName));
						}
					}
				}
			}
			return null;
		}

		static Regex WildcardToRegex(string pattern)
		{
			return new Regex("^" + Regex.Escape(pattern).
				Replace("\\*", ".*").
				Replace("\\?", ".") + "$", RegexOptions.IgnoreCase);
		}

		static int GetFilePatternsMatchRating(ILogProviderFactory factory, string testFileName)
		{
			var patterns = ((IFileBasedLogProviderFactory)factory).SupportedPatterns;
			if (patterns.Any(pattern => WildcardToRegex(pattern).IsMatch(testFileName)))
				return 0;
			if (patterns.Count() == 0)
				return 1;
			return 2;
		}

		static IEnumerable<ILogProviderFactory> GetOrderedListOfRelevantFactories(string fileName, Func<ILogProviderFactory, int> mruIndexGetter,
			ILogProviderFactoryRegistry factoriesRegistry)
		{
			return
				from factory in factoriesRegistry.Items
				where factory is IFileBasedLogProviderFactory && factory is IMediaBasedReaderFactory
				orderby GetFilePatternsMatchRating(factory, fileName), mruIndexGetter(factory)
				select factory;
		}

		readonly Func<ILogProviderFactory, int> mruIndexGetter;
		readonly ILogProviderFactoryRegistry factoriesRegistry;
		readonly ITempFilesManager tempFilesManager;
		readonly ITraceSourceFactory traceSourceFactory;
	}
}
