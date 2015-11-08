using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.Extensibility
{
	public interface IView
	{
		void RegisterToolForm(Form f);
		UI.ILogProviderUIsRegistry LogProviderUIsRegistry { get; }
	};
}
