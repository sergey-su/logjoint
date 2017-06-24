using System;
using AppKit;
using Foundation;

namespace LogJoint.UI.Postprocessing.TimeSeriesVisualizer
{

	[Register ("NSDynamicCollectionView")]
	public class NSDynamicCollectionView: NSCollectionView
	{
		public NSDynamicCollectionView(IntPtr ptr): base(ptr)
		{
		}

		public override void Layout()
		{
			base.Layout();
			InvalidateIntrinsicContentSize();
		}

		public override CoreGraphics.CGSize IntrinsicContentSize
		{
			get
			{
				nfloat w = 0;
				var nrOfItems = this.GetNumberOfItems(0);
				if (nrOfItems > 0)
					using (var idx = NSIndexPath.FromItemSection(nrOfItems - 1, 0))
						w = GetLayoutAttributes(idx).Frame.Bottom;
				return new CoreGraphics.CGSize(0, w);
			}
		}
	}
}
