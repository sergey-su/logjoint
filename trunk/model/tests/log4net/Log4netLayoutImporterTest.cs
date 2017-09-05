using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using System.Xml.Linq;
using System.Reflection;
using System.Threading;
using EM = LogJoint.Tests.ExpectedMessage;
using NUnit.Framework;
using LogJoint.Log4net;

namespace LogJoint.Tests.Log4Net
{
	public class TestsContainer: MarshalByRefObject
	{
		Assembly log4NetAsm = Assembly.Load("log4net");
		ITempFilesManager tempFilesManager = new TempFilesManager();

		enum Log4NetVersion
		{
			Ver1,
			Ver2,
		};

		Log4NetVersion CurrentVersion
		{
			get
			{
				switch (log4NetAsm.GetName().Version.Major)
				{
					case 1:
						return Log4NetVersion.Ver1;
					case 2:
						return Log4NetVersion.Ver2;
					default:
						throw new Exception("Invalid Log4net version: " + log4NetAsm.GetName());
				}
			}
		}

		// Wraps reflected calls to ILog 
		class Logger 
		{
			public void Debug(string str) { Impl("Debug", str); }
			public void Info(string str) { Impl("Info", str); }
			public void Warn(string str) { Impl("Warn", str); }
			public void Error(string str) { Impl("Error", str); }
			public void Fatal(string str) { Impl("Fatal", str); }
			void Impl(string method, params object[] p)
			{
				impl.GetType().InvokeMember(method, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, impl, p);
			}
			internal object impl;
		};

		class TestFormatsRepository : IFormatDefinitionsRepository, IFormatDefinitionRepositoryEntry
		{
			public TestFormatsRepository(XElement formatElement) { this.formatElement = formatElement; }

			public IEnumerable<IFormatDefinitionRepositoryEntry> Entries { get { yield return this; } }
			public string Location { get { return "test"; } }
			public DateTime LastModified { get { return new DateTime(); } }
			public XElement LoadFormatDescription() { return formatElement; }

			XElement formatElement;
		};

		void TestLayout(string layout, Action<Logger, ExpectedLog> loggingCallback)
		{
			var logManagerType = log4NetAsm.GetType("log4net.LogManager");
			dynamic hierarchy = logManagerType.InvokeMember("GetRepository", 
				BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null, null);

			dynamic patternLayout = log4NetAsm.CreateInstance("log4net.Layout.PatternLayout");
			patternLayout.ConversionPattern = "%date [%thread] %-5level %logger - %message%newline";
			patternLayout.ActivateOptions();

			TextWriter logWriter = new StringWriter();

			dynamic memory = log4NetAsm.CreateInstance("log4net.Appender.TextWriterAppender");
			memory.Layout = patternLayout;
			memory.Writer = logWriter;
			memory.ActivateOptions();
			hierarchy.Root.AddAppender(memory);

			//hierarchy.Root.Level = Level.Info;
			hierarchy.Configured = true;

			var logger = logManagerType.InvokeMember("GetLogger", 
				BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null, new [] { GetType() });

			var expectedLog = new ExpectedLog();
			loggingCallback(new Logger() { impl = logger }, expectedLog);

			var logContent = logWriter.ToString();

			var formatDocument = CreateTestFormatSkeleton();

			Log4NetPatternImporter.GenerateRegularGrammarElement(formatDocument.DocumentElement, layout);

			ParseAndVerifyLog(expectedLog, logContent, formatDocument);
		}

		private void ParseAndVerifyLog(ExpectedLog expectedLog, string logContent, XmlDocument formatDocument)
		{
			var formatXml = formatDocument.OuterXml;
			var repo = new TestFormatsRepository(XDocument.Parse(formatXml).Root);
			ILogProviderFactoryRegistry reg = new LogProviderFactoryRegistry();
			IUserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg, tempFilesManager);
			RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();

			ReaderIntegrationTest.Test(reg.Find("Test", "Test") as IMediaBasedReaderFactory, logContent, expectedLog, Encoding.UTF8);
		}

		dynamic GetEnumValue(string type, string name)
		{
			var t = log4NetAsm.GetType(type);
			return t.GetEnumValues().GetValue(t.GetEnumNames().IndexOf(n => n == name).Value);
		}

		private static XmlDocument CreateTestFormatSkeleton()
		{
			var formatDocument = new XmlDocument();
			formatDocument.LoadXml(@"<format><regular-grammar><encoding>utf-8</encoding><head-re/><body-re/><fields-config/></regular-grammar><id company='Test' name='Test'/><description/></format>");
			return formatDocument;
		}

