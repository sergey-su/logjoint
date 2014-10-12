using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface IView
	{
		void SetPresenter(Presenter presenter);
		void UpdateStarted();
		void UpdateFinished();
		void ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage);
		void UpdateScrollSizeToMatchVisibleCount();
		void Invalidate();
		void InvalidateMessage(Presenter.DisplayLine line);
		IEnumerable<Presenter.DisplayLine> GetVisibleMessagesIterator();
		void HScrollToSelectedText();
		void SetClipboard(string text);
		void DisplayEverythingFilteredOutMessage(bool displayOrHide);
		void DisplayNothingLoadedMessage(string messageToDisplayOrNull);
		void PopupContextMenu(object contextMenuPopupData);
		void RestartCursorBlinking();
		void OnFontSizeChanged();
		void OnShowMillisecondsChanged();
		int DisplayLinesPerPage { get; }
		object GetContextMenuPopupDataForCurrentSelection();
		void OnColoringChanged();
		void OnSlaveMessageChanged();
		void AnimateSlaveMessagePosition();
	};
};