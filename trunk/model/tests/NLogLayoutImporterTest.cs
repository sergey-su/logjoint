using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.IO;
//using NLog;
using LogJoint.NLog;
using System.Xml.Linq;
using System.Reflection;
using LogJointTests;
using EM = LogJointTests.ExpectedMessage;

namespace logjoint.model.tests
{
	public class TestsContainer: MarshalByRefObject
	{
		Assembly nlogAsm = Assembly.Load("NLog");

		// Wraps reflected calls to NLog.Logger 
		class Logger 
		{
			public void Debug(string str) { Impl("Debug", str); }
			public void Error(string str) { Impl("Error", str); }
			void Impl(string method, params object[] p)
			{
				impl.GetType().InvokeMember(method, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, impl, p);
			}
			internal object impl;
		};

		// Inits and sets up NLog logger, passes it to given callback. Everything is done via reflection 
		// in order to be able to work with different NLog version choosen at runtime.
		string CreateSimpleLogAndInitExpectation(string layout, Action<Logger, LogJointTests.ExpectedLog> loggingCallback, LogJointTests.ExpectedLog expectation)
		{
			var target = nlogAsm.CreateInstance("NLog.Targets.MemoryTarget");
			object layoutToAssign;
			if (((PropertyInfo)target.GetType().GetMember("Layout")[0]).PropertyType == typeof(string)) // NLog 1.0
			{
				layoutToAssign = layout;
			}
			else // NLog 2.0+
			{
				var layoutType = nlogAsm.GetType("NLog.Layouts.Layout");
				layoutToAssign = layoutType.InvokeMember("FromString", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
					null, null, new object[] { layout });
			}
			target.GetType().InvokeMember("Layout", BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.FlattenHierarchy, 
				null, target, new object[] { layoutToAssign });

			var loggingConfig = nlogAsm.CreateInstance("NLog.Config.LoggingConfiguration");
			var logManagerType = nlogAsm.GetType("NLog.LogManager");
			logManagerType.InvokeMember("Configuration", BindingFlags.Static | BindingFlags.SetProperty | BindingFlags.Public, null, null, new object[] { loggingConfig });

			var simpleConfiguratorType = nlogAsm.GetType("NLog.Config.SimpleConfigurator");
			var logLevelType = nlogAsm.GetType("NLog.LogLevel");
			var traceLevel = logLevelType.InvokeMember("Trace", BindingFlags.Public | BindingFlags.GetField | BindingFlags.Static, null, null, new object[] { });
			simpleConfiguratorType.InvokeMember("ConfigureForTargetLogging", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null,
				new object[] { target, traceLevel });

			var currentClassLogger = logManagerType.InvokeMember("GetCurrentClassLogger", 
				BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, new object[] { });
			loggingCallback(new Logger() { impl = currentClassLogger }, expectation);

			var logs = target.GetType().InvokeMember("Logs", BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.Public, null, target, new object[] {}) 
				as System.Collections.IEnumerable;
			return logs.Cast<string>().Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line)).ToString();
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

		public void SmokeTest()
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

		public void EscapingTest()
		{
			TestLayout(@"${longdate}aa\}bb\\cc\tdd ${literal:text=S\{t\\r\:i\}n\g} ${message}", (logger, expectation) =>
			{
				logger.Debug("qwer");

				expectation.Add(
					0,
					new EM(@"aa\}bb\\cc\tdd S{t\r:i}ng qwer", null)
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
					instance.GetType().InvokeMember(testName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, instance, new object[] { });
					Console.WriteLine("{0} is ok with NLog {1}", testName, nLogVersion);
				}
				catch
				{
					Console.Error.WriteLine("{0} failed for NLog {1}", testName, nLogVersion);
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

		/// <summary>
		/// Actual test code must be added to TestsContainer class. TestsContainer's method name
		/// must be the same as entry point [TestMethod] method name.
		/// </summary>
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
		public void SmokeTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void EscapingTest()
		{
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
