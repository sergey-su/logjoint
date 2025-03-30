using System.Windows.Forms;
using LogJoint.UI.Presenters.Options.Appearance;
using System;
using System.Linq;
using static LogJoint.Settings.Appearance;
using System.Collections.Generic;
using System.Drawing;

namespace LogJoint.UI
{
    public partial class AppearanceSettingsView : UserControl, IView
    {
        public AppearanceSettingsView()
        {
            InitializeComponent();

            controls = new Tuple<ViewControl, Control>[]
            {
                Tuple.Create(ViewControl.ColoringSelector, (Control)coloringModeComboBox),
                Tuple.Create(ViewControl.FontFamilySelector, (Control)fontFamiliesComboBox),
                Tuple.Create(ViewControl.PaletteSelector, (Control)paletteComboBox),
            };
        }

        public void SetViewModel(IViewModel viewModel)
        {
            this.viewModel = viewModel;
            viewModel.SetView(this);

            logViewerControl1.SetViewModel(viewModel.LogView);
        }

        void IView.SetSelectorControl(ViewControl selector, string[] options, int selectedOption)
        {
            var ctrl = IdToControl(selector) as ComboBox;
            if (ctrl == null)
                return;
            ctrl.Items.Clear();
            ctrl.Items.AddRange(options);
            ctrl.SelectedIndex = selectedOption;
        }

        int IView.GetSelectedValue(ViewControl selector)
        {
            var ctrl = IdToControl(selector) as ComboBox;
            if (ctrl == null)
                return -1;
            return ctrl.SelectedIndex;
        }

        string[] IView.AvailablePreferredFamilies
        {
            get
            {
                if (availablePreferredFontFamilies == null)
                    availablePreferredFontFamilies = LogViewerControl.GetAvailablePreferredFontFamilies();
                return availablePreferredFontFamilies;
            }
        }

        KeyValuePair<LogFontSize, int>[] IView.FontSizes
        {
            get
            {
                if (fontSizesMap == null)
                    fontSizesMap = LogViewerControl.MakeFontSizesMap();
                return fontSizesMap;
            }
        }

        Presenters.LabeledStepperPresenter.IView IView.FontSizeControlView => fontSizeEditor;

        Control IdToControl(ViewControl controlId)
        {
            return controls.Where(t => t.Item1 == controlId).Select(t => t.Item2).FirstOrDefault();
        }

        ViewControl? ControlToId(Control control)
        {
            return controls.Where(t => t.Item2 == control).Select(t => new ViewControl?(t.Item1)).FirstOrDefault();
        }

        private void fontFamiliesComboBox_SelectedIndexChanged(object sender, System.EventArgs e)
        {
            var ctrl = ControlToId(sender as Control);
            if (ctrl != null)
                viewModel.OnSelectedValueChanged(ctrl.Value);
        }

        IViewModel viewModel;
        Tuple<ViewControl, Control>[] controls;
        string[] availablePreferredFontFamilies;
        KeyValuePair<LogFontSize, int>[] fontSizesMap;
    }
}
