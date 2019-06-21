using System;
using AppKit;
using PR = LogJoint.UI.Presenters.Reactive;

namespace LogJoint
{
	public interface IApplication
	{
		IModel Model { get; }
		UI.Presenters.IPresentation Presentation { get; }
		UI.Mac.IView View { get; }
	};

	namespace UI.Mac
	{
		public interface IView
		{
			IReactive Reactive { get; }
		};

		public interface IReactive
		{
			Reactive.INSOutlineViewController CreateOutlineViewController(NSOutlineView outlineView);
		};
	}

	namespace UI.Reactive
	{
		public interface INSOutlineViewController
		{
			void Update(PR.ITreeNode newRoot);
			Action<PR.ITreeNode> OnExpand { get; set; }
			Action<PR.ITreeNode> OnCollapse { get; set; }
			Action<PR.ITreeNode[]> OnSelect { get; set; }
			Func<NSTableColumn, PR.ITreeNode, NSView> OnView { get; set; }
		};
	}
}
