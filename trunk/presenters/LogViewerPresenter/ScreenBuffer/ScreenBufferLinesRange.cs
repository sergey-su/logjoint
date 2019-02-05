using System.Collections.Generic;

namespace LogJoint.UI.Presenters.LogViewer
{
	struct ScreenBufferLinesRange
	{
		public List<DisplayLine> Lines;
		public long BeginPosition, EndPosition;
	};
};