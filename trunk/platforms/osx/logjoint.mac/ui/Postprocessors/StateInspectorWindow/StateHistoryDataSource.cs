using System;
using AppKit;
using System.Collections.Generic;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using Foundation;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class StateHistoryDataSource: NSTableViewDataSource
	{
		public List<StateHistoryItem> data = new List<StateHistoryItem>();

		public override nint GetRowCount (NSTableView tableView)
		{
			return data.Count;
		}
	}
}

