using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;
using LogJoint.Analytics;

namespace LogJoint.Chromium.ChromeDriver
{
	[TestClass]
	public class ChromeDriverLogTests
	{
		[TestMethod, TestCategory("SplitAndCompose")]
		public async Task ChromeDriver_SplitAndComposeTest()
		{
			var testStream = Utils.GetResourceStream("chromedriver_2019_01_23");

			var actualContent = new MemoryStream();

			var reader = new Reader();
			var writer = new Writer();

			await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

			Utils.AssertTextsAreEqualLineByLine(
				Utils.SplitTextStream(testStream),
				Utils.SplitTextStream(actualContent)
			);
		}

		[TestMethod, TestCategory("SplitAndCompose")]
		public async Task ChromeDriver_SplitAndCompose_WithForeignLogging()
		{
			var testStream = Utils.GetResourceStream("chromedriver_2019_01_22");

			var actualContent = new MemoryStream();

			var reader = new Reader();
			var writer = new Writer();

			await writer.Write(() => actualContent, _ => { }, reader.Read(() => testStream, _ => { }));

			Utils.AssertTextsAreEqualLineByLine(
				Utils.SplitTextStream(testStream),
				Utils.SplitTextStream(actualContent)
			);
		}

		[TestMethod]
		public async Task ForeignLoggingAtEndOfMssagesIsIgnored()
		{
			var testStream = Utils.GetResourceStream("chromedriver_2019_01_22");
			var messages = await (new Reader()).Read(() => testStream, _ => { }).ToFlatList();

			var parsedMessage = DevTools.Events.LogMessage.Parse(messages[1].Text);
			Assert.AreEqual("loadingFinished", parsedMessage.EventType);
			Assert.AreEqual("Network", parsedMessage.EventNamespace);

			var parsedPayload = parsedMessage.ParsePayload<DevTools.Events.Network.LoadingFinished>();
			Assert.IsNotNull(parsedPayload);
			Assert.AreEqual("27949.24", parsedPayload.requestId);

			var parsedTimeStampsPayload = parsedMessage.ParsePayload<DevTools.Events.TimeStampsInfo>();
			Assert.IsNotNull(parsedTimeStampsPayload);
			Assert.AreEqual(597454.928244, parsedTimeStampsPayload.timestamp.Value, 1e-10);
		}
	}
}
