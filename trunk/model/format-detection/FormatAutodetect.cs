using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using LogJoint.MRU;
using System.Threading.Tasks;

namespace LogJoint
{
	public class FormatAutodetect : IFormatAutodetect
	{
		static int lastPerfOp;

		public FormatAutodetect(IRecentlyUsedEntities recentlyUsedLogs, ILogProviderFactoryRegistry factoriesRegistry, ITraceSourceFactory traceSourceFactory, LogMedia.IFileSystem fileSystem) :
			this(recentlyUsedLogs.MakeFactoryMRUIndexGetter(), factoriesRegistry, traceSourceFactory, fileSystem)
		{
		}

		public FormatAutodetect(Func<ILogProviderFactory, int> mruIndexGetter, ILogProviderFactoryRegistry factoriesRegistry,
			ITraceSourceFactory traceSourceFactory, LogMedia.IFileSystem fileSystem)
		{
			this.mruIndexGetter = mruIndexGetter;
			this.factoriesRegistry = factoriesRegistry;
			this.traceSourceFactory = traceSourceFactory;
			this.fileSystem = fileSystem;
		}

		Task<DetectedFormat> IFormatAutodetect.DetectFormat(string fileName, string loggableName, CancellationToken cancellation, IFormatAutodetectionProgress progress)
		{
			return DetectFormat(fileName, loggableName, mruIndexGetter, factoriesRegistry, cancellation, progress, traceSourceFactory, fileSystem);
		}

		IFormatAutodetect IFormatAutodetect.Clone()
		{
			return new FormatAutodetect(mruIndexGetter, factoriesRegistry, traceSourceFactory, fileSystem);
		}

		static async Task<DetectedFormat> DetectFormat(
			string fileName,
			string loggableName,
			Func<ILogProviderFactory, int> mruIndexGetter,
			ILogProviderFactoryRegistry factoriesRegistry,
			CancellationToken cancellation,
			IFormatAutodetectionProgress progress,
			ITraceSourceFactory traceSourceFactory,
			LogMedia.IFileSystem fileSystem)
		{
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentException("fileName");
			if (mruIndexGetter == null)
				throw new ArgumentNullException("mru");
			Func<Task<SimpleFileMedia>> createFileMedia = () => SimpleFileMedia.Create(fileSystem, SimpleFileMedia.CreateConnectionParamsFromFileName(fileName));
			var log = traceSourceFactory.CreateTraceSource("App", string.Format("fdtc.{0}", Interlocked.Increment(ref lastPerfOp)));
			using (new Profiling.Operation(log, string.Format("format detection of {0}", loggableName)))
			using (ILogSourceThreadsInternal threads = new LogSourceThreads())
			using (var localCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellation))
			{
				var candidateFactories = GetOrderedListOfRelevantFactories(fileName, mruIndexGetter, factoriesRegistry).ToArray();
				var ret = (await Task.WhenAll(candidateFactories.Select((factory, index) => (factory, index)).Select(async candidate =>
				{
					var (factory, idx) = candidate;
					try
					{
						using (var perfOp = new Profiling.Operation(log, factory.ToString()))
						using (var fileMedia = await createFileMedia())
						using (var reader = ((IMediaBasedReaderFactory)factory).CreateMessagesReader(
							new MediaBasedReaderParams(threads, fileMedia,
								MessagesReaderFlags.QuickFormatDetectionMode, parentLoggingPrefix: log.Prefix)))
						{
							if (progress != null)
								progress.Trying(factory);
							if (localCancellation.IsCancellationRequested)
							{
								perfOp.Milestone("cancelled");
								return (fmt: (DetectedFormat)null, idx);
							}
							await reader.UpdateAvailableBounds(false);
							perfOp.Milestone("bounds detected");
							var parser = await reader.CreateParser(new CreateParserParams(0, null,
								MessagesParserFlag.DisableMultithreading | MessagesParserFlag.DisableDejitter, MessagesParserDirection.Forward));
							try
							{
								if (await parser.ReadNext() != null)
								{
									log.Info("Autodetected format of {0}: {1}", fileName, factory);
									localCancellation.Cancel();
									return (fmt: new DetectedFormat(factory, ((IFileBasedLogProviderFactory)factory).CreateParams(fileName)), idx);
								}
							}
							finally
                            {
								await parser.Dispose();
                            }
						}
					}
					catch (Exception e)
					{
						log.Error(e, "Failed to load '{0}' as {1}", fileName, factory);
					}
					return (fmt: (DetectedFormat)null, idx);
				}))).Where(x => x.fmt != null).OrderBy(x => x.idx).Select(x => x.fmt).FirstOrDefault();
				if (ret != null)
					return ret;
				using (var fileMedia = await createFileMedia())
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
				where !(factory.CompanyName == PlainText.Factory.CompanyName && factory.FormatName == PlainText.Factory.FormatName)
				orderby GetFilePatternsMatchRating(factory, fileName), mruIndexGetter(factory)
				select factory;
		}

		readonly Func<ILogProviderFactory, int> mruIndexGetter;
		readonly ILogProviderFactoryRegistry factoriesRegistry;
		readonly ITraceSourceFactory traceSourceFactory;
		readonly LogMedia.IFileSystem fileSystem;
	}
}
