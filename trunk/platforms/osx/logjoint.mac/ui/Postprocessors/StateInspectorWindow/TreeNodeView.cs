using System;
using AppKit;
using Foundation;
using System.Drawing;
using CoreGraphics;
using LogJoint.Drawing;

namespace LogJoint.UI.Postprocessing.StateInspector
{
	class TreeNodeView: NSView
	{
		public StateInspectorWindowController owner;
		public Node node;

		public void Update(Node node)
		{
			this.node = node;
			this.NeedsDisplay = true;
		}

		public override void DrawRect (CGRect dirtyRect)
		{
			base.DrawRect (dirtyRect);

			var mainText = new NSString (node.text);
			var bounds = this.Bounds.ToRectangleF();
			bool isSelected = owner.TreeView.IsRowSelected (owner.TreeView.RowForItem (node));

			var mainTextAttrs = new NSMutableDictionary ();
			var mainTextPara = new NSMutableParagraphStyle ();
			mainTextPara.LineBreakMode = NSLineBreakMode.TruncatingTail;
			mainTextPara.TighteningFactorForTruncation = 0;
			mainTextAttrs.Add (NSStringAttributeKey.ParagraphStyle, mainTextPara);
			if (isSelected)
				mainTextAttrs.Add (NSStringAttributeKey.ForegroundColor, NSColor.SelectedText);
			else
				mainTextAttrs.Add (NSStringAttributeKey.ForegroundColor, NSColor.Text);

			var mainTextSz = mainText.StringSize (mainTextAttrs).ToSizeF ();
			float nodeTextAndPrimaryPropHorzSpacing = 8;
			float spaceAvailableForDefaultPropValue =
				bounds.Width - mainTextSz.Width - nodeTextAndPrimaryPropHorzSpacing;

			var paintInfo = owner.ViewModel.OnPaintNode(node.ToNodeInfo(), spaceAvailableForDefaultPropValue > 30);
			if (paintInfo.PrimaryPropValue != null)
			{
				var r = new RectangleF(
					bounds.Right - spaceAvailableForDefaultPropValue,
					bounds.Y,
					spaceAvailableForDefaultPropValue,
					bounds.Height);

				// todo: move dict to a static field
				var dict = new NSMutableDictionary ();
				var para = new NSMutableParagraphStyle ();
				para.Alignment = NSTextAlignment.Right;
				para.LineBreakMode = NSLineBreakMode.TruncatingTail;
				para.TighteningFactorForTruncation = 0;
				dict.Add (NSStringAttributeKey.ParagraphStyle, para);
				if (isSelected)
					dict.Add (NSStringAttributeKey.ForegroundColor, NSColor.FromDeviceRgba(0.9f, 0.9f, 0.9f, 1f));
				else
					dict.Add (NSStringAttributeKey.ForegroundColor, NSColor.Gray);

				new NSString (paintInfo.PrimaryPropValue).DrawString (r.ToCGRect (), dict);
			}

			mainText.DrawString (bounds.ToCGRect (), mainTextAttrs);
		}
	};

}