		public void SmokeTest()
		{
			TestLayout(@"%date [%thread] %-5level %logger - %message%newline", (logger, expectation) =>
			{
				logger.Debug("Hello world");
				logger.Error("Error");

				expectation.Add(
					0,
					new EM("[Worker#STA_NP] DEBUG LogJoint.Tests.Log4Net.TestsContainer - Hello world", null) { ContentType = MessageFlag.Info },
					new EM("[Worker#STA_NP] ERROR LogJoint.Tests.Log4Net.TestsContainer - Error", null) { ContentType = MessageFlag.Error }
				);
			});
		}
	};

	[TestFixture()]
	public class Log4NetLayoutImporterTest
	{
		struct DomainData
		{
			public AppDomain Domain;
			public string TempLog4NetDir;
			public object TestsContainer;
			public void Dispose()
			{
				AppDomain.Unload(Domain);
				Directory.Delete(TempLog4NetDir, true);
			}
		};

		Dictionary<string, DomainData> log4NetVersionToDomain = new Dictionary<string, DomainData>();

		[OneTimeTearDown]
		public void TearDown()
		{
			foreach (var dom in log4NetVersionToDomain.Values)
				dom.Dispose();
			log4NetVersionToDomain.Clear();
		}

		[Flags]
		enum TestOptions
		{
			None = 0,
			TestAgainstV1Plus = 1,
			TestAgainstV2Plus = 2,
			Default = TestAgainstV1Plus | TestAgainstV2Plus
		};

		void RunTestWithLog4NetVersion(string testName, string log4NetVersion)
		{
			DomainData domain;
			if (!log4NetVersionToDomain.TryGetValue(log4NetVersion, out domain))
			{
				string thisAsmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar;
				string tempLog4NetDirName = "temp-log4net-" + log4NetVersion;
				var tempLog4NetDirPath = thisAsmPath + tempLog4NetDirName + Path.DirectorySeparatorChar;
				domain.TempLog4NetDir = tempLog4NetDirPath;
				if (!File.Exists(tempLog4NetDirPath + "log4net.dll"))
				{
					Directory.CreateDirectory(tempLog4NetDirPath);
					var resName = Assembly.GetExecutingAssembly().GetManifestResourceNames().SingleOrDefault(
						n => n.Contains(string.Format("{0}.log4net.dll", log4NetVersion)));
					var log4NetSourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
					using (var nlogAsmDestStream = new FileStream(tempLog4NetDirPath + "log4net.dll", FileMode.Create))
					{
						log4NetSourceStream.CopyTo(nlogAsmDestStream);
					}
				}

				var setup = new AppDomainSetup();
				setup.ApplicationBase = thisAsmPath;
				setup.PrivateBinPath = log4NetVersion;

				domain.Domain = AppDomain.CreateDomain(log4NetVersion, null, setup);
				domain.Domain.AppendPrivatePath(tempLog4NetDirName);
				domain.TestsContainer = domain.Domain.CreateInstanceAndUnwrap("logjoint.model.tests", typeof(TestsContainer).FullName);

				log4NetVersionToDomain[log4NetVersion] = domain;
			}
			
			try
			{
				domain.TestsContainer.GetType().InvokeMember(testName, 
					BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null,  domain.TestsContainer, null);
				Console.WriteLine("{0} is ok with Log4Net {1}", testName, log4NetVersion);
			}
			catch
			{
				Console.Error.WriteLine("{0} failed for Log4Net {1}", testName, log4NetVersion);
				throw;
			}
		}

		/// <summary>
		/// Actual test code must be added to TestsContainer class. TestsContainer's method name
		/// must be the same as entry point [Test] method name.
		/// </summary>
		void RunThisTestAgainstDifferentLog4NetVersions(TestOptions options = TestOptions.Default, string testName = null)
		{
			if (testName == null)
				testName = new System.Diagnostics.StackFrame(1).GetMethod().Name;
			if ((options & TestOptions.TestAgainstV1Plus) != 0)
				RunTestWithLog4NetVersion(testName, "_1._2");
			if ((options & TestOptions.TestAgainstV2Plus) != 0 || (options & TestOptions.TestAgainstV1Plus) != 0)
				RunTestWithLog4NetVersion(testName, "_2._0");
		}

		[Test]
		public void SmokeTest()
		{
			RunThisTestAgainstDifferentLog4NetVersions();
		}
	}
}
