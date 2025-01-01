using System;
using System.Windows.Forms;
using LogJoint.UI.Presenters.FormatsWizard.ChooseOperationPage;

namespace LogJoint.UI
{
    public partial class ChooseOperationPage : UserControl, IView
    {
        IViewEvents eventsHandler;

        public ChooseOperationPage()
        {
            InitializeComponent();
        }

        ControlId IView.SelectedControl
        {
            get
            {
                if (importLog4NetRadioButton.Checked)
                    return ControlId.ImportLog4NetButton;
                if (importNLogRadioButton.Checked)
                    return ControlId.ImportNLogButton;
                if (changeRadioButton.Checked)
                    return ControlId.ChangeFormatButton;
                if (newREBasedFmtRadioButton.Checked)
                    return ControlId.NewREBasedButton;
                if (newXmlBasedFmtRadioButton.Checked)
                    return ControlId.NewXMLBasedButton;
                if (newJsonBasedFmtRadioButton.Checked)
                    return ControlId.NewJsonBasedButton;
                return ControlId.None;
            }
        }

        private void cloneRadioButton_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Clicks >= 2)
                eventsHandler.OnOptionDblClicked();
        }

        void IView.SetEventsHandler(IViewEvents eventsHandler)
        {
            this.eventsHandler = eventsHandler;
        }
    }
}
