using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using LogJoint.Drawing;
using LogJoint.Preprocessing;

namespace LogJoint.UI.Presenters.SourcePropertiesWindow
{
	public class Presenter: IPresenter, IViewModel
	{
		readonly IView view;
		readonly IPresentersFacade presentersFacade;
		readonly IAlertPopup alerts;
		readonly Preprocessing.IManager preprocessings;
		readonly IClipboardAccess clipboard;
		readonly IShellOpen shellOpen;
		readonly IColorTheme theme;
		readonly IChainedChangeNotification changeNotification;
		readonly LazyUpdateFlag pendingUpdateFlag = new LazyUpdateFlag();
		static readonly ViewState emptyViewState = new ViewState();
		IWindow currentWindow;
		ILogSource source;
		string annotation;
		string timeOffset;
		int forceUpdateRevision;

		Func<ViewState> getViewState;

		public Presenter(
			IView view,
			ILogSourcesManager logSources,
			Preprocessing.IManager preprocessings,
			IModelThreads threads,
			IPresentersFacade navHandler,
			IAlertPopup alerts,
			IClipboardAccess clipboard,
			IShellOpen shellOpen,
			IColorTheme theme,
			IHeartBeatTimer heartBeat,
			IChangeNotification changeNotification
		)
		{
			this.view = view;
			this.presentersFacade = navHandler;
			this.alerts = alerts;
			this.preprocessings = preprocessings;
			this.clipboard = clipboard;
			this.shellOpen = shellOpen;
			this.theme = theme;
			this.changeNotification = changeNotification.CreateChainedChangeNotification(initiallyActive: false);

			logSources.OnLogSourceStatsChanged += (s, e) =>
			{
				if (s == logSources)
					pendingUpdateFlag.Invalidate();
			};

			threads.OnThreadListChanged += (s, e) => pendingUpdateFlag.Invalidate();
			threads.OnThreadPropertiesChanged += (s, e) => pendingUpdateFlag.Invalidate();

			heartBeat.OnTimer += (s, e) =>
			{
				if (pendingUpdateFlag.Validate())
				{
					++forceUpdateRevision;
					changeNotification.Post();
				}
			};

			this.getViewState = () => emptyViewState;

			view.SetViewModel(this);
		}

		async void IPresenter.ShowWindow(ILogSource source)
		{
			this.source = source;

			this.annotation = source.Annotation;
			this.timeOffset = source.TimeOffsets.ToString();

			this.getViewState = Selectors.Create(
				() => annotation,
				() => timeOffset,
				() => (source.Visible, source.TrackingEnabled, source.ColorIndex, source.DisplayName),
				() => theme.ThreadColors,
				() => forceUpdateRevision,
				(annotation, timeOffset, sourceProps, themeThreadColors, rev) => MakeViewState(
					annotation,
					timeOffset,
					sourceProps.Visible,
					sourceProps.TrackingEnabled,
					sourceProps.ColorIndex,
					sourceProps.DisplayName,
					source.Provider,
					preprocessings,
					themeThreadColors,
					source.Threads.Items
				)
			);
			
			currentWindow = view.CreateWindow();

			var autoCloseDialog = Updaters.Create(
				() => this.source.IsDisposed,
				disposed =>
				{
					if (disposed)
						currentWindow.Close();
				}
			);
			using (changeNotification.CreateSubscription(autoCloseDialog))
			{
				try
				{
					changeNotification.Active = true;
					await currentWindow.ShowModalDialog();
				}
				finally
				{
					changeNotification.Active = false;
					this.getViewState = () => emptyViewState;
					currentWindow = null;
				}
			}
		}

		IChangeNotification IViewModel.ChangeNotification => changeNotification;

		IViewState IViewModel.ViewState => getViewState();

		void IViewModel.OnVisibleCheckBoxChange(bool value)
		{
			source.Visible = value;
		}

		void IViewModel.OnSuspendResumeTrackingLinkClicked()
		{
			source.TrackingEnabled = !source.TrackingEnabled;
		}

