using System;
using System.Collections.Generic;
using AppKit;
using Foundation;
using LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public class CollectionViewDataSource : NSCollectionViewDataSource
	{
		public List<LegendItemInfo> Data = new List<LegendItemInfo>();
		public Drawing.Resources Resources;
		public IViewEvents Events;

		public override nint GetNumberOfSections(NSCollectionView collectionView)
		{
			return 1;
		}

		public override nint GetNumberofItems(NSCollectionView collectionView, nint section)
		{
			return Data.Count;
		}

		public override NSCollectionViewItem GetItem(NSCollectionView collectionView, NSIndexPath indexPath)
		{
			var item = collectionView.MakeItem("LegendItemCell", indexPath) as LegendItemController;
			item.Init(Data[(int)indexPath.Item], Resources, Events);
			return item;
		}
	}
}
