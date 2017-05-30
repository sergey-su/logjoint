using System;
using MonoMac.AppKit;
using System.Collections.Generic;
using LogJoint.UI.Presenters.Postprocessing.StateInspectorVisualizer;
using MonoMac.Foundation;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class StateHistoryDataSource: NSTableViewDataSource
	{
		public List<StateHistoryItem> data = new List<StateHistoryItem>();

		public override int GetRowCount (NSTableView tableView)
		{
			return data.Count;
		}
	}
}

