using System;
using System.Linq;

using Foundation;
using AppKit;
using LogJoint.UI.Presenters.Options.Dialog;

namespace LogJoint.UI
{
	public partial class OptionsWindowController : NSWindowController, IDialog, Presenters.Options.Plugins.IView
	{
		readonly Mac.IReactive reactive;
		IDialogViewModel viewModel;
		Reactive.INSTableViewController<Presenters.Options.Plugins.IPluginListItem> pluginsTableController;
		Presenters.Options.Plugins.IViewModel pluginsViewModel;

		public OptionsWindowController (Mac.IReactive reactive) : base ("OptionsWindow")
		{
			this.reactive = reactive;
		}

		public override void AwakeFromNib ()
		{
			base.AwakeFromNib ();

			pluginsTableController = reactive.CreateTableViewController<Presenters.Options.Plugins.IPluginListItem>(pluginsTableView);
		}

		public new OptionsWindow Window {
			get { return (OptionsWindow)base.Window; }
		}

		partial void onCancelButtonClicked (Foundation.NSObject sender) => viewModel.OnCancelPressed();

		partial void onOkButtonClicked (Foundation.NSObject sender) => viewModel.OnOkPressed();

		partial void pluginActionButtonClicked (Foundation.NSObject sender) => pluginsViewModel?.OnAction ();

		void IDialog.SetViewModel (IDialogViewModel viewModel)
		{
			Window.EnsureCreated();

			this.viewModel = viewModel;

			// todo: show/hide pages by viewModel.VisiblePages
		}

		void IDialog.Show (PageId? initiallySelectedPage)
		{
			// todo: initiallySelectedPage is not implemenented because there is only one page atm
			NSApplication.SharedApplication.RunModalForWindow(Window);
		}

		void IDialog.Hide ()
		{
			NSApplication.SharedApplication.StopModal();
			Window.Close();
		}

		void Presenters.Options.Plugins.IView.SetViewModel (Presenters.Options.Plugins.IViewModel viewModel)
		{
			Window.EnsureCreated();

			pluginsViewModel = viewModel;

			pluginsTableController.OnSelect = sel => viewModel.OnSelect(sel.FirstOrDefault());

			var updateList = Updaters.Create(
				() => viewModel.ListItems,
				pluginsTableController.Update
			);

			var updateStatus = Updaters.Create(
				() => viewModel.Status,
				status =>
				{
					pluginsLoadingIndicator.Hidden =
						(status.flags & Presenters.Options.Plugins.StatusFlags.IsProgressIndicatorVisible) == 0;
					pluginsStatusLabel.Hidden = status.text == null;
					pluginsStatusLabel.StringValue = status.text ?? "";
					pluginsStatusLabel.TextColor =
						(status.flags & Presenters.Options.Plugins.StatusFlags.IsError) != 0 ?
							NSColor.SystemRedColor : NSColor.LabelColor;
				}
			);

			var updateSelectedPluginControls = Updaters.Create(
				() => viewModel.SelectedPluginData,
				data =>
				{
					pluginActionButton.Enabled = data.ActionButton.Enabled;
					pluginActionButton.Title = data.ActionButton.Caption;
					pluginHeaderLabel.StringValue = data.Caption;
					pluginDetailsLabel.Value = data.Description;
				}
			);

			viewModel.ChangeNotification.CreateSubscription(() =>
			{
				updateList();
				updateStatus();
				updateSelectedPluginControls();
			});
		}

		Presenters.Options.MemAndPerformancePage.IView IDialog.MemAndPerformancePage => null;

		Presenters.Options.Appearance.IView IDialog.ApperancePage => null;

		Presenters.Options.UpdatesAndFeedback.IView IDialog.UpdatesAndFeedbackPage => null;

		Presenters.Options.Plugins.IView IDialog.PluginsPage => this;
	}

	public class OptionsView : IView
	{
		readonly Mac.IReactive reactive;

		public OptionsView(Mac.IReactive reactive)
		{
			this.reactive = reactive;
		}

		IDialog IView.CreateDialog ()
		{
			return new OptionsWindowController(reactive);
		}
	};
}
