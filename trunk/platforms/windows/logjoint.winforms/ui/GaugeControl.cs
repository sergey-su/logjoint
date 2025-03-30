using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.LabeledStepperPresenter;

namespace LogJoint.UI
{
    public partial class GaugeControl : UserControl
    {
        IViewModel viewModel;
        ISubscription subscription;

        public GaugeControl()
        {
            InitializeComponent();
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;

            var updateView = Updaters.Create(
                () => viewModel.EnabledLabel, () => viewModel.Label,
                () => viewModel.EnabledUp, () => viewModel.EnabledDown,
                (enabledLabel, label, enabledUp, enabledDown) =>
                {
                    valueLabel.Text = label;
                    upButton.Enabled = enabledUp;
                    downButton.Enabled = enabledDown;
                    this.Enabled = enabledUp | enabledDown | enabledLabel;
                });

            subscription = viewModel.ChangeNotification.CreateSubscription(updateView);
        }

        private void upButton_Click(object sender, EventArgs e)
        {
            viewModel.OnUpButtonClicked();
        }

        private void downButton_Click(object sender, EventArgs e)
        {
            viewModel.OnDownButtonClicked();
        }
    }
}
