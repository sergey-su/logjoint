using System;
using System.Linq;
using System.Text;

namespace LogJoint.UI.Presenters.SourcePropertiesWindow
{
	public class Presenter: IPresenter, IViewEvents
	{
		readonly IView view;
		readonly IPresentersFacade presentersFacade;
		readonly IAlertPopup alerts;
		readonly ILogSourcesManager logSources;
		readonly Preprocessing.ILogSourcesPreprocessingManager preprocessings;
		readonly IClipboardAccess clipboard;
		readonly IShellOpen shellOpen;
		IWindow currentWindow;
		ILogSource source;
		string previouslySetAnnotation;
		string previouslySetOffset;
		IBookmark firstMessageBmk, lastMessageBmk;
		string stateDetailsErrorMessage;
		string loadedMessagesWarningMessage;
		LoadedMessageWarningStatus loadedMessageWarningStatus;
		string copyablePath;
		string containingFolderPath;


		public Presenter(
			IView view,
			ILogSourcesManager logSources,
			Preprocessing.ILogSourcesPreprocessingManager preprocessings,
			IPresentersFacade navHandler,
			IAlertPopup alerts,
			IClipboardAccess clipboard,
			IShellOpen shellOpen
		)
		{
			this.view = view;
			this.presentersFacade = navHandler;
			this.alerts = alerts;
			this.preprocessings = preprocessings;
			this.clipboard = clipboard;
			this.shellOpen = shellOpen;
			this.logSources = logSources;

			view.SetEventsHandler(this);

			logSources.OnLogSourceColorChanged += (s, e) =>
			{
				if (object.ReferenceEquals(s, source) && currentWindow != null)
				{
					UpdateColorPanel();
				}
			};
		}

		void IPresenter.UpdateOpenWindow()
		{
			UpdateView(initialUpdate: false);
		}

		void IPresenter.ShowWindow(ILogSource forSource)
		{
			currentWindow = view.CreateWindow();
			source = forSource;
			try
			{
				UpdateView(initialUpdate: true);
				currentWindow.ShowDialog();
			}
			finally
			{
				currentWindow = null;
			}
		}


		void IViewEvents.OnVisibleCheckBoxClicked()
		{
			source.Visible = currentWindow.ReadControl(ControlFlag.VisibleCheckBox | ControlFlag.Checked) != null;
			UpdateSuspendResumeTrackingLink();
		}

		void IViewEvents.OnSuspendResumeTrackingLinkClicked()
		{
			source.TrackingEnabled = !source.TrackingEnabled;
			UpdateSuspendResumeTrackingLink();
		}

		void IViewEvents.OnStateDetailsLinkClicked()
		{
			string msg = stateDetailsErrorMessage;
			if (!string.IsNullOrEmpty(msg))
				alerts.ShowPopup("Error details", msg, AlertFlags.Ok | AlertFlags.WarningIcon);
		}

		void IViewEvents.OnBookmarkLinkClicked(ControlFlag controlId)
		{
			IBookmark bmk = null;
			if (controlId == ControlFlag.FirstMessageLinkLabel)
				bmk = firstMessageBmk;
			else if (controlId == ControlFlag.LastMessageLinkLabel)
				bmk = lastMessageBmk;
			if (bmk != null)
				presentersFacade.ShowMessage(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet | BookmarkNavigationOptions.NoLinksInPopups);
		}

		void IViewEvents.OnSaveAsButtonClicked()
		{
			presentersFacade.SaveLogSourceAs(source);
		}

		void IViewEvents.OnClosingDialog()
		{
			source.Annotation = currentWindow.ReadControl(ControlFlag.AnnotationTextBox | ControlFlag.Value);
			ITimeOffsets newTimeOffset;
			if (TimeOffsets.TryParse(currentWindow.ReadControl(ControlFlag.TimeOffsetTextBox | ControlFlag.Value), out newTimeOffset))
			{
				source.TimeOffsets = newTimeOffset;
			}
		}

		void IViewEvents.OnLoadedMessagesWarningIconClicked()
		{
			if (loadedMessageWarningStatus == LoadedMessageWarningStatus.Unfixable)
			{
				alerts.ShowPopup("Problem with the log", loadedMessagesWarningMessage, 
					AlertFlags.Ok | AlertFlags.WarningIcon);
			}
			else if (loadedMessageWarningStatus == LoadedMessageWarningStatus.FixableByReordering)
			{
				if (alerts.ShowPopup("Problem with the log", loadedMessagesWarningMessage, 
					AlertFlags.YesNoCancel | AlertFlags.QuestionIcon) == AlertFlags.Yes)
				{
					var cp = preprocessings.AppendReorderingStep(source.Provider.ConnectionParams, source.Provider.Factory);
					if (cp != null)
					{
						currentWindow.Close();
						source.Dispose();
						preprocessings.Preprocess(
							new MRU.RecentLogEntry(source.Provider.Factory, cp, "", null), makeHiddenLog: false);
					}
				}
			}
		}

