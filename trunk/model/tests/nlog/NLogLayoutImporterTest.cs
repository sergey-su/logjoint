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
using LSL = LogJoint.NLog.ImportLog.Message.LayoutSliceLink;
using NUnit.Framework;
using LogJoint.NLog;

namespace LogJoint.Tests.NLog
{
	public class TestsContainer: MarshalByRefObject
	{
		Assembly nlogAsm = Assembly.Load("NLog");
		ITempFilesManager tempFilesManager = new TempFilesManager(new TraceSourceFactory());

		enum NLogVersion
		{
			Ver1,
			Ver2,
			Ver4
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
					case 4:
						return NLogVersion.Ver4;
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
			public void Exception(Exception e, string str) { Impl("Error", e, str); }
			void Impl(string method, params object[] p)
			{
				impl.GetType().InvokeMember(method, BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.Public, null, impl, p);
			}
			internal object impl;
		};

		private object CreateMemoryTarget(object layoutToAssign)
		{
			var target = nlogAsm.CreateInstance("NLog.Targets.MemoryTarget");
			target.GetType().InvokeMember(CurrentVersion == NLogVersion.Ver1 ? "CompiledLayout" : "Layout",
				BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.Public | BindingFlags.FlattenHierarchy,
				null, target, new object[] { layoutToAssign });
			return target;
		}

