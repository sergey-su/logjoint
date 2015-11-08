using LogJoint.UI;
using System.Windows.Forms;

namespace LogJoint.Extensibility
{
	class View: IView
	{
		public View(
			UI.ILogProviderUIsRegistry logProviderUIsRegistry,
			IWinFormsComponentsInitializer winFormsComponentsInitializer
		)
		{
			this.logProviderUIsRegistry = logProviderUIsRegistry;
			this.winFormsComponentsInitializer = winFormsComponentsInitializer;
		}

		void IView.RegisterToolForm(Form f)
		{
			winFormsComponentsInitializer.InitOwnedForm(f, false);
		}

		UI.ILogProviderUIsRegistry IView.LogProviderUIsRegistry { get { return logProviderUIsRegistry; } }

		UI.ILogProviderUIsRegistry logProviderUIsRegistry;
		IWinFormsComponentsInitializer winFormsComponentsInitializer;
	};
}
