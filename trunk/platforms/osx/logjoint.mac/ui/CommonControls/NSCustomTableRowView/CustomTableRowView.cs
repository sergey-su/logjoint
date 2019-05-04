using AppKit;

namespace LogJoint.UI
{
	public class NSCustomTableRowView : NSTableRowView
	{
		public bool InvalidateSubviewsOnSelectionChange { get; set; }

		public override bool Selected {
			get => base.Selected;
			set {
				if (InvalidateSubviewsOnSelectionChange && base.Selected != value) {
					foreach (var sv in Subviews)
						sv.NeedsDisplay = true;
				}
				base.Selected = value;
			}
		}
	};
}
