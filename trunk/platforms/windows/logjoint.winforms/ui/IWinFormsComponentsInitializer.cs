using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace LogJoint.UI
{
	public interface IWinFormsComponentsInitializer
	{
		void InitOwnedForm(Form form, bool takeOwnership = true);
	}
}
