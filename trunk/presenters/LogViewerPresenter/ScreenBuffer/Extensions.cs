namespace LogJoint.UI.Presenters.LogViewer
{
	public static class Extenstions
	{
		public static ViewLine ToViewLine(this ScreenBufferEntry e)
		{
			return new ViewLine()
			{
				Message = e.Message,
				LineIndex = e.Index,
				TextLineIndex = e.TextLineIndex,
			};
		}
	};
};