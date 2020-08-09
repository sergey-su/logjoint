using System;
using System.Text;
using System.Collections.Generic;
using System.IO;
using LogJoint;
using NUnit.Framework;
using NFluent;
using Newtonsoft.Json.Linq;

namespace LogJoint.Google.Tests
{
	[TestFixture]
	public class TextProtoParserTests
	{
		static IEnumerable<string> SplitText(string str)
		{
			using (var reader = new StringReader(str))
				for (var l = reader.ReadLine(); l != null; l = reader.ReadLine())
					yield return l.TrimEnd();
		}

		static void TestParsing(string text, string expectedJson)
		{
			var expectedToken = JToken.Parse(expectedJson);
			var formattedExpectedJson = expectedToken.ToString(Newtonsoft.Json.Formatting.Indented);
			var actualToken = TextProtoParser.Parse(text);
			var actualJson = actualToken.ToString(Newtonsoft.Json.Formatting.Indented);
			Check.That(actualJson).AsLines().ContainsExactly(SplitText(formattedExpectedJson));
		}

		[Test]
		public void SacalarValues()
		{
			TestParsing("12", "'12'");
			TestParsing("\"test 123\"", "'test 123'");
			TestParsing("true", "'true'");
		}

		[Test]
		public void EmptyObject()
		{
			TestParsing("{}", "{}");
		}

		[Test]
		public void ObjectWithNumericProperty()
		{
			TestParsing("{ a: 1 }", "{'a': '1'}");
		}


		[Test]
		public void ObjectWithNumericProperties()
		{
			TestParsing("{ a: 1 b: 2 }", "{'a': '1', 'b': '2'}");
		}

		[Test]
		public void ObjectWithEscapedProperties()
		{
			TestParsing("{ a: \"hello there [13] {bar}\" }", "{'a': 'hello there [13] {bar}'}");
		}

		[Test]
		public void RepeatedValuesInObject()
		{
			TestParsing("{ a { b: 1 } a { b: 2 } }", "{'a': [ {'b': '1'}, {'b': '2'} ] }");
		}

		[Test]
		public void MixedRepeatedAndSingleValuesInObject()
		{
			TestParsing("{ a { b: 1 } c: 4 a { b: 2 } }", "{'a': [ {'b': '1'}, {'b': '2'} ], 'c': '4' }");
		}

		[Test]
		public void ObjectType()
		{
			TestParsing("{ [type.googleapis.com/my.type] { a: 1 } }", "{ 'type': 'type.googleapis.com/my.type', 'value': { 'a': '1' } }");
		}

		[Test]
		public void DefaultScalarValueInsideObject()
		{
			TestParsing("{ MAJKChhe7TIbNElWSE0@[2002:a2a:cac8::]:9876 }", "{ 'value': 'MAJKChhe7TIbNElWSE0@[2002:a2a:cac8::]:9876' }");
		}

		[Test]
		public void DefaultStringValueInsideObject()
		{
			TestParsing("{ \"test 123\" }", "{ 'value': 'test 123' }");
		}

		[Test]
		public void CustomFormattedValue()
		{
			TestParsing("{ a: [ A:B/C X[Y{Z}] QWE-123 ] }", "{ 'a': 'A:B/C X[Y{Z}] QWE-123' }");
		}
	}
}
