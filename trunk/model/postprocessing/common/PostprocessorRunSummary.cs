using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Postprocessing
{
	public interface IStructuredPostprocessorRunSummary: IPostprocessorRunSummary
    {
		IEnumerable<(string text, bool isError, IBookmark bookmark)> Entries { get; }
	};


	public class PostprocessorRunSummary : IPostprocessorRunSummary, IPostprocessorRunSummaryBuilder, IStructuredPostprocessorRunSummary
	{
		readonly List<Entry> entries = new List<Entry>();

		void IPostprocessorRunSummaryBuilder.AddWarning(string message) => AddMessage(message, bookmark: null, isError: false);

		void IPostprocessorRunSummaryBuilder.AddError(string message) => AddMessage(message, bookmark: null, isError: true);

		void IPostprocessorRunSummaryBuilder.AddWarning(string message, IBookmark triggerBookmark) => AddMessage(message, triggerBookmark, isError: false);

		void IPostprocessorRunSummaryBuilder.AddError(string message, IBookmark triggerBookmark) => AddMessage(message, triggerBookmark, isError: true);

		IPostprocessorRunSummary IPostprocessorRunSummaryBuilder.ToSummary() => this;

		private void AddMessage(string message, IBookmark bookmark, bool isError)
		{
			entries.Add(new Entry() { message = message, isError = isError, bookmark = bookmark });
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

		IEnumerable<(string text, bool isError, IBookmark bookmark)> IStructuredPostprocessorRunSummary.Entries =>
			entries.Select(e => (e.message, e.isError, e.bookmark));


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
			public IBookmark bookmark;
		};
	};
}
