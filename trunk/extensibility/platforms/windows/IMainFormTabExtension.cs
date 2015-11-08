using System.Windows.Forms;

namespace LogJoint.Extensibility
{
	public interface IMainFormTabExtension
	{
		Control PageControl { get; }
		string Caption { get; }
		void OnTabPageSelected();
	};
}
