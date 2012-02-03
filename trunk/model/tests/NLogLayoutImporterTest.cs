using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Xml;
using System.IO;
using LogJoint.NLog;
using System.Xml.Linq;
using System.Reflection;
using LogJointTests;
using System.Threading;
using EM = LogJointTests.ExpectedMessage;
using LSL = LogJoint.NLog.ImportLog.Message.LayoutSliceLink;

namespace logjoint.model.tests
{
	public class TestsContainer: MarshalByRefObject
	{
		Assembly nlogAsm = Assembly.Load("NLog");

		enum NLogVersion
		{
			Ver1,
			Ver2
		};

		NLogVersion CurrentVersion
		{
			get
			{
				switch (nlogAsm.GetName().Version.Major)
				{
					case 1:
						return NLogVersion.Ver1;
					case 2:
						return NLogVersion.Ver2;
					default:
						throw new Exception("Invalid NLog version: " + nlogAsm.GetName());
				}
			}
		}

		// Wraps reflected calls to NLog.Logger 
		class Logger 
		{
			public void Trace(string str) { Impl("Trace", str); }
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

		void TestLayout(string layout, Action<Logger, LogJointTests.ExpectedLog> loggingCallback, Action<ImportLog> verifyImportLogCallback = null)
		{
			var expectedLog = new LogJointTests.ExpectedLog();
			var logContent = CreateSimpleLogAndInitExpectation(layout, loggingCallback, expectedLog);

			var importLog = new ImportLog();
			var formatDocument = new XmlDocument();
			formatDocument.LoadXml(@"<format><regular-grammar><encoding>utf-8</encoding></regular-grammar><id company='Test' name='Test'/><description/></format>");

			try
			{
				LayoutImporter.GenerateRegularGrammarElement(formatDocument.SelectSingleNode("format/regular-grammar") as XmlElement, layout, importLog);
			}
			catch (ImportErrorDetectedException)
			{
				Assert.IsTrue(importLog.HasErrors);
				if (verifyImportLogCallback != null)
					verifyImportLogCallback(importLog);
				return;
			}

			if (verifyImportLogCallback != null)
				verifyImportLogCallback(importLog);

			var formatXml = formatDocument.OuterXml;
			var repo = new TestFormatsRepository(XDocument.Parse(formatXml).Root);
			LogProviderFactoryRegistry reg = new LogProviderFactoryRegistry();
			UserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg);
			LogJoint.RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();

			LogJointTests.ReaderIntegrationTest.Test(reg.Find("Test", "Test") as IMediaBasedReaderFactory, logContent, expectedLog, Encoding.UTF8);
		}

		public void SmokeTest()
		{
			TestLayout(@"${longdate}|${level}|${message}", (logger, expectation) =>
			{
				logger.Debug("Hello world");
				logger.Error("Error");

				expectation.Add(
					0,
					new EM("||Hello world", null) { ContentType = MessageBase.MessageFlag.Info },
					new EM("||Error", null) { ContentType = MessageBase.MessageFlag.Error }
				);
			});
		}

		public void NLogWrapperNamesAndParamsAreNotCaseSensitive()
		{
			TestLayout(@"${longdate}${LiTeRal:TeXt=qwe}|${MesSage}", (logger, expectation) =>
			{
				logger.Info("hello");
				logger.Info("world");

				expectation.Add(
					0,
					new EM("qwe|hello", null),
					new EM("qwe|world", null)
				);
			});
		}

		public void InsignificantSpacesInParamsAreIngnored()
		{
			TestLayout(@"${longdate}${literal: text =qwe}|${message}|${counter: increment   =  10   :   sequence  =  alskl :  value  =  1000 }", (logger, expectation) =>
			{
				logger.Info("hello");
				logger.Info("world");

				expectation.Add(
					0,
					new EM("qwe|hello|1000", null),
					new EM("qwe|world|1010", null)
				);
			});
			if (CurrentVersion != NLogVersion.Ver1)
			{
				TestLayout(@"${longdate}+${pad:  padding  = 10  : inner    =${message: lowercase   = true}}", (logger, expectation) =>
				{
					logger.Info("Hello");
					logger.Info("World");

					expectation.Add(
						0,
						new EM("+     hello", null),
						new EM("+     world", null)
					);
				});
			}
		}

