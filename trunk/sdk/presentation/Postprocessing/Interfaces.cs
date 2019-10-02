using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// todo
[assembly: InternalsVisibleTo("logjoint.presenters.tests")]
[assembly: InternalsVisibleTo("logjoint.presenters")]
[assembly: InternalsVisibleTo("logjoint.sdk")]

namespace LogJoint.UI.Presenters.Postprocessing
{
	public interface IPostprocessorOutputForm
	{
		void Show();
	};
}
