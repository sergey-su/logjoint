using System.Windows.Forms;

namespace LogJoint.UI.Windows
{
	public interface IView
	{
		void RegisterToolForm(Form f);
		Reactive.IReactive Reactive { get; }
	};
}
