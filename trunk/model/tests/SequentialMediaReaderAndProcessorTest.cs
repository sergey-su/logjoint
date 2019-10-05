using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Diagnostics;
using LogJoint;
using System.Threading.Tasks;
using System.Linq;
using System.Text.RegularExpressions;
using LJRE = System.Text.RegularExpressions.LogJointVersion;
using NUnit.Framework;

namespace LogJoint.Tests
{
	[TestFixture]
	public class SequentialMediaReaderAndProcessorTest
	{
		class FakeCallback : SequentialMediaReaderAndProcessor<object, object, object>.ICallback
		{
			readonly int items;
			readonly TimeSpan rawDataReadTime;
			readonly TimeSpan dataProcessingTime;
			int current = 0;
			LogJoint.Diagnostics.AverageLong aveActiveWorkItems = new LogJoint.Diagnostics.AverageLong();

			public FakeCallback(int itemsCount, TimeSpan rawDataReadTime, TimeSpan dataProcessingTime)
			{
				this.items = itemsCount;
				this.rawDataReadTime = rawDataReadTime;
				this.dataProcessingTime = dataProcessingTime;
			}

			public int ItemsCount
			{
				get { return items; }
			}

			public static int ItemIdxToData(int idx)
			{
				return idx + 100000;
			}

			public IEnumerable<object> ReadRawDataFromMedia(CancellationToken cancellationToken)
			{
				for (;;)
				{
					if (current >= items)
						break;
					DoFakeJob(rawDataReadTime);
					yield return current++;
				}
			}

			public object InitializeThreadLocalState()
			{
				return null;
			}

			public void FinalizeThreadLocalState(ref object state)
			{
			}

			public object ProcessRawData(object rawData, object state, CancellationToken cancellationToken)
			{
				Assert.IsNotNull(rawData);
				DoFakeJob(dataProcessingTime);
				return ItemIdxToData((int)rawData);
			}

			static readonly Regex re = new Regex(@"\d{2}-\d{2}", RegexOptions.Compiled);

			public static void DoFakeJob(TimeSpan time)
			{
				Stopwatch w = new Stopwatch();
				for (w.Start(); w.ElapsedTicks < time.Ticks; )
				{
					re.Match(w.ToString());
				}
			}
		};

		[Test]
		public void NullIsReturnedWhenEOFReached_CorrectValueOtherwise()
		{
			FakeCallback callback = new FakeCallback(50, TimeSpan.FromMilliseconds(10), TimeSpan.FromMilliseconds(20));
			var reader = new SequentialMediaReaderAndProcessor<object, object, object>(callback, CancellationToken.None);
			for (int i = 0;; ++i)
			{
				object obj = reader.ReadAndProcessNextPieceOfData();
				if (i == callback.ItemsCount)
					Assert.IsNull(obj);
				else
					Assert.AreEqual(FakeCallback.ItemIdxToData(i), (int)obj);
				if (obj == null)
					break;
			}
		}

		[Test]
		public void ProcessingIsMoreExpensiveThanReading()
		{
			FakeCallback callback = new FakeCallback(20, TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(100));
			var reader = new SequentialMediaReaderAndProcessor<object, object, object>(callback, CancellationToken.None);
			for (int i = 0; i < callback.ItemsCount; ++i)
			{
				object obj = reader.ReadAndProcessNextPieceOfData();
				Assert.AreEqual(FakeCallback.ItemIdxToData(i), (int)obj);
				FakeCallback.DoFakeJob(TimeSpan.FromMilliseconds(8));
			}
		}

		[Test]
		public void ProcessingIsMoreExpensiveThanReading_NanosecondsLevel()
		{
			FakeCallback callback = new FakeCallback(100000, TimeSpan.FromTicks(30), TimeSpan.FromTicks(100));
			var reader = new SequentialMediaReaderAndProcessor<object, object, object>(callback, CancellationToken.None);
			for (int i = 0; i < callback.ItemsCount; ++i)
			{
				object obj = reader.ReadAndProcessNextPieceOfData();
				Assert.AreEqual(FakeCallback.ItemIdxToData(i), (int)obj);
				FakeCallback.DoFakeJob(TimeSpan.FromTicks(8));
			}
		}

