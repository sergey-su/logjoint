using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogJoint;
using System.Reflection;
using System.IO;
using EM = LogJointTests.ExpectedMessage;

namespace LogJointTests
{
	[Flags]
	public enum TestOptions
	{
	};

	public class ExpectedMessage
	{
		public long? Position;
		public string Text;
		public string Thread;
		public DateTime? Date;
		public MessageBase.MessageFlag? Type;
		public MessageBase.MessageFlag? ContentType;
		public int? FrameLevel;
		public Func<DateTime, bool> DateVerifier;
		internal bool Verified;

		public ExpectedMessage()
		{
		}

		public ExpectedMessage(string text, string thread = null, DateTime? date = null) 
		{
			Text = text;
			Thread = thread;
			Date = date;
		}
	};

	public class ExpectedLog
	{
		public ExpectedLog Add(int expectedLine, params ExpectedMessage[] expectedMessages)
		{
			foreach (var m in expectedMessages)
			{
				Assert.IsNotNull(m);
				Assert.IsFalse(this.expectedMessages.ContainsKey(expectedLine));
				this.expectedMessages.Add(expectedLine, m);
				++expectedLine;
			}
			return this;
		}

		public void StartVerification()
		{
			foreach (var m in expectedMessages.Values)
				m.Verified = false;
		}

		public void FinishVerification()
		{
			foreach (var m in expectedMessages)
				Assert.IsTrue(m.Value.Verified, string.Format("Message {0} left unverified", m.Key));
		}

		public void Verify(int actualLine, MessageBase actualMessage, int actualFrameLevel)
		{
			ExpectedMessage expectedMessage;
			if (expectedMessages.TryGetValue(actualLine, out expectedMessage))
			{
				expectedMessage.Verified = true;
				Assert.IsNotNull(actualMessage);
				if (expectedMessage.Date != null)
					Assert.AreEqual(expectedMessage.Date.Value, actualMessage.Time);
				else if (expectedMessage.DateVerifier != null)
					Assert.IsTrue(expectedMessage.DateVerifier(actualMessage.Time));
				if (expectedMessage.Thread != null)
					Assert.AreEqual(expectedMessage.Thread, actualMessage.Thread.ID);
				if (expectedMessage.Type != null)
					Assert.AreEqual(expectedMessage.Type.Value, actualMessage.Flags & MessageBase.MessageFlag.TypeMask);
				if (expectedMessage.ContentType != null)
					Assert.AreEqual(expectedMessage.ContentType.Value, actualMessage.Flags & MessageBase.MessageFlag.ContentTypeMask);
				if (expectedMessage.Text != null)
					Assert.AreEqual(expectedMessage.Text, actualMessage.Text.Value);
				if (expectedMessage.FrameLevel != null)
					Assert.AreEqual(expectedMessage.FrameLevel.Value, actualFrameLevel);
			}
		}

		public int Count { get { return expectedMessages.Count; } }

		Dictionary<int, ExpectedMessage> expectedMessages = new Dictionary<int, ExpectedMessage>();
	};

	public static class ReaderIntegrationTest
	{
		public static void Test(IMediaBasedReaderFactory factory, ILogMedia media, ExpectedLog expectation)
		{
			using (LogSourceThreads threads = new LogSourceThreads())
			using (IPositionedMessagesReader reader = factory.CreateMessagesReader(threads, media))
			{
				reader.UpdateAvailableBounds(false);

				List<MessageBase> msgs = new List<MessageBase>();

				using (var parser = reader.CreateParser(new CreateParserParams(reader.BeginPosition)))
				{
					for (; ; )
					{
						var msg = parser.ReadNext();
						if (msg == null)
							break;
						msgs.Add(msg);
					}
				}

				expectation.StartVerification();
				int frameLevel = 0;
				for (int i = 0; i < msgs.Count; ++i)
				{
					switch (msgs[i].Flags & MessageBase.MessageFlag.TypeMask)
					{
						case MessageBase.MessageFlag.StartFrame:
							++frameLevel;
							break;
						case MessageBase.MessageFlag.EndFrame:
							--frameLevel;
							break;
					}

					expectation.Verify(i, msgs[i], frameLevel);
				}
				expectation.FinishVerification();
			}
		}

