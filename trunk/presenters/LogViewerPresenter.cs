// test
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
		void UpdateStarted();
		void UpdateFinished();
		void ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage);
		void UpdateScrollSizeToMatchVisibleCount();
		void Invalidate();
		void InvalidateMessage(MessageBase msg, int displayPosition);
		IEnumerable<IndexedMessage> GetVisibleMessagesIterator();
		void HScrollHightlighedTextInView();
		void SetClipboard(string text);
		void DisplayEverythingFilteredOutMessage(bool displayOrHide);
		void PopupContextMenu(object contextMenuPopupData);
	};

	public interface IModel
	{
		IMessagesCollection Messages { get; }
		IEnumerable<IThread> Threads { get; }
		FiltersList DisplayFilters { get; }
		FiltersList HighlightFilters { get; }
		IBookmarks Bookmarks { get; }
		IUINavigationHandler UINavigationHandler { get; }
		LJTraceSource Tracer { get; }
		void ShiftUp();
		bool IsShiftableUp { get; }
		void ShiftDown();
		bool IsShiftableDown { get; }
		void ShiftAt(DateTime t);

		event EventHandler<Model.MessagesChangedEventArgs> OnMessagesChanged;
	};

	public struct FocusedMessageInfo
	{
		public MessageBase Message;
		public int DisplayPosition;
		public HighlightRange Highlight;
	};

	public struct SearchOptions
	{
		public struct SearchPosition
		{
			public int Message;
			public int Position;
			public SearchPosition(int msg, int pos)
			{
				Message = msg;
				Position = pos;
			}
		}
		public SearchPosition? StartFrom;
		public string Template;
		public bool WholeWord;
		public bool ReverseSearch;
		public bool Regexp;
		public bool SearchHiddenText;
		public IThread SearchWithinThisThread;
		public bool MatchCase;
		public MessageBase.MessageFlag TypesToLookFor;
		public bool HighlightResult;
	};

	public struct SearchResult
	{
		public bool Succeeded;
		public int Position;
		public MessageBase Message;
	};

	public class SearchTemplateException : Exception
	{
	};

	public class Presenter : INextBookmarkCallback
	{
		#region Public interface

		public interface ICallback
		{
		};

		public Presenter(IModel model, IView view, ICallback callback)
		{
			this.model = model;
			this.view = view;
			this.callback = callback;
			
			this.tracer = model.Tracer;

			loadedMessagesCollection = new MergedMessagesCollection(this, false);
			displayMessagesCollection = new MergedMessagesCollection(this, true);

			this.model.OnMessagesChanged += (sender, e) =>
			{
				displayFiltersPreprocessingResultCacheIsValid = false;
				highlightFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.DisplayFilters.OnFiltersListChanged += (sender, e) =>
			{
				displayFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.DisplayFilters.OnFilteringEnabledChanged += (sender, e) =>
			{
				displayFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.DisplayFilters.OnPropertiesChanged += (sender, e) =>
			{
				if (e.ChangeAffectsPreprocessingResult)
					displayFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.HighlightFilters.OnFiltersListChanged += (sender, e) =>
			{
				highlightFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.HighlightFilters.OnPropertiesChanged += (sender, e) =>
			{
				if (e.ChangeAffectsPreprocessingResult)
					highlightFiltersPreprocessingResultCacheIsValid = false;
			};
			this.model.HighlightFilters.OnFilteringEnabledChanged += (sender, e) =>
			{
				highlightFiltersPreprocessingResultCacheIsValid = false;
			};
		}

		public void UpdateView()
		{
			InternalUpdate();
		}

		public event EventHandler FocusedMessageChanged;
		public event EventHandler BeginShifting;
		public event EventHandler EndShifting;

		public FocusedMessageInfo FocusedMessageInfo { get { return focused; } }
		public LJTraceSource Tracer { get { return tracer; } }

		public MergedMessagesCollection DisplayMessagesCollection { get { return displayMessagesCollection; } }

		public int SelectedCount { get { return selectedCount; } }

		public void OulineBoxClicked(MessageBase msg, bool controlIsHeld)
		{
			DoExpandCollapse(msg, controlIsHeld, new bool?());
		}

		public void MessageRectClicked(
			IndexedMessage msg, 
			bool rightMouseButton, 
			bool controlIsHeld,
			object preparedContextMenuPopupData)
		{
			if (rightMouseButton)
			{
				if (!msg.Message.IsSelected)
				{
					DeselectAll();
					SelectMessage(msg.Message, msg.Index);
				}
				view.PopupContextMenu(preparedContextMenuPopupData);
			}
			else
			{
				if (!controlIsHeld)
					DeselectAll();

				// In condition that frame is collaped and closed select content of frame
				FrameBegin fb = msg.Message as FrameBegin;
				if (fb != null && fb.Collapsed && fb.End != null)
					foreach (MessageBase j in EnumFrameContent(fb))
						SelectMessage(j, -1);

				// Select line itself (and focus it)
				SelectMessage(msg.Message, msg.Index);
			}
		}

		public void GoToParentFrame()
		{
			if (focused.Message == null)
				return;
			bool inFrame = false;
			int level = 0;
			IThread focusedThread = focused.Message.Thread;
			bool found = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, int.MinValue, new ShiftPermissions(true, false)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (!inFrame)
				{
					if (it.Message == focused.Message)
						inFrame = true;
				}
				else
				{
					MessageBase.MessageFlag type = it.Message.Flags & MessageBase.MessageFlag.TypeMask;
					if (type == MessageBase.MessageFlag.EndFrame)
					{
						--level;
					}
					else if (type == MessageBase.MessageFlag.StartFrame)
					{
						if (level != 0)
						{
							++level;
						}
						else
						{
							SelectOnly(it.Message, it.Index);
							found = true;
							break;
						}
					}
				}
			}
			if (!found)
			{
				foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, 1))
				{
					SelectOnly(it.Message, it.Index);
				}
			}
		}

		public void GoToEndOfFrame()
		{
			if (focused.Message == null)
				return;
			bool inFrame = false;
			int level = 0;
			IThread focusedThread = focused.Message.Thread;
			bool found = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, int.MaxValue, new ShiftPermissions(false, true)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (!inFrame)
				{
					if (it.Message == focused.Message)
						inFrame = true;
				}
				else
				{
					MessageBase.MessageFlag type = it.Message.Flags & MessageBase.MessageFlag.TypeMask;
					if (type == MessageBase.MessageFlag.StartFrame)
					{
						++level;
					}
					else if (type == MessageBase.MessageFlag.EndFrame)
					{
						if (level != 0)
						{
							--level;
						}
						else
						{
							SelectOnly(it.Message, it.Index);
							found = true;
							break;
						}
					}
				}
			}
			if (!found)
			{
				foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, -1))
				{
					SelectOnly(it.Message, it.Index);
					break;
				}
			}
		}

		public void GoToNextMessageInThread()
		{
			if (focused.Message == null)
				return;
			IThread focusedThread = focused.Message.Thread;
			bool afterFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, int.MaxValue, new ShiftPermissions(false, true)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!afterFocused)
				{
					afterFocused = it.Message == focused.Message;
				}
				else
				{
					SelectOnly(it.Message, it.Index);
					break;
				}
			}
		}

		public void GoToPrevMessageInThread()
		{
			if (focused.Message == null)
				return;
			IThread focusedThread = focused.Message.Thread;
			bool beforeFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, int.MinValue, new ShiftPermissions(true, false)))
			{
				if (it.Message.Thread != focusedThread)
					continue;
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!beforeFocused)
				{
					beforeFocused = it.Message == focused.Message;
				}
				else
				{
					SelectOnly(it.Message, it.Index);
					break;
				}
			}
		}

		public void GoToNextHighlightedMessage()
		{
			if (focused.Message == null || model.HighlightFilters == null)
				return;
			bool afterFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Forward(0, int.MaxValue, new ShiftPermissions(false, true)))
			{
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!afterFocused)
				{
					afterFocused = it.Message == focused.Message;
				}
				else if (it.Message.IsHighlighted)
				{
					SelectOnly(it.Message, it.Index);
					break;
				}
			}
		}

		public void GoToPrevHighlightedMessage()
		{
			if (focused.Message == null)
				return;
			IThread focusedThread = focused.Message.Thread;
			bool beforeFocused = false;
			foreach (IndexedMessage it in loadedMessagesCollection.Reverse(int.MaxValue, int.MinValue, new ShiftPermissions(true, false)))
			{
				if (it.Message.IsHiddenAsFilteredOut)
					continue;
				if (!beforeFocused)
				{
					beforeFocused = it.Message == focused.Message;
				}
				else if (it.Message.IsHighlighted)
				{
					SelectOnly(it.Message, it.Index);
					break;
				}
			}
		}

		public void InvalidateFocusedMessage()
		{
			if (focused.Message != null)
			{
				view.InvalidateMessage(focused.Message, focused.DisplayPosition);
			}
		}

		public void DeselectMessage(MessageBase msg, int displayPosition)
		{
			if (!msg.IsSelected)
				return;
			--selectedCount;
			msg.SetSelected(false);
			if (displayPosition >= 0)
				view.InvalidateMessage(msg, displayPosition);
		}

		public void DeselectAll()
		{
			if (selectedCount > 0)
			{
				foreach (IndexedMessage i in view.GetVisibleMessagesIterator())
				{
					if (i.Message.IsSelected)
						view.InvalidateMessage(i.Message, i.Index);
				}
				foreach (IndexedMessage i in loadedMessagesCollection.Forward(0, int.MaxValue))
				{
					DeselectMessage(i.Message, -1);
				}
			}
		}

		public void SelectMessage(MessageBase msg, int displayPosition)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Selecting line {0}. Display position = {1}", msg.GetHashCode(), displayPosition);
				if (!msg.IsSelected)
				{
					msg.SetSelected(true);
					++selectedCount;
					tracer.Info("The amount of selected lines has become = {0}", selectedCount);
					if (displayPosition >= 0)
					{
						view.InvalidateMessage(msg, displayPosition);
					}
				}
				else
				{
					tracer.Info("Line is already selected");
				}
				if (displayPosition >= 0 && focused.Message != msg)
				{
					InvalidateFocusedMessage();

					focused.Message = msg;
					focused.DisplayPosition = displayPosition;
					focused.Highlight = new HighlightRange();

					view.InvalidateMessage(msg, displayPosition);
					OnFocusedMessageChanged();
					tracer.Info("Focused line changed to the new selection");
				}
			}
		}

		public void CopySelectionToClipboard()
		{
			if (selectedCount == 0)
				return;
			StringBuilder sb = new StringBuilder();
			foreach (IndexedMessage i in loadedMessagesCollection.Forward(0, int.MaxValue))
			{
				if (i.Message.IsSelected)
				{
					i.Message.Text.Append(sb);
					sb.AppendLine();
				}
			}
			view.SetClipboard(sb.ToString());
		}

		public void SelectMessageAt(DateTime d, NavigateFlag alignFlag)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Date={0}, alag={1}", d, alignFlag);

				DeselectAll();

				if (visibleCount == 0)
				{
					tracer.Info("No visible messages. Returning");
					return;
				}

				int lowerBound = ListUtils.BinarySearch(mergedMessages, 0, visibleCount,
					delegate(MergedMessagesEntry x) { return x.DisplayMsg.Time < d; });
				int upperBound = ListUtils.BinarySearch(mergedMessages, 0, visibleCount,
					delegate(MergedMessagesEntry x) { return x.DisplayMsg.Time <= d; });

				int idx;
				switch (alignFlag & NavigateFlag.AlignMask)
				{
					case NavigateFlag.AlignTop:
						if (upperBound != lowerBound) // if the date in question is present among messages
							idx = lowerBound; // return the first message with this date
						else // ok, no messages with date (d), lowerBound points to the message with date greater than (d)
							idx = lowerBound - 1; // return the message before lowerBound, it will have date less than (d)
						break;
					case NavigateFlag.AlignBottom:
						if (upperBound > lowerBound) // if the date in question is present among messages
							idx = upperBound - 1; // return the last message with date (d)
						else // no messages with date (d), lowerBound points to the message with date greater than (d)
							idx = upperBound; // we bound to return upperBound
						break;
					case NavigateFlag.AlignCenter:
						if (upperBound > lowerBound) // if the date in question is present among messages
						{
							idx = (lowerBound + upperBound - 1) / 2; // return the middle of the range
						}
						else
						{
							// otherwise choose the message nearest message

							int p1 = Math.Max(lowerBound - 1, 0);
							int p2 = Math.Min(upperBound, visibleCount - 1);
							MessageBase m1 = mergedMessages[p1].DisplayMsg;
							MessageBase m2 = mergedMessages[p2].DisplayMsg;
							if (Math.Abs((m1.Time - d).Ticks) < Math.Abs((m2.Time - d).Ticks))
							{
								idx = p1;
							}
							else
							{
								idx = p2;
							}
						}
						break;
					default:
						throw new ArgumentException();
				}


				if (idx < 0)
					idx = 0;
				else if (idx >= visibleCount)
					idx = visibleCount - 1;

				tracer.Info("Index of the line to be selected: {0}", idx);

				if (idx >= 0 && idx < visibleCount)
				{
					SelectMessage(mergedMessages[idx].DisplayMsg, idx);
					view.ScrollInView(idx, true);
				}
				else
				{
					tracer.Warning("The index is out of visible range [0-{0})", visibleCount);
				}
			}
		}

		public SearchResult Search(SearchOptions opts)
		{
			// init return value with default value (rv.Succeeded = false)
			SearchResult rv = new SearchResult();

			SearchOptions.SearchPosition startFrom = new SearchOptions.SearchPosition();
			if (opts.StartFrom.HasValue)
			{
				startFrom = opts.StartFrom.Value;
			}
			else if (focused.Message != null)
			{
				startFrom.Message = DisplayPosition2Position(focused.DisplayPosition);
				startFrom.Position = opts.ReverseSearch ? focused.Highlight.Begin : focused.Highlight.End;
			}

			IRegex re = null;
			if (!string.IsNullOrEmpty(opts.Template))
			{
				if (opts.Regexp)
				{
					ReOptions reOpts = ReOptions.None;
					if (!opts.MatchCase)
						reOpts |= ReOptions.IgnoreCase;
					if (opts.ReverseSearch)
						reOpts |= ReOptions.RightToLeft;
					try
					{
						re = RegexFactory.Instance.Create(opts.Template, reOpts);
					}
					catch (Exception)
					{
						throw new SearchTemplateException();
					}
				}
				else
				{
					if (!opts.MatchCase)
						opts.Template = opts.Template.ToLower();
				}
			}

			MessageBase.MessageFlag typeMask = MessageBase.MessageFlag.TypeMask & opts.TypesToLookFor;
			MessageBase.MessageFlag msgTypeMask = MessageBase.MessageFlag.ContentTypeMask & opts.TypesToLookFor;

			int messagesProcessed = 0;
			IMatch searchMatch = null;

			foreach (IndexedMessage it in opts.ReverseSearch ?
				  loadedMessagesCollection.Reverse(startFrom.Message, int.MinValue, new ShiftPermissions(true, false))
				: loadedMessagesCollection.Forward(startFrom.Message, int.MaxValue, new ShiftPermissions(false, true)))
			{
				++messagesProcessed;

				MessageBase.MessageFlag f = it.Message.Flags;

				if ((f & (MessageBase.MessageFlag.HiddenBecauseOfInvisibleThread | MessageBase.MessageFlag.HiddenAsFilteredOut)) != 0)
					continue; // dont search in excluded lines

				if ((f & MessageBase.MessageFlag.HiddenAsCollapsed) != 0)
					if (!opts.SearchHiddenText) // if option is specified
						continue; // dont search in lines hidden by collapsing

				if (opts.TypesToLookFor != MessageBase.MessageFlag.None) // None is treated as 'type selection isn't required'
				{
					if ((f & typeMask) == 0)
						continue;

					if (msgTypeMask != MessageBase.MessageFlag.None && (f & msgTypeMask) == 0)
						continue;
				}

				if (opts.SearchWithinThisThread != null)
					if (it.Message.Thread != opts.SearchWithinThisThread)
						continue;

				// matched string position
				int matchBegin = 0; // index of the first matched char
				int matchEnd = 0; // index of following after the last matched one

				StringSlice text = it.Message.Text;

				if (!string.IsNullOrEmpty(opts.Template)) // empty/null template means that text matching isn't required
				{
					int textPos;
					if (messagesProcessed == 1)
						textPos = startFrom.Position;
					else if (opts.ReverseSearch)
						textPos = text.Length;
					else
						textPos = 0;

					if (re != null)
					{
						if (!re.Match(text, textPos, ref searchMatch))
							continue;
						matchBegin = searchMatch.Index;
						matchEnd = matchBegin + searchMatch.Length;
					}
					else
					{
						StringComparison cmp = opts.MatchCase ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
						int i;
						if (opts.ReverseSearch)
							i = text.LastIndexOf(opts.Template, textPos, cmp);
						else
							i = text.IndexOf(opts.Template, textPos, cmp);
						if (i < 0)
							continue;
						matchBegin = i;
						matchEnd = matchBegin + opts.Template.Length;
					}

					if (opts.WholeWord)
					{
						if (matchBegin > 0)
							if (StringUtils.IsLetterOrDigit(text[matchBegin - 1]))
								continue;
						if (matchEnd < text.Length - 1)
							if (StringUtils.IsLetterOrDigit(text[matchEnd]))
								continue;
					}
				}
				else
				{
					if (messagesProcessed == 1)
						continue;
					matchBegin = 0;
					matchEnd = text.Length;
				}

				// init successful return value
				rv.Succeeded = true;
				rv.Position = it.Index;
				rv.Message = it.Message;

				SelectOnly(it.Message, it.Index);

				if (opts.HighlightResult)
				{
					focused.Highlight = new HighlightRange(matchBegin, matchEnd);
				}
				else
				{
					focused.Highlight = new HighlightRange();
				}

				view.HScrollHightlighedTextInView();

				view.Invalidate();

				break;
			}

			if (!rv.Succeeded && focused.Message == null)
				MoveSelection(opts.ReverseSearch ? 0 : visibleCount - 1, true, true);

			// return value initialized by-default as non-successful search

			return rv;
		}

		public void MoveSelection(int newDisplayPosition, bool forwardMode, bool showExtraLinesAroundMessage)
		{
			ShiftPermissions sp = GetShiftPermissions();
			foreach (IndexedMessage l in
				forwardMode ?
					displayMessagesCollection.Forward(newDisplayPosition, int.MaxValue, sp) :
					displayMessagesCollection.Reverse(newDisplayPosition, int.MinValue, sp)
			)
			{
				DeselectAll();
				SelectMessage(l.Message, l.Index);
				view.ScrollInView(l.Index, showExtraLinesAroundMessage);
				break;
			}
		}

		public void Next()
		{
			MoveSelection(focused.DisplayPosition + 1, false, true);
		}

		public void Prev()
		{
			MoveSelection(focused.DisplayPosition - 1, true, true);
		}


		public DateTime? FocusedMessageTime
		{
			get
			{
				if (focused.Message != null)
					return focused.Message.Time;
				return null;
			}
		}

		public MessageBase FocusedMessage
		{
			get
			{
				return focused.Message;
			}
		}

		public MergedMessagesCollection LoadedMessagesCollection
		{
			get { return loadedMessagesCollection; }
		}

		// It takes O(n) time
		// todo: optimize by using binary rearch over mergedMessages
		public int DisplayPosition2Position(int dposition)
		{
			MessageBase l = mergedMessages[dposition].DisplayMsg;
			if (l == null)
				return -1;
			foreach (IndexedMessage i in loadedMessagesCollection.Forward(0, int.MaxValue))
				if (i.Message == l)
					return i.Index;
			return -1;
		}

		public bool NextBookmark(bool forward)
		{
			if (focused.Message == null || model.Bookmarks == null)
				return false;
			IBookmark bmk = model.Bookmarks.GetNext(focused.Message, forward, this);
			SelectMessageAt(bmk);
			return bmk != null;
		}

		public void SelectMessageAt(IBookmark bmk)
		{
			SelectMessageAt(bmk, null);
		}

		public void SelectMessageAt(IBookmark bmk, Predicate<MessageBase> messageMatcherWhenNoHashIsSpecified)
		{
			if (bmk == null)
				return;

			int begin;
			int end;
			GetDateEqualRange(bmk.Time, out begin, out end);

			if (begin == end)
			{
				// If we are at the boundary positions
				if (end == 0 || begin == mergedMessages.Count)
				{
					// We can try to load the messages around the time of the bookmark
					OnBeginShifting();
					model.ShiftAt(bmk.Time);
					OnEndShifting();

					// Refresh the lines. The messages are expected to be changed.
					this.InternalUpdate();

					// Find a new equal range
					GetDateEqualRange(bmk.Time, out begin, out end);
				}
				else
				{
					// We found empty equal range of bookmark's time,
					// this range points to the middle of mergedMessages array.
					// That means that the bookmark is invalid: it points 
					// into the time that doesn't exist among messages.
				}
			}

			if (bmk.MessageHash != 0)
			{
				// Search for the bookmarked message by hash
				for (; begin != end; ++begin)
				{
					MessageBase l = mergedMessages[begin].LoadedMsg;
					if (l.GetHashCode() == bmk.MessageHash)
						break;
				}
			}
			else if (messageMatcherWhenNoHashIsSpecified != null)
			{
				// Search for the bookmarked message by user provided criteria
				for (; begin != end; ++begin)
				{
					MessageBase l = mergedMessages[begin].LoadedMsg;
					if (messageMatcherWhenNoHashIsSpecified(l))
						break;
				}
			}
			else
			{
				begin = end;
			}

			if (begin == end)
				return;

			int idx = Position2DisplayPosition(begin);
			if (idx < 0)
				return;

			DeselectAll();
			SelectMessage(mergedMessages[idx].DisplayMsg, idx);
			view.ScrollInView(idx, true);
		}

		public bool DoExpandCollapse(MessageBase line, bool recursive, bool? collapse)
		{
			using (tracer.NewFrame)
			{
				FrameBegin fb = line as FrameBegin;
				if (fb == null)
					return false;

				if (!recursive && collapse.HasValue)
					if (collapse.Value == fb.Collapsed)
						return false;

				fb.Collapsed = !fb.Collapsed;
				if (recursive)
					foreach (MessageBase i in EnumFrameContent(fb))
					{
						FrameBegin fb2 = i as FrameBegin;
						if (fb2 != null)
							fb2.Collapsed = fb.Collapsed;
					}

				InternalUpdate();

				return true;
			}
		}

		public IEnumerable<MessageBase> EnumFrameContent(FrameBegin fb)
		{
			bool inFrame = false;
			foreach (IndexedMessage il in loadedMessagesCollection.Forward(0, int.MaxValue))
			{
				if (il.Message.Thread != fb.Thread)
					continue;
				bool stop = false;
				if (!inFrame)
					inFrame = il.Message == fb;
				if (il.Message == fb.End)
					stop = true;
				if (inFrame)
					yield return il.Message;
				if (stop)
					break;
			}
		}

		public void EnsureNotCollapsed(int position)
		{
			IEnumerator<IndexedMessage> it = loadedMessagesCollection.Reverse(position, -1).GetEnumerator();
			if (!it.MoveNext())
				return;
			IThread thread = it.Current.Message.Thread;
			int frameCount = 0;
			for (; it.MoveNext(); )
			{
				if (it.Current.Message.Thread != thread)
					continue;
				MessageBase.MessageFlag f = it.Current.Message.Flags;
				switch (f & MessageBase.MessageFlag.TypeMask)
				{
					case MessageBase.MessageFlag.StartFrame:
						if (frameCount == 0)
							DoExpandCollapse(it.Current.Message, false, false);
						else
							--frameCount;
						break;
					case MessageBase.MessageFlag.EndFrame:
						++frameCount;
						break;
				}
			}
		}

		void SelectOnly(MessageBase msg, int position)
		{
			DeselectAll();
			EnsureNotCollapsed(position);
			int dispPos = Position2DisplayPosition(position);
			SelectMessage(msg, dispPos);
			view.ScrollInView(dispPos, true);
		}


		public void SelectFirstMessage()
		{
			using (tracer.NewFrame)
			{
				MoveSelection(0, true, false);
			}
		}

		public void SelectLastMessage()
		{
			using (tracer.NewFrame)
			{
				MoveSelection(visibleCount - 1, false, false);
			}
		}

		public bool BookmarksAvailable
		{
			get { return model.Bookmarks != null; }
		}

		public void ToggleBookmark(MessageBase line)
		{
			if (model.Bookmarks == null)
				return;
			model.Bookmarks.ToggleBookmark(line);
			InternalUpdate();
		}

		public void ShowMessageDetails()
		{
			if (model.UINavigationHandler != null)
				model.UINavigationHandler.ShowMessageProperties();
		}

		public class MergedMessagesCollection : IMessagesCollection
		{
			Presenter control;
			bool displayMessagesMode;

			public MergedMessagesCollection(Presenter control, bool displayMessagesMode)
			{
				this.control = control;
				this.displayMessagesMode = displayMessagesMode;
			}

			public int Count
			{
				get { return displayMessagesMode ? control.visibleCount : control.mergedMessages.Count; }
			}

			public IEnumerable<IndexedMessage> Forward(int begin, int end)
			{
				return Forward(begin, end, new ShiftPermissions());
			}

			public IEnumerable<IndexedMessage> Forward(int begin, int end, ShiftPermissions shiftPerm)
			{
				using (Shifter shifter = new Shifter(control, displayMessagesMode, shiftPerm))
				{
					shifter.InitialValidatePositions(ref begin, ref end, false);
					for (; begin < end; ++begin)
					{
						if (!shifter.ValidatePositions(ref begin, ref end, false))
							yield break;
						MergedMessagesEntry entry = control.mergedMessages[begin];
						yield return new IndexedMessage(begin, displayMessagesMode ? entry.DisplayMsg : entry.LoadedMsg);
					}
				}
			}

			public IEnumerable<IndexedMessage> Reverse(int begin, int end)
			{
				return Reverse(begin, end, new ShiftPermissions());
			}

			public IEnumerable<IndexedMessage> Reverse(int begin, int end, ShiftPermissions shiftPerm)
			{
				using (Shifter shifter = new Shifter(control, displayMessagesMode, shiftPerm))
				{
					shifter.InitialValidatePositions(ref begin, ref end, true);

					for (; begin > end; --begin)
					{
						if (!shifter.ValidatePositions(ref begin, ref end, true))
							yield break;
						MergedMessagesEntry entry = control.mergedMessages[begin];
						yield return new IndexedMessage(begin, displayMessagesMode ? entry.DisplayMsg : entry.LoadedMsg);
					}
				}
			}
		};

		public ShiftPermissions GetShiftPermissions()
		{
			return new ShiftPermissions(focused.DisplayPosition == 0, focused.DisplayPosition == (visibleCount - 1));
		}

		public void OnShowFiltersClicked()
		{
			model.UINavigationHandler.ShowFiltersView();
		}

		#endregion

		#region Protected methods

		protected virtual void OnFocusedMessageChanged()
		{
			if (FocusedMessageChanged != null)
				FocusedMessageChanged(this, EventArgs.Empty);
		}

		protected virtual void OnBeginShifting()
		{
			if (BeginShifting != null)
				BeginShifting(this, EventArgs.Empty);
		}

		protected virtual void OnEndShifting()
		{
			if (EndShifting != null)
				EndShifting(this, EventArgs.Empty);
		}

		#endregion

		#region Implementation

		struct MergedMessagesEntry
		{
			public MessageBase LoadedMsg;
			public MessageBase DisplayMsg;
			public FiltersList.PreprocessingResult DisplayFiltersPreprocessingResult;
			public FiltersList.PreprocessingResult HighlightFiltersPreprocessingResult;
		};

		public struct ShiftPermissions
		{
			public readonly bool AllowUp;
			public readonly bool AllowDown;
			public ShiftPermissions(bool allowUp, bool allowDown)
			{
				this.AllowUp = allowUp;
				this.AllowDown = allowDown;
			}
		};

		class Shifter : IDisposable
		{
			bool active;
			IMessagesCollection collection;
			Presenter control;
			ShiftPermissions shiftPerm;

			public Shifter(
				Presenter control, bool displayMode,
				ShiftPermissions shiftPerm)
			{
				this.control = control;
				this.shiftPerm = shiftPerm;
				if (displayMode)
					collection = control.displayMessagesCollection;
				else
					collection = control.loadedMessagesCollection;
			}

			public void InitialValidatePositions(ref int begin, ref int end, bool reverse)
			{
				if (!reverse)
				{
					if (!shiftPerm.AllowUp)
						begin = Math.Max(0, begin);
					if (!shiftPerm.AllowDown)
						end = Math.Min(collection.Count, end);
				}
				else
				{
					if (!shiftPerm.AllowUp)
						end = Math.Max(-1, end);
					if (!shiftPerm.AllowDown)
						begin = Math.Min(collection.Count - 1, begin);
				}
			}

			public bool ValidatePositions(ref int begin, ref int end, bool reverse)
			{
				if (shiftPerm.AllowUp)
				{
					while (begin < 0)
					{
						int delta = ShiftUp();
						if (delta == 0)
							return false;
						end += delta;
						begin += delta;
					}
				}
				if (shiftPerm.AllowDown)
				{
					while (begin >= collection.Count)
					{
						int delta = ShiftDown();
						if (delta == 0)
							return false;
						end -= delta;
						begin -= delta;
					}
				}
				return true;
			}

			public int ShiftDown()
			{
				// Check if it is possible to shift down. Return immediately if not.
				// Just to avoid annoying flickering.
				if (!control.model.IsShiftableDown)
					return 0;

				// Parameters of the current last message
				long lastPosition = 0; // hash
				int lastIndex = -1; // display index

				MessageBase lastMessage = null;

				int countBeforeShifting = collection.Count;

				// Get the info about the last (pivot) message.
				// Later after actual shifting it will be used to find the position 
				// where to start the iterating from.
				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
				{
					lastPosition = l.Message.Position;
					lastIndex = l.Index;
					lastMessage = l.Message;
					break;
				}

				// That can happen if there are no messages loaded yet. Shifting cannot be done.
				if (lastIndex < 0)
					return 0;

				// Start shifting it has not been started yet
				EnsureActive();

				// Ask the host to do actual shifting
				control.model.ShiftDown();

				// Remerge the messages. Not sure if it is needed. 
				// It may be done in ShiftDown(). todo: check if it is really needed.
				control.InternalUpdate();

				// Check if the last message has changed. 
				// It is an optimization for the case when there is no room to shift.
				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
				{
					if (lastPosition == l.Message.Position)
						return 0;
					break;
				}

				// Search for the pivot message among new merged collection
				foreach (IndexedMessage l in collection.Forward(0, int.MaxValue))
					if (l.Message.Position == lastPosition)
						return lastIndex - l.Index;

				// We didn't find our pivot message after the shifting.
				return 0;
			}

			public int ShiftUp()
			{
				// This function is symmetric to ShiftDown(). See comments there.

				if (!control.model.IsShiftableUp)
					return 0;

				long firstPosition = 0;
				int firstIndex = -1;

				int countBeforeShifting = collection.Count;

				foreach (IndexedMessage l in collection.Forward(0, 1))
				{
					firstPosition = l.Message.Position;
					firstIndex = l.Index;
					break;
				}

				if (firstIndex < 0)
					return 0;

				EnsureActive();

				control.model.ShiftUp();
				control.InternalUpdate();

				foreach (IndexedMessage l in collection.Forward(0, 1))
				{
					if (firstPosition == l.Message.Position)
						return 0;
					break;
				}

				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
					if (l.Message.Position == firstPosition)
						return l.Index - firstIndex;

				return 0;
			}

			void EnsureActive()
			{
				if (active)
					return;
				active = true;
				control.OnBeginShifting();
			}

			#region IDisposable Members

			public void Dispose()
			{
				if (!active)
					return;
				control.OnEndShifting();
			}

			#endregion
		};

		static MergedMessagesEntry InitMergedMessagesEntry(
			MessageBase message, 
			MergedMessagesEntry cachedMessageEntry,
			ThreadLocal<FiltersList> displayFilters,
			ThreadLocal<FiltersList> highlighFilters,
			bool displayFiltersPreprocessingResultCacheIsValid,
			bool highlightFiltersPreprocessingResultCacheIsValid)
		{
			MergedMessagesEntry ret;

			ret.LoadedMsg = message;
			ret.DisplayMsg = null;

			//displayFiltersPreprocessingResultCacheIsValid = false;

			if (displayFiltersPreprocessingResultCacheIsValid)
				ret.DisplayFiltersPreprocessingResult = cachedMessageEntry.DisplayFiltersPreprocessingResult;
			else
				ret.DisplayFiltersPreprocessingResult = displayFilters.Value.PreprocessMessage(message);

			if (highlightFiltersPreprocessingResultCacheIsValid)
				ret.HighlightFiltersPreprocessingResult = cachedMessageEntry.HighlightFiltersPreprocessingResult;
			else
				ret.HighlightFiltersPreprocessingResult = highlighFilters.Value.PreprocessMessage(message);

			return ret;
		}

		static IEnumerable<IndexedMessage> ToEnumerable(IEnumerator<IndexedMessage> enumerator, int count)
		{
			for (; ; )
			{
				if (count == 0)
					break;
				yield return enumerator.Current;
				--count;
				if (count == 0)
					break;
				enumerator.MoveNext();
			}
		}

		void InternalUpdate()
		{
			using (tracer.NewFrame)
			using (new ScopedGuard(view.UpdateStarted, view.UpdateFinished))
			{
				++mergedMessagesVersion;

				// Zero thread's counters
				foreach (IThread t in model.Threads)
				{
					t.ResetCounters(ThreadCounter.All);
				}

				FocusedMessageInfo prevFocused = focused;
				long prevFocusedPosition = prevFocused.Message != null ? prevFocused.Message.Position : long.MinValue;

				focused = new FocusedMessageInfo();

				int modelMessagesCount = model.Messages.Count;

				ResizeMergedMessages(modelMessagesCount);

				int loadedCount = 0;
				visibleCount = 0;

				FiltersList displayFilters = model.DisplayFilters;
				FiltersList.BulkProcessingHandle displayFiltersProcessingHandle = BeginBulkProcessing(displayFilters);

				FiltersList hlFilters = model.HighlightFilters;
				FiltersList.BulkProcessingHandle hlFiltersProcessingHandle = BeginBulkProcessing(hlFilters);

				IBookmarksHandler bmk = CreateBookmarksHandler();

				// Flag that will indicate that there are ending frames that don't 
				// have appropriate begining frames. Such end frames can appear 
				// if the log is not loaded completely and some begin frames are
				// before the the loaded log range.
				// This flag is a little optminization: we don't do the second pass
				// through the loaded lines if this flag stays false after the first pass.
				bool thereAreHangingEndFrames = false;

				using (var enumerator = model.Messages.Forward(0, int.MaxValue).GetEnumerator())
				using (ThreadLocal<FiltersList> displayFiltersThreadLocal = new ThreadLocal<FiltersList>(() => displayFilters.Clone()))
				using (ThreadLocal<FiltersList> highlightFiltersThreadLocal = new ThreadLocal<FiltersList>(() => hlFilters.Clone()))
				{
					enumerator.MoveNext();
					foreach (MergedMessagesEntry preprocessedMessage in 
						ToEnumerable(enumerator, modelMessagesCount)
						.AsParallel().AsOrdered()
						.Select(im => InitMergedMessagesEntry(
							im.Message, mergedMessages[im.Index],
							displayFiltersThreadLocal, highlightFiltersThreadLocal, 
							displayFiltersPreprocessingResultCacheIsValid,
							highlightFiltersPreprocessingResultCacheIsValid)))
					{
						MessageBase loadedMessage = preprocessedMessage.LoadedMsg;
						IThread messageThread = loadedMessage.Thread;

						messageThread.CountLine(loadedMessage);
						bool excludedBecauseOfInvisibleThread = !messageThread.ThreadMessagesAreVisible;
						bool collapsed = messageThread.IsInCollapsedRegion;

						FilterAction filterAction = displayFilters.ProcessNextMessageAndGetItsAction(loadedMessage,
							preprocessedMessage.DisplayFiltersPreprocessingResult, 
							messageThread.DisplayFilterContext);
						bool excludedAsFilteredOut = filterAction == FilterAction.Exclude;

						loadedMessage.SetHidden(collapsed, excludedBecauseOfInvisibleThread, excludedAsFilteredOut);

						bool isHighlighted = false;
						if (!loadedMessage.IsHiddenAsFilteredOut)
						{
							FilterAction hlFilterAction = hlFilters.ProcessNextMessageAndGetItsAction(loadedMessage,
								preprocessedMessage.HighlightFiltersPreprocessingResult, 
								messageThread.HighlightFilterContext);
							isHighlighted = hlFilterAction == FilterAction.Include;
						}
						loadedMessage.SetHighlighted(isHighlighted);

						HandleBookmarks(bmk, loadedMessage);

						mergedMessages[loadedCount] = preprocessedMessage;
						++loadedCount;

						if (loadedMessage.IsVisible)
						{
							if (prevFocusedPosition == loadedMessage.Position)
								FoundPreviouslyFocusedMessage(loadedMessage);
							SetDisplayMessage(visibleCount, loadedMessage);
							++visibleCount;
						}

						HandleFrameFlagsAndSetLevel(loadedMessage, ref thereAreHangingEndFrames);
					}
				}

				System.Diagnostics.Debug.Assert(loadedCount == modelMessagesCount);

				if (thereAreHangingEndFrames)
				{
					HandleHangingFrames();
				}

				tracer.Info("Update finished: visibleCount={0}, loaded lines={1}", visibleCount, mergedMessages.Count);

				if (displayFilters != null)
					displayFilters.EndBulkProcessing(displayFiltersProcessingHandle);
				if (hlFilters != null)
					hlFilters.EndBulkProcessing(hlFiltersProcessingHandle);

				displayFiltersPreprocessingResultCacheIsValid = true;
				highlightFiltersPreprocessingResultCacheIsValid = true;

				view.UpdateScrollSizeToMatchVisibleCount();
				TakeNewFocusedMessageInUse(prevFocused);
				DisplayEverythingFilteredOutMessageIfNeeded();
				view.Invalidate();
			}
		}

		private void ResizeMergedMessages(int modelMessagesCount)
		{
			if (mergedMessages.Capacity < modelMessagesCount)
				mergedMessages.Capacity = modelMessagesCount;
			int missingElementtsCount = modelMessagesCount - mergedMessages.Count;
			if (missingElementtsCount > 0)
				mergedMessages.AddRange(Enumerable.Repeat(new MergedMessagesEntry(), missingElementtsCount));
			else if (missingElementtsCount < 0)
				mergedMessages.RemoveRange(modelMessagesCount, -missingElementtsCount);
		}

		private void DisplayEverythingFilteredOutMessageIfNeeded()
		{
			bool everythingFilteredOut =
				displayMessagesCollection.Count == 0
			 && loadedMessagesCollection.Count > 0
			 && model.DisplayFilters != null;
			view.DisplayEverythingFilteredOutMessage(everythingFilteredOut);
		}

		private void TakeNewFocusedMessageInUse(FocusedMessageInfo prevFocused)
		{
			if (prevFocused.Message != focused.Message)
			{
				OnFocusedMessageChanged();
			}
			else
			{
				focused.Highlight = prevFocused.Highlight;
			}

			if (focused.Message != null)
			{
				if (prevFocused.Message == null)
				{
					view.ScrollInView(focused.DisplayPosition, true);
				}
			}
		}

		private void SetDisplayMessage(int displayIndex, MessageBase m)
		{
			MergedMessagesEntry tmp = mergedMessages[displayIndex];
			tmp.DisplayMsg = m;
			mergedMessages[displayIndex] = tmp;
		}

		private void FoundPreviouslyFocusedMessage(MessageBase m)
		{
			focused.DisplayPosition = visibleCount;
			focused.Message = m;
			tracer.Info("Found the line that was focused before the update. Changing the focused line. Position={0}, DispPosition={1}",
				focused.Message.Position, focused.DisplayPosition);
		}

		private IBookmarksHandler CreateBookmarksHandler()
		{
			return model.Bookmarks != null ? model.Bookmarks.CreateHandler() : null;
		}

		private static FiltersList.BulkProcessingHandle BeginBulkProcessing(FiltersList filters)
		{
			if (filters != null)
			{
				filters.PurgeDisposedFiltersAndFiltersHavingDisposedThreads();
				return filters.BeginBulkProcessing();
			}
			return null;
		}

		private static void HandleBookmarks(IBookmarksHandler bmk, MessageBase m)
		{
			if (bmk != null)
			{
				m.SetBookmarked(bmk.ProcessNextMessageAndCheckIfItIsBookmarked(m));
			}
			else
			{
				m.SetBookmarked(false);
			}
		}

		private void HandleHangingFrames()
		{
			tracer.Info("Hanging end frames have been detected. Making the second pass.");

			foreach (IThread t in model.Threads)
				t.ResetCounters(ThreadCounter.FramesInfo);

			foreach (IndexedMessage r in loadedMessagesCollection.Reverse(int.MaxValue, -1))
			{
				IThread t = r.Message.Thread;
				r.Message.SetLevel(r.Message.Level + t.Frames.Count);

				FrameEnd fe = r.Message as FrameEnd;
				if (fe != null && fe.Start == null)
					t.Frames.Push(r.Message);
			}
		}

		private static void HandleFrameFlagsAndSetLevel(MessageBase message, ref bool thereAreHangingEndFrames)
		{
			MessageBase.MessageFlag f = message.Flags;
			IThread td = message.Thread;
			int level = td.Frames.Count;
			switch (f & MessageBase.MessageFlag.TypeMask)
			{
				case MessageBase.MessageFlag.StartFrame:
					td.Frames.Push(message);
					if ((f & MessageBase.MessageFlag.Collapsed) != 0)
						td.BeginCollapsedRegion();
					break;
				case MessageBase.MessageFlag.EndFrame:
					FrameEnd end = (FrameEnd)message;
					if (td.Frames.Count > 0)
					{
						FrameBegin begin = (FrameBegin)td.Frames.Pop();
						end.SetStart(begin);
						begin.SetEnd(end);
						--level;
					}
					else
					{
						thereAreHangingEndFrames = true;
						end.SetStart(null);
					}
					if ((f & MessageBase.MessageFlag.Collapsed) != 0)
						td.EndCollapsedRegion();
					break;
			}

			message.SetLevel(level);
		}

		IEnumerable<MessageBase> INextBookmarkCallback.EnumMessages(DateTime time, bool forward)
		{
			if (forward)
			{
				foreach (IndexedMessage l in loadedMessagesCollection.Forward(
					GetDateLowerBound(time), int.MaxValue,
						new ShiftPermissions(false, true)))
				{
					if (l.Message.Time > time)
						break;
					yield return l.Message;
				}
			}
			else
			{
				foreach (IndexedMessage l in loadedMessagesCollection.Reverse(
					GetDateUpperBound(time) - 1, int.MinValue,
						new ShiftPermissions(true, false)))
				{
					if (l.Message.Time < time)
						break;
					yield return l.Message;
				}
			}
		}

		int GetDateLowerBound(DateTime d)
		{
			return ListUtils.BinarySearch(mergedMessages, 0, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.LoadedMsg.Time < d; });
		}

		int GetDateUpperBound(DateTime d)
		{
			return ListUtils.BinarySearch(mergedMessages, 0, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.LoadedMsg.Time <= d; });
		}

		void GetDateEqualRange(DateTime d, out int begin, out int end)
		{
			begin = GetDateLowerBound(d);
			end = ListUtils.BinarySearch(mergedMessages, begin, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.LoadedMsg.Time <= d; });
		}

		// It takes O(n) time
		// todo: optimize by using binary rearch over mergedMessages
		int Position2DisplayPosition(int position)
		{
			MessageBase l = this.mergedMessages[position].LoadedMsg;
			foreach (IndexedMessage it2 in displayMessagesCollection.Forward(0, int.MaxValue))
				if (it2.Message == l)
					return it2.Index;
			return -1;
		}

		readonly IModel model;
		readonly IView view;
		readonly ICallback callback;

		readonly LJTraceSource tracer;
		readonly List<MergedMessagesEntry> mergedMessages = new List<MergedMessagesEntry>();
		int visibleCount;
		int mergedMessagesVersion;
		MergedMessagesCollection loadedMessagesCollection;
		MergedMessagesCollection displayMessagesCollection;
		FocusedMessageInfo focused;
		int selectedCount;
		bool displayFiltersPreprocessingResultCacheIsValid;
		bool highlightFiltersPreprocessingResultCacheIsValid;

		#endregion
	};

	public class DefaultModelImpl : IModel
	{
		Model model;

		public DefaultModelImpl(Model model)
		{
			this.model = model;
			this.model.OnMessagesChanged += delegate(object sender, Model.MessagesChangedEventArgs e) 
			{ 
				if (OnMessagesChanged != null) 
					OnMessagesChanged(sender, e); 
			};
		}

		public IMessagesCollection Messages
		{
			get { return model.Messages; }
		}

		public IEnumerable<IThread> Threads
		{
			get { return model.Threads; }
		}

		public FiltersList DisplayFilters
		{
			get { return model.DisplayFilters; }
		}

		public FiltersList HighlightFilters
		{
			get { return model.HighlightFilters; }
		}

		public IBookmarks Bookmarks
		{
			get { return model.Bookmarks; }
		}

		public IUINavigationHandler UINavigationHandler
		{
			get { return model.UINavigationHandler; }
		}

		public LJTraceSource Tracer
		{
			get { return model.Tracer; }
		}

		public void ShiftUp()
		{
			model.ShiftUp();
		}

		public bool IsShiftableUp
		{
			get { return model.IsShiftableUp; }
		}

		public void ShiftDown()
		{
			model.ShiftDown();
		}

		public bool IsShiftableDown
		{
			get { return model.IsShiftableUp; }
		}

		public void ShiftAt(DateTime t)
		{
			model.ShiftAt(t);
		}

		public event EventHandler<Model.MessagesChangedEventArgs> OnMessagesChanged;
	};
};