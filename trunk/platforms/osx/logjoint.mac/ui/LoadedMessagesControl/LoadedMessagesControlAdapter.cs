using System.Linq;
using System.Diagnostics;
using Foundation;
using LogJoint.UI.Presenters.LoadedMessages;
using AppKit;

namespace LogJoint.UI
{
	public partial class LoadedMessagesControlAdapter: NSViewController, IView
	{
		readonly LogViewerControlAdapter logViewerControlAdapter;
		IViewModel viewModel;

		public LoadedMessagesControlAdapter()
			: base("LoadedMessagesControl", NSBundle.MainBundle)
		{
			logViewerControlAdapter = new LogViewerControlAdapter ();
		}
			
		public override void AwakeFromNib()
		{
			logViewerControlAdapter.View.MoveToPlaceholder(logViewerPlaceholder);
		}

		void IView.SetViewModel(IViewModel viewModel)
		{
			this.viewModel = viewModel;

			var updateView = Updaters.Create (
				() => viewModel.ViewState,
				state => {
					toggleBookmarkButton.Hidden = !state.ToggleBookmark.Visible;
					toggleBookmarkButton.ToolTip = state.ToggleBookmark.Tooltip;

					rawViewButton.Hidden = !state.RawViewButton.Visible;
					rawViewButton.State = state.RawViewButton.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
					rawViewButton.ToolTip = state.RawViewButton.Tooltip;

					viewTailButton.Hidden = !state.ViewTailButton.Visible;
					viewTailButton.State = state.ViewTailButton.Checked ? NSCellStateValue.On : NSCellStateValue.Off;
					viewTailButton.ToolTip = state.ViewTailButton.Tooltip;

					navigationProgressIndicator.Hidden = !state.NavigationProgressIndicator.Visible;
					navigationProgressIndicator.ToolTip = state.NavigationProgressIndicator.Tooltip;

					coloringButton.Hidden = !state.Coloring.Visible;
					coloringLabel.Hidden = !state.Coloring.Visible;
					Debug.Assert (state.Coloring.Options.Count == coloringButton.ItemCount);
					foreach (var option in state.Coloring.Options.Select ((opt, idx) => (opt, idx))) {
						var item = coloringButton.ItemAtIndex (option.idx);
						item.Title = option.opt.Text;
						item.ToolTip = option.opt.Tooltip;
						item.Tag = option.idx;
					}
					coloringButton.SelectItem (state.Coloring.Selected);
				}
			);

			viewModel.ChangeNotification.CreateSubscription (updateView);
		}

		Presenters.LogViewer.IView IView.MessagesView => logViewerControlAdapter;

		partial void OnRawViewButtonClicked (NSObject sender) => viewModel.OnToggleRawView ();

		partial void OnToggleBookmarkButtonClicked (NSObject sender) => viewModel.OnToggleBookmark ();

		partial void OnViewTailButtonClicked (NSObject sender) => viewModel.OnToggleViewTail ();

		partial void OnColoringButtonClicked (NSObject sender) => viewModel.OnColoringButtonClicked ((int)coloringButton.SelectedItem.Tag);
	}
}

