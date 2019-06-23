using LogJoint.UI;
using LogJoint.UI.Windows;
using LogJoint.UI.Windows.Reactive;
using System.Windows.Forms;

namespace LogJoint.Extensibility
{
	class View: UI.Windows.IView, UI.Windows.Reactive.IReactive
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

		IReactive IView.Reactive => this;

		ITreeViewController IReactive.CreateTreeViewController(TreeView treeView)
		{
			return new TreeViewController(treeView);
		}


		readonly IWinFormsComponentsInitializer winFormsComponentsInitializer;
	};
}
