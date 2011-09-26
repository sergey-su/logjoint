using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint
{
	class UIFactory : IFactoryUIFactory
	{
		public ILogProviderFactoryUI CreateFileProviderFactoryUI(IFileBasedLogProviderFactory readerFactory)
		{
			return new FileLogFactoryUI(readerFactory);
		}

		public ILogProviderFactoryUI CreateDebugOutputStringUI()
		{
			return new DebugOutput.DebugOutputFactoryUI();
		}
	}
}
