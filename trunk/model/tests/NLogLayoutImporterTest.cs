using LogJoint;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Xml;

namespace logjoint.model.tests
{
	[TestClass()]
	public class NLogLayoutImporterTest
	{
		[TestMethod()]
		public void GenerateRegularGrammarElementTest()
		{
			NLogLayoutImporter.GenerateRegularGrammarElement(null,
				@"aa\}bb\\cc\tdd ${literal:text=S\}t\\r\:ing} ${shortdate} ${pad:padCharacter= :padding=100:fixedLength=True:inner=${message}} xyz");

		}

		// ideas for intergation test:
		//    1. many datetimes in layout   yyyy MM yyyy MM
		//    2. Embedded layouts
		//    3. Significant spaces in layouts (like ${pad:padCharacter= })
		//    4. Single \ at the end of layout string
	}
}
