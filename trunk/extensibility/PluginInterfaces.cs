using System;
using System.Collections.Generic;
using System.Text;

#if WIN
using System.Windows.Forms;
#elif MONOMAC
using MonoMac.AppKit;
#endif

namespace LogJoint
{
	// todo: refactor to expose clear object model
	// todo: rename to IApplication, put to ns Extensibility
	public interface ILogJointApplication
	{
		Extensibility.IPresentation Presentation { get; }
		//Extensibility.IModel Model { get; }

		// todo: consider getting rid of this IModel all together
		IModel Model { get; }

		// todo: model stuff. migrate user to ILogJointApplication.Model.XXX
		Telemetry.ITelemetryCollector Telemetry { get; }
		Persistence.IWebContentCache WebContentCache { get; }
		Persistence.IStorageManager StorageManager { get; }
		IInvokeSynchronization UIInvokeSynchronization { get; }

		// below is UI related stuff. todo: develop and expose presenters interfaces
		IMessage FocusedMessage { get; }
		IMessagesCollection LoadedMessagesCollection { get; }
		void SelectMessageAt(IBookmark bmk, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified);
		UI.Presenters.LoadedMessages.IPresenter LoadedMessagesPresenter { get; }
		UI.Presenters.IPresentersFacade PresentersFacade { get; }
		event EventHandler FocusedMessageChanged;
		event EventHandler SourcesChanged;

		#if WIN
		void RegisterToolForm(Form f);
		UI.ILogProviderUIsRegistry LogProviderUIsRegistry { get; }
		#endif
	};

	public class PluginBase : IDisposable
	{
		public virtual void Init(ILogJointApplication app) { }
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
