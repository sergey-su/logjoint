using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace LogJoint.UI
{
	public class MonoChecker
	{
		readonly double minMonoVersion = 4.5;

		public MonoChecker (
			Presenters.MainForm.IPresenter mainWindow,
			Presenters.IAlertPopup alerts,
			Telemetry.ITelemetryCollector telemetryCollector,
			Presenters.IShellOpen shellOpen
		)
		{
			mainWindow.Loaded += (sender, evt) => {
				try {
					CheckMono ();
				} catch (Exception ex) {
					telemetryCollector.ReportException (ex, "mono checking");
					if (alerts.ShowPopup (
						"Error",
						$"Required mono framework v{minMonoVersion}+ is not found on your system. " +
						"The program will terminate. " + 
						"Do you want to start mono download before termination?",
						Presenters.AlertFlags.YesNoCancel
					) == Presenters.AlertFlags.Yes) {
						shellOpen.OpenInWebBrowser (
							new Uri ("http://download.mono-project.com/archive/mdk-latest.pkg"));
					}
					Environment.Exit (1);
				}
			};
		}

		void CheckMono ()
		{
			var monoPath = @"/Library/Frameworks/Mono.framework/Versions/Current/bin/mono";
			if (!File.Exists (monoPath)) {
				throw new Exception ("mono file is not found");
			}
			var pi = new ProcessStartInfo {
				UseShellExecute = false,
				FileName = monoPath,
				Arguments = "-V",
				RedirectStandardOutput = true
			};
			using (var process = Process.Start (pi)) {
				var monoVersionOutput = process.StandardOutput.ReadToEnd ();
				var m = Regex.Match(monoVersionOutput, @"compiler version (\d+\.\d+)");
				if (!m.Success) {
					throw new Exception ($"mono output can not be parsed: {monoVersionOutput}");
				}
				var monoVersionStr = m.Groups [1].Value;
				if (!double.TryParse (m.Groups [1].Value, out var monoVersion)) {
					throw new Exception ($"mono version can not be parsed: {monoVersionStr} from {monoVersionOutput}");
				}
				if (monoVersion < minMonoVersion) {
					throw new Exception ($"mono is too old: {monoVersion}");
				}
			}
		}
	}
}
