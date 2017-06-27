using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using LogJoint.Analytics;

namespace LogJoint.Chromium.ChromeDebugLog
{
	[TestClass]
	public class ChromeDebugLogTests
	{
		[TestMethod, TestCategory("SplitAndCompose")]
		public async Task ChromeDebugLog_SplitAndComposeTest()
		{
			var testStream = Utils.GetResourceStream("chrome_debug_2017_06_26");

			var actualContent = new MemoryStream();

			var reader = new Reader();
			var writer = new Writer();

			await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

			Utils.AssertTextsAreEqualLineByLine(
				Utils.SplitTextStream(testStream),
				Utils.SplitTextStream(actualContent)
			);
		}
	}
}
