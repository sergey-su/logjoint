using System.Linq;
using AppKit;
using Foundation;
using System;
using System.Collections.Generic;

namespace LogJoint.UI
{
	public class SourcesListOutlineDataSource: NSOutlineViewDataSource
	{
		public List<SourcesListItem> Items = new List<SourcesListItem>();

		public override nint GetChildrenCount (NSOutlineView outlineView, NSObject item)
		{
			if (item == null)
				return Items.Count;
			else
				return ((item as SourcesListItem)?.items?.Count).GetValueOrDefault();
		}

		public override NSObject GetChild (NSOutlineView outlineView, nint childIndex, NSObject item)
		{
			if (item == null)
				return Items [(int)childIndex];
			else
				return (item as SourcesListItem)?.items?.ElementAtOrDefault((int)childIndex);
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			return (item as SourcesListItem)?.items?.Count > 0;
		}
	}
}

