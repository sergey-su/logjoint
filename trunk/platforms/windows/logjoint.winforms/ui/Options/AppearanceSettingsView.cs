using System.Windows.Forms;
using LogJoint.UI.Presenters.Options.Appearance;
using System.Collections.Generic;
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
				Tuple.Create(ViewControl.ColoringNoneRadioButton, (Control)radioButton1),
				Tuple.Create(ViewControl.ColoringThreadsRadioButton, (Control)radioButton2),
				Tuple.Create(ViewControl.ColoringSourcesRadioButton, (Control)radioButton3)
			};
		}

		void IView.SetPresenter(IViewEvents presenter)
		{
			this.presenter = presenter;
		}

		Presenters.LogViewer.IView IView.PreviewLogView { get { return logViewerControl1; } }

		void IView.SetControlChecked(ViewControl control, bool value)
		{
			var btn = IdToControl(control) as RadioButton;
			if (btn != null)
				btn.Checked = value;
		}

		bool IView.GetControlChecked(ViewControl control)
		{
			var btn = IdToControl(control) as RadioButton;
			return btn != null ? btn.Checked : false;
		}

		void IView.SetFontFamiliesControl(string[] options, int selectedOption)
		{
			fontFamiliesComboBox.Items.Clear();
			fontFamiliesComboBox.Items.AddRange(options);
			fontFamiliesComboBox.SelectedIndex = selectedOption;
		}
		void IView.SetFontSizeControl(int[] options, int currentValue)
		{
			fontSizeEditor.AllowedValues = options;
			fontSizeEditor.Value = currentValue;
		}

		int IView.GetSelectedFontFamily()
		{
			return fontFamiliesComboBox.SelectedIndex;
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
			presenter.OnSelectedFontChanged();
		}

		private void fontSizeEditor_ValueChanged(object sender, System.EventArgs e)
		{
			presenter.OnFontSizeValueChanged();
		}

		private void radioButton1_CheckedChanged(object sender, System.EventArgs e)
		{
			var rb = sender as RadioButton;
			var ctrlId = ControlToId(rb);
			if (rb == null || !rb.Checked || ctrlId == null)
				return;
			presenter.OnRadioButtonChecked(ctrlId.Value);
		}


		IViewEvents presenter;
		Tuple<ViewControl, Control>[] controls;
	}
}