		private static string ReadMemoryTargetLogs(dynamic target)
		{
			System.Collections.IEnumerable logs = target.Logs;
			return logs.Cast<string>().Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line)).ToString();
		}

		// Creates NLog logger wrapper that is configured to accept all log levels
		private Logger ConfigureAndCreateLogger(object target)
		{
			var loggingConfig = nlogAsm.CreateInstance("NLog.Config.LoggingConfiguration");
			var logManagerType = nlogAsm.GetType("NLog.LogManager");
			logManagerType.InvokeMember("Configuration", BindingFlags.Static | BindingFlags.SetProperty | BindingFlags.Public, null, null, new object[] { loggingConfig });
			var simpleConfiguratorType = nlogAsm.GetType("NLog.Config.SimpleConfigurator");
			var logLevelType = nlogAsm.GetType("NLog.LogLevel");
			var traceLevel = logLevelType.InvokeMember("Trace", BindingFlags.Public | BindingFlags.GetField | BindingFlags.Static, null, null, null);

			simpleConfiguratorType.InvokeMember("ConfigureForTargetLogging", BindingFlags.Static | BindingFlags.InvokeMethod | BindingFlags.Public, null, null,
				new object[] { target, traceLevel });

			var currentClassLogger = logManagerType.InvokeMember("GetCurrentClassLogger",
				BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod, null, null, null);
			var logger = new Logger() { impl = currentClassLogger };
			return logger;
		}

		class TestFormatsRepository : IFormatDefinitionsRepository, IFormatDefinitionRepositoryEntry
		{
			public TestFormatsRepository(XElement formatElement) { this.formatElement = formatElement; }

			public IEnumerable<IFormatDefinitionRepositoryEntry> Entries { get { yield return this; } }
			public string Location { get { return "test"; } }
			public DateTime LastModified { get { return new DateTime(); } }
			public XElement LoadFormatDescription() { return formatElement; }

			XElement formatElement;
		};

		void TestSimpleLayout(string layout, Action<Logger, ExpectedLog> loggingCallback, Action<ImportLog> verifyImportLogCallback = null)
		{
			object layoutObject = CreateSimpleLayoutObject(layout);

			object memoryLogTarget = CreateMemoryTarget(layoutObject);
			var expectedLog = new ExpectedLog();
			loggingCallback(ConfigureAndCreateLogger(memoryLogTarget), expectedLog);

			var logContent = ReadMemoryTargetLogs(memoryLogTarget);

			var importLog = new ImportLog();
			var formatDocument = CreateTestFormatSkeleton();

			try
			{
				LayoutImporter.GenerateRegularGrammarElementForSimpleLayout(formatDocument.DocumentElement, layout, importLog);
			}
			catch (ImportErrorDetectedException)
			{
				Assert.IsTrue(importLog.HasErrors);
				verifyImportLogCallback?.Invoke(importLog);
				return;
			}

			verifyImportLogCallback?.Invoke(importLog);

			ParseAndVerifyLog(expectedLog, logContent, formatDocument);
		}

		private dynamic CreateSimpleLayoutObject(string layoutString)
		{
			if (CurrentVersion == NLogVersion.Ver1)
				return nlogAsm.CreateInstance("NLog.Layout", false,
					BindingFlags.Instance | BindingFlags.Public, null, new object[] { layoutString }, null, null);
			else
				return nlogAsm.GetType("NLog.Layouts.Layout").InvokeMember(
					"FromString", BindingFlags.Public | BindingFlags.Static | BindingFlags.InvokeMethod,
					null, null, new object[] { layoutString });
		}

		private void ParseAndVerifyLog(ExpectedLog expectedLog, string logContent, XmlDocument formatDocument)
		{
			var formatXml = formatDocument.OuterXml;
			var repo = new TestFormatsRepository(XDocument.Parse(formatXml).Root);
			ILogProviderFactoryRegistry reg = new LogProviderFactoryRegistry();
			IUserDefinedFormatsManager formatsManager = new UserDefinedFormatsManager(repo, reg, tempFilesManager, new TraceSourceFactory());
			RegularGrammar.UserDefinedFormatFactory.Register(formatsManager);
			formatsManager.ReloadFactories();

			ReaderIntegrationTest.Test(reg.Find("Test", "Test") as IMediaBasedReaderFactory, logContent, expectedLog, Encoding.UTF8);
		}

		dynamic GetEnumValue(string type, string name)
		{
			var t = nlogAsm.GetType(type);
			return t.GetEnumValues().GetValue(t.GetEnumNames().IndexOf(n => n == name).Value);
		}

		void TestCSVLayout(
			string[] columnLayouts, 
			Action<Logger, ExpectedLog> loggingCallback, 
			Action<ImportLog> verifyImportLogCallback = null,
			CsvParams.QuotingMode quoting = CsvParams.QuotingMode.Auto,
			char quoteChar = '"',
			string delimiter = CsvParams.AutoDelimiter
		)
		{
			dynamic layoutObject = nlogAsm.CreateInstance("NLog.Layouts.CsvLayout");
			dynamic columnsObject = layoutObject.Columns;
			foreach (var columnLayoutStr in columnLayouts)
			{
				dynamic columnObject = nlogAsm.CreateInstance("NLog.Layouts.CsvColumn");
				dynamic columnLayoutObject = CreateSimpleLayoutObject(columnLayoutStr);
				if (CurrentVersion == NLogVersion.Ver1)
					columnObject.CompiledLayout = columnLayoutObject;
				else
					columnObject.Layout = columnLayoutObject;
				columnsObject.Add(columnObject);
			}
			layoutObject.Quoting = GetEnumValue("NLog.Layouts.CsvQuotingMode",
				quoting == CsvParams.QuotingMode.Always ? "All" :
				quoting == CsvParams.QuotingMode.Never ? "Nothing" :
				"Auto"
			);
			layoutObject.QuoteChar = new string(quoteChar, 1);
			string delimiterEnumName =
				delimiter == CsvParams.AutoDelimiter ? "Auto" :
				delimiter == "," ? "Comma" :
				delimiter == ";" ? "Semicolon" :
				delimiter == "\t" ? "Tab" :
				delimiter == " " ? "Space" :
				"Custom";
			layoutObject.Delimiter = GetEnumValue(
				CurrentVersion == NLogVersion.Ver1 ? "NLog.Layouts.CsvLayout+ColumnDelimiterMode" : "NLog.Layouts.CsvColumnDelimiterMode", 
				delimiterEnumName
			);
			if (delimiterEnumName == "Custom")
				layoutObject.CustomColumnDelimiter = delimiter;
				

			object memoryLogTargetObject = CreateMemoryTarget(layoutObject);
			var expectedLog = new ExpectedLog();
			loggingCallback(ConfigureAndCreateLogger(memoryLogTargetObject), expectedLog);

			var logContent = ReadMemoryTargetLogs(memoryLogTargetObject);

			var importLog = new ImportLog();
			var formatDocument = CreateTestFormatSkeleton();

			var importerParams = new CsvParams()
			{
				Quoting = quoting,
				QuoteChar = quoteChar,
				Delimiter = delimiter
			};
			foreach (var columnLayoutStr in columnLayouts)
				importerParams.ColumnLayouts.Add(importerParams.ColumnLayouts.Count.ToString(), columnLayoutStr);

			try
			{
				LayoutImporter.GenerateRegularGrammarElementForCSVLayout(formatDocument.DocumentElement, importerParams, importLog);
			}
			catch (ImportErrorDetectedException)
			{
				Assert.IsTrue(importLog.HasErrors);
				verifyImportLogCallback?.Invoke(importLog);
				return;
			}

			verifyImportLogCallback?.Invoke(importLog);

			ParseAndVerifyLog(expectedLog, logContent, formatDocument);
		}

		dynamic CreateJsonLayout(
			JsonParams.Layout layout
		)
		{
			dynamic layoutObject = nlogAsm.CreateInstance("NLog.Layouts.JsonLayout");
			dynamic attributesObject = layoutObject.Attributes;
			foreach (var attribute in layout.Attrs)
			{
				dynamic attributeObject = nlogAsm.CreateInstance("NLog.Layouts.JsonAttribute");
				attributeObject.Name = attribute.Key;
				attributeObject.Layout = attribute.Value.SimpleLayout != null ?
					CreateSimpleLayoutObject(attribute.Value.SimpleLayout) :
					CreateJsonLayout(attribute.Value.JsonLayout);
				attributeObject.Encode = attribute.Value.Encode;
				attributeObject.EscapeUnicode = attribute.Value.EscapeUnicode;
				attributesObject.Add(attributeObject);
			}
			layoutObject.SuppressSpaces = layout.SuppressSpaces;
			layoutObject.IncludeAllProperties = layout.IncludeAllProperties;
			layoutObject.IncludeMdlc = layout.IncludeMdlc;
			layoutObject.IncludeMdc = layout.IncludeMdc;
			layoutObject.RenderEmptyObject = layout.RenderEmptyObject;
			return layoutObject;
		}

		void TestJsonLayout(
			JsonParams jsonParams,
			Action<Logger, ExpectedLog> loggingCallback,
			Action<ImportLog> verifyImportLogCallback = null
		)
		{
			object memoryLogTargetObject = CreateMemoryTarget(CreateJsonLayout(jsonParams.Root));
			var expectedLog = new ExpectedLog();
			loggingCallback(ConfigureAndCreateLogger(memoryLogTargetObject), expectedLog);

			var logContent = ReadMemoryTargetLogs(memoryLogTargetObject);

			var importLog = new ImportLog();
			XmlDocument formatDocument = CreateTestFormatSkeleton();

			try
			{
				LayoutImporter.GenerateRegularGrammarElementForJsonLayout(formatDocument.DocumentElement, jsonParams, importLog);
			}
			catch (ImportErrorDetectedException)
			{
				Assert.IsTrue(importLog.HasErrors);
				verifyImportLogCallback?.Invoke(importLog);
				return;
			}

			verifyImportLogCallback?.Invoke(importLog);

			ParseAndVerifyLog(expectedLog, logContent, formatDocument);
		}

		private static XmlDocument CreateTestFormatSkeleton()
		{
			var formatDocument = new XmlDocument();
			formatDocument.LoadXml(@"<format><regular-grammar><encoding>utf-8</encoding></regular-grammar><id company='Test' name='Test'/><description/></format>");
			return formatDocument;
		}

		public void SmokeTest()
		{
			TestSimpleLayout(@"${longdate}|${level}|${message}", (logger, expectation) =>
			{
				logger.Debug("Hello world");
				logger.Error("Error");

				expectation.Add(
					0,
					new EM("Hello world", null) { ContentType = MessageFlag.Info },
					new EM("Error", null) { ContentType = MessageFlag.Error }
				);
			});
		}

		public void NLogWrapperNamesAndParamsAreNotCaseSensitive()
		{
			TestSimpleLayout(@"${longdate}${LiTeRal:TeXt=qwe}|${MesSage}", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate}${literal: text =qwe}|${message}|${counter: increment   =  10   :   sequence  =  alskl :  value  =  1000 }", (logger, expectation) =>
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
				TestSimpleLayout(@"${longdate}+${pad:  padding  = 10  : inner    =${message: lowercase   = true}}", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate}+${literal:text=  qwe }|${message}", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate}aa\}bb\\cc\tdd ${literal:text=S\{t\\r\:i\}n\g} ${level} ${message}", (logger, expectation) =>
			{
				logger.Debug("qwer");

				expectation.Add(
					0,
					new EM(@"aa\}bb\\cc\tdd S{t\r:i}ng  qwer", null)
				);
			});
		}

		public void LevelsTest()
		{
			TestSimpleLayout(@"${longdate}${level}${message}", (logger, expectation) =>
			{
				logger.Trace("1");
				logger.Debug("2");
				logger.Info("3");
				logger.Warn("4");
				logger.Error("5");
				logger.Fatal("6");

				expectation.Add(
					0,
					new EM(@"1", null) { ContentType = MessageFlag.Info },
					new EM(@"2", null) { ContentType = MessageFlag.Info },
					new EM(@"3", null) { ContentType = MessageFlag.Info },
					new EM(@"4", null) { ContentType = MessageFlag.Warning },
					new EM(@"5", null) { ContentType = MessageFlag.Error },
					new EM(@"6", null) { ContentType = MessageFlag.Error }
				);
			});
		}

		public void NegativePadding()
		{
			TestSimpleLayout(@"${longdate} ${pad:padding=-10:inner=${level}}${message}", (logger, expectation) =>
			{
				logger.Trace("hello");
				logger.Info("world");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM(@"hello", null),
					new EM(@"world", null),
					new EM(@"ups", null) { ContentType = MessageFlag.Error }
				);
			});
		}

		public void PositivePadding()
		{
			TestSimpleLayout(@"${longdate} ${pad:padding=10:inner=${level}}${message:padding=20}", (logger, expectation) =>
			{
				logger.Trace("hello");
				logger.Info("world");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM(@"hello", null),
					new EM(@"world", null),
					new EM(@"ups", null) { ContentType = MessageFlag.Error }
				);
			});
		}

		public void DefaultPaddingCharacter()
		{
			TestSimpleLayout(@"${longdate} ++${pad:padding=10:inner=${message}}--", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate} ++${pad:padding=10:padCharacter=*:inner=${message}}--", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate:padding=-60} ++${message:padding=10}--", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate:padding=-40::padCharacter=*} ++${message:padCharacter=_:padding=10}--", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate} ${pad:padding=10:fixedLength=True:inner=${level}} qwe", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate} ${uppercase:inner=${pad:padding=10:padCharacter=x:inner=|a|${message}}}", (logger, expectation) =>
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
			TestSimpleLayout(@"${longdate}+${literal:text=Hello:padding=10:padCharacter=X:lowercase=True}|${literal:text=World:uppercase=True:padding=10::padCharacter=x}${level:padding=10:lowercase=True}", (logger, expectation) =>
			{
				logger.Info("Test");
				logger.Warn("Test2");

				expectation.Add(
					0,
					new EM(@"+XXXXXhello|XXXXXWORLD", null) { ContentType = MessageFlag.Info },
					new EM(@"+XXXXXhello|XXXXXWORLD", null) { ContentType = MessageFlag.Warning }
				);
			});
		}

		public void ZeroPadding()
		{
			TestSimpleLayout(@"${longdate}${pad:padding=0:inner=${level} ${message}}", (logger, expectation) =>
			{
				logger.Info("Test");
				logger.Warn("Test2");

				expectation.Add(
					0,
					new EM(@"Test", null) { ContentType = MessageFlag.Info },
					new EM(@"Test2", null) { ContentType = MessageFlag.Warning }
				);
			});
		}

		public void EmbeddedPadding()
		{
			TestSimpleLayout(@"${longdate}${pad:padding=20:padCharacter=*:inner=${pad:padding=-10:padCharacter=-:inner=${level}}}${message}", (logger, expectation) =>
			{
				logger.Info("Test");
				logger.Warn("Test2");

				expectation.Add(
					0,
					new EM(null, null) { ContentType = MessageFlag.Info },
					new EM(null, null) { ContentType = MessageFlag.Warning }
				);
			});
		}

		static bool CompareDatesWithTolerance(MessageTimestamp d1, DateTime d2, TimeSpan tolerance)
		{
			return (d2 - d1.ToLocalDateTime()) < tolerance;
		}

		static bool CompareDatesWithTolerance(MessageTimestamp d1, DateTime d2)
		{
			return CompareDatesWithTolerance(d1, d2, TimeSpan.FromMilliseconds(10));
		}

		public void Longdate()
		{
			TestSimpleLayout(@"${longdate}${message}", (logger, expectation) =>
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
				AssertThereIsRendererUsageReport(importLog, "${longdate}", 2, 10);
			});
		}

		public void Ticks()
		{
			TestSimpleLayout(@"${ticks} ${message}", (logger, expectation) =>
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
				AssertThereIsRendererUsageReport(importLog, "${ticks}", 2, 7);
			});
		}

		void AssertThereIsRendererUsageReport(ImportLog importLog, string rendererName, int renderStartPosition, int renderEndPosition)
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
			TestSimpleLayout(@"${shortdate} ${message}", (logger, expectation) =>
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
				AssertThereIsRendererUsageReport(importLog, "${shortdate}", 2, 11);
			});
		}

		public void Time()
		{
			TestSimpleLayout(@"${time} ${message}", (logger, expectation) =>
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
			TestSimpleLayout(@"d: ${shortdate} ${message} t: ${time}", (logger, expectation) =>
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
				AssertThereIsRendererUsageReport(importLog, "${shortdate}", 5, 14);
				AssertThereIsRendererUsageReport(importLog, "${time}");
			});
		}

		public void ShortdateAndTimeAreTakenIntoUseEvenThereIsAConditionalLongdateAtTheBeginning()
		{
			TestSimpleLayout(@"${onexception:inner=Exception at ${longdate}} ${shortdate} ${message} ${time}", (logger, expectation) =>
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
			TestSimpleLayout(@"${level} ${message}", (logger, expectation) =>
			{
			},
			importLog =>
			{
				Assert.AreEqual(1, importLog.Messages.Count(m => m.Type == ImportLog.MessageType.NoDateTimeFound));
			});
		}

		public void FailIfDateTimeRenderersAreConditional()
		{
			TestSimpleLayout(@"${level} ${whenEmpty:whenEmpty=${longdate}:inner=${message}}", (logger, expectation) =>
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
			TestSimpleLayout(@"${date:format=yyyy~MM~dd HH*mm*ss.ffff} ${message}", (logger, expectation) =>
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
				AssertThereIsRendererUsageReport(importLog, "${date}", 2, 6);
			});
		}

		void TestDateTimeFormatStringAndCulture(string format1, string culture1 = null, string format2 = null, string culture2 = null)
		{
			Func<string, string> makeCultureParam = culture => culture != null ? (":culture=" + culture) : "";
			var layout = new StringBuilder();
			layout.Append(@"${date:format=" + format1 + makeCultureParam(culture1) + "}");
			if (format2 != null)
				layout.Append(@"   ${date:format=" + format2 + makeCultureParam(culture2) + "}");
			layout.Append(@" ${level} ${message}");
			TestSimpleLayout(layout.ToString(), (logger, expectation) =>
			{
				DateTime now = DateTime.Now;
				logger.Info("hi!");
				logger.Warn("there?");

				expectation.Add(
					0,
					new EM("hi!", null) { 
						ContentType = MessageFlag.Info, DateVerifier = d => CompareDatesWithTolerance(d, now, TimeSpan.FromMinutes(1)) },
					new EM("there?", null) { 
						ContentType = MessageFlag.Warning, DateVerifier = d => CompareDatesWithTolerance(d, now, TimeSpan.FromMinutes(1)) }
				);
			});
		}

		void TestStdDateTimeFormatStrings(string culture)
		{
			TestDateTimeFormatStringAndCulture("F", culture);
			TestDateTimeFormatStringAndCulture("f", culture);
			TestDateTimeFormatStringAndCulture("d", culture, "T", culture);
			TestDateTimeFormatStringAndCulture("t", culture, "D", culture);
			TestDateTimeFormatStringAndCulture("G", culture);
			TestDateTimeFormatStringAndCulture("g", culture);
			TestDateTimeFormatStringAndCulture("m", culture, "G", culture);
			TestDateTimeFormatStringAndCulture("M", culture, "g", culture);
			TestDateTimeFormatStringAndCulture("O", culture);
			TestDateTimeFormatStringAndCulture("o", culture);
			TestDateTimeFormatStringAndCulture("R", culture);
			TestDateTimeFormatStringAndCulture("r", culture);
			TestDateTimeFormatStringAndCulture("s", culture);
			TestDateTimeFormatStringAndCulture("u", culture);
			TestDateTimeFormatStringAndCulture("U", culture);
			TestDateTimeFormatStringAndCulture("y", culture, "f", culture);
			TestDateTimeFormatStringAndCulture("Y", culture, "O", culture);
		}

		public void TestStdDateFormatStrings_InvariantCulture()
		{
			TestStdDateTimeFormatStrings(null);
		}

		public void TestStdDateFormatStrings_RuCulture()
		{
			TestStdDateTimeFormatStrings("ru-RU");
		}

		public void TestStdDateFormatStrings_JpCulture()
		{
			TestStdDateTimeFormatStrings("ja-JP");
		}

		public void DateAndTimeHaveDifferentCultures()
		{
			TestDateTimeFormatStringAndCulture("D", "ru" /*note: russian has genetive months names*/, "T", "el");
			TestDateTimeFormatStringAndCulture("D", "ja", "T", "el");
			TestDateTimeFormatStringAndCulture("t", "zh", "d", "se");
		}

		void TestCustomDateTimeFormatStrings(string culture)
		{
			TestDateTimeFormatStringAndCulture("yyyy&MMMM^d#HH $mm$ss!fffff", culture);
			TestDateTimeFormatStringAndCulture("yy MM d MMMM dd H-mm-ss", culture);
			TestDateTimeFormatStringAndCulture("y MM dd", culture, "HH^mm^ss~fff", culture);
		}

		public void TestCustomDateTimeFormatStrings_InvariantCulture()
		{
			TestCustomDateTimeFormatStrings(null);
		}

		public void TestCustomDateTimeFormatStrings_RuCulture()
		{
			TestCustomDateTimeFormatStrings("ru-RU");
		}

		public void TestCustomDateTimeFormatStrings_JpCulture()
		{
			TestCustomDateTimeFormatStrings("ja-JP");
		}

		public void EmptyDateFormat()
		{
			TestDateTimeFormatStringAndCulture("");
		}

		public void LocaleDependentDateWithCasing()
		{
			TestSimpleLayout("${date:uppercase=True:format=yyyy-MM-dd (ddd)} ${date:format=HH.mm.ss.fff} ${message}", (logger, expectation) =>
			{
				DateTime now = DateTime.Now;
				logger.Info("hi!");
				logger.Warn("there?");

				expectation.Add(
					0,
					new EM("hi!", null) { DateVerifier = d => CompareDatesWithTolerance(d, now) },
					new EM("there?", null) { DateVerifier = d => CompareDatesWithTolerance(d, now) }
				);
			});
		}

		public void TheOnlyNonConditionalLevelRenderer()
		{
			TestSimpleLayout("${longdate} ${level} ${message}", (logger, expectation) =>
			{
				logger.Info("hi");
				logger.Warn("there");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM("hi", null) { ContentType = MessageFlag.Info },
					new EM("there", null) { ContentType = MessageFlag.Warning },
					new EM("ups", null) { ContentType = MessageFlag.Error }
				);
			});
		}

		public void ConditionalLevelRendererFollowedByUnconditionalLevelRenderer()
		{
			TestSimpleLayout("${longdate} ${onexception:inner=Exception in ${level}} ${level} ${message}", (logger, expectation) =>
			{
				logger.Info("hi");
				logger.Warn("there");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM("hi", null) { ContentType = MessageFlag.Info },
					new EM("there", null) { ContentType = MessageFlag.Warning },
					new EM("ups", null) { ContentType = MessageFlag.Error }
				);
			});
		}
		
		public void ManyConditionalLevelRenderers()
		{
			TestSimpleLayout("${longdate} ${when:when=starts-with('${message}', 'h'):inner=h message ${level}}${when:when=starts-with('${message}', 't'):inner=t message ${level}} ${message}", (logger, expectation) =>
			{
				logger.Fatal("hi");
				logger.Warn("there");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM("h message  hi", null) { ContentType = MessageFlag.Error },
					new EM("t message  there", null) { ContentType = MessageFlag.Warning },
					new EM("ups", null) { ContentType = MessageFlag.Info } // severity of this message is not captured because both conditional ${level}s were not triggered
				);
			});
		}

		public void LevelRendererAndCasing()
		{
			TestSimpleLayout("${longdate} ${level:uppercase=True} ${message}", (logger, expectation) =>
			{
				logger.Trace("hi");
				logger.Warn("there");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM("hi", null) { ContentType = MessageFlag.Info },
					new EM("there", null) { ContentType = MessageFlag.Warning },
					new EM("ups", null) { ContentType = MessageFlag.Error }
				);
			});
		}

		public void TheOnlyNonConditionalThreadRenderer()
		{
			TestSimpleLayout("${longdate} ${threadname} ${message}", (logger, expectation) =>
			{
				Thread t;
				t = new Thread(() => logger.Trace("hi"));
				t.Name = "test1";
				t.Start();
				t.Join();
				t = new Thread(() => { logger.Warn("there"); logger.Error("ups"); });
				t.Name = "test2";
				t.Start();
				t.Join();

				expectation.Add(
					0,
					new EM("hi", "test1"),
					new EM("there", "test2"),
					new EM("ups", "test2")
				);
			});
			TestSimpleLayout("${longdate} ${threadid} ${message}", (logger, expectation) =>
			{
				logger.Trace("hi");
				logger.Warn("there");
				logger.Error("ups");

				string expectedId = Thread.CurrentThread.ManagedThreadId.ToString();

				expectation.Add(
					0,
					new EM("hi", expectedId),
					new EM("there", expectedId),
					new EM("ups", expectedId)
				);
			});
		}

		public void ConditionalThreadRendererFolowedByNonConditionalOne()
		{
			TestSimpleLayout("${longdate} ${onexception:inner=Exception in ${threadid}!} t=${threadid} ${message}", (logger, expectation) =>
			{
				logger.Trace("hi");
				logger.Warn("there");
				logger.Error("ups");

				string expectedId = Thread.CurrentThread.ManagedThreadId.ToString();

				expectation.Add(
					0,
					new EM(null, expectedId),
					new EM(null, expectedId),
					new EM(null, expectedId)
				);
			});
		}

		public void ManyConditionalThreadRenderers()
		{
			TestSimpleLayout("${longdate} t1=${threadid:when='${message}'=='hi'} t2=${threadid:when='${message}'=='there'} ${message}", (logger, expectation) =>
			{
				logger.Trace("hi");
				logger.Warn("there");
				logger.Error("ups");

				string expectedId = Thread.CurrentThread.ManagedThreadId.ToString();

				expectation.Add(
					0,
					new EM(null, expectedId),
					new EM(null, expectedId),
					new EM(null, "")
				);
			});
		}

		public void CachedRendererTest()
		{
			TestSimpleLayout("${longdate} ${level:cached=True} text=${message:cached=True} ", (logger, expectation) =>
			{
				logger.Fatal("hi");
				logger.Warn("there");
				logger.Error("ups");

				expectation.Add(
					0,
					new EM("text=hi") { ContentType = MessageFlag.Error },
					new EM("text=hi") { ContentType = MessageFlag.Error },
					new EM("text=hi") { ContentType = MessageFlag.Error }
				);
			});
		}

		void AssertRendererIgnored(ImportLog importLog, string rendererName, string ignoredBecauseOf = null)
		{
			Assert.AreEqual(1, importLog.Messages.Count(m =>
				m.Type == ImportLog.MessageType.RendererIgnored &&
				m.Fragments.Where(f => f is LSL).Cast<LSL>().Any(f => f.Value == rendererName) &&
				(ignoredBecauseOf == null) || (m.Fragments.Where(f => f is LSL).Cast<LSL>().Any(f => f.Value == ignoredBecauseOf))
			), string.Format("Expected {0} to be ignored", rendererName));
		}

		public void NotHandlableRenderersTest()
		{
			foreach (string notHandlableRenderer in new string[] { "filesystem-normalize", "json-encode", "xml-encode" })
			{
				TestSimpleLayout("${longdate} ${" + notHandlableRenderer + ":inner=${level}} ${message} ", (logger, expectation) =>
				{
					logger.Fatal("hi");

					expectation.Add(
						0,
						new EM() { TextVerifier = t => t.Contains("hi"), ContentType = MessageFlag.Info }
					);
				},
				log =>
				{
					AssertRendererIgnored(log, "${level}", "${" + notHandlableRenderer + "}");
				});
			}
			TestSimpleLayout("${longdate} ${replace:inner=${level}:searchFor='acb':replaceWith='jhj'} ${message} ", (logger, expectation) =>
			{
				logger.Fatal("hi");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("hi"), ContentType = MessageFlag.Info }
				);
			},
			log =>
			{
				AssertRendererIgnored(log, "${level}", "${replace}");
			});
			TestSimpleLayout("${longdate} ${xml-encode:inner=${level}:xmlEncode=False} ${message} ", (logger, expectation) =>
			{
				logger.Fatal("hi");

				expectation.Add(
					0,
					new EM("hi") { ContentType = MessageFlag.Error }
				);
			},
			log =>
			{
				AssertThereIsRendererUsageReport(log, "${level}");
			});
		}

		public void TrimWhitespaceTest()
		{
			TestSimpleLayout("${longdate} ${trim-whitespace:inner=${message}}>${level}", (logger, expectation) =>
			{
				logger.Fatal("  hi  ");

				expectation.Add(
					0,
					new EM("hi>") { ContentType = MessageFlag.Error }
				);
			},
			log =>
			{
				AssertThereIsRendererUsageReport(log, "${level}");
			});
			TestSimpleLayout("${longdate} >${trim-whitespace:${message}}<", (logger, expectation) =>
			{
				logger.Info("  hi  ");
				expectation.Add(0, new EM(">hi<"));
			});
			TestSimpleLayout("${longdate} >${message:trimWhiteSpace=True}<", (logger, expectation) =>
			{
				logger.Info("  hi  ");
				expectation.Add(0, new EM(">hi<"));
			});
		}

		public void CounterTest()
		{
			TestSimpleLayout("${longdate} ${counter}:${message} ${level}", (logger, expectation) =>
			{
				logger.Info("aaa");
				logger.Warn("bbb");
				logger.Info("ccc");

				expectation.Add(
					0,
					new EM("1:aaa"),
					new EM("2:bbb") { ContentType = MessageFlag.Warning },
					new EM("3:ccc")
				);
			});
		}

		public void GCTest()
		{
			TestSimpleLayout("${longdate} g0=${gc:property=CollectionCount0},g1=${gc:property=CollectionCount1},gmax=${gc:property=MaxGeneration},${gc:property=TotalMemory},${gc:property=TotalMemoryForceCollection} ${message} ${level}", (logger, expectation) =>
			{
				logger.Info("aaa");
				logger.Warn("bbb");
				logger.Info("ccc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("aaa") },
					new EM() { TextVerifier = t => t.Contains("bbb"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("ccc") }
				);
			});
		}

		public void GuidTest()
		{
			TestSimpleLayout("${longdate} ${guid} ${guid:format=N} ${guid:format=D} ${guid:format=B} ${guid:format=P} ${guid:format=X} ${message} ${level}", (logger, expectation) =>
			{
				logger.Info("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe") },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc") }
				);
			});
		}

		public void LoggerTest()
		{
			TestSimpleLayout("${longdate} ${logger} ${logger:shortName=True} ${message} ${level}", (logger, expectation) =>
			{
				Action logFromLambda = () => logger.Info("qwe");
				logFromLambda();
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe") },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc") }
				);
			});
		}

		public void NewLineTest()
		{
			TestSimpleLayout("${longdate} ${message} ${newline} Hello ${level}", (logger, expectation) =>
			{
				logger.Info("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe") },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc") }
				);
			});
		}

		public void ProcessIdTest()
		{
			TestSimpleLayout("${longdate} ${message} ${processid} ${level}", (logger, expectation) =>
			{
				logger.Info("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe") },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc") }
				);
			});
		}

		public void ProcessTimeTest()
		{
			TestSimpleLayout("${longdate} ${message} ${processtime} ${level}", (logger, expectation) =>
			{
				logger.Info("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe") },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc") }
				);
			});
		}

		public void ProcessInfoTest()
		{
			// ${processinfo:property=MachineName} 
			TestSimpleLayout(@"${longdate} ${message} 
${processinfo:property=BasePriority} 
${processinfo:property=ExitCode} 
${processinfo:property=ExitTime} 
${processinfo:property=Handle} 
${processinfo:property=HandleCount} 
${processinfo:property=HasExited} 
${processinfo:property=Id} 
${processinfo:property=MachineName} 
${processinfo:property=MainWindowHandle} 
${processinfo:property=MainWindowTitle} 
${processinfo:property=MaxWorkingSet} 
${processinfo:property=MinWorkingSet} 
${processinfo:property=PagedMemorySize} 
${processinfo:property=PagedMemorySize64} 
${processinfo:property=PagedSystemMemorySize} 
${processinfo:property=PagedSystemMemorySize64} 
${processinfo:property=PeakPagedMemorySize} 
${processinfo:property=PeakPagedMemorySize64} 
${processinfo:property=PeakVirtualMemorySize} 
${processinfo:property=PeakVirtualMemorySize64} 
${processinfo:property=PeakWorkingSet} 
${processinfo:property=PeakWorkingSet64} 
${processinfo:property=PriorityBoostEnabled} 
${processinfo:property=PriorityClass} 
${processinfo:property=PrivateMemorySize} 
${processinfo:property=PrivateMemorySize64} 
${processinfo:property=PrivilegedProcessorTime} 
${processinfo:property=ProcessName} 
${processinfo:property=Responding} 
${processinfo:property=SessionId} 
${processinfo:property=StartTime} 
${processinfo:property=TotalProcessorTime} 
${processinfo:property=UserProcessorTime} 
${processinfo:property=VirtualMemorySize} 
${processinfo:property=VirtualMemorySize64} 
${processinfo:property=WorkingSet} 
${processinfo:property=WorkingSet64} 
${level}", (logger, expectation) =>
			{
				logger.Info("qwe");
				logger.Warn("asd");
				logger.Error("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe") },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc"), ContentType = MessageFlag.Error }
				);
			});
		}

		public void ProcessNameTest()
		{
			TestSimpleLayout("${longdate} ${message} ${processname} ${level}", (logger, expectation) =>
			{
				logger.Info("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe") },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc") }
				);
			});
		}

		public void QpcTest()
		{
			TestSimpleLayout("${longdate} ${message} ${qpc:normalize=False} ${qpc:difference=True} ${qpc:normalize=True} ${qpc:alignDecimalPoint=False} ${qpc:alignDecimalPoint=False:precision=10} ${qpc:precision=0} ${qpc:seconds=False} ${qpc:seconds=False:difference=True} ${level}", (logger, expectation) =>
			{
				logger.Fatal("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe"), ContentType = MessageFlag.Error },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc"), ContentType = MessageFlag.Info }
				);
			});
		}

		public void WindowsIdentityTest()
		{
			TestSimpleLayout("${longdate} ${message} ${windows-identity} ${windows-identity:userName=False}  ${windows-identity:domain=False} -->${windows-identity:userName=False:domain=False}<-- ${level}", (logger, expectation) =>
			{
				logger.Fatal("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM() { TextVerifier = t => t.Contains("qwe"), ContentType = MessageFlag.Error },
					new EM() { TextVerifier = t => t.Contains("asd"), ContentType = MessageFlag.Warning },
					new EM() { TextVerifier = t => t.Contains("zxc"), ContentType = MessageFlag.Info }
				);
			});
		}

		public void UnknownRendererTest()
		{
			string unknownRenderer = "${skakdjaskdj}";
			var importLog = new ImportLog();
			var formatDocument = new XmlDocument();
			formatDocument.LoadXml(@"<format><regular-grammar></regular-grammar></format>");

			LayoutImporter.GenerateRegularGrammarElementForSimpleLayout(
				formatDocument.SelectSingleNode("format/regular-grammar") as XmlElement,
				"${longdate} ${message} --->" + unknownRenderer + "<--- ${level}", 
				importLog);

			Assert.IsTrue(importLog.Messages.Any(m =>
				m.Type == ImportLog.MessageType.UnknownRenderer &&
				m.Fragments.Where(f => f is LSL).Cast<LSL>().Any(f => f.Value == unknownRenderer)
			), unknownRenderer + "expected to be reported unknown");
		}

		public void InsignificantSeparatorsAtTheBeginningOfBody()
		{
			TestSimpleLayout("- '${longdate}' (${level}) | [${threadid}] ${message} ", (logger, expectation) =>
			{
				logger.Fatal("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM("qwe") { ContentType = MessageFlag.Error },
					new EM("asd") { ContentType = MessageFlag.Warning },
					new EM("zxc") { ContentType = MessageFlag.Info }
				);
			});
		}

		public void DefaultLayoutTest()
		{
			string methodName;
			if (CurrentVersion == NLogVersion.Ver4)
			{
				methodName = "LogJoint.Tests.NLog.TestsContainer";
			}
			else
			{
#if MONO
				methodName = "System.Reflection.MonoMethod";
#else
				methodName = "System.RuntimeMethodHandle";
#endif
			}
			TestSimpleLayout("${longdate}|${level:uppercase=true}|${logger}|${message}", (logger, expectation) =>
			{
				logger.Fatal("qwe");
				logger.Warn("asd");
				logger.Info("zxc");

				expectation.Add(
					0,
					new EM(methodName + "|qwe") { ContentType = MessageFlag.Error },
					new EM(methodName + "|asd") { ContentType = MessageFlag.Warning },
					new EM(methodName + "|zxc") { ContentType = MessageFlag.Info }
				);
			});
		}

		public void ActivityIdTest()
		{
			TestSimpleLayout("${longdate}|${activityid}|${message}", (logger, expectation) =>
			{
				var testActivityId = "90883c04-2fa6-4d4a-957f-6631a7c89354";
				System.Diagnostics.Trace.CorrelationManager.ActivityId = Guid.Parse(testActivityId);

				logger.Info("foo");
				logger.Info("0484be57-8149-4ab7-9bd9-004b654116db");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM(testActivityId + "|foo"),
					new EM(testActivityId + "|0484be57-8149-4ab7-9bd9-004b654116db"),
					new EM(testActivityId + "|bar")
				);
			});
		}

		public void AllEventPropertiesTest()
		{
			TestSimpleLayout("${longdate}|${all-event-properties}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");
				logger.Info("[aa]=[23]");

				expectation.Add(
					0,
					new EM("|foo"),
					new EM("|bar"),
					new EM("|[aa]=[23]")
				);
			});
		}

		public void AppDomainTest()
		{
			TestSimpleLayout("${longdate}|${appdomain:format=short}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM(string.Format("{0:D2}|foo", AppDomain.CurrentDomain.Id)),
					new EM(string.Format("{0:D2}|bar", AppDomain.CurrentDomain.Id))
				);
			});

			TestSimpleLayout("${longdate}|${appdomain:format=long}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM(string.Format("{0:D4}:{1}|foo", AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName)),
					new EM(string.Format("{0:D4}:{1}|bar", AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName))
				);
			});

			TestSimpleLayout("${longdate}|${appdomain:format=long}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM(string.Format("{0:D4}:{1}|foo", AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName)),
					new EM(string.Format("{0:D4}:{1}|bar", AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName))
				);
			});

			TestSimpleLayout(@"${longdate}|${appdomain:format={1\} - {0\}}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM(string.Format("{1} - {0}|foo", AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName)),
					new EM(string.Format("{1} - {0}|bar", AppDomain.CurrentDomain.Id, AppDomain.CurrentDomain.FriendlyName))
				);
			});
		}

		public void AssemblyVersionTest()
		{
			TestSimpleLayout("${longdate}|${assembly-version:name=NLog}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				var nlog = Assembly.Load("NLog");

				expectation.Add(
					0,
					new EM(nlog.GetName().Version.ToString() + "|foo"),
					new EM(nlog.GetName().Version.ToString() + "|bar")
				);
			});
			TestSimpleLayout("${longdate}|${assembly-version:name=NonExistentAsm}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM("|foo"),
					new EM("|bar")
				);
			});
			TestSimpleLayout("${longdate}|${assembly-version}|${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM("Could not find entry assembly|foo"),
					new EM("Could not find entry assembly|bar")
				);
			});
		}

		public void CallsiteLineNumberTest()
		{
			TestSimpleLayout("${longdate}|${callsite-linenumber} ${message}", (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM("59 foo"),
					new EM("59 bar")
				);
			});
		}

		public void WrapLineTest()
		{
			string longStr = string.Join(" ", Enumerable.Repeat("a", 100));
			string wrappedStr =
				  string.Join("", Enumerable.Repeat("a ", 40)) + Environment.NewLine
				+ string.Join("", Enumerable.Repeat("a ", 40)) + Environment.NewLine
				+ string.Join(" ", Enumerable.Repeat("a", 20));

			TestSimpleLayout("${longdate}|${wrapline:${message}}", (logger, expectation) =>
			{
				logger.Info(longStr);
				expectation.Add(0, new EM(wrappedStr));
			});

			TestSimpleLayout("${longdate}|${wrapline:Inner=${message}}", (logger, expectation) =>
			{
				logger.Info(longStr);
				expectation.Add(0, new EM(wrappedStr));
			});

			TestSimpleLayout("${longdate}|${message:wrapline=80}", (logger, expectation) =>
			{
				logger.Info(longStr);
				expectation.Add(0, new EM(wrappedStr));
			});
		}

		public void CsvSmokeTest()
		{
			TestCSVLayout(new[] { "${longdate}", "${level:upperCase=true}", "${message}" }, (logger, expectation) =>
			{
				logger.Debug("Hello world");
				logger.Error("Error");

				expectation.Add(
					0,
					new EM("Hello world", null) { ContentType = MessageFlag.Info },
					new EM("Error", null) { ContentType = MessageFlag.Error }
				);
			});
		}

		void CsvTest(CsvParams.QuotingMode quoting, char quoteChar, string delimiter = CsvParams.AutoDelimiter)
		{
			TestCSVLayout(new[] { "${longdate}", "${message}", "${level:upperCase=true}" }, (logger, expectation) =>
			{
				logger.Debug("Hello \"big\" world");
				logger.Error("Error 'err'");
				logger.Warn("Multiline\r\nmessage");

				expectation.Add(
					0,
					new EM("Hello \"big\" world", null) { ContentType = MessageFlag.Info },
					new EM("Error 'err'", null) { ContentType = MessageFlag.Error },
					new EM("Multiline\r\nmessage", null) { ContentType = MessageFlag.Warning }
				);
			}, quoting: quoting, quoteChar: quoteChar, delimiter: delimiter);
		}

		public void CsvQuotingAlwaysDoubleQuote()
		{
			CsvTest(CsvParams.QuotingMode.Always, '"');
		}

		public void CsvQuotingAutoDoubleQuote()
		{
			CsvTest(CsvParams.QuotingMode.Auto, '"');
		}

		public void CsvQuotingNeverDoubleQuote()
		{
			CsvTest(CsvParams.QuotingMode.Never, '"');
		}

		public void CsvQuotingAlwaysSingleQuote()
		{
			CsvTest(CsvParams.QuotingMode.Always, '\'');
		}

		public void CsvQuotingAutoSingleQuote()
		{
			CsvTest(CsvParams.QuotingMode.Auto, '\'');
		}

		public void CsvQuotingNeverSingleQuote()
		{
			CsvTest(CsvParams.QuotingMode.Never, '\'');
		}

		public void CsvTabSeparator()
		{
			CsvTest(CsvParams.QuotingMode.Auto, '"', "\t");
		}

		public void CsvSemicolonSeparator()
		{
			CsvTest(CsvParams.QuotingMode.Auto, '"', ";");
		}

		public void CsvSpaceSeparator()
		{
			CsvTest(CsvParams.QuotingMode.Auto, '"', " ");
		}

		public void CsvCustomSeparator()
		{
			CsvTest(CsvParams.QuotingMode.Auto, '"', "||");
		}

		public void JsonSmokeTest()
		{
			TestJsonLayout(new JsonParams()
			{
				Root = new JsonParams.Layout()
				{
					Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
					{
						{ "dt", new JsonParams.Layout.Attr() { SimpleLayout = "${longdate}" } },
						{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${message}" } },
						{ "level", new JsonParams.Layout.Attr() { SimpleLayout = "${level:upperCase=true}" } },
					}
				}
			}, (logger, expectation) =>
			{
				LogAndExpectTestJsonMessages(logger, expectation);
			});
		}

		private static void LogAndExpectTestJsonMessages(Logger logger, ExpectedLog expectation)
		{
			logger.Debug("Hello \"big\" world");
			logger.Error("Error 'err'");
			logger.Warn("Multiline\r\nmessage");
			logger.Info("{ \"here\": \"some json inside\" }");
			logger.Error("");

			expectation.Add(
				0,
				new EM("Hello \"big\" world", null) { ContentType = MessageFlag.Info },
				new EM("Error 'err'", null) { ContentType = MessageFlag.Error },
				new EM("Multiline\r\nmessage", null) { ContentType = MessageFlag.Warning },
				new EM("{ \"here\": \"some json inside\" }", null) { ContentType = MessageFlag.Info },
				new EM("", null) { ContentType = MessageFlag.Error }
			);
		}

		public void JsonSuppressSpacesTest()
		{
			TestJsonLayout(new JsonParams()
			{
				Root = new JsonParams.Layout()
				{
					Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
					{
						{ "dt", new JsonParams.Layout.Attr() { SimpleLayout = "${longdate}" } },
						{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${message}" } },
						{ "level", new JsonParams.Layout.Attr() { SimpleLayout = "${level}" } },
					},
					SuppressSpaces = true
				}
			}, (logger, expectation) =>
			{
				LogAndExpectTestJsonMessages(logger, expectation);
			});
		}

		public void JsonNestedLayoutTest()
		{
			TestJsonLayout(new JsonParams()
			{
				Root = new JsonParams.Layout()
				{
					Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
					{
						{ "dt", new JsonParams.Layout.Attr() { SimpleLayout = "${longdate}" } },
						{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${message}" } },
						{ "exception", new JsonParams.Layout.Attr() { SimpleLayout = "${exception:format=Type}" } },
						{ "innerException", new JsonParams.Layout.Attr()
							{
								JsonLayout = new JsonParams.Layout()
								{
									Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
									{
										{ "type", new JsonParams.Layout.Attr() { SimpleLayout = "${exception:format=:innerFormat=Type:MaxInnerExceptionLevel=1:InnerExceptionSeparator=}" } },
										{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${exception:format=:innerFormat=Message:MaxInnerExceptionLevel=1:InnerExceptionSeparator=}" } },
									}
								},
								Encode = false // todo: report an import error if nested encode is true
							}
						},
					},
				}
			}, (logger, expectation) =>
			{
				logger.Exception(new InvalidDataException("foo bar", new ArgumentException("test", "arg")), "my exception");
				logger.Info("test message");

				expectation.Add(
					0,
					// todo: this output sucks. consider building message texts differently for json.
					new EM("my exceptionSystem.IO.InvalidDataExceptionSystem.ArgumentExceptiontest" + Environment.NewLine + "Parameter name: arg", null),
					new EM("test message", null)
				);
			});
		}

		public void JsonExtraPropsTest()
		{
			TestJsonLayout(new JsonParams()
			{
				Root = new JsonParams.Layout()
				{
					Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
					{
						{ "dt", new JsonParams.Layout.Attr() { SimpleLayout = "${longdate}" } },
						{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${message}" } },
						{ "level", new JsonParams.Layout.Attr() { SimpleLayout = "${level:upperCase=true}" } },
					},
					IncludeMdc = true
				}
			}, (logger, expectation) =>
			{
				LogAndExpectMessagesWithMdcProps(logger, expectation);
			});
		}

		public void JsonExtraPropsInNestedObjectTest()
		{
			TestJsonLayout(new JsonParams()
			{
				Root = new JsonParams.Layout()
				{
					Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
					{
						{ "dt", new JsonParams.Layout.Attr() { SimpleLayout = "${longdate}" } },
						{ "nested", new JsonParams.Layout.Attr()
							{
								JsonLayout = new JsonParams.Layout()
								{
									Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
									{
										{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${message}" } },
										{ "level", new JsonParams.Layout.Attr() { SimpleLayout = "${level:upperCase=true}" } }
									},
									IncludeMdc = true,
								},
								Encode = false
							}
						}
					},
				}
			}, (logger, expectation) =>
			{
				LogAndExpectMessagesWithMdcProps(logger, expectation);
			});
		}

		private void LogAndExpectMessagesWithMdcProps(Logger logger, ExpectedLog expectation)
		{
			Action<string, object[]> mdcOp = (op, args) => 
				nlogAsm.GetType("NLog.MappedDiagnosticsContext").InvokeMember(op, BindingFlags.Public | BindingFlags.InvokeMethod | BindingFlags.Static, null, null, args);

			logger.Info("foo");
			mdcOp("Set", new[] { "prop 1", "hi there" });
			logger.Info("bar");
			mdcOp("Remove", new[] { "prop 1" });

			expectation.Add(
				0,
				new EM("foo", null),
				new EM("bar", null)
			);
		}

		public void JsonNestedEmptyObjectNotRenderedTest()
		{
			TestJsonLayout(new JsonParams()
			{
				Root = new JsonParams.Layout()
				{
					Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
					{
						{ "dt", new JsonParams.Layout.Attr() { SimpleLayout = "${longdate}" } },
						{ "nested", new JsonParams.Layout.Attr()
							{
								JsonLayout = new JsonParams.Layout()
								{
									Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
									{
										{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${message}" } },
									},
									RenderEmptyObject = false
								},
								Encode = false
							}
						},
					},
				}
			}, (logger, expectation) =>
			{
				logger.Info("foo");
				logger.Info("");
				logger.Info("bar");

				expectation.Add(
					0,
					new EM("foo", null),
					new EM("", null),
					new EM("bar", null)
				);
			});
		}

		public void JsonUnicodeTest()
		{
			TestJsonLayout(new JsonParams()
			{
				Root = new JsonParams.Layout()
				{
					Attrs = new Dictionary<string, JsonParams.Layout.Attr>()
					{
						{ "dt", new JsonParams.Layout.Attr() { SimpleLayout = "${longdate}" } },
						{ "message", new JsonParams.Layout.Attr() { SimpleLayout = "${message}", EscapeUnicode = true } },
					},
				},
			}, (logger, expectation) =>
			{
				var msg = "Non-ascii->Превед, медвед🐻<-here";
				logger.Info(msg);
				expectation.Add(0, new EM(msg));
			});
		}
	};

	[TestFixture()]
	public class NLogLayoutImporterTest
	{
		struct DomainData
		{
			public AppDomain Domain;
			public string TempNLogDir;
			public object TestsContainer;
			public void Dispose()
			{
				AppDomain.Unload(Domain);
				Directory.Delete(TempNLogDir, true);
			}
		};

		Dictionary<string, DomainData> nlogVersionToDomain = new Dictionary<string, DomainData>();

		[OneTimeTearDown]
		public void TearDown()
		{
			foreach (var dom in nlogVersionToDomain.Values)
				dom.Dispose();
			nlogVersionToDomain.Clear();
		}

		[Flags]
		enum TestOptions
		{
			None = 0,
			TestAgainstNLog1 = 1,
			TestAgainstNLog2Plus = 2,
			TestAgainstNLog4Plus = 4,
			Default = TestAgainstNLog1 | TestAgainstNLog2Plus | TestAgainstNLog4Plus
		};

		void RunTestWithNLogVersion(string testName, string nLogVersion)
		{
			DomainData domain;
			if (!nlogVersionToDomain.TryGetValue(nLogVersion, out domain))
			{
				string thisAsmPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + Path.DirectorySeparatorChar;
				string tempNLogDirName = "temp-nlog-" + nLogVersion;
				var tempNLogDirPath = thisAsmPath + tempNLogDirName + Path.DirectorySeparatorChar;
				domain.TempNLogDir = tempNLogDirPath;
				if (!File.Exists(tempNLogDirPath + "NLog.dll"))
				{
					Directory.CreateDirectory(tempNLogDirPath);
					var resName = Assembly.GetExecutingAssembly().GetManifestResourceNames().SingleOrDefault(
						n => n.Contains(string.Format("{0}.NLog.dll", nLogVersion)));
					var nlogSourceStream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resName);
					using (var nlogAsmDestStream = new FileStream(tempNLogDirPath + "NLog.dll", FileMode.Create))
					{
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
				domain.TestsContainer.GetType().InvokeMember(testName, 
					BindingFlags.InvokeMethod | BindingFlags.Public | BindingFlags.Instance, null,  domain.TestsContainer, null);
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
		/// must be the same as entry point [Test] method name.
		/// </summary>
		void RunThisTestAgainstDifferentNLogVersions(TestOptions options = TestOptions.Default, string testName = null)
		{
			if (testName == null)
				testName = new System.Diagnostics.StackFrame(1).GetMethod().Name;
			if ((options & TestOptions.TestAgainstNLog1) != 0)
				RunTestWithNLogVersion(testName, "_1._0");
			if ((options & TestOptions.TestAgainstNLog2Plus) != 0)
				RunTestWithNLogVersion(testName, "_2._0");
			if ((options & (TestOptions.TestAgainstNLog2Plus | TestOptions.TestAgainstNLog4Plus)) != 0)
				RunTestWithNLogVersion(testName, "_4._4");
		}

		[Test]
		public void SmokeTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void NLogWrapperNamesAndParamsAreNotCaseSensitive()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void InsignificantSpacesInParamsAreIngnored()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void SignificantSpacesInParamsAreNotIgnored()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}		

		[Test]
		public void EscapingTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void LevelsTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void NegativePadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void PositivePadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void DefaultPaddingCharacter()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void NonDefaultPaddingCharacter()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void AmbientPaddingAttribute()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void AmbientPaddingAndPadCharAttributes()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void FixedLengthPaddingMakesInterestingAttrsIgnored()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void PaddingEmbeddedIntoCasing()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void PaddingAndCasingAsAmbientProps()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void ZeroPadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void EmbeddedPadding()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void Longdate()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void Time()
		{
			// {time} seems not to be supported by NLog1?
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void Ticks()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void Shortdate()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ShortdateAndTimeSeparated()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void ShortdateAndTimeAreTakenIntoUseEvenThereIsAConditionalLongdateAtTheBeginning()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}
		
		[Test]
		public void FailIfNoDateTimeRenderers()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void FailIfDateTimeRenderersAreConditional()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void FullySpecifiedDate()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TestStdDateFormatStrings_InvariantCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TestStdDateFormatStrings_RuCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TestStdDateFormatStrings_JpCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void DateAndTimeHaveDifferentCultures()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TestCustomDateTimeFormatStrings_InvariantCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TestCustomDateTimeFormatStrings_RuCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TestCustomDateTimeFormatStrings_JpCulture()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void EmptyDateFormat()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void LocaleDependentDateWithCasing()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TheOnlyNonConditionalLevelRenderer()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ConditionalLevelRendererFollowedByUnconditionalLevelRenderer()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}
		
		[Test]
		public void ManyConditionalLevelRenderers()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void LevelRendererAndCasing()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void TheOnlyNonConditionalThreadRenderer()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ConditionalThreadRendererFolowedByNonConditionalOne()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void ManyConditionalThreadRenderers()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void CachedRendererTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void NotHandlableRenderersTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void TrimWhitespaceTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void CounterTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void GCTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void GuidTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void LoggerTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void NewLineTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ProcessIdTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ProcessTimeTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ProcessInfoTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ProcessNameTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void QpcTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog2Plus);
		}

		[Test]
		public void WindowsIdentityTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void UnknownRendererTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void InsignificantSeparatorsAtTheBeginningOfBody()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void DefaultLayoutTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void ActivityIdTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void AllEventPropertiesTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void AppDomainTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void AssemblyVersionTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void CallsiteLineNumberTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void WrapLineTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void CsvSmokeTest()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvQuotingAlwaysDoubleQuote()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvQuotingNeverDoubleQuote()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvQuotingAutoDoubleQuote()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvQuotingAlwaysSingleQuote()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvQuotingNeverSingleQuote()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvQuotingAutoSingleQuote()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvTabSeparator()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvSemicolonSeparator()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvSpaceSeparator()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void CsvCustomSeparator()
		{
			RunThisTestAgainstDifferentNLogVersions();
		}

		[Test]
		public void JsonSmokeTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void JsonSuppressSpacesTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void JsonNestedLayoutTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void JsonExtraPropsTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void JsonExtraPropsInNestedObjectTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void JsonNestedEmptyObjectNotRenderedTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}

		[Test]
		public void JsonUnicodeTest()
		{
			RunThisTestAgainstDifferentNLogVersions(TestOptions.TestAgainstNLog4Plus);
		}
	}
}
