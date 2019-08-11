using System;
using System.Linq;
using System.Threading.Tasks;

namespace LogJoint.Tests.Integration
{
	public static class TestAppExtensions
	{
		public static Task EmulateFileDragAndDrop(this TestAppInstance app, string fileName)
		{
			return app.Model.LogSourcesPreprocessings.Preprocess(
				new[] { app.Model.PreprocessingStepsFactory.CreateFormatDetectionStep(new Preprocessing.PreprocessingStepParams(fileName)) },
				fileName
			);
		}

		public static Task EmulateUrlDragAndDrop(this TestAppInstance app, string url)
		{
			return app.Model.LogSourcesPreprocessings.Preprocess(
				new[] { app.Model.PreprocessingStepsFactory.CreateURLTypeDetectionStep(new Preprocessing.PreprocessingStepParams(url)) },
				url
			);
		}

		public static async Task WaitFor(this TestAppInstance app, Func<bool> condition, string operationName = null, TimeSpan? timeout = null)
		{
			if (condition())
				return;
			var tcs = new TaskCompletionSource<int>();
			using (var subs = app.Model.ChangeNotification.CreateSubscription(() =>
			{
				if (condition())
					tcs.TrySetResult(0);
			}))
			{
				var delay = Task.Delay(timeout.GetValueOrDefault(TimeSpan.FromSeconds(15)));
				if (await Task.WhenAny(tcs.Task, delay) == delay)
				{
					throw new TimeoutException("Time out waiting for " + (operationName ?? "condition"));
				}
			}
		}

		public static string GetDisplayedLog(this TestAppInstance app)
		{
			var viewLines = app.ViewModel.LoadedMessagesLogViewer.ViewLines;
			var displayedText = string.Join("\n", viewLines.Select(vl => vl.TextLineValue));
			return displayedText;
		}

		public static bool IsLogDisplayed(this TestAppInstance app, string text)
		{
			var displayedText = app.GetDisplayedLog();
			return StringUtils.NormalizeLinebreakes(text) == StringUtils.NormalizeLinebreakes(displayedText);
		}

		public static async Task WaitForLogDisplayed(this TestAppInstance app, string expectedLog, string operationName = null, TimeSpan? timeout = null)
		{
			try
			{
				await app.WaitFor(
					() => app.IsLogDisplayed(expectedLog),
					operationName: operationName,
					timeout: timeout
				);
			}
			catch (TimeoutException)
			{
				Console.WriteLine("Actually displayed log: '{0}'", app.GetDisplayedLog());
				throw;
			}
		}
	};
}
