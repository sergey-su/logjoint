using System;
using System.Collections.Generic;
using System.Linq;
using Foundation;
using AppKit;
using LogJoint.PacketAnalysis.UI.Presenters.MessagePropertiesDialog;

namespace LogJoint.PacketAnalysis.UI
{
	public partial class MessageContentViewController : AppKit.NSViewController, IView
	{
		#region Constructors

		// Called when created from unmanaged code
		public MessageContentViewController(IntPtr handle) : base(handle)
		{
			Initialize();
		}

		// Called when created directly from a XIB file
		[Export("initWithCoder:")]
		public MessageContentViewController(NSCoder coder) : base(coder)
		{
			Initialize();
		}

		// Call to load from the XIB/NIB file
		public MessageContentViewController() : base("MessageContentView", NSBundle.MainBundle)
		{
			Initialize();
		}

		// Shared initialization code
		void Initialize()
		{
		}

		#endregion

		//strongly typed view accessor
		public new MessageContentView View
		{
			get
			{
				return (MessageContentView)base.View;
			}
		}

		object IView.OSView => View;

		void IView.SetViewModel(IViewModel viewModel)
		{
			View.GetHashCode();

			var @delegate = new TreeViewDelegate { viewModel = viewModel };
			treeView.Delegate = @delegate;

			var updateTree = Updaters.Create(
				() => viewModel.Root,
				root =>
				{
					@delegate.updating = true;
					try
					{
						var rootNode = new Node(root);
						treeView.DataSource = new TreeDataSource { root = rootNode };
						void ApplyNodeState(Node node)
						{
							if (node.Reference.IsExpanded)
								treeView.ExpandItem(node, expandChildren: false);
							if (node.Reference.IsSelected)
								treeView.SelectRow(treeView.RowForItem(node), byExtendingSelection: false);
							node.Children.ForEach(ApplyNodeState);
						}
						ApplyNodeState(rootNode);
					}
					finally
					{
						@delegate.updating = false;
					}
				}
			);

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateTree();
			});
		}

		class TreeDataSource : NSOutlineViewDataSource
		{
			public Node root;

			public override nint GetChildrenCount(NSOutlineView outlineView, NSObject item)
			{
				return GetNode(item).Children.Count;
			}

			public override NSObject GetChild(NSOutlineView outlineView, nint childIndex, NSObject item)
			{
				return GetNode(item).Children[(int)childIndex];
			}

			public override bool ItemExpandable(NSOutlineView outlineView, NSObject item)
			{
				return GetNode(item).Children.Count > 0;
			}

			Node GetNode(NSObject item)
			{
				return item == null ? root : (Node)item; ;
			}
		};

		class Node : NSObject
		{
			public IViewTreeNode Reference { get; private set; }
			public List<Node> Children { get; private set; }

			public Node(IViewTreeNode n)
			{
				Reference = n ?? throw new ArgumentNullException();
				Children = n.Children.Select(c => new Node(c)).ToList();
			}
		};

		class TreeViewDelegate : NSOutlineViewDelegate
		{
			public IViewModel viewModel;
			public bool updating;

			public override NSView GetView(NSOutlineView outlineView,
				NSTableColumn tableColumn, NSObject item)
			{
				NSTextField view = (NSTextField)outlineView.MakeView("view", this);
				if (view == null)
				{
					view = NSTextField.CreateLabel("");
					view.Font = NSFont.SystemFontOfSize(NSFont.SmallSystemFontSize); // todo: monospace?
				}
				view.StringValue = ((Node)item).Reference.Text;
				return view;
			}

			public override bool ShouldExpandItem(NSOutlineView outlineView, NSObject item)
			{
				if (updating)
					return true;
				viewModel.OnExpand((item as Node)?.Reference);
				return false;
			}

			public override bool ShouldCollapseItem(NSOutlineView outlineView, NSObject item)
			{
				if (updating)
					return true;
				viewModel.OnCollapse((item as Node)?.Reference);
				return false;
			}

			public override bool ShouldSelectItem(NSOutlineView outlineView, NSObject item)
			{
				if (updating)
					return true;
				viewModel.OnSelect((item as Node)?.Reference);
				return true;
			}
		};
	}
}
