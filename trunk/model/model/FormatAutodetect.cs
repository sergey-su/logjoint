using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using System.IO;
using System.Text.RegularExpressions;

namespace LogJoint
{
	public class DetectedFormat
	{
		public readonly ILogProviderFactory Factory;
		public readonly IConnectionParams ConnectParams;
		public DetectedFormat(ILogProviderFactory fact, IConnectionParams cp)
		{
			Factory = fact;
			ConnectParams = cp;
		}
	};

	public interface IFormatAutodetect
	{
		DetectedFormat DetectFormat(string fileName);
		IFormatAutodetect Clone();
	};

	public class FormatAutodetect : IFormatAutodetect
	{
		public FormatAutodetect(Func<ILogProviderFactory, int> mruIndexGetter)
		{
			this.mruIndexGetter = mruIndexGetter;
		}

		public DetectedFormat DetectFormat(string fileName)
		{
			return DetectFormat(fileName, mruIndexGetter);
		}

		public IFormatAutodetect Clone()
		{
			return new FormatAutodetect(mruIndexGetter);
		}

		public static DetectedFormat DetectFormat(string fileName, Func<ILogProviderFactory, int> mruIndexGetter)
		{
			if (string.IsNullOrEmpty(fileName))
				throw new ArgumentException("fileName");
			if (mruIndexGetter == null)
				throw new ArgumentNullException("mru");
			var log = LJTraceSource.EmptyTracer;
			using (log.NewFrame)
			using (SimpleFileMedia fileMedia = new SimpleFileMedia(
					SimpleFileMedia.CreateConnectionParamsFromFileName(fileName),
					new MediaInitParams(log)))
			using (LogSourceThreads threads = new LogSourceThreads())
			{
				foreach (ILogProviderFactory factory in GetOrderedListOfRelevantFactories(fileName, mruIndexGetter))
				{
					log.Info("Trying {0}", factory);
					try
					{
						using (var reader = ((IMediaBasedReaderFactory)factory).CreateMessagesReader(new MediaBasedReaderParams(threads, fileMedia)))
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

		static IEnumerable<ILogProviderFactory> GetOrderedListOfRelevantFactories(string fileName, Func<ILogProviderFactory, int> mruIndexGetter)
		{
			return 
				from factory in LogProviderFactoryRegistry.DefaultInstance.Items
				where factory is IFileBasedLogProviderFactory && factory is IMediaBasedReaderFactory
				orderby GetFilePatternsMatchRating(factory, fileName), mruIndexGetter(factory)
				select factory;
		}

		readonly Func<ILogProviderFactory, int> mruIndexGetter;
	}
}