		void IViewEvents.OnChangeColorLinkClicked()
		{
			currentWindow.ShowColorSelector(
				source.Threads.UnderlyingThreadsContainer.ColorTable.Items.Where(c => c.Argb != source.Color.Argb).ToArray());
		}

		void IViewEvents.OnColorSelected(ModelColor color)
		{
			source.Color = color;
		}

		void IViewEvents.OnCopyButtonClicked()
		{
			if (copyablePath != null && clipboard != null)
				clipboard.SetClipboard(copyablePath);
		}

		void IViewEvents.OnOpenContainingFolderButtonClicked()
		{
			if (containingFolderPath != null)
				shellOpen.OpenFileBrowser(containingFolderPath);
		}

		#region Implementation

		void UpdateView(bool initialUpdate)
		{
			if (currentWindow == null)
			{
				return;
			}
			if (source.IsDisposed)
			{
				currentWindow.Close();
				return;
			}

			SetTextBoxValue(ControlFlag.NameEditbox, source.DisplayName);
			SetTextBoxValue(ControlFlag.FormatTextBox, LogProviderFactoryRegistry.ToString(source.Provider.Factory));

			WriteControl(ControlFlag.VisibleCheckBox | ControlFlag.Checked, source.Visible);
			UpdateColorPanel();
			ShowTechInfoPanel();
			UpdateStatsView(source.Provider.Stats);
			UpdateSuspendResumeTrackingLink();
			UpdateFirstAndLastMessages();
			UpdateSaveAs();
			UpdateAnnotation(initialUpdate);
			UpdateTimeOffset(initialUpdate);
			UpdateCopyPathButton();
			UpdateOpenContainingFolderButton();
		}

		private void UpdateColorPanel()
		{
			WriteControl(ControlFlag.ColorPanel | ControlFlag.BackColor, source.Color.Argb.ToString());
		}

		void WriteControl(ControlFlag flags, string value)
		{
			currentWindow.WriteControl(flags, value);
		}

		void WriteControl(ControlFlag flags, bool value)
		{
			currentWindow.WriteControl(flags, value ? "" : null);
		}

		void SetTextBoxValue(ControlFlag box, string value)
		{
			if (currentWindow.ReadControl(ControlFlag.Value | box) != value)
			{
				currentWindow.WriteControl(ControlFlag.Value | box, value);
			}
		}

		[System.Diagnostics.Conditional("DEBUG")]
		void ShowTechInfoPanel()
		{
			//techInfoGroupBox.Visible = true;
		}

		void UpdateStatsView(LogProviderStats stats)
		{
			string errorMsg = null;
			string labelValue = null;
			switch (stats.State)
			{
				case LogProviderState.DetectingAvailableTime:
					labelValue = "Processing the data";
					break;
				case LogProviderState.Idle:
					labelValue = "Idling";
					break;
				case LogProviderState.LoadError:
					labelValue = "Loading failed";
					if (stats.Error != null)
						errorMsg = stats.Error.Message;
					break;
				case LogProviderState.NoFile:
					labelValue = "No file";
					break;
				default:
					labelValue = "";
					break;
			}

			WriteControl(ControlFlag.StateLabel | ControlFlag.Value, labelValue);

			WriteControl(ControlFlag.StateDetailsLink | ControlFlag.Visibility, errorMsg != null);
			stateDetailsErrorMessage = errorMsg;
			if (errorMsg != null)
			{
				WriteControl(ControlFlag.StateLabel | ControlFlag.ForeColor, 0xffff0000.ToString());
			}
			else
			{
				WriteControl(ControlFlag.StateLabel | ControlFlag.ForeColor, view.DefaultControlForeColor.ToString());
			}

			WriteControl(ControlFlag.LoadedMessagesTextBox | ControlFlag.Value, stats.MessagesCount.ToString());

			UpdateLoadingWarning(stats);
		}

		private void UpdateLoadingWarning(LogProviderStats stats)
		{
			var firstMessageWithTimeConstraintViolation = stats.FirstMessageWithTimeConstraintViolation;
			bool showWarning = firstMessageWithTimeConstraintViolation != null;
			WriteControl(ControlFlag.LoadedMessagesWarningIcon | ControlFlag.Visibility, showWarning);
			WriteControl(ControlFlag.LoadedMessagesWarningLinkLabel | ControlFlag.Visibility, showWarning);
			if (showWarning)
			{
				StringBuilder warningMessage = new StringBuilder();
				warningMessage.AppendFormat(
					"One or more messages were skipped because they have incorrect timestamp. The first skipped message:\n\n"
				);
				if (firstMessageWithTimeConstraintViolation.RawText.IsInitialized)
					warningMessage.Append(firstMessageWithTimeConstraintViolation.RawText.ToString());
				else
					warningMessage.AppendFormat("'{0}' at {1}",
						firstMessageWithTimeConstraintViolation.Text.ToString(), firstMessageWithTimeConstraintViolation.Time.ToUserFrendlyString(true));
				warningMessage.AppendLine();
				warningMessage.AppendLine();
				warningMessage.Append("Messages must be strictly ordered by time.");
				var formatFlags = source.Provider.Factory.Flags;
				if ((formatFlags & LogProviderFactoryFlag.SupportsReordering) != 0)
				{
					warningMessage.AppendLine();
					warningMessage.AppendLine();
					warningMessage.Append("Select Yes to open reordered temporary copy of the log.");
					loadedMessageWarningStatus = LoadedMessageWarningStatus.FixableByReordering;
				}
				else if ((formatFlags & LogProviderFactoryFlag.DejitterEnabled) != 0)
				{
					warningMessage.Append(" Consider increasing reordering buffer size. " +
						"That can be done in formats management wizard.");
					loadedMessageWarningStatus = LoadedMessageWarningStatus.Unfixable;
				}
				else if ((formatFlags & LogProviderFactoryFlag.SupportsDejitter) != 0)
				{
					warningMessage.Append(" Consider enabling automatic messages reordering. " +
						"That can be done in formats management wizard.");
					loadedMessageWarningStatus = LoadedMessageWarningStatus.Unfixable;
				}
				loadedMessagesWarningMessage = warningMessage.ToString();
			}
			else
			{
				loadedMessagesWarningMessage = null;
				loadedMessageWarningStatus = LoadedMessageWarningStatus.None;
			}
		}

