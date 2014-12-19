using System.Windows.Forms;
using LogJoint.UI.Presenters.Options.Appearance;
using System;
using System.Linq;

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

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		Presenters.LogViewer.IView IView.PreviewLogView { get { return logViewerControl1; } }

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

		void IView.SetFontSizeControl(int[] options, int currentValue)
		{
			fontSizeEditor.AllowedValues = options;
			fontSizeEditor.Value = currentValue;
		}

		int IView.GetFontSizeControlValue()
		{
			return fontSizeEditor.Value;
		}

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
				presenter.OnSelectedValueChanged(ctrl.Value);
		}

		private void fontSizeEditor_ValueChanged(object sender, System.EventArgs e)
		{
			presenter.OnFontSizeValueChanged();
		}


		IViewEvents presenter;
		Tuple<ViewControl, Control>[] controls;

		private void label1_Click(object sender, EventArgs e)
		{

		}
	}
}
