using System;
using MonoMac.AppKit;
using MonoMac.Foundation;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public class SourcesListOutlineDataSource: NSOutlineViewDataSource
	{
		public List<SourcesListItem> Items = new List<SourcesListItem>();

		public override int GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			if (item == null)
				return Items.Count;
			else
				return 0;
		}

		public override NSObject GetChild (NSOutlineView outlineView, int childIndex, NSObject item)
		{
			if (item == null)
				return Items [childIndex];
			else
				return null;
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			return false;
		}
	}
}