		void UpdateSuspendResumeTrackingLink()
		{
			if (source.Visible)
			{
				WriteControl(ControlFlag.TrackChangesLabel | ControlFlag.Value, source.TrackingEnabled ? "enabled" : "disabled");
				WriteControl(ControlFlag.SuspendResumeTrackingLink | ControlFlag.Value, source.TrackingEnabled ? "suspend tracking" : "resume tracking");
				WriteControl(ControlFlag.TrackChangesLabel | ControlFlag.Enabled, true);
				WriteControl(ControlFlag.SuspendResumeTrackingLink | ControlFlag.Visibility, true);
			}
			else
			{
				WriteControl(ControlFlag.TrackChangesLabel | ControlFlag.Value, "disabled (source is hidden)");
				WriteControl(ControlFlag.TrackChangesLabel | ControlFlag.Enabled, false);
				WriteControl(ControlFlag.SuspendResumeTrackingLink | ControlFlag.Visibility, false);
			}
		}

		void UpdateFirstAndLastMessages()
		{
			IBookmark first = null;
			IBookmark last = null;
			foreach (IThread t in source.Threads.Items)
			{
				IBookmark tmp;

				if ((tmp = t.FirstKnownMessage) != null)
					if (first == null || tmp.Time < first.Time)
						first = tmp;

				if ((tmp = t.LastKnownMessage) != null)
					if (last == null || tmp.Time > last.Time)
						last = tmp;
			}

			SetBookmark(ControlFlag.FirstMessageLinkLabel, firstMessageBmk = first);
			SetBookmark(ControlFlag.LastMessageLinkLabel, lastMessageBmk = last);
		}

		void SetBookmark(ControlFlag label, IBookmark bmk)
		{
			if (bmk != null && source.Visible)
			{
				WriteControl(label | ControlFlag.Value, bmk.Time.ToUserFrendlyString());
				WriteControl(label | ControlFlag.Enabled, true);
			}
			else
			{
				WriteControl(label | ControlFlag.Value, "-");
				WriteControl(label | ControlFlag.Enabled, false);
			}
		}

		void UpdateSaveAs()
		{
			bool isSavable = false;
			ISaveAs saveAs = source.Provider as ISaveAs;
			if (saveAs != null)
				isSavable = saveAs.IsSavableAs;
			WriteControl(ControlFlag.SaveAsButton | ControlFlag.Enabled, isSavable);
		}

		void UpdateCopyPathButton()
		{
			copyablePath = preprocessings.ExtractCopyablePathFromConnectionParams(source.Provider.ConnectionParams);
			WriteControl(ControlFlag.CopyPathButton | ControlFlag.Enabled, copyablePath != null);
		}

		void UpdateOpenContainingFolderButton()
		{
			containingFolderPath = preprocessings.ExtractUserBrowsableFileLocationFromConnectionParams(source.Provider.ConnectionParams);
			WriteControl(ControlFlag.OpenContainingFolderButton | ControlFlag.Enabled, containingFolderPath != null);
		}

		void UpdateAnnotation(bool initialUpdate)
		{
			var annotation = source.Annotation;
			if (initialUpdate || annotation != previouslySetAnnotation)
			{
				WriteControl(ControlFlag.AnnotationTextBox | ControlFlag.Value, annotation);
				previouslySetAnnotation = annotation;
			}
		}

		void UpdateTimeOffset(bool initialUpdate)
		{
			var offset = source.TimeOffsets.ToString();
			if (initialUpdate || offset != previouslySetOffset)
			{
				WriteControl(ControlFlag.TimeOffsetTextBox | ControlFlag.Value, offset);
				previouslySetOffset = offset;
			}
		}

		#endregion

		enum LoadedMessageWarningStatus
		{
			None,
			Unfixable,
			FixableByReordering
		};
	};
};