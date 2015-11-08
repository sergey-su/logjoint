using System;
using System.Collections.Generic;
using System.Text;

#if WIN
using System.Windows.Forms;
#elif MONOMAC
using MonoMac.AppKit;
#endif

namespace LogJoint.Extensibility
{
	public interface IApplication
	{
		Extensibility.IPresentation Presentation { get; }
		Extensibility.IModel Model { get; }

		#if WIN
		void RegisterToolForm(Form f);
		UI.ILogProviderUIsRegistry LogProviderUIsRegistry { get; }
		#endif
	};

	public class PluginBase : IDisposable
	{
		public virtual void Init(IApplication app) { }
		public virtual IEnumerable<IMainFormTabExtension> MainFormTabExtensions { get { yield break; } }
		public virtual void Dispose() { }
	};

	public interface IMainFormTabExtension
	{
		#if WIN
		Control PageControl { get; }
		#elif MONOMAC
		NSView PageControl { get; }
		#endif
		string Caption { get; }
		void OnTabPageSelected();
	};
}
