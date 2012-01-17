using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using NLog;
using LogJoint.NLog;
using System.Xml.Linq;
using System.Reflection;
using LogJointTests;
using EM = LogJointTests.ExpectedMessage;

namespace logjoint.model.tests
{
	public class TestsContainer: MarshalByRefObject
	{
		public TestsContainer()
		{
			new NLog.Config.LoggingConfiguration(); // referencing NLog to force its load
		}

		string CreateSimpleLogAndInitExpectation(string layout, Action<Logger, LogJointTests.ExpectedLog> loggingCallback, LogJointTests.ExpectedLog expectation)
		{
			var target = new NLog.Targets.MemoryTarget();
			target.Layout = layout;

			NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
			NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, NLog.LogLevel.Debug);

			loggingCallback(NLog.LogManager.GetCurrentClassLogger(), expectation);

			return target.Logs.Cast<string>().Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line)).ToString();
		}

		class TestFormatsRepository : IFormatsRepository, IFormatsRepositoryEntry
		{
			public TestFormatsRepository(XElement formatElement) { this.formatElement = formatElement; }

			public IEnumerable<IFormatsRepositoryEntry> Entries { get { yield return this; } }
			public string Location { get { return "test"; } }
			public DateTime LastModified { get { return new DateTime(); } }
			public XElement LoadFormatDescription() { return formatElement; }

			XElement formatElement;
		};

		void TestLayout(string layout, Action<Logger, LogJointTests.ExpectedLog> loggingCallback)
		{
			var expectedLog = new LogJointTests.ExpectedLog();
			var logContent = CreateSimpleLogAndInitExpectation(layout, loggingCallback, expectedLog);

			var importLog = new ImportLog();
			var formatDocument = new XmlDocument();
			formatDocument.LoadXml(@"<format><regular-grammar/><id company='Test' name='Test'/><description/></format>");
			LayoutImporter.GenerateRegularGrammarElement(formatDocument.SelectSingleNode("format/regular-grammar") as XmlElement, layout, importLog);

			var repo = new TestFormatsRepository(XDocument.Parse(formatDocument.OuterXml).Root);
			LogProviderFactoryRegistry reg = new LogProviderFactoryRegistry();
			UserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg);
			LogJoint.RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();

			LogJointTests.ReaderIntegrationTest.Test(reg.Find("Test", "Test") as IMediaBasedReaderFactory, logContent, expectedLog);
		}

		public void GenerateRegularGrammarElementTest()
		{
			TestLayout(@"${longdate}|${level}|${message}", (logger, expectation) =>
			{
				logger.Debug("Hello world");
				logger.Error("Error");

				expectation.Add(
					0,
					new EM("|Hello world", null) { ContentType = MessageBase.MessageFlag.Info },
					new EM("|Error", null) { ContentType = MessageBase.MessageFlag.Error }
				);
			});
		}
	};

	[TestClass()]
	public class NLogLayoutImporterTest
	{
		[Flags]
		enum TestOptions
		{
			None = 0,
			TestAgainstNLog1 = 1,
			TestAgainstNLog2 = 2,
			Default = TestAgainstNLog1 | TestAgainstNLog2
		};

		void RunTestWithNLogVersion(string testName, string nLogVersion)
		{
			string thisAsmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location)+"\\";
			var tempPath = Guid.NewGuid().ToString();
			Directory.CreateDirectory(thisAsmPath+tempPath);
			try
			{
				using (var nlogAsmDestStream = new FileStream(thisAsmPath + tempPath + "\\NLog.dll", FileMode.CreateNew))
				{
					var nlogSourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
						string.Format("logjoint.model.tests.nlog.{0}.NLog.dll", nLogVersion));
					nlogSourceStream.CopyTo(nlogAsmDestStream);
				}

				var setup = new AppDomainSetup();
				setup.ApplicationBase = thisAsmPath;
				setup.PrivateBinPath = tempPath;

				var domain = AppDomain.CreateDomain(Guid.NewGuid().ToString(), null, setup);
				try
				{
					domain.AppendPrivatePath(tempPath);
					var instance = domain.CreateInstanceAndUnwrap("logjoint.model.tests", typeof(TestsContainer).FullName);
					//var domainAsms = domain.GetAssemblies().Where(asm => asm.FullName.ToLower().Contains("nlog"));
					//var actualNLogVersion = domainAsms.Select(asm => asm.GetName().Version).First();
					instance.GetType().InvokeMember(testName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, instance, new object[] { });
					//Console.WriteLine("{0} is ok with nlog {1}", testName, actualNLogVersion);
				}
				catch
				{
					Console.Error.WriteLine("{0} failed for {1}", testName, nLogVersion);
					throw;
				}
				finally
				{
					AppDomain.Unload(domain);
				}
			}
			finally
			{
				Directory.Delete(thisAsmPath + tempPath, true);
			}
		}

		void RunThisTestAgainstDifferentNLogVersions(TestOptions options = TestOptions.Default, string testName = null)
		{
			if (testName == null)
				testName = new System.Diagnostics.StackFrame(1).GetMethod().Name;
			if ((options & TestOptions.TestAgainstNLog1) != 0)
				RunTestWithNLogVersion(testName, "_1._0");
			if ((options & TestOptions.TestAgainstNLog2) != 0)
				RunTestWithNLogVersion(testName, "_2._0");
		}

		[TestMethod()]
		public void GenerateRegularGrammarElementTest()
		{
			//var s1 = CreateSimpleLog(@"${whenEmpty:whenEmpty=Layout:inner=${shortdate}}", LogBasicLines);
			//var s2 = CreateSimpleLog(@"${shortdate} ${pad:padCharacter= :padding=100:fixedLength=True:inner=${message}}", LogBasicLines);
			// @"aa\}bb\\cc\tdd ${literal:text=S\}t\\r\:ing} ${shortdate} ${pad:padCharacter= :padding=100:fixedLength=True:inner=${message}} xyz"
			RunThisTestAgainstDifferentNLogVersions();
		}

		//renderers to capture:
		//  ${shortdate} // fixed yyyy-MM-dd
		//  ${time} // fixed HH:mm:ss.mmm

		//  ${date} // custom string, is not affected by casing!

		//  ${longdate} // fixed yyyy-MM-dd HH:mm:ss.mmm
		//  ${ticks} // long number new DateTime(ticks)

		//  ${level}  // fixed set of strings
  
		//  ${threadid} // digits
		//  ${threadname} // any string

  
		//wrappers to handle:
		//  ${lowercase}   
		//  ${uppercase} 
		//  ${pad}
		//  ${trim-whitespace}

		// ideas for intergation test:
		//    1. many datetimes in layout   yyyy MM yyyy MM
		//    2. Embedded layouts
		//    3. Significant spaces in layouts (like ${pad:padCharacter= })
		//    4. Single \ at the end of layout string
		//    5. Renderer or param name uppercase
		//    6. Embedded renderers with ambient props
		//    7. Locale specific fields + casing  ${date:lowercase=True:format=yyyy-MM-dd (ddd)}
		//    8. Warnings on conditional interesting fields
		//    9. Warnings on interesting not specific not matchable fields
		//   10. All possible levels
	}
}
