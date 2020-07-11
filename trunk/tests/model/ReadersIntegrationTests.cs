using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using LogJoint;
using System.Reflection;
using System.IO;
using EM = LogJoint.Tests.ExpectedMessage;
using NUnit.Framework;
using System.Xml.Linq;
using NSubstitute;
using System.Threading.Tasks;

namespace LogJoint.Tests
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
		public MessageTimestamp? Date;
		public MessageFlag? Type;
		public MessageFlag? ContentType;
		public Func<MessageTimestamp, bool> DateVerifier;
		public Func<string, bool> TextVerifier;
		internal bool Verified;
		public bool TextNeedsNormalization;

		public ExpectedMessage()
		{
		}

		public ExpectedMessage(string text, string thread = null, DateTime? date = null) 
		{
			Text = text;
			Thread = thread;
			Date = null;
			if (date != null)
				Date = new MessageTimestamp(date.Value);
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

		public void Verify(int actualLine, IMessage actualMessage)
		{
			if (expectedMessages.TryGetValue(actualLine, out EM expectedMessage))
			{
				expectedMessage.Verified = true;
				Assert.IsNotNull(actualMessage);
				if (expectedMessage.Date != null)
					Assert.IsTrue(MessageTimestamp.EqualStrict(expectedMessage.Date.Value, actualMessage.Time),
						string.Format("Expected message timestamp: {0}, actual: {1}", expectedMessage.Date.Value, actualMessage.Time));
				else if (expectedMessage.DateVerifier != null)
					Assert.IsTrue(expectedMessage.DateVerifier(actualMessage.Time));
				if (expectedMessage.Thread != null)
					Assert.AreEqual(expectedMessage.Thread, actualMessage.Thread.ID);
				if (expectedMessage.ContentType != null)
					Assert.AreEqual(expectedMessage.ContentType.Value, actualMessage.Flags & MessageFlag.ContentTypeMask);
				if (expectedMessage.Text != null)
					if (expectedMessage.TextNeedsNormalization)
						Assert.AreEqual(StringUtils.NormalizeLinebreakes(expectedMessage.Text), StringUtils.NormalizeLinebreakes(actualMessage.Text.Value));
					else
						Assert.AreEqual(expectedMessage.Text, actualMessage.Text.Value);
				else if (expectedMessage.TextVerifier != null)
					Assert.IsTrue(expectedMessage.TextVerifier(actualMessage.Text.Value));
			}
		}

		public int Count { get { return expectedMessages.Count; } }

		Dictionary<int, ExpectedMessage> expectedMessages = new Dictionary<int, ExpectedMessage>();
	};

	static class Mocks
	{
		public static FieldsProcessor.IFactory SetupFieldsProcessorFactory()
		{
			var storageManager = Substitute.For<Persistence.IStorageManager>();
			var cacheEntry = Substitute.For<Persistence.IStorageEntry>();
			storageManager.GetEntry(null, 0).ReturnsForAnyArgs(cacheEntry);
			var cacheSection = Substitute.For<Persistence.IRawStreamStorageSection>();
			cacheSection.Data.Returns(new MemoryStream());
			cacheEntry.OpenRawStreamSection(null, Persistence.StorageSectionOpenFlag.None, 0).ReturnsForAnyArgs(cacheSection);
			return new FieldsProcessor.FieldsProcessorImpl.Factory(storageManager, Substitute.For<Telemetry.ITelemetryCollector>(), null);
		}
	};

	public static class ReaderIntegrationTest
	{
		static ITempFilesManager tempFilesManager = new TempFilesManager();

		public static IMediaBasedReaderFactory CreateFactoryFromAssemblyResource(Assembly asm, string companyName, string formatName)
		{
			var repo = new DirectoryFormatsRepository(Path.Combine(Path.GetDirectoryName(asm.Location), "formats"));
			ILogProviderFactoryRegistry reg = new LogProviderFactoryRegistry();
			IUserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg, tempFilesManager,
				new TraceSourceFactory(), RegularExpressions.FCLRegexFactory.Instance,
				Mocks.SetupFieldsProcessorFactory());
			LogJoint.RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			LogJoint.XmlFormat.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();
			var factory = reg.Find(companyName, formatName);
			Assert.IsNotNull(factory);
			return factory as IMediaBasedReaderFactory;
		}

		public static async Task Test(IMediaBasedReaderFactory factory, ILogMedia media, ExpectedLog expectation)
		{
			using (ILogSourceThreadsInternal threads = new LogSourceThreads())
			using (IPositionedMessagesReader reader = factory.CreateMessagesReader(new MediaBasedReaderParams(threads, media)))
			{
				await reader.UpdateAvailableBounds(false);

				List<IMessage> msgs = new List<IMessage>();

				await DisposableAsync.Using(await reader.CreateParser(new CreateParserParams(reader.BeginPosition)), async parser =>
				{
					for (; ; )
					{
						var msg = await parser.ReadNext();
						if (msg == null)
							break;
						msgs.Add(msg);
					}
				});

				expectation.StartVerification();
				for (int i = 0; i < msgs.Count; ++i)
				{
					expectation.Verify(i, msgs[i]);
				}
				expectation.FinishVerification();
			}
		}

		public static async Task Test(IMediaBasedReaderFactory factory, string testLog, ExpectedLog expectation)
		{
			await Test(factory, testLog, expectation, Encoding.ASCII);
		}

		public static async Task Test(IMediaBasedReaderFactory factory, string testLog, ExpectedLog expectation, Encoding encoding)
		{
			using (StringStreamMedia media = new StringStreamMedia(testLog, encoding))
			{
				await Test(factory, media, expectation);
			}
		}

		public static async Task Test(IMediaBasedReaderFactory factory, System.IO.Stream testLogStream, ExpectedLog expectation)
		{
			using (StringStreamMedia media = new StringStreamMedia())
			{
				media.SetData(testLogStream);

				await Test(factory, media, expectation);
			}
		}
	}

	[TestFixture]
	public class TextWriterTraceListenerIntegrationTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			return ReaderIntegrationTest.CreateFactoryFromAssemblyResource(Assembly.GetExecutingAssembly(), "Microsoft", "TextWriterTraceListener");
		}

		async Task DoTest(string testLog, ExpectedLog expectedLog)
		{
			await ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		async Task DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			await DoTest(testLog, expectedLog);
		}

		[Test]
		public async Task TextWriterTraceListenerSmokeTest()
		{
			await DoTest(
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
				new EM("Timestamp parsed and ignored", "4756 - 6", null),
				new EM("Test frame", "4756 - 6", null),
				new EM("", "4756 - 6", null)
			);
		}
		
		[Test]
		public async Task TextWriterTraceListener_FindPrevMessagePositionTest()
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
			using (ILogSourceThreadsInternal threads = new LogSourceThreads())
			using (IPositionedMessagesReader reader = CreateFactory().CreateMessagesReader(new MediaBasedReaderParams(threads, media)))
			{
				await reader.UpdateAvailableBounds(false);
				long? prevMessagePos = await PositionedMessagesUtils.FindPrevMessagePosition(reader, 0x0000004A);
				Assert.IsTrue(prevMessagePos.HasValue);
				Assert.AreEqual(0, prevMessagePos.Value);
			}
		}


	}

	[TestFixture]
	public class XmlWriterTraceListenerIntegrationTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			return ReaderIntegrationTest.CreateFactoryFromAssemblyResource(Assembly.GetExecutingAssembly(),
				"Microsoft", "XmlWriterTraceListener");
		}

		async Task DoTest(string testLog, ExpectedLog expectedLog)
		{
			await ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		async Task DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			await DoTest(testLog, new ExpectedLog().Add(0, expectedMessages));
		}

		[Test]
		public async Task XmlWriterTraceListenerSmokeTest()
		{
			await DoTest(
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
				new EM("Error message.", "trace_cs.vshost(5620), 10") { ContentType = MessageFlag.Error },
				new EM("message 2", "trace_cs.vshost(5620), 20") { ContentType = MessageFlag.Info }
			);
		}

		[Test]
		public async Task RealLogTest()
		{
			await ReaderIntegrationTest.Test(
				CreateFactory(),
				Assembly.GetExecutingAssembly().GetManifestResourceStream(
					Assembly.GetExecutingAssembly().GetManifestResourceNames().SingleOrDefault(n => n.Contains("XmlWriterTraceListener1.xml"))),
				new ExpectedLog()
				.Add(0, 
					new EM("Void Main(System.String[])", "SampleLoggingApp(1956), 1"),
					new EM("----- Sample application started 07/24/2011 12:37:26 ----", "SampleLoggingApp(1956), 1")
				)
				.Add(8,
					new EM("Void Producer()", "SampleLoggingApp(1956), 6")
				)
				.Add(11,
					new EM("", "SampleLoggingApp(1956), 1")
				)
			);			
		}
	}

	[TestFixture]
	public class HTTPERRIntegrationTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			return ReaderIntegrationTest.CreateFactoryFromAssemblyResource(Assembly.GetExecutingAssembly(), "Microsoft", "HTTPERR");
		}

		async Task DoTest(string testLog, ExpectedLog expectedLog)
		{
			await ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		async Task DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			await DoTest(testLog, expectedLog);
		}

		[Test]
		public async Task HTTPERR_SmokeTest()
		{
			await DoTest(
				@"
#Software: Microsoft HTTP API 2.0
#Version: 1.0
#Date: 2011-12-08 06:06:19
#Fields: date time c-ip c-port s-ip s-port cs-version cs-method cs-uri sc-status s-siteid s-reason s-queuename
2011-12-08 06:06:19 192.168.150.1 2774 192.168.150.122 2869 HTTP/1.1 NOTIFY /upnp/eventing/gerpeyxyas - - Connection_Abandoned_By_ReqQueue -
#Software: Microsoft HTTP API 2.0
#Version: 1.0
#Date: 2012-02-06 13:31:17
#Fields: date time c-ip c-port s-ip s-port cs-version cs-method cs-uri sc-status s-siteid s-reason s-queuename
2012-02-06 13:31:17 2001:4898:0:fff:0:5efe:10.164.167.30%0 54697 2001:4898:0:fff:0:5efe:10.85.220.4%0 80 - - - - - Timer_ConnectionIdle -
2012-02-06 13:31:17 2001:4898:0:fff:0:5efe:10.164.167.30%0 54699 2001:4898:0:fff:0:5efe:10.85.220.4%0 80 - - - - - Timer_ConnectionIdle -
2012-02-06 13:45:43 2001:4898:0:fff:0:5efe:10.164.167.30%0 54856 2001:4898:0:fff:0:5efe:10.85.220.4%0 80 - - - - - Timer_ConnectionIdle -
2012-02-06 13:51:58 2001:4898:0:fff:0:5efe:10.164.167.30%0 54863 2001:4898:0:fff:0:5efe:10.85.220.4%0 80 - - - - - Timer_ConnectionIdle -
2012-02-06 13:59:18 2001:4898:0:fff:0:5efe:10.164.167.30%0 54865 2001:4898:0:fff:0:5efe:10.85.220.4%0 80 - - - - - Timer_ConnectionIdle -
2012-02-06 14:11:23 2001:4898:0:fff:0:5efe:10.164.167.30%0 54875 2001:4898:0:fff:0:5efe:10.85.220.4%0 80 - - - - - Timer_ConnectionIdle -
#Software: Microsoft HTTP API 2.0
#Version: 1.0
#Date: 2012-02-29 12:48:58
#Fields: date time c-ip c-port s-ip s-port cs-version cs-method cs-uri sc-status s-siteid s-reason s-queuename
2012-02-29 12:48:58 10.36.206.59 50228 10.85.220.5 80 HTTP/1.0 GET / 404 - NotFound -
2012-02-29 12:49:34 10.36.206.59 50330 10.85.220.5 80 HTTP/1.1 GET / 404 - NotFound -
2012-02-29 12:49:50 10.36.206.59 50422 10.85.220.5 80 HTTP/1.1 GET / 404 - NotFound -
				",
				new EM("Client: 192.168.150.1:2774, Server: 192.168.150.122:2869, Protocol: HTTP/1.1, Verb: NOTIFY, URL: /upnp/eventing/gerpeyxyas, Status: -, SideID: -, Reason: Connection_Abandoned_By_ReqQueue", null, new DateTime(2011, 12, 8, 6, 6, 19)),
				new EM("Client: 2001:4898:0:fff:0:5efe:10.164.167.30%0:54697, Server: 2001:4898:0:fff:0:5efe:10.85.220.4%0:80, Protocol: -, Verb: -, URL: -, Status: -, SideID: -, Reason: Timer_ConnectionIdle", null, new DateTime(2012, 2, 6, 13, 31, 17))
			);
		}
	}

	[TestFixture]
	public class IISIntegrationTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			return ReaderIntegrationTest.CreateFactoryFromAssemblyResource(Assembly.GetExecutingAssembly(), "Microsoft", "IIS");
		}

		async Task DoTest(string testLog, ExpectedLog expectedLog)
		{
			await ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		async Task DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			await DoTest(testLog, expectedLog);
		}

		[Test]
		public async Task IIS_SmokeTest()
		{
			await DoTest(
@"192.168.114.201, -, 03/20/01, 7:55:20, W3SVC2, SERVER, 172.21.13.45, 4502, 163, 3223, 200, 0, GET, /DeptLogo.gif, -,
192.168.110.54, -, 03/20/01, 7:57:20, W3SVC2, SERVER, 172.21.13.45, 411, 221, 1967, 200, 0, GET, /style.css, -,
192.168.1.109, -, 6/10/2009, 10:11:59, W3SVC1893743816, SPUTNIK01, 192.168.1.109, 0, 261, 1913, 401, 2148074254, GET, /, -, 
192.168.1.109, -, 6/10/2009, 10:11:59, W3SVC1893743816, SPUTNIK01, 192.168.1.109, 15, 363, 2113, 401, 0, GET, /, -, 
192.168.1.109, NT AUTHORITY\LOCAL SERVICE, 6/10/2009, 10:11:59, W3SVC1893743816, SPUTNIK01, 192.168.1.109, 46, 379, 336, 200, 0, GET, /, -, 
192.168.1.109, -, 6/10/2009, 10:11:59, W3SVC1893743816, SPUTNIK01, 192.168.1.109, 0, 336, 1889, 401, 2148074254, POST, /_vti_bin/sitedata.asmx, -,
				",
				new EM("ClientIP=192.168.114.201, Service=W3SVC2, Server=SERVER, ServerIP=172.21.13.45, TimeTaken=4502, ClientBytes=163, ServerBytes=3223, ServiceStatus=200, WindowsStatus=0, Request=GET, Target=/DeptLogo.gif, body=", null, new DateTime(2001, 3, 20, 7, 55, 20)),
				new EM("ClientIP=192.168.110.54, Service=W3SVC2, Server=SERVER, ServerIP=172.21.13.45, TimeTaken=411, ClientBytes=221, ServerBytes=1967, ServiceStatus=200, WindowsStatus=0, Request=GET, Target=/style.css, body=", null, new DateTime(2001, 3, 20, 7, 57, 20)) { ContentType = MessageFlag.Info },
				new EM("ClientIP=192.168.1.109, Service=W3SVC1893743816, Server=SPUTNIK01, ServerIP=192.168.1.109, TimeTaken=0, ClientBytes=261, ServerBytes=1913, ServiceStatus=401, WindowsStatus=2148074254, Request=GET, Target=/, body=", null, new DateTime(2009, 6, 10, 10, 11, 59)) { ContentType = MessageFlag.Warning }
			);
		}

		[Test]
		public async Task IIS7_Test()
		{
			await DoTest(
@"::1, -, 2/23/2013, 12:12:46, W3SVC1, MSA3644463, ::1, 324, 285, 935, 200, 0, GET, /, -,
::1, -, 2/23/2013, 12:12:46, W3SVC1, MSA3644463, ::1, 5, 337, 185196, 200, 0, GET, /welcome.png, -,
::1, -, 2/23/2013, 12:12:46, W3SVC1, MSA3644463, ::1, 3, 238, 5375, 404, 2, GET, /favicon.ico, -,
::1, -, 2/23/2013, 12:12:50, W3SVC1, MSA3644463, ::1, 1, 238, 5375, 404, 2, GET, /favicon.ico, -,
",
				new EM("ClientIP=::1, Service=W3SVC1, Server=MSA3644463, ServerIP=::1, TimeTaken=324, ClientBytes=285, ServerBytes=935, ServiceStatus=200, WindowsStatus=0, Request=GET, Target=/, body=", null, new DateTime(2013, 2, 23, 12, 12, 46)),
				new EM("ClientIP=::1, Service=W3SVC1, Server=MSA3644463, ServerIP=::1, TimeTaken=5, ClientBytes=337, ServerBytes=185196, ServiceStatus=200, WindowsStatus=0, Request=GET, Target=/welcome.png, body=", null, new DateTime(2013, 2, 23, 12, 12, 46)),
				new EM("ClientIP=::1, Service=W3SVC1, Server=MSA3644463, ServerIP=::1, TimeTaken=3, ClientBytes=238, ServerBytes=5375, ServiceStatus=404, WindowsStatus=2, Request=GET, Target=/favicon.ico, body=", null, new DateTime(2013, 2, 23, 12, 12, 46)) { ContentType = MessageFlag.Warning },
				new EM("ClientIP=::1, Service=W3SVC1, Server=MSA3644463, ServerIP=::1, TimeTaken=1, ClientBytes=238, ServerBytes=5375, ServiceStatus=404, WindowsStatus=2, Request=GET, Target=/favicon.ico, body=", null, new DateTime(2013, 2, 23, 12, 12, 50)) { ContentType = MessageFlag.Warning }
			);
		}

	}

	[TestFixture]
	public class WindowsUpdateIntegrationTests
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			return ReaderIntegrationTest.CreateFactoryFromAssemblyResource(Assembly.GetExecutingAssembly(), "Microsoft", "WindowsUpdate.log");
		}

		async Task DoTest(string testLog, ExpectedLog expectedLog)
		{
			await ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		async Task DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			await DoTest(testLog, expectedLog);
		}

		[Test]
		public async Task WindowsUpdate_SmokeTest()
		{
			await DoTest(
				@"
2013-01-27	10:55:33:204	1160	3ca0	DnldMgr	  * BITS job initialized, JobId = {082DB2AF-902B-4457-810C-62B6E2D3A034}
2013-01-27	10:55:33:207	1160	3ca0	DnldMgr	  * Downloading from http://sup-eu1-nlb.europe.corp.microsoft.com/Content/E7/BA6933C31C37166A9CAAC87AA635AB5A5BFDF7E7.exe to C:\windows\SoftwareDistribution\Download\29e9d7b4b531db72a29aea5b8094b5cd\ba6933c31c37166a9caac87aa635ab5a5bfdf7e7 (full file).
2013-01-27	10:55:33:210	1160	3ca0	Agent	*********
2013-01-27	10:55:33:210	1160	3ca0	Agent	**  END  **  Agent: Downloading updates [CallerId = AutomaticUpdates]
2013-01-27	10:55:33:210	1160	3ca0	Agent	*************
2013-01-27	10:55:33:210	1160	2320	AU	Successfully wrote event for AU health state:0
2013-01-27	10:55:38:171	1160	3ca0	Report	REPORT EVENT: {023764A7-9115-43D9-966E-18496EE41A09}	2013-01-27 10:55:33:171+0100	1	147	101	{00000000-0000-0000-0000-000000000000}	0	0	AutomaticUpdates	Success	Software Synchronization	Windows Update Client successfully detected 1 updates.
2013-01-27	10:55:38:171	1160	3ca0	Report	REPORT EVENT: {96655A05-A1D9-450B-8A1A-FBFE75A860C3}	2013-01-27 10:55:33:172+0100	1	156	101	{00000000-0000-0000-0000-000000000000}	0	0	AutomaticUpdates	Success	Pre-Deployment Check	Reporting client status.
2013-01-27	10:55:38:171	1160	3ca0	Report	CWERReporter finishing event handling. (00000000)
2013-01-27	10:55:44:276	1160	4348	DnldMgr	BITS job {082DB2AF-902B-4457-810C-62B6E2D3A034} completed successfully
				",
				new EM(@"DnldMgr   * BITS job initialized, JobId = {082DB2AF-902B-4457-810C-62B6E2D3A034}", "Process: 1160; Thread: 3ca0", new DateTime(2013, 1, 27, 10, 55, 33, 204)),
				new EM(@"DnldMgr   * Downloading from http://sup-eu1-nlb.europe.corp.microsoft.com/Content/E7/BA6933C31C37166A9CAAC87AA635AB5A5BFDF7E7.exe to C:\windows\SoftwareDistribution\Download\29e9d7b4b531db72a29aea5b8094b5cd\ba6933c31c37166a9caac87aa635ab5a5bfdf7e7 (full file).", "Process: 1160; Thread: 3ca0", new DateTime(2013, 1, 27, 10, 55, 33, 207)),
				new EM(@"Agent *********", "Process: 1160; Thread: 3ca0", new DateTime(2013, 1, 27, 10, 55, 33, 210)),
				new EM(@"Agent **  END  **  Agent: Downloading updates [CallerId = AutomaticUpdates]", "Process: 1160; Thread: 3ca0", new DateTime(2013, 1, 27, 10, 55, 33, 210)),
				new EM(@"Agent *************", "Process: 1160; Thread: 3ca0", new DateTime(2013, 1, 27, 10, 55, 33, 210)),
				new EM(@"AU Successfully wrote event for AU health state:0", "Process: 1160; Thread: 2320", new DateTime(2013, 1, 27, 10, 55, 33, 210))
			);
		}
	}

	[TestFixture]
	public class W3CExtendedLogFormatTest
	{
		IMediaBasedReaderFactory CreateFactory()
		{
			return ReaderIntegrationTest.CreateFactoryFromAssemblyResource(Assembly.GetExecutingAssembly(), "W3C", "Extended Log Format");
		}

		async Task DoTest(string testLog, ExpectedLog expectedLog)
		{
			await ReaderIntegrationTest.Test(CreateFactory(), testLog, expectedLog);
		}

		async Task DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			await DoTest(testLog, expectedLog);
		}

		[Test]
		public async Task W3CExtendedLogFormat_SmokeTest()
		{
			await DoTest(
@"#Software: Microsoft Internet Information Services 7.5
#Version: 1.0
#Date: 2013-02-07 08:35:37
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) sc-status sc-substatus sc-win32-status time-taken
2013-02-07 08:35:37 fe80::5d3d:c591:3026:46ee%14 OPTIONS /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 200 0 0 340
2013-02-07 08:35:37 fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 4
2013-02-07 08:35:37 fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32 - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 1",
				new EM("fe80::5d3d:c591:3026:46ee%14 OPTIONS /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 200 0 0 340", null, new DateTime(2013, 02, 07, 8, 35, 37)),
				new EM("fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 4", null, new DateTime(2013, 02, 07, 8, 35, 37))
			);

			await DoTest(
@"#Software: Microsoft Internet Information Services 7.5
#Version: 1.0
#Date: 2013-02-07 08:35:37
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) sc-status sc-substatus sc-win32-status time-taken
2013-02-07 08:35 fe80::5d3d:c591:3026:46ee%14 OPTIONS /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 200 0 0 340
2013-02-07 08:35 fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 4
2013-02-07 08:35 fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32 - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 1",
				new EM("fe80::5d3d:c591:3026:46ee%14 OPTIONS /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 200 0 0 340", null, new DateTime(2013, 02, 07, 8, 35, 0)),
				new EM("fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 4", null, new DateTime(2013, 02, 07, 8, 35, 0))
			);

			await DoTest(
@"#Software: Microsoft Internet Information Services 7.5
#Version: 1.0
#Date: 2013-02-07 08:35:37
#Fields: date time s-ip cs-method cs-uri-stem cs-uri-query s-port cs-username c-ip cs(User-Agent) sc-status sc-substatus sc-win32-status time-taken
2013-02-07 08:35:37.234 fe80::5d3d:c591:3026:46ee%14 OPTIONS /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 200 0 0 340
2013-02-07 08:35:37.235 fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 4
2013-02-07 08:35:37.678 fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32 - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 1",
				new EM("fe80::5d3d:c591:3026:46ee%14 OPTIONS /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 200 0 0 340", null, new DateTime(2013, 02, 07, 8, 35, 37, 234)),
				new EM("fe80::5d3d:c591:3026:46ee%14 PROPFIND /System32/TPHDEXLG64.exe - 80 - fe80::5d3d:c591:3026:46ee%14 Microsoft-WebDAV-MiniRedir/6.1.7601 404 0 2 4", null, new DateTime(2013, 02, 07, 8, 35, 37, 235))
			);

		}

	}

	class SingleEntryFormatsRepository : IFormatDefinitionsRepository, IFormatDefinitionRepositoryEntry
	{
		public SingleEntryFormatsRepository(string formatDescription)
		{
			this.formatElement = XDocument.Parse(formatDescription).Root;
		}

		public IEnumerable<IFormatDefinitionRepositoryEntry> Entries { get { yield return this; } }
		public string Location { get { return "test"; } }
		public DateTime LastModified { get { return new DateTime(); } }
		public XElement LoadFormatDescription() { return formatElement; }

		XElement formatElement;
	};

	[TestFixture]
	public class JsonReaderTests
	{
		IMediaBasedReaderFactory CreateFactory(string formatDescription)
		{
			var repo = new SingleEntryFormatsRepository(formatDescription);
			ITempFilesManager tempFilesManager = new TempFilesManager();
			ILogProviderFactoryRegistry reg = new LogProviderFactoryRegistry();
			IUserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg, tempFilesManager,
				new TraceSourceFactory(), RegularExpressions.FCLRegexFactory.Instance, Mocks.SetupFieldsProcessorFactory());
			JsonFormat.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();
			var factory = reg.Items.FirstOrDefault();
			Assert.IsNotNull(factory);
			return factory as IMediaBasedReaderFactory;
		}

		async Task DoTest(string formatDescription, string testLog, params EM[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			await ReaderIntegrationTest.Test(CreateFactory(formatDescription), testLog, expectedLog);
		}

		[Test]
		public async Task SerilogJsonTest()
		{
			await DoTest(
				@"
<format>
  <json>
    <head-re><![CDATA[^\{\""@t\""\:]]></head-re>
    <transform><![CDATA[{ ""d"": ""#valueof($.@t)"", ""m"": ""#ifcondition(#exists($.@mt),true,#valueof($.@mt),#valueof($.@m))"" }]]></transform>
    <encoding>utf-8</encoding>
  </json>
  <id company=""Test"" name=""JSON"" />
</format>",
				@"
{""@t"":""2018-05-22T20:25:35.9680000Z"",""@mt"":""Hello world""}
{""@t"":""2018-05-22T20:25:35.9960000Z"",""@m"":""Foo bar""}
{""@t"":""2018-05-22T20:25:35.9980000Z""}
{""@t"":""2018-05-22T20:25:35.9990000Z"",""@mt"":""Multiline\nstuff""}
				",
				new EM("Hello world", null, new DateTime(2018, 5, 22, 20, 25, 35, 968, DateTimeKind.Utc)),
				new EM("Foo bar", null, new DateTime(2018, 5, 22, 20, 25, 35, 996, DateTimeKind.Utc)),
				new EM("", null, new DateTime(2018, 5, 22, 20, 25, 35, 998, DateTimeKind.Utc)),
				new EM("Multiline\nstuff", null, new DateTime(2018, 5, 22, 20, 25, 35, 999, DateTimeKind.Utc))
			);
		}

		[Test]
		public async Task CustomJsonFunctionsTest()
		{
			await DoTest(
				@"
<format>
  <json>
    <head-re><![CDATA[^\{\ \""time\""\:]]></head-re>
    <transform><![CDATA[{ ""d"": ""#customfunction(logjoint.model,LogJoint.Json.Functions.TO_DATETIME,#valueof($.time),yyyy-MM-dd HH:mm:ss.ffff)"", ""m"": ""#valueof($.nested.message)"", ""s"": ""#substring(#valueof($.level),0,1)"" }]]></transform>
    <encoding>utf-8</encoding>
  </json>
  <id company=""Test"" name=""JSON"" />
</format>",
			@"
{ ""time"": ""2017-09-03 22:23:14.5340"", ""level"": ""INFO"", ""nested"": { ""message"": ""Hello world"" } }
{ ""time"": ""2017-09-03 22:23:14.5590"", ""level"": ""WARN"", ""nested"": { ""message"": ""Foo\nbar"" } }
{ ""time"": ""2017-09-03 22:23:14.5610"", ""level"": ""INFO"", ""nested"": {  } }
",
				new EM("Hello world", null, new DateTime(2017, 9, 3, 22, 23, 14, 534, DateTimeKind.Unspecified)) { ContentType = MessageFlag.Info },
				new EM("Foo\nbar", null, new DateTime(2017, 9, 3, 22, 23, 14, 559, DateTimeKind.Unspecified)) { ContentType = MessageFlag.Warning },
				new EM("", null, new DateTime(2017, 9, 3, 22, 23, 14, 561, DateTimeKind.Unspecified)) { ContentType = MessageFlag.Info }
			);
		}

		[Test]
		public async Task MalformedInput_ExtraCharachtersBetweenObjects()
		{
			await DoTest(
				@"
<format>
  <json>
    <head-re><![CDATA[^\{\""@t\""\:]]></head-re>
    <transform><![CDATA[{ ""d"": ""#valueof($.@t)"", ""m"": ""#ifcondition(#exists($.@mt),true,#valueof($.@mt),#valueof($.@m))"" }]]></transform>
    <encoding>utf-8</encoding>
  </json>
  <id company=""Test"" name=""JSON"" />
</format>",
				@"
{""@t"":""2018-05-22T20:25:35.9680000Z"",""@mt"":""Hello world""}
not a json line
another not a json line
json-looking stuff {0} {{}{}{}{{{}
{""@t"":""2018-05-22T20:25:35.9960000Z"",""@m"":""Foo bar""}
				",
				new EM("Hello world", null, new DateTime(2018, 5, 22, 20, 25, 35, 968, DateTimeKind.Utc)),
				new EM("Foo bar", null, new DateTime(2018, 5, 22, 20, 25, 35, 996, DateTimeKind.Utc))
			);
		}
	}
}
