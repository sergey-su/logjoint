using System;
using MonoMac.AppKit;
using System.Collections.Generic;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class PropertiesViewDataSource: NSTableViewDataSource
	{
		public IList<KeyValuePair<string, object>> data = new KeyValuePair<string, object>[0];

		public override int GetRowCount (NSTableView tableView)
		{
			return data.Count;
		}
	};
}

