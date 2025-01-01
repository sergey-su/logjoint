using LogJoint.UI.Presenters.TagsList;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace LogJoint.UI
{
    public partial class TagsListControl : UserControl, IView
    {
        IViewModel viewModel;
        ISubscription subscription;

        public TagsListControl()
        {
            InitializeComponent();
        }

        private void allTagsLinkLabel_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            viewModel.OnEditLinkClicked();
        }

        void IView.SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;
            var linkTextUpdater = Updaters.Create(
                () => viewModel.EditLinkValue,
                value =>
                {
                    var (text, clickablePartBegin, clickablePartLength) = value;
                    allTagsLinkLabel.Text = text;
                    allTagsLinkLabel.LinkArea = new LinkArea(clickablePartBegin, clickablePartLength);
                }
            );
            subscription = viewModel.ChangeNotification.CreateSubscription(() =>
            {
                linkTextUpdater();
            });
        }

        IDialogView IView.CreateDialog(IDialogViewModel dialogViewModel, IEnumerable<string> tags, string initiallyFocusedTag)
        {
            return new AllTagsDialog(viewModel.ChangeNotification, dialogViewModel, tags, initiallyFocusedTag);
        }
    }
}
