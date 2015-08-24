using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public interface ILogProviderUI : IDisposable
	{
		Control UIControl { get; }
		void Apply(IModel model);
	};

	public interface ILogProviderUIsRegistry
	{
		ILogProviderUI CreateProviderUI(ILogProviderFactory factory);
		void Register(string key, Func<ILogProviderFactory, ILogProviderUI> createUi);
	};


}
