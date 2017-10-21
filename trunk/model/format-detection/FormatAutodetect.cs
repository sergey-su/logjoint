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
		public FormatAutodetect(IRecentlyUsedEntities recentlyUsedLogs, ILogProviderFactoryRegistry factoriesRegistry, ITempFilesManager tempFilesManager) :
			this(recentlyUsedLogs.MakeFactoryMRUIndexGetter(), factoriesRegistry, tempFilesManager)
		{
		}

		public FormatAutodetect(Func<ILogProviderFactory, int> mruIndexGetter, ILogProviderFactoryRegistry factoriesRegistry, ITempFilesManager tempFilesManager)
		{
			this.mruIndexGetter = mruIndexGetter;
			this.factoriesRegistry = factoriesRegistry;
			this.tempFilesManager = tempFilesManager;
		}

		DetectedFormat IFormatAutodetect.DetectFormat(string fileName, CancellationToken cancellation, IFormatAutodetectionProgress progress)
		{
			return DetectFormat(fileName, mruIndexGetter, factoriesRegistry, cancellation, progress, tempFilesManager);
		}

		IFormatAutodetect IFormatAutodetect.Clone()
		{
			return new FormatAutodetect(mruIndexGetter, factoriesRegistry, tempFilesManager);
		}

		static DetectedFormat DetectFormat(
			string fileName,
			Func<ILogProviderFactory, int> mruIndexGetter,
			ILogProviderFactoryRegistry factoriesRegistry,
			CancellationToken cancellation,
			IFormatAutodetectionProgress progress,
			ITempFilesManager tempFilesManager)
		{
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentException("fileName");
			if (mruIndexGetter == null)
				throw new ArgumentNullException("mru");
			var log = LJTraceSource.EmptyTracer;
			using (log.NewFrame)
			using (SimpleFileMedia fileMedia = new SimpleFileMedia(
					SimpleFileMedia.CreateConnectionParamsFromFileName(fileName)))
			using (ILogSourceThreads threads = new LogSourceThreads())
			{
				foreach (ILogProviderFactory factory in GetOrderedListOfRelevantFactories(fileName, mruIndexGetter, factoriesRegistry))
				{
					log.Info("Trying {0}", factory);
					if (progress != null)
						progress.Trying(factory);
					if (cancellation.IsCancellationRequested)
						return null;
					try
					{
						using (var reader = ((IMediaBasedReaderFactory)factory).CreateMessagesReader(
							new MediaBasedReaderParams(threads, fileMedia, tempFilesManager, MessagesReaderFlags.QuickFormatDetectionMode)))
						{
							reader.UpdateAvailableBounds(false);
							using (var parser = reader.CreateParser(new CreateParserParams(0, null, MessagesParserFlag.DisableMultithreading, MessagesParserDirection.Forward)))
							{
								if (parser.ReadNext() != null)
								{
									log.Info("Autodetected format of {0}: {1}", fileName, factory);
									return new DetectedFormat(factory, ((IFileBasedLogProviderFactory)factory).CreateParams(fileName));
								}
							}
						}
					}
					catch (Exception e)
					{
						log.Error(e, "Failed to load '{0}' as {1}", fileName, factory);
					}
				}
				if (!IOUtils.IsBinaryFile(fileMedia.DataStream))
				{
					IFileBasedLogProviderFactory factory = PlainText.Factory.Instance;
					return new DetectedFormat(factory, factory.CreateParams(fileName));
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
	}
}
