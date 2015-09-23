using System;
using MonoMac.Foundation;
using LogJoint.UI.Presenters.LogViewer;
using System.Collections.Generic;
using LogJoint.Settings;

namespace LogJoint.UI
{
	public class LogViewerControlAdapter: NSObject, IView
	{
		IViewEvents viewEvents;
		IPresentationDataAccess presentationDataAccess;

		[Export("view")]
		public LogViewerControl View { get; set;}

		public LogViewerControlAdapter()
		{
			NSBundle.LoadNib ("LogViewerControl", this);
		}

		#region IView implementation

		void IView.SetViewEvents(IViewEvents viewEvents)
		{
			this.viewEvents = viewEvents;
		}

		void IView.SetPresentationDataAccess(IPresentationDataAccess presentationDataAccess)
		{
			this.presentationDataAccess = presentationDataAccess;
		}

		void IView.UpdateFontDependentData(string fontName, LogJoint.Settings.Appearance.LogFontSize fontSize)
		{
		}

		void IView.SaveViewScrollState(SelectionInfo selection)
		{
		}

		void IView.RestoreViewScrollState(SelectionInfo selection)
		{
		}

		void IView.HScrollToSelectedText(SelectionInfo selection)
		{
		}

		object IView.GetContextMenuPopupDataForCurrentSelection(SelectionInfo selection)
		{
			return null;
		}

		void IView.PopupContextMenu(object contextMenuPopupData)
		{
		}

		void IView.ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage)
		{
		}

		void IView.UpdateScrollSizeToMatchVisibleCount()
		{
		}

		void IView.Invalidate()
		{
		}

		void IView.InvalidateMessage(DisplayLine line)
		{
		}

		void IView.SetClipboard(string text)
		{
		}

		void IView.DisplayEverythingFilteredOutMessage(bool displayOrHide)
		{
		}

		void IView.DisplayNothingLoadedMessage(string messageToDisplayOrNull)
		{
		}

		void IView.RestartCursorBlinking()
		{
		}

		void IView.UpdateMillisecondsModeDependentData()
		{
		}

		void IView.AnimateSlaveMessagePosition()
		{
		}

		int IView.DisplayLinesPerPage
		{
			get
			{
				return 10;
			}
		}

		#endregion

		#region IViewFonts implementation

		string[] IViewFonts.AvailablePreferredFamilies
		{
			get
			{
				return new string[] { "courier new" };
			}
		}

		KeyValuePair<Appearance.LogFontSize, int>[] IViewFonts.FontSizes
		{
			get
			{
				return new []
				{
					new KeyValuePair<Settings.Appearance.LogFontSize, int>(Appearance.LogFontSize.Normal, 10)
				};
			}
		}

		#endregion
	}
}

