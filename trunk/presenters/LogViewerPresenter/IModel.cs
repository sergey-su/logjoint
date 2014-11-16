using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IModel
	{
		IMessagesCollection Messages { get; }
		IModelThreads Threads { get; }
		IFiltersList DisplayFilters { get; }
		IFiltersList HighlightFilters { get; }
		IBookmarks Bookmarks { get; }
		LJTraceSource Tracer { get; }
		string MessageToDisplayWhenMessagesCollectionIsEmpty { get; }
		void ShiftUp();
		bool IsShiftableUp { get; }
		void ShiftDown();
		bool IsShiftableDown { get; }
		void ShiftAt(DateTime t);
		void ShiftHome();
		void ShiftToEnd();
		bool GetAndResetPendingUpdateFlag();

		event EventHandler<MessagesChangedEventArgs> OnMessagesChanged;
	};
};