using LogJoint.UI;
using System.Windows.Forms;

namespace LogJoint.Extensibility
{
	class View: UI.Windows.IView
	{
		public View(
			IWinFormsComponentsInitializer winFormsComponentsInitializer
		)
		{
			this.winFormsComponentsInitializer = winFormsComponentsInitializer;
		}

		void UI.Windows.IView.RegisterToolForm(Form f)
		{
			winFormsComponentsInitializer.InitOwnedForm(f, false);
		}

		readonly IWinFormsComponentsInitializer winFormsComponentsInitializer;
	};
}
