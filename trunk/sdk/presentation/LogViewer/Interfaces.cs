using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IPresenter
	{
		IMessage FocusedMessage { get; }
		IReadOnlyList<VisibleLine> VisibleLines { get; }
		Task GoHome();
		Task GoToEnd();
	}

	[DebuggerDisplay("{Value}")]

	public struct VisibleLine
	{
		public string Value { get; internal set; }
	};
}
