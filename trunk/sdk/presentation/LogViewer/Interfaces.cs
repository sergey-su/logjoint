using System.Collections.Generic;
using System.Diagnostics;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IPresenter
	{
		IMessage FocusedMessage { get; }
		IReadOnlyList<VisibleLine> VisibleLines { get; }
	}

	[DebuggerDisplay("{Value}")]

	public struct VisibleLine
	{
		public string Value { get; internal set; }
	};
}