		async void IViewModel.OnStateDetailsLinkClicked()
		{
			string msg = getViewState().stateDetailsErrorMessage;
			if (!string.IsNullOrEmpty(msg))
				await alerts.ShowPopupAsync("Error details", msg, AlertFlags.Ok | AlertFlags.WarningIcon);
		}

		void IViewModel.OnFirstKnownMessageLinkClicked()
		{
			HandleBookmarkClick(getViewState().firstMessageBmk);
		}

		void IViewModel.OnLastKnownMessageLinkClicked()
		{
			HandleBookmarkClick(getViewState().lastMessageBmk);
		}

		async void IViewModel.OnSaveAsButtonClicked()
		{
			await presentersFacade.SaveLogSourceAs(source);
		}

		void IViewModel.OnChangeAnnotation(string value)
		{
			annotation = value;
			changeNotification.Post();
		}

		void IViewModel.OnChangeChangeTimeOffset(string value)
		{
			timeOffset = value;
			changeNotification.Post();
		}

		void IViewModel.OnClosingDialog()
		{
			source.Annotation = annotation;
			if (TimeOffsets.TryParse(timeOffset, out var newTimeOffset))
			{
				source.TimeOffsets = newTimeOffset;
			}
		}

		async void IViewModel.OnLoadedMessagesWarningIconClicked()
		{
			var loadedMessageWarningStatus = getViewState().loadedMessageWarningStatus;
			var loadedMessagesWarningMessage = getViewState().loadedMessagesWarningMessage;
			if (loadedMessageWarningStatus == LoadedMessageWarningStatus.Unfixable)
			{
				await alerts.ShowPopupAsync("Problem with the log", loadedMessagesWarningMessage, 
					AlertFlags.Ok | AlertFlags.WarningIcon);
			}
			else if (loadedMessageWarningStatus == LoadedMessageWarningStatus.FixableByReordering)
			{
				if (await alerts.ShowPopupAsync("Problem with the log", loadedMessagesWarningMessage, 
					AlertFlags.YesNoCancel | AlertFlags.QuestionIcon) == AlertFlags.Yes)
				{
					var cp = preprocessings.AppendReorderingStep(source.Provider.ConnectionParams, source.Provider.Factory);
					if (cp != null)
					{
						currentWindow.Close();
						source.Dispose();
						preprocessings.Preprocess(
							new MRU.RecentLogEntry(source.Provider.Factory, cp, "", null));
					}
				}
			}
		}

		void IViewModel.OnChangeColorLinkClicked()
		{
			currentWindow.ShowColorSelector(
				theme.ThreadColors.ZipWithIndex().Where(x => x.Key != source.ColorIndex).Select(x => x.Value).ToArray());
		}

		void IViewModel.OnColorSelected(Color color)
		{
			var cl = theme.ThreadColors.IndexOf(color);
			if (cl != -1)
				source.ColorIndex = cl;
		}

		void IViewModel.OnCopyButtonClicked()
		{
			var copyablePath = getViewState().copyablePath;
			if (copyablePath != null && clipboard != null)
				clipboard.SetClipboard(copyablePath);
		}

		void IViewModel.OnOpenContainingFolderButtonClicked()
		{
			var containingFolderPath = getViewState().containingFolderPath;
			if (containingFolderPath != null)
				shellOpen.OpenFileBrowser(containingFolderPath);
		}

		static ViewState MakeViewState(
			string annotation,
			string timeOffset,
			bool sourceVisible,
			bool sourceTrackingEnabled,
			int sourceColorIndex,
			string sourceDisplayName,
			ILogProvider sourceProvider,
			Preprocessing.IManager preprocessings,
			ImmutableArray<Color> themeThreadColors,
			IReadOnlyList<IThread> sourceThreads
		)
		{
			ViewState state = new ViewState();

			state.NameEditbox = new ControlState
			{
				Text = sourceDisplayName
			};
			state.FormatTextBox = new ControlState
			{
				Text = LogProviderFactoryRegistry.ToString(sourceProvider.Factory)
			};
			state.VisibleCheckBox = new ControlState
			{
				Checked = sourceVisible
			};

			state.ColorPanel = new ControlState
			{
				BackColor = themeThreadColors.GetByIndex(sourceColorIndex)
			};

			state.AnnotationTextBox = new ControlState
			{
				Text = annotation
			};

			state.TimeOffsetTextBox = new ControlState
			{
				Text = timeOffset
			};

			UpdateStatsView(state, sourceProvider);
			UpdateSuspendResumeTrackingLink(state, sourceVisible, sourceTrackingEnabled);
			UpdateFirstAndLastMessages(state, sourceThreads, sourceVisible);
			UpdateSaveAs(state, sourceProvider);
			UpdateCopyPathButton(state, sourceProvider, preprocessings);
			UpdateOpenContainingFolderButton(state, sourceProvider, preprocessings);

			return state;
		}

