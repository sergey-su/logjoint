using Foundation;
using AppKit;
using LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{
	public class CollectionViewDelegate : NSCollectionViewDelegateFlowLayout
	{
		CollectionViewDataSource dataSource;
		Drawing.Resources resources;

		public CollectionViewDelegate(CollectionViewDataSource dataSource, Drawing.Resources resources)
		{
			this.dataSource = dataSource;
			this.resources = resources;
		}

		public override void ItemsSelected(NSCollectionView collectionView, NSSet indexPaths)
		{
			var paths = indexPaths.ToArray<NSIndexPath>();
			var index = (int)paths[0].Item;

			// Datasource.Data[index];
		}

		public override void ItemsDeselected(NSCollectionView collectionView, NSSet indexPaths)
		{
			var paths = indexPaths.ToArray<NSIndexPath>();
			var index = paths[0].Item;

			// Clear selection
			// ParentViewController.PersonSelected = null;
		}

		public override CoreGraphics.CGSize SizeForItem(NSCollectionView collectionView, NSCollectionViewLayout collectionViewLayout, NSIndexPath indexPath)
		{
			// NSCollectionView sucks. It's hard to make it calc items sizes.
			// Calculating the size manually here.

			var i = dataSource.Data[(int)indexPath.Item];

			using (var g = new Graphics())
			{
				var sz = g.MeasureString(i.Label, resources.AxesFont);
				sz.Width += 
					40 /* preview width */ 
					+ 3 /* preview left padding */ 
					+ 3 /* space between preview and label */
					+ 1 /* extra space just in case */;
				sz.Height += 
					1 /* extra space just in case */;
		
				return sz.ToCGSize();
			}
		}
	}
}
