using LogJoint.UI.Presenters;
using System;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	class ClipboardAccess : IClipboardAccess
	{
		public ClipboardAccess(Telemetry.ITelemetryCollector errorTelemetry)
		{
			this.errorTelemetry = errorTelemetry;
		}

		void IClipboardAccess.SetClipboard(string value)
		{
			try
			{
				Clipboard.SetText(value);
			}
			catch (Exception e)
			{
				HandleError(e);
			}
		}

		void IClipboardAccess.SetClipboard(string plainText, string html)
		{
			try
			{
				var dataObject = new DataObject();
				dataObject.SetData(DataFormats.Html, GetHtmlDataString(html));
				dataObject.SetData(DataFormats.Text, plainText);
				Clipboard.SetDataObject(dataObject, true);
			}
			catch (Exception e)
			{
				HandleError(e);
			}
		}

		static string GetHtmlDataString(string html)
		{
			var sb = new StringBuilder();
			sb.AppendLine(htmlHeaderTemplate);
			sb.AppendLine(@"<!DOCTYPE HTML PUBLIC ""-//W3C//DTD HTML 4.0 Transitional//EN"">");

			// if given html already provided the fragments we won't add them
			int fragmentStart, fragmentEnd;
			int fragmentStartIdx = html.IndexOf(htmlStartFragment, StringComparison.OrdinalIgnoreCase);
			int fragmentEndIdx = html.LastIndexOf(htmlEndFragment, StringComparison.OrdinalIgnoreCase);

			// if html tag is missing add it surrounding the given html (critical)
			int htmlOpenIdx = html.IndexOf("<html", StringComparison.OrdinalIgnoreCase);
			int htmlOpenEndIdx = htmlOpenIdx > -1 ? html.IndexOf('>', htmlOpenIdx) + 1 : -1;
			int htmlCloseIdx = html.LastIndexOf("</html", StringComparison.OrdinalIgnoreCase);

			if (fragmentStartIdx < 0 && fragmentEndIdx < 0)
			{
				int bodyOpenIdx = html.IndexOf("<body", StringComparison.OrdinalIgnoreCase);
				int bodyOpenEndIdx = bodyOpenIdx > -1 ? html.IndexOf('>', bodyOpenIdx) + 1 : -1;

				if (htmlOpenEndIdx < 0 && bodyOpenEndIdx < 0)
				{
					// the given html doesn't contain html or body tags so we need to add them and place start/end fragments around the given html only
					sb.Append("<html><body>");
					sb.Append(htmlStartFragment);
					fragmentStart = GetByteCount(sb);
					sb.Append(html);
					fragmentEnd = GetByteCount(sb);
					sb.Append(htmlEndFragment);
					sb.Append("</body></html>");
				}
				else
				{
					// insert start/end fragments in the proper place (related to html/body tags if exists) so the paste will work correctly
					int bodyCloseIdx = html.LastIndexOf("</body", StringComparison.OrdinalIgnoreCase);

					if (htmlOpenEndIdx < 0)
						sb.Append("<html>");
					else
						sb.Append(html, 0, htmlOpenEndIdx);

					if (bodyOpenEndIdx > -1)
						sb.Append(html, htmlOpenEndIdx > -1 ? htmlOpenEndIdx : 0, bodyOpenEndIdx - (htmlOpenEndIdx > -1 ? htmlOpenEndIdx : 0));

					sb.Append(htmlStartFragment);
					fragmentStart = GetByteCount(sb);

					var innerHtmlStart = bodyOpenEndIdx > -1 ? bodyOpenEndIdx : (htmlOpenEndIdx > -1 ? htmlOpenEndIdx : 0);
					var innerHtmlEnd = bodyCloseIdx > -1 ? bodyCloseIdx : (htmlCloseIdx > -1 ? htmlCloseIdx : html.Length);
					sb.Append(html, innerHtmlStart, innerHtmlEnd - innerHtmlStart);

					fragmentEnd = GetByteCount(sb);
					sb.Append(htmlEndFragment);

					if (innerHtmlEnd < html.Length)
						sb.Append(html, innerHtmlEnd, html.Length - innerHtmlEnd);

					if (htmlCloseIdx < 0)
						sb.Append("</html>");
				}
			}
			else
			{
				// handle html with existing start\end fragments just need to calculate the correct bytes offset (surround with html tag if missing)
				if (htmlOpenEndIdx < 0)
					sb.Append("<html>");
				int start = GetByteCount(sb);
				sb.Append(html);
				fragmentStart = start + GetByteCount(sb, start, start + fragmentStartIdx) + htmlStartFragment.Length;
				fragmentEnd = start + GetByteCount(sb, start, start + fragmentEndIdx);
				if (htmlCloseIdx < 0)
					sb.Append("</html>");
			}

			// Back-patch offsets (scan only the header part for performance)
			sb.Replace("<<<<<<<<1", htmlHeaderTemplate.Length.ToString("D9"), 0, htmlHeaderTemplate.Length);
			sb.Replace("<<<<<<<<2", GetByteCount(sb).ToString("D9"), 0, htmlHeaderTemplate.Length);
			sb.Replace("<<<<<<<<3", fragmentStart.ToString("D9"), 0, htmlHeaderTemplate.Length);
			sb.Replace("<<<<<<<<4", fragmentEnd.ToString("D9"), 0, htmlHeaderTemplate.Length);

			return sb.ToString();
		}

		static int GetByteCount(StringBuilder sb, int begin = 0, int? end = null)
		{
			return Encoding.UTF8.GetByteCount(sb.ToString(begin, end.GetValueOrDefault(sb.Length) - begin));
		}

		void HandleError(Exception e)
		{
			errorTelemetry.ReportException(e, "setting clipboard");
			MessageBox.Show("Failed to copy data to the clipboard");
		}

		readonly Telemetry.ITelemetryCollector errorTelemetry;

		const string htmlHeaderTemplate = @"Version:0.9
StartHTML:<<<<<<<<1
EndHTML:<<<<<<<<2
StartFragment:<<<<<<<<3
EndFragment:<<<<<<<<4
StartSelection:<<<<<<<<3
EndSelection:<<<<<<<<4";

		const string htmlStartFragment = "<!--StartFragment-->";

		const string htmlEndFragment = @"<!--EndFragment-->";
	}
}