		public static void Test(IMediaBasedReaderFactory factory, string testLog, ExpectedLog expectation)
		{
			using (StringStreamMedia media = new StringStreamMedia(testLog, Encoding.ASCII))
			{
				Test(factory, media, expectation);
			}
		}

		public static void Test(IMediaBasedReaderFactory factory, System.IO.Stream testLogStream, ExpectedLog expectation)
		{
			using (StringStreamMedia media = new StringStreamMedia())
			{
				media.SetData(testLogStream);

				Test(factory, media, expectation);
			}
		}
	}

	[TestClass]
	public class TextWriterTraceListenerIntegrationTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			var repo = new ResourcesFormatsRepository(Assembly.GetExecutingAssembly());
			var reg = new LogProviderFactoryRegistry();
			var formatsManager = new UserDefinedFormatsManager(repo, reg);
			LogJoint.RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();
			var factory = reg.Find("Microsoft", "TextWriterTraceListener");
			return factory as IMediaBasedReaderFactory;
		}

		void DoTest(string testLog, ExpectedLog expectedLog)
		{
			ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		void DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			DoTest(testLog, expectedLog);
		}

		[TestMethod]
		public void TextWriterTraceListenerSmokeTest()
		{
			DoTest(
				@"
SampleApp Information: 0 : No free data file found. Going sleep.
  ProcessId=4756
  ThreadId=7
  DateTime=2011-07-12T12:10:34.3694222Z
SampleApp Information: 0 : Searching for data files
  ProcessId=4756
  ThreadId=7
  DateTime=2011-07-12T12:10:34.4294223Z
SampleApp Information: 0 : No free data file found. Going sleep.
  ProcessId=4756
  ThreadId=7
  DateTime=2011-07-12T12:10:34.4294223Z
SampleApp Information: 0 : File cannot be open which means that it was handled
  ProcessId=4756
  ThreadId=6
  DateTime=2011-07-12T12:10:35.3294235Z
SampleApp Start: 0 : Test frame 
  ProcessId=4756
  ThreadId=6
  DateTime=2011-07-12T12:10:35.3294260Z
SampleApp Stop: 0 : 
  ProcessId=4756
  ThreadId=6
  DateTime=2011-07-12T12:10:35.3294260Z
SampleApp Information: 0 : Timestamp parsed and ignored
  ProcessId=4756
  ThreadId=6
  DateTime=2011-07-12T12:10:35.3294235Z
  Timestamp=232398
				",
				new EM("No free data file found. Going sleep.", "4756 - 7"),
				new EM("Searching for data files", "4756 - 7", null),
				new EM("No free data file found. Going sleep.", "4756 - 7", null),
				new EM("File cannot be open which means that it was handled", "4756 - 6", null),
				new EM("Test frame", "4756 - 6", null) { Type = MessageBase.MessageFlag.StartFrame },
				new EM("", "4756 - 6", null) { Type = MessageBase.MessageFlag.EndFrame },
				new EM("Timestamp parsed and ignored", "4756 - 6", null)
			);
		}
		
		[TestMethod]
		public void TextWriterTraceListener_FindPrevMessagePositionTest()
		{
			var testLog = 
@"SampleApp Information: 0 : No free data file found. Going sleep.
  ProcessId=4756
  ThreadId=7
  DateTime=2011-07-12T12:10:00.0000000Z
SampleApp Information: 0 : Searching for data files
  ProcessId=4756
  ThreadId=7
  DateTime=2011-07-12T12:12:00.0000000Z
SampleApp Information: 0 : No free data file found. Going sleep.
  ProcessId=4756
  ThreadId=7
  DateTime=2011-07-12T12:14:00.0000000Z
";
			using (StringStreamMedia media = new StringStreamMedia(testLog, Encoding.ASCII))
			using (LogSourceThreads threads = new LogSourceThreads())
			using (IPositionedMessagesReader reader = CreateFactory().CreateMessagesReader(threads, media))
			{
				reader.UpdateAvailableBounds(false);
				long? prevMessagePos = PositionedMessagesUtils.FindPrevMessagePosition(reader, 0x0000004A);
				Assert.IsTrue(prevMessagePos.HasValue);
				Assert.AreEqual(0, prevMessagePos.Value);
			}
		}


	}

	[TestClass]
	public class XmlWriterTraceListenerIntegrationTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			var repo = new ResourcesFormatsRepository(Assembly.GetExecutingAssembly());
			var reg = new LogProviderFactoryRegistry();
			var formatsManager = new UserDefinedFormatsManager(repo, reg);
			LogJoint.XmlFormat.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();
			var factory = reg.Find("Microsoft", "XmlWriterTraceListener");
			return factory as IMediaBasedReaderFactory;
		}

		void DoTest(string testLog, ExpectedLog expectedLog)
		{
			ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		void DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			DoTest(testLog, new ExpectedLog().Add(0, expectedMessages));
		}

		[TestMethod]
		public void XmlWriterTraceListenerSmokeTest()
		{
			DoTest(
				@"
<E2ETraceEvent xmlns='http://schemas.microsoft.com/2004/06/E2ETraceEvent'>
 <System xmlns='http://schemas.microsoft.com/2004/06/windows/eventlog/system'>
  <EventID>1</EventID>
  <Type>3</Type>
  <SubType Name='Error'>0</SubType>
  <Level>2</Level>
  <TimeCreated SystemTime='2007-01-16T15:20:07.0781250Z' />
  <Source Name='TestApp' />
  <Correlation ActivityID='{00000000-0000-0000-0000-000000000000}' />
  <Execution ProcessName='trace_cs.vshost' ProcessID='5620' ThreadID='10' />
  <Channel/>
  <Computer>TEST</Computer>
 </System>
 <ApplicationData>Error message.</ApplicationData>
</E2ETraceEvent>
<E2ETraceEvent xmlns='http://schemas.microsoft.com/2004/06/E2ETraceEvent'>
 <System xmlns='http://schemas.microsoft.com/2004/06/windows/eventlog/system'>
  <EventID>1</EventID>
  <Type>3</Type>
  <SubType Name='Information'>0</SubType>
  <Level>2</Level>
  <TimeCreated SystemTime='2007-01-16T15:20:07.0781250Z' />
  <Source Name='TestApp' />
  <Correlation ActivityID='{00000000-0000-0000-0000-000000000000}' />
  <Execution ProcessName='trace_cs.vshost' ProcessID='5620' ThreadID='20' />
  <Channel/>
  <Computer>TEST</Computer>
 </System>
 <ApplicationData>message 2</ApplicationData>
</E2ETraceEvent>
				",
				new EM("Error message.", "trace_cs.vshost(5620), 10") { ContentType = MessageBase.MessageFlag.Error },
				new EM("message 2", "trace_cs.vshost(5620), 20") { ContentType = MessageBase.MessageFlag.Info }
			);
		}

		[TestMethod]
		public void RealLogTest()
		{
			ReaderIntegrationTest.Test(
				CreateFactory(),
				Assembly.GetExecutingAssembly().GetManifestResourceStream("logjoint.model.tests.Samples.XmlWriterTraceListener1.xml"),
				new ExpectedLog()
				.Add(0, 
					new EM("Void Main(System.String[])", "SampleLoggingApp(1956), 1") { Type = MessageBase.MessageFlag.StartFrame },
					new EM("----- Sample application started 07/24/2011 12:37:26 ----", "SampleLoggingApp(1956), 1")
				)
				.Add(8,
					new EM("Void Producer()", "SampleLoggingApp(1956), 6") { Type = MessageBase.MessageFlag.StartFrame }
				)
				.Add(11,
					new EM("", "SampleLoggingApp(1956), 1") { Type = MessageBase.MessageFlag.EndFrame }
				)
			);			
		}
	}
}
