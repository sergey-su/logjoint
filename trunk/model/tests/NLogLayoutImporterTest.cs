using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Text;
using System.Linq;
using System.Xml;
using NLog;

namespace logjoint.model.tests
{
	[TestClass()]
	public class NLogLayoutImporterTest
	{
		string CreateSimpleLog(string layout, Action<Logger> loggingCallback)
		{
			var target = new NLog.Targets.MemoryTarget();
			target.Layout = layout;

			NLog.LogManager.Configuration = new NLog.Config.LoggingConfiguration();
			NLog.Config.SimpleConfigurator.ConfigureForTargetLogging(target, NLog.LogLevel.Debug);

			loggingCallback(NLog.LogManager.GetCurrentClassLogger());

			return target.Logs.Aggregate(new StringBuilder(), (sb, line) => sb.AppendLine(line)).ToString();
		}

		void LogBasicLines(Logger l)
		{
			l.Debug("Hello");
			l.Error("Error");
		}



		[TestMethod()]
		public void GenerateRegularGrammarElementTest()
		{
			var s1 = CreateSimpleLog(@"${whenEmpty:whenEmpty=Layout:inner=${shortdate}}", LogBasicLines);
			var s2 = CreateSimpleLog(@"${shortdate} ${pad:padCharacter= :padding=100:fixedLength=True:inner=${message}}", LogBasicLines);

			NLogLayoutImporter.GenerateRegularGrammarElement(null, @"${longdate}|${message}");

			// @"aa\}bb\\cc\tdd ${literal:text=S\}t\\r\:ing} ${shortdate} ${pad:padCharacter= :padding=100:fixedLength=True:inner=${message}} xyz"

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
	}
}
