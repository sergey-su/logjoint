namespace LogJoint.UI.Presenters.LogViewer
{
	public struct SearchOptions
	{
		public Search.Options CoreOptions;
		public IUserDefinedSearch UDS;
		public bool HighlightResult;
		public bool SearchOnlyWithinFocusedMessage;
	};
};