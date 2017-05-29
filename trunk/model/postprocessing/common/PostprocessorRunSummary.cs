using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Postprocessing
{
	public class PostprocessorRunSummary : IPostprocessorRunSummary
	{
		List<Entry> entries = new List<Entry>();

		public void AddMessage(string message, bool isError)
		{
			entries.Add(new Entry() { message = message, isError = isError });
		}

		bool IPostprocessorRunSummary.HasErrors
		{
			get { return entries.Any(e => e.isError); }
		}

		bool IPostprocessorRunSummary.HasWarnings
		{
			get { return entries.Any(e => !e.isError); }
		}

		string IPostprocessorRunSummary.Report
		{
			get
			{
				var ret = new StringBuilder();
				Print(entries.Where(e => e.isError),  "----------------  errors  ----------------", ret);
				Print(entries.Where(e => !e.isError), "---------------- warnings ----------------", ret);
				return ret.ToString();
			}
		}

		IPostprocessorRunSummary IPostprocessorRunSummary.GetLogSpecificSummary(ILogSource ls)
		{
			return null;
		}

		static void Print(IEnumerable<Entry> entries, string caption, StringBuilder sb)
		{
			bool captionAdded = false;
			foreach (var e in entries)
			{
				if (!captionAdded)
				{
					sb.AppendLine(caption);
					sb.AppendLine();
					captionAdded = true;
				}
				sb.AppendLine(e.message);
				sb.AppendLine();
			}
		}

		struct Entry
		{
			public string message;
			public bool isError;
		};
	};
}
