using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Net;

namespace LogJoint.Preprocessing
{
	public class PreprocessingManagerExtentionsRegistry : IPreprocessingManagerExtensionsRegistry
	{
		public PreprocessingManagerExtentionsRegistry()
		{
		}

		IEnumerable<IPreprocessingManagerExtension> IPreprocessingManagerExtensionsRegistry.Items
		{
			get { return items; }
		}

		void IPreprocessingManagerExtensionsRegistry.Register(IPreprocessingManagerExtension detector)
		{
			items.Add(detector);
		}

		readonly HashSet<IPreprocessingManagerExtension> items = new HashSet<IPreprocessingManagerExtension>();
	};
}