		static void UpdateStatsView(
			ViewState viewState,
			ILogProvider logProvider
		)
		{
			var stats = logProvider.Stats;
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

			viewState.StateDetailsLink = new ControlState
			{
				Hidden = errorMsg == null,
				Text = "details"
			};
			viewState.stateDetailsErrorMessage = errorMsg;

			viewState.StateLabel = new ControlState
			{
				Text = labelValue,
				ForeColor = errorMsg != null ? Color.Red : new Color?()
			};

			viewState.LoadedMessagesTextBox = new ControlState
			{
				Text = stats.MessagesCount.ToString()
			};

			UpdateLoadingWarning(viewState, stats, logProvider);
		}

		private static void UpdateLoadingWarning(
			ViewState viewState,
			LogProviderStats stats,
			ILogProvider logProvider
		)
		{
			var firstMessageWithTimeConstraintViolation = stats.FirstMessageWithTimeConstraintViolation;
			bool showWarning = firstMessageWithTimeConstraintViolation != null;
			viewState.LoadedMessagesWarningIcon = new ControlState
			{
				Hidden = !showWarning,
				Tooltip = "Log source has warnings",
			};
			viewState.LoadedMessagesWarningLinkLabel = new ControlState
			{
				Hidden = !showWarning,
				Text = "see warnings",
				Tooltip = "see warnings",
			};
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
				var formatFlags = logProvider.Factory.Flags;
				if ((formatFlags & LogProviderFactoryFlag.SupportsReordering) != 0)
				{
					warningMessage.AppendLine();
					warningMessage.AppendLine();
					warningMessage.Append("Select Yes to open reordered temporary copy of the log.");
					viewState.loadedMessageWarningStatus = LoadedMessageWarningStatus.FixableByReordering;
				}
				else if ((formatFlags & LogProviderFactoryFlag.DejitterEnabled) != 0)
				{
					warningMessage.Append(" Consider increasing reordering buffer size. " +
						"That can be done in formats management wizard.");
					viewState.loadedMessageWarningStatus = LoadedMessageWarningStatus.Unfixable;
				}
				else if ((formatFlags & LogProviderFactoryFlag.SupportsDejitter) != 0)
				{
					warningMessage.Append(" Consider enabling automatic messages reordering. " +
						"That can be done in formats management wizard.");
					viewState.loadedMessageWarningStatus = LoadedMessageWarningStatus.Unfixable;
				}
				viewState.loadedMessagesWarningMessage = warningMessage.ToString();
			}
			else
			{
				viewState.loadedMessagesWarningMessage = null;
				viewState.loadedMessageWarningStatus = LoadedMessageWarningStatus.None;
			}
		}

		static void UpdateSuspendResumeTrackingLink(
			ViewState viewState,
			bool sourceVisible,
			bool sourceTrackingEnabled
		)
		{
			if (sourceVisible)
			{
				viewState.TrackChangesLabel = new ControlState
				{
					Text = sourceTrackingEnabled ? "enabled" : "disabled"
				};
				viewState.SuspendResumeTrackingLink = new ControlState
				{
					Text = sourceTrackingEnabled ? "suspend tracking" : "resume tracking"
				};
			}
			else
			{
				viewState.TrackChangesLabel = new ControlState
				{
					Text = "disabled (source is hidden)",
					Disabled = true
				};
				viewState.SuspendResumeTrackingLink = new ControlState
				{
					Hidden = true
				};
			}
		}

