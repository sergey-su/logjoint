using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using LogJoint;
using LogJointTests;
using EM = LogJointTests.ExpectedMessage;

namespace LogJoint.dmarshal.Tests
{
	[TestClass]
	public class DavidMarshalIntegrationTests
	{
		[TestInitialize]
		public void Init()
		{
			new Extension(); // ensure extension's assembly is loaded
		}

		IMediaBasedReaderFactory CreateReaderFactory()
		{
			var repo = new ResourcesFormatsRepository(System.Reflection.Assembly.GetExecutingAssembly());
			ILogProviderFactoryRegistry reg = new LogProviderFactoryRegistry();
			IUserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg, new TempFilesManager());
			LogJoint.RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();
			return reg.Find("David Marshall", "OSWtop") as IMediaBasedReaderFactory;
		}

		void DoTest(string testLog, ExpectedLog expectedLog)
		{
			ReaderIntegrationTest.Test(CreateReaderFactory(), testLog, expectedLog);
		}

		void DoTest(string testLog, params ExpectedMessage[] expectedMessages)
		{
			ExpectedLog expectedLog = new ExpectedLog();
			expectedLog.Add(0, expectedMessages);
			DoTest(testLog, expectedLog);
		}

		[TestMethod]
		public void SmokeTest()
		{
			DoTest(
				@"
zzz ***Thu Nov 3 08:02:38 EDT 2011 Sample interval: 5 seconds. All measurements in KB (1024 bytes)
top - 08:02:39 XXX
top - 08:02:50 YYY
top - 08:02:56 ZZZ
				",
				new EM("XXX", "", new DateTime(2011, 11, 3, 8 + 4, 02, 39)), // 4 - timezone offset
				new EM("YYY", "", new DateTime(2011, 11, 3, 8 + 4, 02, 50)),
				new EM("ZZZ", "", new DateTime(2011, 11, 3, 8 + 4, 02, 56))
			);
		}

		[TestMethod]
		public void FrenchLocaleTest()
		{
			DoTest(
				@"
zzz ***jeu. nov. 3 08:02:38 CET 2011 Sample interval: 5 seconds. All measurements in KB (1024 bytes)
top - 08:02:39 XXX
				",
				new EM("XXX", "", new DateTime(2011, 11, 3, 8 - 1, 02, 39))
			);
		}

		[TestMethod]
		public void PSTTimeZoneTest()
		{
			DoTest(
				@"
zzz ***Thu Nov 3 08:02:38 PST 2011 Sample interval: 5 seconds. All measurements in KB (1024 bytes)
top - 08:02:39 XXX
				",
				new EM("XXX", "", new DateTime(2011, 11, 3, 8 + 8, 02, 39))
			);
		}

		static string LogFromResource(string resourceName)
		{
			using (var resourceStream = System.Reflection.Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName))
			using (var reader = new System.IO.StreamReader(resourceStream, Encoding.ASCII))
			{
				return reader.ReadToEnd();
			}
		}

		[TestMethod]
		public void RealSizeLogTest()
		{
			DoTest(
				LogFromResource("logjoint.dmarshal.tests.Samples.FQDN_top_11.11.03.0800.dat"),
				new EM(null, "", new DateTime(2011, 11, 3, 8 + 4, 02, 39))
			);			
		}
	}
}
