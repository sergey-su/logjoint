using LogJoint.UI;
using System.Windows.Forms;

namespace LogJoint.Extensibility
{
	class View: IView
	{
		public View(
			IWinFormsComponentsInitializer winFormsComponentsInitializer
		)
		{
			this.winFormsComponentsInitializer = winFormsComponentsInitializer;
		}

		void IView.RegisterToolForm(Form f)
		{
			winFormsComponentsInitializer.InitOwnedForm(f, false);
		}

		IWinFormsComponentsInitializer winFormsComponentsInitializer;
	};
}