		public void SignificantSpacesInParamsAreNotIgnored()
		{
			TestLayout(@"${longdate}+${literal:text=  qwe }|${message}", (logger, expectation) =>
			{
				logger.Info("hello");
				logger.Info("world");

				expectation.Add(
					0,
					new EM("+  qwe |hello", null),
					new EM("+  qwe |world", null)
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

		public void LevelsTest()
		{
			TestLayout(@"${longdate}${level}${message}", (logger, expectation) =>
			{
				logger.Trace("1");
				logger.Debug("2");
				logger.Info("3");
				logger.Warn("4");
				logger.Error("5");
				logger.Fatal("6");

				expectation.Add(
					0,
					new EM(@"1", null) { ContentType = MessageBase.MessageFlag.Info },
					new EM(@"2", null) { ContentType = MessageBase.MessageFlag.Info },
					new EM(@"3", null) { ContentType = MessageBase.MessageFlag.Info },
					new EM(@"4", null) { ContentType = MessageBase.MessageFlag.Warning },
					new EM(@"5", null) { ContentType = MessageBase.MessageFlag.Error },
					new EM(@"6", null) { ContentType = MessageBase.MessageFlag.Error }
				);
			});
		}

		public void NegativePadding()
		{
			TestLayout(@"${longdate} ${pad:padding=-10:inner=${level}}${message}", (logger, expectation) =>
			{
				logger.Trace("hello");
				logger.Info("world");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM(@"hello", null),
					new EM(@"world", null),
					new EM(@"ups", null) { ContentType = MessageBase.MessageFlag.Error }
				);
			});
		}

		public void PositivePadding()
		{
			TestLayout(@"${longdate} ${pad:padding=10:inner=${level}}${message:padding=20}", (logger, expectation) =>
			{
				logger.Trace("hello");
				logger.Info("world");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM(@"hello", null),
					new EM(@"world", null),
					new EM(@"ups", null) { ContentType = MessageBase.MessageFlag.Error }
				);
			});
		}

		public void DefaultPaddingCharacter()
		{
			TestLayout(@"${longdate} ++${pad:padding=10:inner=${message}}--", (logger, expectation) =>
			{
				logger.Info("test");
				logger.Info("test2");

				expectation.Add(
					0,
					new EM(@"++      test--", null),
					new EM(@"++     test2--", null)
				);
			});
		}

		public void NonDefaultPaddingCharacter()
		{
			TestLayout(@"${longdate} ++${pad:padding=10:padCharacter=*:inner=${message}}--", (logger, expectation) =>
			{
				logger.Info("test");
				logger.Info("test2");

				expectation.Add(
					0,
					new EM(@"++******test--", null),
					new EM(@"++*****test2--", null)
				);
			});
		}

		public void AmbientPaddingAttribute()
		{
			TestLayout(@"${longdate:padding=-60} ++${message:padding=10}--", (logger, expectation) =>
			{
				logger.Info("test");
				logger.Info("test2");

				expectation.Add(
					0,
					new EM(@"++      test--", null),
					new EM(@"++     test2--", null)
				);
			});
		}

		public void AmbientPaddingAndPadCharAttributes()
		{
			TestLayout(@"${longdate:padding=-40::padCharacter=*} ++${message:padCharacter=_:padding=10}--", (logger, expectation) =>
			{
				logger.Info("test");
				logger.Info("test2");

				expectation.Add(
					0,
					new EM(@"**************** ++______test--", null),
					new EM(@"**************** ++_____test2--", null)
				);
			});
		}

		public void FixedLengthPaddingMakesInterestingAttrsIgnored()
		{
			TestLayout(@"${longdate} ${pad:padding=10:fixedLength=True:inner=${level}} qwe", (logger, expectation) =>
			{
				logger.Info("test");
				logger.Warn("test2");

				expectation.Add(
					0,
					new EM(@"Info qwe", null),
					new EM(@"Warn qwe", null)
				);
			});
		}

		public void PaddingEmbeddedIntoCasing()
		{
			TestLayout(@"${longdate} ${uppercase:inner=${pad:padding=10:padCharacter=x:inner=|a|${message}}}", (logger, expectation) =>
			{
				logger.Info("test");
				logger.Warn("test2");

				expectation.Add(
					0,
					new EM(@"XXX|A|TEST", null),
					new EM(@"XX|A|TEST2", null)
				);
			});
		}

		public void PaddingAndCasingAsAmbientProps()
		{
			TestLayout(@"${longdate}+${literal:text=Hello:padding=10:padCharacter=X:lowercase=True}|${literal:text=World:uppercase=True:padding=10::padCharacter=x}${level:padding=10:lowercase=True}", (logger, expectation) =>
			{
				logger.Info("Test");
				logger.Warn("Test2");

				expectation.Add(
					0,
					new EM(@"+XXXXXhello|XXXXXWORLD", null) { ContentType = MessageBase.MessageFlag.Info },
					new EM(@"+XXXXXhello|XXXXXWORLD", null) { ContentType = MessageBase.MessageFlag.Warning }
				);
			});
		}

		public void ZeroPadding()
		{
			TestLayout(@"${longdate}${pad:padding=0:inner=${level} ${message}}", (logger, expectation) =>
			{
				logger.Info("Test");
				logger.Warn("Test2");

				expectation.Add(
					0,
					new EM(@"Test", null) { ContentType = MessageBase.MessageFlag.Info },
					new EM(@"Test2", null) { ContentType = MessageBase.MessageFlag.Warning }
				);
			});
		}

		public void EmbeddedPadding()
		{
			TestLayout(@"${longdate}${pad:padding=20:padCharacter=*:inner=${pad:padding=-10:padCharacter=-:inner=${level}}}${message}", (logger, expectation) =>
			{
				logger.Info("Test");
				logger.Warn("Test2");

				expectation.Add(
					0,
					new EM(null, null) { ContentType = MessageBase.MessageFlag.Info },
					new EM(null, null) { ContentType = MessageBase.MessageFlag.Warning }
				);
			});
		}

		static bool CompareDatesWithTolerance(DateTime d1, DateTime d2, TimeSpan tolerance)
		{
			return (d2 - d1) < tolerance;
		}

		static bool CompareDatesWithTolerance(DateTime d1, DateTime d2)
		{
			return CompareDatesWithTolerance(d1, d2, TimeSpan.FromMilliseconds(10));
		}

		public void Longdate()
		{
			TestLayout(@"${longdate}${message}", (logger, expectation) =>
			{
				DateTime d1 = DateTime.Now;
				logger.Info("hi");
				Thread.Sleep(100);
				DateTime d2 = DateTime.Now;
				logger.Warn("there");

				expectation.Add(
					0,
					new EM("hi", null) { DateVerifier = d => CompareDatesWithTolerance(d, d1) },
					new EM("there", null) { DateVerifier = d => CompareDatesWithTolerance(d, d2) }
				);
			},
			importLog =>
			{
				AssertRendererUsageReport(importLog, "${longdate}", 2, 10);
			});
		}

		public void Ticks()
		{
			TestLayout(@"${ticks} ${message}", (logger, expectation) =>
			{
				DateTime d1 = DateTime.Now;
				logger.Info("hi");
				Thread.Sleep(100);
				DateTime d2 = DateTime.Now;
				logger.Warn("there");

				expectation.Add(
					0,
					new EM("hi", null) { DateVerifier = d => CompareDatesWithTolerance(d, d1) },
					new EM("there", null) { DateVerifier = d => CompareDatesWithTolerance(d, d2) }
				);
			},
			importLog =>
			{
				AssertRendererUsageReport(importLog, "${ticks}", 2, 7);
			});
		}

		void AssertRendererUsageReport(ImportLog importLog, string rendererName, int renderStartPosition, int renderEndPosition)
		{
			Assert.IsTrue(importLog.Messages.Any(m =>
				m.Type == ImportLog.MessageType.RendererUsageReport &&
				m.Fragments.Where(f => f is LSL).Cast<LSL>().Any(f => 
					f.Value == rendererName && f.LayoutSliceStart == renderStartPosition && f.LayoutSliceEnd == renderEndPosition)
			), string.Format("Expected {0} at {1}-{2} to used", rendererName, renderStartPosition, renderEndPosition));
		}

		void AssertThereIsRendererUsageReport(ImportLog importLog, string rendererName)
		{
			Assert.IsTrue(importLog.Messages.Any(m =>
				m.Type == ImportLog.MessageType.RendererUsageReport &&
				m.Fragments.Where(f => f is LSL).Cast<LSL>().Any(f => f.Value == rendererName)
			), string.Format("Expected {0} to be used", rendererName));
		}

		void AssertThereIsWarningAboutConditionalImportantField(ImportLog importLog, string rendererName)
		{
			Assert.IsTrue(importLog.Messages.Any(m =>
				m.Type == ImportLog.MessageType.ImportantFieldIsConditional &&
				m.Fragments.Where(f => f is LSL).Cast<LSL>().Any(f => f.Value == rendererName)
			), string.Format("Expected {0} to be repoerted as conditional", rendererName));
		}
		

		public void Shortdate()
		{
			TestLayout(@"${shortdate} ${message}", (logger, expectation) =>
			{
				DateTime d1 = DateTime.Now.Date;
				logger.Info("hi");
				Thread.Sleep(100);
				DateTime d2 = DateTime.Now.Date;
				logger.Warn("there");

				expectation.Add(
					0,
					new EM("hi", null) { DateVerifier = d => CompareDatesWithTolerance(d, d1) },
					new EM("there", null) { DateVerifier = d => CompareDatesWithTolerance(d, d2) }
				);
			}, 
			importLog =>
			{
				Assert.IsTrue(importLog.Messages.Any(m => m.Type == ImportLog.MessageType.NoTimeParsed));
				AssertRendererUsageReport(importLog, "${shortdate}", 2, 11);
			});
		}

		public void Time()
		{
			TestLayout(@"${time} ${message}", (logger, expectation) =>
			{
				DateTime d1 = new DateTime() + DateTime.Now.TimeOfDay;
				logger.Info("hi");
				Thread.Sleep(100);
				DateTime d2 = new DateTime() + DateTime.Now.TimeOfDay;
				logger.Warn("there");

				expectation.Add(
					0,
					new EM("hi", null) { DateVerifier = d => CompareDatesWithTolerance(d, d1) },
					new EM("there", null) { DateVerifier = d => CompareDatesWithTolerance(d, d2) }
				);
			});
		}

		public void ShortdateAndTimeSeparated()
		{
			TestLayout(@"d: ${shortdate} ${message} t: ${time}", (logger, expectation) =>
			{
				DateTime d1 = DateTime.Now;
				logger.Info("hi");
				Thread.Sleep(100);
				DateTime d2 = DateTime.Now;
				logger.Warn("there");

				expectation.Add(
					0,
					new EM(null) { TextVerifier = t => t.Contains("hi"), DateVerifier = d => CompareDatesWithTolerance(d, d1) },
					new EM(null) { TextVerifier = t => t.Contains("there"), DateVerifier = d => CompareDatesWithTolerance(d, d2) }
				);
			},
			importLog =>
			{
				AssertRendererUsageReport(importLog, "${shortdate}", 5, 14);
				AssertThereIsRendererUsageReport(importLog, "${time}");
			});
		}

		public void ShortdateAndTimeAreTakenIntoUseEvenThereIsAConditionalLongdateAtTheBeginning()
		{
			TestLayout(@"${onexception:inner=Exception at ${longdate}} ${shortdate} ${message} ${time}", (logger, expectation) =>
			{
				DateTime d1 = DateTime.Now;
				logger.Info("hi");
				Thread.Sleep(100);
				DateTime d2 = DateTime.Now;
				logger.Warn("there");

				expectation.Add(
					0,
					new EM("hi", null) { DateVerifier = d => CompareDatesWithTolerance(d, d1) },
					new EM("there", null) { DateVerifier = d => CompareDatesWithTolerance(d, d2) }
				);
			},
			importLog =>
			{
				AssertThereIsRendererUsageReport(importLog, "${shortdate}");
				AssertThereIsRendererUsageReport(importLog, "${time}");
			});
		}

		public void FailIfNoDateTimeRenderers()
		{
			TestLayout(@"${level} ${message}", (logger, expectation) =>
			{
			},
			importLog =>
			{
				Assert.AreEqual(1, importLog.Messages.Count(m => m.Type == ImportLog.MessageType.NoDateTimeFound));
			});
		}

		public void FailIfDateTimeRenderersAreConditional()
		{
			TestLayout(@"${level} ${whenEmpty:whenEmpty=${longdate}:inner=${message}}", (logger, expectation) =>
			{
			},
			importLog =>
			{
				AssertThereIsWarningAboutConditionalImportantField(importLog, "${longdate}");
				Assert.AreEqual(1, importLog.Messages.Count(m => m.Type == ImportLog.MessageType.DateTimeCannotBeParsed));
			});
		}

		public void FullySpecifiedDate()
		{
			TestLayout(@"${date:format=yyyy~MM~dd HH*mm*ss.ffff} ${message}", (logger, expectation) =>
			{
				DateTime d1 = DateTime.Now;
				logger.Info("hi");
				Thread.Sleep(100);
				DateTime d2 = DateTime.Now;
				logger.Warn("there");

				expectation.Add(
					0,
					new EM("hi", null) { DateVerifier = d => CompareDatesWithTolerance(d, d1) },
					new EM("there", null) { DateVerifier = d => CompareDatesWithTolerance(d, d2) }
				);
			},
			importLog =>
			{
				AssertRendererUsageReport(importLog, "${date}", 2, 6);
			});
		}

		void TestDateFormatStringAndCulture(string format1, string culture1 = null, string format2 = null, string culture2 = null)
		{
			Func<string, string> makeCultureParam = culture => culture != null ? (":culture=" + culture) : "";
			var layout = new StringBuilder();
			layout.Append(@"${date:format=" + format1 + makeCultureParam(culture1) + "}");
			if (format2 != null)
				layout.Append(@"   ${date:format=" + format2 + makeCultureParam(culture2) + "}");
			layout.Append(@" ${level} ${message}");
			TestLayout(layout.ToString(), (logger, expectation) =>
			{
				DateTime now = DateTime.Now;
				logger.Info("hi!");
				logger.Warn("there?");

				expectation.Add(
					0,
					new EM("hi!", null) { 
						ContentType = MessageBase.MessageFlag.Info, DateVerifier = d => CompareDatesWithTolerance(d, now, TimeSpan.FromMinutes(1)) },
					new EM("there?", null) { 
						ContentType = MessageBase.MessageFlag.Warning, DateVerifier = d => CompareDatesWithTolerance(d, now, TimeSpan.FromMinutes(1)) }
				);
			});
		}

		void TestStdDateFormatStrings(string culture)
		{
			TestDateFormatStringAndCulture("F", culture);
			TestDateFormatStringAndCulture("f", culture);
			TestDateFormatStringAndCulture("d", culture, "T", culture);
			TestDateFormatStringAndCulture("t", culture, "D", culture);
			TestDateFormatStringAndCulture("G", culture);
			TestDateFormatStringAndCulture("g", culture);
			TestDateFormatStringAndCulture("m", culture, "G", culture);
			TestDateFormatStringAndCulture("M", culture, "g", culture);
			TestDateFormatStringAndCulture("O", culture);
			TestDateFormatStringAndCulture("o", culture);
			TestDateFormatStringAndCulture("R", culture);
			TestDateFormatStringAndCulture("r", culture);
			TestDateFormatStringAndCulture("s", culture);
			TestDateFormatStringAndCulture("u", culture);
			TestDateFormatStringAndCulture("U", culture);
			TestDateFormatStringAndCulture("y", culture, "f", culture);
			TestDateFormatStringAndCulture("Y", culture, "O", culture);
		}

		public void TestStdDateFormatStrings_InvariantCulture()
		{
			TestStdDateFormatStrings(null);
		}

		public void TestStdDateFormatStrings_RuCulture()
		{
			TestStdDateFormatStrings("ru-RU");
		}

		public void TestStdDateFormatStrings_JpCulture()
		{
			TestStdDateFormatStrings("ja-JP");
		}

		public void DateAndTimeHaveDifferentCultures()
		{
			TestDateFormatStringAndCulture("D", "ru" /*note: russian has genetive months names*/, "T", "el");
			TestDateFormatStringAndCulture("D", "ja", "T", "el");
		}

		// todo:
		// custom date formats
		// date with no format -> G
		// date with time (t) + date with date (custom)
	};

	[TestClass()]
	public class NLogLayoutImporterTest
	{
		struct DomainData
		{
			public AppDomain Domain;
			public string TempNLogDir;
			public object TestsContainer;
		};

		Dictionary<string, DomainData> nlogVersionToDomain = new Dictionary<string, DomainData>();

		[TestCleanup]
		public void TearDown()
		{
			foreach (var dom in nlogVersionToDomain.Values)
			{
				AppDomain.Unload(dom.Domain);
				Directory.Delete(dom.TempNLogDir, true);
			}
		}

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
			DomainData domain;
			if (!nlogVersionToDomain.TryGetValue(nLogVersion, out domain))
			{
				string thisAsmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\";
				string tempNLogDirName = "temp-nlog-" + nLogVersion;
				var tempNLogDirPath = thisAsmPath + tempNLogDirName + "\\";
				domain.TempNLogDir = tempNLogDirPath;
				if (!File.Exists(tempNLogDirPath + "NLog.dll"))
				{
					Directory.CreateDirectory(tempNLogDirPath);
					using (var nlogAsmDestStream = new FileStream(tempNLogDirPath + "NLog.dll", FileMode.CreateNew))
					{
						var nlogSourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(
							string.Format("logjoint.model.tests.nlog.{0}.NLog.dll", nLogVersion));
						nlogSourceStream.CopyTo(nlogAsmDestStream);
					}
				}

				var setup = new AppDomainSetup();
				setup.ApplicationBase = thisAsmPath;
				setup.PrivateBinPath = nLogVersion;

				domain.Domain = AppDomain.CreateDomain(nLogVersion, null, setup);
				domain.Domain.AppendPrivatePath(tempNLogDirName);
				domain.TestsContainer = domain.Domain.CreateInstanceAndUnwrap("logjoint.model.tests", typeof(TestsContainer).FullName);

				nlogVersionToDomain[nLogVersion] = domain;
			}
				
			try
			{
				domain.TestsContainer.GetType().InvokeMember(testName, BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null, 
					domain.TestsContainer, new object[] { });
				Console.WriteLine("{0} is ok with NLog {1}", testName, nLogVersion);
			}
			catch
			{
				Console.Error.WriteLine("{0} failed for NLog {1}", testName, nLogVersion);
				throw;
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
		public void NLogWrapperNamesAndParamsAreNotCaseSensitive()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void InsignificantSpacesInParamsAreIngnored()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void SignificantSpacesInParamsAreNotIgnored()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}		

		[TestMethod()]
		public void EscapingTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void LevelsTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void NegativePadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void PositivePadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void DefaultPaddingCharacter()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void NonDefaultPaddingCharacter()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void AmbientPaddingAttribute()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void AmbientPaddingAndPadCharAttributes()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void FixedLengthPaddingMakesInterestingAttrsIgnored()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void PaddingEmbeddedIntoCasing()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void PaddingAndCasingAsAmbientProps()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void ZeroPadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void EmbeddedPadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void Longdate()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void Time()
		{
			// {time} seems not to be supported by NLog1?
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void Ticks()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void Shortdate()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void ShortdateAndTimeSeparated()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}

		[TestMethod()]
		public void ShortdateAndTimeAreTakenIntoUseEvenThereIsAConditionalLongdateAtTheBeginning()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2);
		}
		
		[TestMethod()]
		public void FailIfNoDateTimeRenderers()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void FailIfDateTimeRenderersAreConditional()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void FullySpecifiedDate()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void TestStdDateFormatStrings_InvariantCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void TestStdDateFormatStrings_RuCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void TestStdDateFormatStrings_JpCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[TestMethod()]
		public void DateAndTimeHaveDifferentCultures()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		//renderers to capture:
		//  ${level}  // fixed set of strings
  
		//  ${threadid} // digits
		//  ${threadname} // any string

		//wrappers to handle:
		//  ${lowercase}   
		//  ${uppercase} 
		//  ${trim-whitespace}

		// ideas for intergation test:
		//    1. many datetimes in layout   yyyy MM yyyy MM
		//    4. Single \ at the end of layout string
		//    6. Embedded renderers with ambient props
		//    7. Locale specific fields + casing  ${date:lowercase=True:format=yyyy-MM-dd (ddd)}
		//    8. Warnings on conditional interesting fields
		//    9. Warnings on interesting not specific not matchable fields
	}
}
