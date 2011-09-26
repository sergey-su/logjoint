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
	internal static class EncodingUtils
	{
		public static Encoding GetEncodingFromConfigXMLName(string encoding)
		{
			if (encoding == null)
				encoding = "";
			switch (encoding)
			{
				case "ACP": // use current ANSI code page
					return Encoding.Default;
				case "":
					return null;
				case "BOM": // detect from byte-order-mask
				case "PI": // detect from processing instructions
					return null;
				default:
					try
					{
						return Encoding.GetEncoding(encoding);
					}
					catch (ArgumentException)
					{
						return null;
					}
			}
		}

		public static Encoding DetectEncodingFromBOM(Stream stream, Encoding defaultEncoding)
		{
			stream.Position = 0;
			StreamReader tmpReader = new StreamReader(stream, defaultEncoding, true);
			tmpReader.Read();
			return tmpReader.CurrentEncoding ?? defaultEncoding;
		}

		public static Encoding DetectEncodingFromProcessingInstructions(Stream stream)
		{
			stream.Position = 0;
			XmlReaderSettings rs = new XmlReaderSettings();
			rs.CloseInput = false;
			rs.ConformanceLevel = ConformanceLevel.Fragment;

			using (XmlReader tmpReader = XmlReader.Create(stream, rs))
			try
			{
				while (tmpReader.Read())
				{
					if (tmpReader.NodeType == XmlNodeType.XmlDeclaration)
					{
						string encoding = tmpReader.GetAttribute("encoding");
						if (!string.IsNullOrEmpty(encoding))
						{
							try
							{
								return Encoding.GetEncoding(encoding);
							}
							catch (ArgumentException)
							{
								return null;
							}
						}
						return null;
					}
					else if (tmpReader.NodeType == XmlNodeType.Element)
					{
						break;
					}
				}
			}
			catch (XmlException)
			{
				return null; // XML might be not well formed. Ignore such errors.
			}
			return null;
		}
	};

	public abstract class StreamBasedPositionedMessagesProvider : IPositionedMessagesProvider, ITextFileStreamHost
	{
		readonly Regex headerRe;
		readonly BoundFinder beginFinder;
		readonly BoundFinder endFinder;
		readonly ILogMedia media;

		TextFileStream stream;
		Encoding cachedEncoding;

		long mediaSize;
		long beginPosition, endPosition;

		public StreamBasedPositionedMessagesProvider(
			ILogMedia media,
			Regex headerRe,
			BoundFinder beginFinder,
			BoundFinder endFinder
		)
		{
			this.headerRe = headerRe;
			this.beginFinder = beginFinder;
			this.endFinder = endFinder;
			this.media = media;
		}

		#region IPositionedMessagesProvider

		public long BeginPosition
		{
			get
			{
				return beginPosition;
			}
		}

		public long EndPosition
		{
			get
			{
				return endPosition;
			}
		}

		public long ActiveRangeRadius
		{
			get { return 1024 * 512; }
		}

		public long MaximumMessageSize
		{
			get { return TextFileStream.MaximumMessageSize; }
		}

		public long PositionRangeToBytes(FileRange.Range range)
		{
			// Here is not precise calculation: TextStreamPosition cannot be converted to bytes 
			// directly. But this function is used only for statistics, so it's OK to 
			// treat differece between TextStreamPosition's as bytes range.
			return range.Length;
		}

		public long SizeInBytes
		{
			get { return mediaSize; }
		}

		public UpdateBoundsStatus UpdateAvailableBounds(bool incrementalMode)
		{
			media.Update();

			// Save the current phisical stream end
			long prevFileSize = mediaSize;

			// Reread the phisical stream end
			if (!UpdateMediaSize())
			{
				// The stream has the same size as it had before
				return UpdateBoundsStatus.NothingUpdated;
			}

			bool oldMessagesAreInvalid = false;

			if (mediaSize < prevFileSize)
			{
				// The size of source file has reduced. This means that the 
				// file was probably overwritten. We have to delete all the messages 
				// we have loaded so far and start loading the file from the beginning.
				// Otherwise there is a high posiblity of messages' integrity violation.
				// Fall to non-incremental mode
				incrementalMode = false;
				oldMessagesAreInvalid = true;
			}

			FindLogicalBounds(incrementalMode);

			if (oldMessagesAreInvalid)
				return UpdateBoundsStatus.OldMessagesAreInvalid;

			return UpdateBoundsStatus.NewMessagesAvailable;
		}

		public abstract IPositionedMessagesParser CreateParser(long startPosition, FileRange.Range? range, bool isMainStreamReader);

		#endregion

		#region IDisposable

		public void Dispose()
		{
			if (stream != null)
			{
				stream.Dispose();
			}
		}

		#endregion

		#region ITextFileStreamHost Members

		public Encoding DetectEncoding(TextFileStreamBase stream)
		{
			if (cachedEncoding == null)
				cachedEncoding = GetStreamEncoding(stream);
			return cachedEncoding;
		}

		#endregion

		protected abstract class Parser : IPositionedMessagesParser
		{
			readonly FileRange.Range? range;
			readonly bool isMainStreamParser;
			TextFileStream fso;

			public Parser(IPositionedMessagesProvider provider, TextFileStream fso, FileRange.Range? range, long startPosition, bool isMainStreamParser)
			{
				this.range = range;
				this.fso = fso;
				this.isMainStreamParser = isMainStreamParser;
				fso.AttachParser(this, startPosition);

				if (fso.CurrentMessageIsEmpty)
				{
					if ((startPosition == provider.BeginPosition)
					 || ((provider.EndPosition - startPosition) >= TextFileStream.MaximumMessageSize))
					{
						throw new InvalidFormatException();
					}
				}
			}

			public bool IsMainStreamParser
			{
				get { return isMainStreamParser; }
			}

			protected TextFileStream Stream
			{
				get { return fso; }
			}

			public FileRange.Range? Range
			{
				get { return range; }
			}

			public abstract MessageBase ReadNext();

			public virtual void Dispose()
			{
				if (fso != null)
				{
					fso.DetachParser();
					fso = null;
				}
			}
		};

		protected abstract Encoding GetStreamEncoding(TextFileStreamBase stream);

		protected class TextFileStream : TextFileStreamBase
		{
			Parser currentParser;

			public TextFileStream(StreamBasedPositionedMessagesProvider reader)
				: base(reader.media, reader.headerRe, reader)
			{
			}

			internal void AttachParser(Parser parser, long startPosition)
			{
				if (parser == null)
					throw new ArgumentNullException("parser");
				if (currentParser != null)
					throw new InvalidOperationException("Cannot create more than one parser for a single stream");

				base.BeginReadSession(parser.Range, startPosition);
				currentParser = parser;
			}

			internal void DetachParser()
			{
				if (currentParser == null)
					throw new InvalidOperationException("No parser is attached to the steam. Nothing to detach");
				currentParser = null;
				base.EndReadSession();
			}
		}

		protected TextFileStream FStream
		{
			get
			{
				if (stream == null)
				{
					stream = new TextFileStream(this);
				}
				return stream;
			}
		}

		private bool UpdateMediaSize()
		{
			long tmp = media.Size;
			if (tmp == mediaSize)
				return false;
			mediaSize = tmp;
			return true;
		}

		private static long FindBound(BoundFinder finder, Stream stm, Encoding encoding, string boundName)
		{
			long? pos = finder.Find(stm, encoding);
			if (!pos.HasValue)
				throw new Exception(string.Format("Cannot detect the {0} of the log", boundName));
			return pos.Value;
		}

		private void FindLogicalBounds(bool incrementalMode)
		{
			long newBegin = incrementalMode ? beginPosition : 0;
			long newEnd = mediaSize;

			beginPosition = 0;
			endPosition = mediaSize;
			try
			{
				if (!incrementalMode && beginFinder != null)
				{
					newBegin = FindBound(beginFinder, FStream, FStream.TextEncoding, "beginning");
				}
				if (endFinder != null)
				{
					newEnd = FindBound(endFinder, FStream, FStream.TextEncoding, "end");
				}
			}
			finally
			{
				beginPosition = newBegin;
				endPosition = newEnd;
			}
		}
	};

	class StreamBasedFormatInfo
	{
		public readonly Type LogMediaType;

		public StreamBasedFormatInfo(Type logMediaType)
		{
			LogMediaType = logMediaType;
		}
	};

	class StreamBasedMediaInitParams : MediaInitParams
	{
		public readonly Type ProviderType;
		public readonly StreamBasedFormatInfo FormatInfo;
		public StreamBasedMediaInitParams(Source trace, Type providerType, StreamBasedFormatInfo formatInfo):
			base(trace)
		{
			this.ProviderType = providerType;
			this.FormatInfo = formatInfo;
		}
	};

	internal class StreamLogReader : RangeManagingReader
	{
		ILogMedia media;
		IPositionedMessagesProvider provider;

		public StreamLogReader(
			ILogReaderHost host, 
			ILogReaderFactory factory,
			IConnectionParams connectParams,
			StreamBasedFormatInfo formatInfo,
			Type providerType
		):
			base (host, factory)
		{
			using (host.Trace.NewFrame)
			{
				host.Trace.Info("providerType={0}", providerType);

				this.stats.ConnectionParams.Assign(connectParams);

				media = (ILogMedia)Activator.CreateInstance(
					formatInfo.LogMediaType, connectParams, new StreamBasedMediaInitParams(host.Trace, providerType, formatInfo));

				provider = (IPositionedMessagesProvider)Activator.CreateInstance(
					providerType, this.threads, media, formatInfo);

				StartAsyncReader("Reader thread: " + connectParams.ToString());
			}
		}

		public override void Dispose()
		{
			if (media != null)
			{
				media.Dispose();
			}
			if (provider != null)
			{
				provider.Dispose();
			}
			base.Dispose();
		}

		protected override IPositionedMessagesProvider GetProvider()
		{
			CheckDisposed();
			return provider;
		}
	};
}
