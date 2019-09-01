using System;
using AppKit;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class PropertiesViewDataSource: NSTableViewDataSource
	{
		public IReadOnlyList<KeyValuePair<string, object>> data = new KeyValuePair<string, object>[0];

		public override nint GetRowCount (NSTableView tableView)
		{
			return data.Count;
		}
	};
}

