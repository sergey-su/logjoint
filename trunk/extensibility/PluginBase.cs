using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint.Extensibility
{
	public class PluginBase : IDisposable
	{
		public virtual void Init(IApplication app) { }
		public virtual IEnumerable<IMainFormTabExtension> MainFormTabExtensions { get { yield break; } }
		public virtual void Dispose() { }
	};
}