		static void UpdateFirstAndLastMessages(
			ViewState viewState,
			IReadOnlyList<IThread> threads,
			bool sourceVisible
		)
		{
			IBookmark first = null;
			IBookmark last = null;
			foreach (IThread t in threads)
			{
				IBookmark tmp;

				if ((tmp = t.FirstKnownMessage) != null)
					if (first == null || tmp.Position < first.Position)
						first = tmp;

				if ((tmp = t.LastKnownMessage) != null)
					if (last == null || tmp.Position > last.Position)
						last = tmp;
			}

			ControlState makeBookmarkState(IBookmark bmk) =>
				bmk != null && sourceVisible ?
					new ControlState
					{
						Text = bmk.Time.ToUserFrendlyString()
					}
					: new ControlState
					{
						Text = "-",
						Disabled = true
					};

			viewState.FirstMessageLinkLabel = makeBookmarkState(viewState.firstMessageBmk = first);
			viewState.LastMessageLinkLabel = makeBookmarkState(viewState.lastMessageBmk = last);
		}

		static void UpdateSaveAs(ViewState viewState, ILogProvider sourceProvider)
		{
			bool isSavable = false;
			if (sourceProvider is ISaveAs saveAs)
				isSavable = saveAs.IsSavableAs;
			viewState.SaveAsButton = new ControlState
			{
				Disabled = !isSavable,
				Text = "Save As...",
			};
		}

		static void UpdateCopyPathButton(ViewState viewState,
			ILogProvider sourceProvider, Preprocessing.IManager preprocessings)
		{
			viewState.copyablePath = preprocessings.ExtractCopyablePathFromConnectionParams(sourceProvider.ConnectionParams);
			viewState.CopyPathButton = new ControlState
			{
				Disabled = viewState.copyablePath == null,
				Text = "Copy path",
				Tooltip = "copy log source path"
			};
		}

		static void UpdateOpenContainingFolderButton(ViewState viewState,
			ILogProvider sourceProvider, Preprocessing.IManager preprocessings)
		{
			viewState.containingFolderPath = preprocessings.ExtractUserBrowsableFileLocationFromConnectionParams(
				sourceProvider.ConnectionParams);
			viewState.OpenContainingFolderButton = new ControlState
			{
				Disabled = viewState.containingFolderPath == null,
				Text =
					RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
						"Reveal in Finder" :
						"Open Containing Folder"
			};
		}

		private void HandleBookmarkClick(IBookmark bmk)
		{
			if (bmk != null)
				presentersFacade.ShowMessage(bmk, BookmarkNavigationOptions.EnablePopups | BookmarkNavigationOptions.GenericStringsSet | BookmarkNavigationOptions.NoLinksInPopups);
		}

		enum LoadedMessageWarningStatus
		{
			None,
			Unfixable,
			FixableByReordering
		};

		class ViewState : IViewState
		{
			public ControlState NameEditbox { get; set; }
			public ControlState FormatTextBox { get; set; }
			public ControlState VisibleCheckBox { get; set; }
			public ControlState ColorPanel { get; set; }
			public ControlState StateDetailsLink { get; set; }
			public ControlState StateLabel { get; set; }
			public ControlState LoadedMessagesTextBox { get; set; }
			public ControlState LoadedMessagesWarningIcon { get; set; }
			public ControlState LoadedMessagesWarningLinkLabel { get; set; }
			public ControlState TrackChangesLabel { get; set; }
			public ControlState SuspendResumeTrackingLink { get; set; }
			public ControlState FirstMessageLinkLabel { get; set; }
			public ControlState LastMessageLinkLabel { get; set; }
			public ControlState SaveAsButton { get; set; }
			public ControlState AnnotationTextBox { get; set; }
			public ControlState TimeOffsetTextBox { get; set; }
			public ControlState CopyPathButton { get; set; }
			public ControlState OpenContainingFolderButton { get; set; }

			public string stateDetailsErrorMessage;
			public string loadedMessagesWarningMessage;
			public LoadedMessageWarningStatus loadedMessageWarningStatus;
			public IBookmark firstMessageBmk, lastMessageBmk;
			public string copyablePath;
			public string containingFolderPath;
		};
	};
};