		[Test]
		public void ProcessingIsLessExpensiveThanReading()
		{
			FakeCallback callback = new FakeCallback(20, TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(20));
			var reader = new SequentialMediaReaderAndProcessor<object, object, object>(callback, CancellationToken.None);
			for (int i = 0; i < callback.ItemsCount; ++i)
			{
				object obj = reader.ReadAndProcessNextPieceOfData();
				Assert.AreEqual(FakeCallback.ItemIdxToData(i), (int)obj);
				FakeCallback.DoFakeJob(TimeSpan.FromMilliseconds(20));
			}
		}

		[Test]
		public void ClientStopsReadingInTheMiddleOfSenquence()
		{
			FakeCallback callback = new FakeCallback(20, TimeSpan.FromMilliseconds(20), TimeSpan.FromMilliseconds(100));
			var reader = new SequentialMediaReaderAndProcessor<object, object, object>(callback, CancellationToken.None);
			for (int i = 0; i < callback.ItemsCount/2; ++i)
			{
				object obj = reader.ReadAndProcessNextPieceOfData();
				Assert.AreEqual(FakeCallback.ItemIdxToData(i), (int)obj);
				FakeCallback.DoFakeJob(TimeSpan.FromMilliseconds(20));
			}
		}

		IEnumerable<byte[]> GetEnum(FileStream fs)
		{
			for (; fs.Position < 32 * 1024 * 1024; )
			{
				byte[] data = new byte[64 * 1024];
				fs.Read(data, 0, data.Length);
				yield return data;
			}
		}

		class TLocal
		{
			public LogJoint.RegularExpressions.IRegex Re;
			public LogJoint.RegularExpressions.IMatch Match;
		};

		[Test]
		public void RunningRegexInParallelTest()
		{
			StringBuilder testBuffer = new StringBuilder();
			int c = 1000;
			for (int i = 0; i < c; ++i)
				testBuffer.Append("2010-04-23 23.22.11.333 This is simple log message\n");
			string testBufferStr = testBuffer.ToString();
			object[] outData = new object[c];

			string reTemplate = @"(?<date>\d{4}-\d{2}-\d{2}\ \d{2}\.\d{2}\.\d{3})";

			StringBuilder resultStr = new StringBuilder();

			LogJoint.RegularExpressions.IRegexFactory factory;
			
			factory = LogJoint.RegularExpressions.FCLRegexFactory.Instance;
			
			Func<LogJoint.RegularExpressions.IRegex> makeRe = () => 
				factory.Create(reTemplate, LogJoint.RegularExpressions.ReOptions.Multiline);

			Func<object> createOutObject = () => new StringBuilder(34);

			var re = makeRe();

			Stopwatch sw = new Stopwatch();
			sw.Start();

			Parallel.For(0, c, () =>
				new TLocal() { Re = makeRe() },
				(idx, state, local) =>
				{
					local.Re.Match(testBufferStr, idx, ref local.Match);
					outData[idx] = createOutObject();
					return local;
				},
				(local) => { });

			sw.Stop();
			long parallelTime = sw.ElapsedMilliseconds;
			resultStr.AppendFormat("parallel: {0}   ", sw.ElapsedMilliseconds.ToString());

			
			LogJoint.RegularExpressions.IMatch match = null;

			sw.Start();

			for (int i = 0; i < c; ++i)
			{
				re.Match(testBufferStr, i, ref match);
				outData[i] = createOutObject();
			};

			sw.Stop();
			long sequesialTime = sw.ElapsedMilliseconds;
			resultStr.AppendFormat("sequential: {0}    ", sw.ElapsedMilliseconds.ToString());

			resultStr.AppendFormat("Parallel is {0:0.00} times faster", (double)sequesialTime / (double)parallelTime);

			Console.Write(resultStr);
		}
	}
}
