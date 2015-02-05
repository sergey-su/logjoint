using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;

namespace LogJoint
{
	public interface IMainFormTabExtension
	{
		Control PageControl { get; }
		string Caption { get; }
		void OnTabPageSelected();
	};

	public interface ILogJointApplication
	{
		IModel Model { get; }
		IInvokeSynchronization UIInvokeSynchronization { get; }

		// below is UI related stuff. todo: develop and expose presenters interfaces
		IMessage FocusedMessage { get; }
		IMessagesCollection LoadedMessagesCollection { get; }
		void RegisterToolForm(Form f);
		void SelectMessageAt(IBookmark bmk, Predicate<IMessage> messageMatcherWhenNoHashIsSpecified);
		UI.Presenters.LoadedMessages.IPresenter LoadedMessagesPresenter { get; }
		UI.Presenters.IPresentersFacade PresentersFacade { get; }

		event EventHandler FocusedMessageChanged;
		event EventHandler SourcesChanged;
	};

	public class PluginBase : IDisposable
	{
		public virtual void Init(ILogJointApplication app) { }
		public virtual IEnumerable<IMainFormTabExtension> MainFormTabExtensions { get { yield break; } }
		public virtual void Dispose() { }
	};
}
