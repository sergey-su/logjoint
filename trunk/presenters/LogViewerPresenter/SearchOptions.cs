namespace LogJoint.UI.Presenters.LogViewer
{
	public struct SearchOptions
	{
		public IFiltersList Filters;
		public bool ReverseSearch;
		public bool HighlightResult;
		public bool SearchOnlyWithinFocusedMessage;
	};
};