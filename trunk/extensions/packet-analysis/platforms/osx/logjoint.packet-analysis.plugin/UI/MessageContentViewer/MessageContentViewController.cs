using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.PacketAnalysis.UI.Presenters.MessagePropertiesDialog;
using LogJoint.UI;

namespace LogJoint.PacketAnalysis.UI
{
	public partial class MessageContentViewController : AppKit.NSViewController, IView
	{
		private readonly LogJoint.UI.Mac.IView ljView;
		private LogJoint.UI.Reactive.INSOutlineViewController<IViewTreeNode> treeController;

		public MessageContentViewController(LogJoint.UI.Mac.IView ljView, NSBundle bundle) :
			base("MessageContentView", bundle)
		{
			this.ljView = ljView;
		}

		public new MessageContentView View => (MessageContentView)base.View;

		object IView.OSView => View;

		void IView.SetViewModel(IViewModel viewModel)
		{
			View.GetHashCode();

			treeController = ljView.Reactive.CreateOutlineViewController<IViewTreeNode>(treeView);
			treeController.OnExpand = viewModel.OnExpand;
			treeController.OnCollapse = viewModel.OnCollapse;
			treeController.OnSelect = n => viewModel.OnSelect(n.FirstOrDefault());
			treeController.OnCreateView = (column, n) => CreateTreeNodeView(n);

			var updateTree = Updaters.Create(
				() => viewModel.Root,
				root => {
					treeController.Update(root);
					AutoSizeColumn(treeView, 0);
				}
			);

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateTree();
			});

			View.onCopy = viewModel.OnCopy;
		}

		NSView CreateTreeNodeView(IViewTreeNode node)
		{
			var view = (NSTextField)treeView.MakeView("view", this);
			if (view == null)
			{
				view = NSTextField.CreateLabel("");
				view.Font = NSFont.SystemFontOfSize(NSFont.SmallSystemFontSize);
			}
			view.StringValue = node.Text;
			return view;
		}

		public static void AutoSizeColumn(NSTableView table, int columnIdx)
		{
			nfloat width = 0;
			for (nint rowIdx = 0; rowIdx < table.RowCount; ++rowIdx)
			{
				var view = table.GetView(columnIdx, rowIdx, makeIfNecessary: true);
				if (view == null)
					continue;
				var w = view.IntrinsicContentSize.Width;
				if (w > width)
					width = w;
			}
			table.TableColumns()[columnIdx].Width = width;
		}
	}
}
