using System.Linq;
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
				return ((item as SourcesListItem)?.items?.Count).GetValueOrDefault();
		}

		public override NSObject GetChild (NSOutlineView outlineView, int childIndex, NSObject item)
		{
			if (item == null)
				return Items [childIndex];
			else
				return (item as SourcesListItem)?.items?.ElementAtOrDefault(childIndex);
		}

		public override bool ItemExpandable (NSOutlineView outlineView, NSObject item)
		{
			return (item as SourcesListItem)?.items?.Count > 0;
		}
	}
}

