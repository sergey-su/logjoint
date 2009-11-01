using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;

namespace LogJoint.UI
{
	public interface ILogViewerControlHost
	{
		Source Trace { get; }
		IMessagesCollection Messages { get; }
		IEnumerable<IThread> Threads { get; }
		bool IsShiftableUp { get; }
		void ShiftUp();
		bool IsShiftableDown { get; }
		void ShiftDown();
		void ShiftAt(DateTime t);
		IBookmarks Bookmarks { get; }
		IUINavigationHandler UINavigationHandler { get; }
		IMainForm MainForm { get; }
		FiltersList Filters { get; }
		IStatusReport GetStatusReport();
	};

	public partial class LogViewerControl : Control, INextBookmarkCallback,
		IMessagePropertiesFormHost
	{
		public event EventHandler ManualRefresh;
		public event EventHandler BeginShifting;
		public event EventHandler EndShifting;
		public event EventHandler CurrentMessageChanged;

		public bool ShowTime
		{
			get
			{
				return drawContext.ShowTime;
			}
			set
			{
				if (drawContext.ShowTime == value)
					return;
				drawContext.ShowTime = value;
				Invalidate();
			}
		}

		public bool ShowMilliseconds 
		{ 
			get
			{
				return drawContext.ShowMilliseconds;
			}
			set
			{
				if (drawContext.ShowMilliseconds == value)
					return;
				drawContext.ShowMilliseconds = value;
				UpdateTimeAreaSize();
				if (drawContext.ShowTime)
				{
					Invalidate();
				}
			}
		}


		public LogViewerControl()
		{
			InitializeComponent();

			this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);

			loadedMessagesCollection = new MergedMessagesCollection(this, false);
			displayMessagesCollection = new MergedMessagesCollection(this, true);

			base.BackColor = Color.White;
			base.TabStop = true;
			base.Enabled = true;

			drawContext.Font = new Font("Courier New", 10);
			drawContext.NewlineFont = new Font("Symbol", 10, FontStyle.Bold);

			drawContext.SingleLineFormat = new StringFormat(StringFormatFlags.LineLimit);
			drawContext.OutlineMarkupPen = new Pen(Color.Gray, 1);
			drawContext.SelectedOutlineMarkupPen = new Pen(Color.White, 1);

			drawContext.InfoMessagesBrush = SystemBrushes.ControlText;
			drawContext.SelectedTextBrush = SystemBrushes.HighlightText;
			drawContext.SelectedFocuslessTextBrush = SystemBrushes.ControlText;
			drawContext.CommentsBrush = SystemBrushes.GrayText;

			drawContext.DefaultBackgroundBrush = SystemBrushes.Window;
			drawContext.SelectedBkBrush = SystemBrushes.Highlight;
			drawContext.SelectedFocuslessBkBrush = Brushes.Gray;
			
			drawContext.ErrorIcon = errPictureBox.Image;
			drawContext.WarnIcon = warnPictureBox.Image;
			drawContext.BookmarkIcon = bookmarkPictureBox.Image;
			drawContext.SmallBookmarkIcon = smallBookmarkPictureBox.Image;

			drawContext.HighlightPen = new Pen(Color.Red, 3);
			drawContext.HighlightPen.LineJoin = LineJoin.Round;

			drawContext.TimeSeparatorLine = new Pen(Color.Gray, 1);

			using (Graphics tmp = Graphics.FromHwnd(IntPtr.Zero))
			{
				int count = 64;
				drawContext.CharSize = tmp.MeasureString(new string('0', count), drawContext.Font);
				drawContext.CharSize.Width /= (float)count;
			}

			drawContext.MessageHeight = (int)Math.Floor(drawContext.CharSize.Height);
			UpdateTimeAreaSize();
			drawContext.ShowTime = false;
		}

		void UpdateTimeAreaSize()
		{
			string testStr = MessageBase.FormatTime(
				new DateTime(2011, 11, 11, 11, 11, 11, 111), 
				drawContext.ShowMilliseconds
			);
			drawContext.TimeAreaSize = (int)Math.Floor(
				drawContext.CharSize.Width * (float)testStr.Length
			) + 5;
		}

		public DateTime? CurrentViewTime
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

		public void SetHost(ILogViewerControlHost host)
		{
			Debug.Assert(this.host == null);
			this.host = host;
			this.tracer = host.Trace;
		}

		static class Native
		{
			[StructLayout(LayoutKind.Sequential)]
			public struct SCROLLINFO
			{
				public int cbSize;
				public SIF fMask;
				public int nMin;
				public int nMax;
				public int nPage;
				public int nPos;
				public int nTrackPos;
			}

			[StructLayout(LayoutKind.Sequential)]
			public struct RECT
			{
				public int left;
				public int top;
				public int right;
				public int bottom;
				public RECT(Rectangle r)
				{
					left = r.Left;
					top = r.Top;
					right = r.Right;
					bottom = r.Bottom;
				}
				public Rectangle ToRectangle()
				{
					return new Rectangle(left, top, right - left, bottom - top);
				}
			}

			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
			public static extern int SetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si, bool redraw);

			[DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
			public static extern bool GetScrollInfo(HandleRef hWnd, SB fnBar, ref SCROLLINFO si);

			[DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
			public static extern int ScrollWindowEx(
				HandleRef hWnd, 
				int nXAmount, int nYAmount,
				ref RECT rectScrollRegion,
				ref RECT rectClip, 
				HandleRef hrgnUpdate, 
				ref RECT prcUpdate, 
				int flags);

			public static int LOWORD(int n)
			{
				return (n & 0xffff);
			}

			public static int LOWORD(IntPtr n)
			{
				return LOWORD((int)((long)n));
			}

			public enum SB: int
			{
				LINEUP = 0,
				LINELEFT = 0,
				LINEDOWN = 1,
				LINERIGHT = 1,
				PAGEUP = 2,
				PAGELEFT = 2,
				PAGEDOWN = 3,
				PAGERIGHT = 3,
				THUMBPOSITION = 4,
				THUMBTRACK = 5,
				TOP = 6,
				LEFT = 6,
				BOTTOM = 7,
				RIGHT = 7,
				ENDSCROLL = 8,

				HORZ = 0,
				VERT = 1,
				BOTH = 3,
			}

			public enum SIF: uint
			{
				RANGE = 0x0001,
				PAGE = 0x0002,
				POS = 0x0004,
				DISABLENOSCROLL = 0x0008,
				TRACKPOS = 0x0010,
				ALL = (RANGE | PAGE | POS | TRACKPOS),
			}
		};

		static readonly Regex SingleNRe = new Regex(@"(?<ch>[^\r])\n+", RegexOptions.ExplicitCapture);

		static public string FixLineBreaks(string str)
		{
			// replace all single \n with \r\n 
			// (single \n is the \n that is not preceded by \r)
			return SingleNRe.Replace(str, "${ch}\r\n", str.Length, 0);
		}

		public bool IsCursorAtAndOfLog()
		{
			return false;
		}

		public void PutCursorAtEndOfLog()
		{
			if (visibleCount == 0)
				return;

			Invalidate();
		}

		public void UpdateView()
		{
			using (tracer.NewFrame)
			{
				InternalUpdate();
			}
		}

		public void SelectMessageAt(DateTime d, bool lowerBound)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Date={0}, lowerBound flag={1}", d, lowerBound);

				DeselectAll();

				Predicate<MergedMessagesEntry> cmp;
				if (lowerBound)
					cmp = delegate(MergedMessagesEntry x) { return x.DisplayMsg.Time < d; };
				else
					cmp = delegate(MergedMessagesEntry x) { return x.DisplayMsg.Time <= d; };

				int idx = ListUtils.BinarySearch(mergedMessages, 0, visibleCount, cmp);

				tracer.Info("Index of the line to be selected: {0}", idx);

				if (idx == visibleCount)
					idx = visibleCount - 1;

				if (idx >= 0 && idx < visibleCount)
				{
					SelectMessage(mergedMessages[idx].DisplayMsg, idx);
					ScrollInView(idx, true);
				}
				else
				{
					tracer.Warning("The index is out of visible range (0-{0})", visibleCount);
				}
			}
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

		int GetDateLowerBound(DateTime d)
		{
			return ListUtils.BinarySearch(mergedMessages, 0, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.Msg.Time < d; });
		}

		int GetDateUpperBound(DateTime d)
		{
			return ListUtils.BinarySearch(mergedMessages, 0, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.Msg.Time <= d; });
		}

		void GetDateEqualRange(DateTime d, out int begin, out int end)
		{
			begin = GetDateLowerBound(d);
			end = ListUtils.BinarySearch(mergedMessages, begin, mergedMessages.Count,
				delegate(MergedMessagesEntry entry) { return entry.Msg.Time <= d; });
		}

		public void SelectMessageAt(IBookmark bmk)
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
					host.ShiftAt(bmk.Time);
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

			// Search for the bookmarked message by hash
			for (; begin != end; ++begin)
			{
				MessageBase l = mergedMessages[begin].Msg;
				if (l.GetHashCode() == bmk.MessageHash)
					break;
			}

			if (begin == end)
				return;

			int idx = Position2DisplayPosition(begin);
			if (idx < 0)
				return;

			DeselectAll();
			SelectMessage(mergedMessages[idx].DisplayMsg, idx);
			ScrollInView(idx, true);
		}

		public IEnumerable<MessageBase> EnumMessages(DateTime time, bool forward)
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

		public bool NextBookmark(bool forward)
		{
			if (focused.Message == null || host.Bookmarks == null)
				return false;
			IBookmark bmk = host.Bookmarks.GetNext(focused.Message, forward, this);
			SelectMessageAt(bmk);
			return bmk != null;
		}

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
			public bool MatchCase;
			public MessageBase.MessageFlag TypesToLookFor;
		};

		public struct SearchResult
		{
			public bool Succeeded;
			public int Position;
			public MessageBase Message;
		};

		public SearchResult Search(SearchOptions opts)
		{
			// init return value by default (rv.Succeeded = false)
			SearchResult rv = new SearchResult();

			SearchOptions.SearchPosition startFrom = new SearchOptions.SearchPosition();
			if (opts.StartFrom.HasValue)
			{
				startFrom = opts.StartFrom.Value;
			}
			else if (focused.Message != null)
			{
				startFrom.Message = DisplayPosition2Position(focused.DisplayPosition);
				startFrom.Position = opts.ReverseSearch ? focused.Highligt.Begin : focused.Highligt.End;
			}

			Regex re = null;
			if (!string.IsNullOrEmpty(opts.Template))
			{
				if (opts.Regexp)
				{
					RegexOptions reOpts = RegexOptions.None;
					if (opts.MatchCase)
						reOpts |= RegexOptions.IgnoreCase;
					if (opts.ReverseSearch)
						reOpts |= RegexOptions.RightToLeft;
					re = new Regex(opts.Template, reOpts);
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

				// matched string position
				int matchBegin = 0; // index of the first matched char
				int matchEnd = 0; // index of following after the last matched one

				string text = it.Message.Text;

				int textPos;

				if (!string.IsNullOrEmpty(opts.Template)) // empty/null template means that text matching isn't required
				{
					if (messagesProcessed == 1)
						textPos = startFrom.Position;
					else if (opts.ReverseSearch)
						textPos = text.Length;
					else
						textPos = 0;

					if (re != null)
					{
						Match m = re.Match(text, textPos);
						if (!m.Success)
							continue;
						matchBegin = m.Index;
						matchEnd = matchBegin + m.Length;
					}
					else
					{
						string txtToAnalize = opts.MatchCase ? text : text.ToLower();
						int i;
						if (opts.ReverseSearch)
							i = txtToAnalize.LastIndexOf(opts.Template, textPos);
						else
							i = txtToAnalize.IndexOf(opts.Template, textPos);
						if (i < 0)
							continue;
						matchBegin = i;
						matchEnd = matchBegin + opts.Template.Length;
					}

					if (opts.WholeWord)
					{
						if (matchBegin > 0)
							if (char.IsLetterOrDigit(text, matchBegin - 1))
								continue;
						if (matchEnd < text.Length - 1)
							if (char.IsLetterOrDigit(text, matchEnd))
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

				focused.Highligt.Begin = matchBegin;
				focused.Highligt.End = matchEnd;

				Invalidate();

				break;
			}

			if (!rv.Succeeded && focused.Message == null)
				MoveSelection(opts.ReverseSearch ? 0 : visibleCount - 1, true, true);

			// return value initialized by-default as non-successful search

			return rv;
		}

		// It takes O(n) time
		// todo: optimize by using binary rearch over mergedMessages
		int Position2DisplayPosition(int position)
		{
			MessageBase l = this.mergedMessages[position].Msg;
			foreach (IndexedMessage it2 in displayMessagesCollection.Forward(0, int.MaxValue))
				if (it2.Message == l)
					return it2.Index;
			return -1;
		}

		// It takes O(n) time
		// todo: optimize by using binary rearch over mergedMessages
		int DisplayPosition2Position(int dposition)
		{
			MessageBase l = mergedMessages[dposition].DisplayMsg;
			if (l == null)
				return -1;
			foreach (IndexedMessage i in loadedMessagesCollection.Forward(0, int.MaxValue))
				if (i.Message == l)
					return i.Index;
			return -1;
		}

		void SelectOnly(MessageBase msg, int position)
		{
			DeselectAll();
			EnsureNotCollapsed(position);
			int dispPos = Position2DisplayPosition(position);
			SelectMessage(msg, dispPos);
			ScrollInView(dispPos, true);
		}

		internal void InternalUpdate()
		{
			using (tracer.NewFrame)
			{
				++mergedMessagesVersion;

				// Zero thread's counters
				foreach (IThread t in host.Threads)
				{
					t.ResetCounters();
				}

				FocusedMessageInfo prevFocused = focused;
				int prevFocusedRelativeScrollPosition = focused.DisplayPosition * drawContext.MessageHeight - scrollPos.Y;

				focused = new FocusedMessageInfo();

				mergedMessages.Clear();
				visibleCount = 0;

				FiltersList filters = host.Filters;
				if (filters != null)
				{
					filters.ResetFiltersCounters();
				}

				IBookmarksHandler bmk = host.Bookmarks != null ? host.Bookmarks.CreateHandler() : null;

				// Flag that will inidicate that there are ending frames that don't 
				// have appropriate begining frames. Such end frames can appear 
				// if the log is not loaded completely and some begin frames are
				// before the the loaded log range.
				// This flag is a little optminization: we don't do the second pass
				// through the loaded lines if this flag stayed false after the first pass.
				bool thereAreHangingEndFrames = false;
				
				foreach (IndexedMessage im in host.Messages.Forward(0, int.MaxValue))
				{
					MessageBase m = im.Message;
					IThread td = m.Thread;
					MessageBase.MessageFlag f = m.Flags;

					td.CountLine(m);

					MergedMessagesEntry ml = new MergedMessagesEntry();
					ml.Msg = m;
					mergedMessages.Add(ml);

					bool collapsed = td.IsInCollapsedRegion;

					bool excludedBecauseOfInvisibleThread = !td.ThreadMessagesAreVisible;

					bool excludedAsFilteredOut = false;

					if (filters != null)
					{
						FilterAction filterAction = filters.ProcessNextMessageAndGetItsAction(m);
						excludedAsFilteredOut = filterAction == FilterAction.Hide;
					}

					int level = td.Frames.Count;

					m.SetHidden(collapsed, excludedBecauseOfInvisibleThread, excludedAsFilteredOut);

					if (bmk != null)
					{
						m.SetBookmarked(bmk.ProcessNextMessageAndCheckIfItIsBookmarked(m));
					}
					else
					{
						m.SetBookmarked(false);
					}

					if (m.Visible)
					{
						if (prevFocused.Message == m)
						{
							focused.DisplayPosition = visibleCount;
							focused.Message = m;
							tracer.Info("The line that was focused before the update is found. Changing the focused line. Hash={0}, DispPosition={1}", 
								focused.Message.GetHashCode(), focused.DisplayPosition);
						}
						MergedMessagesEntry tmp = mergedMessages[visibleCount];
						tmp.DisplayMsg = m;
						mergedMessages[visibleCount] = tmp;
						++visibleCount;
					}

					switch (f & MessageBase.MessageFlag.TypeMask)
					{
						case MessageBase.MessageFlag.StartFrame:
							td.Frames.Push(m);
							if ((f & MessageBase.MessageFlag.Collapsed) != 0)
								td.BeginCollapsedRegion();
							break;
						case MessageBase.MessageFlag.EndFrame:
							FrameEnd end = (FrameEnd)m;
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

					m.SetLevel(level);
				}

				if (thereAreHangingEndFrames)
				{
					tracer.Info("Hanging end frames have been detected. Making the second pass.");

					foreach (IThread t in host.Threads)
						t.ResetCounters();

					foreach (IndexedMessage r in loadedMessagesCollection.Reverse(int.MaxValue, -1))
					{
						IThread t = r.Message.Thread;
						r.Message.SetLevel(r.Message.Level + t.Frames.Count);

						FrameEnd fe = r.Message as FrameEnd;
						if (fe != null && fe.Begin == null)
							t.Frames.Push(r.Message);
					}
				}

				tracer.Info("Update finished: visibleCount={0}, loaded lines={1}", visibleCount, mergedMessages.Count);

				SetScrollSize(new Size(10, visibleCount * drawContext.MessageHeight), true, false);

				if (prevFocused.Message != focused.Message)
				{
					OnCurrentMessageChanged();
				}
				else
				{
					focused.Highligt = prevFocused.Highligt;
				}

				
				if (focused.Message != null)
				{
					if (prevFocused.Message == null)
					{
						ScrollInView(focused.DisplayPosition, true);
					}
					else
					{
						SetScrollPos(new Point(scrollPos.X, focused.DisplayPosition * drawContext.MessageHeight - prevFocusedRelativeScrollPosition));
					}					
				}

				Invalidate();
			}
		}

		struct VisibleMessages
		{
			public int begin;
			public int end;
			public int fullyVisibleEnd;
		};

		VisibleMessages GetVisibleMessages(Rectangle viewRect)
		{
			DrawContext dc = drawContext;
			VisibleMessages rv;
			
			viewRect.Offset(0, dc.ScrollPos.Y);
			
			rv.begin = viewRect.Y / dc.MessageHeight;
			rv.fullyVisibleEnd = rv.end = viewRect.Bottom / dc.MessageHeight;

			if ((viewRect.Bottom % dc.MessageHeight) != 0)
				++rv.end;
			
			rv.end = Math.Min(visibleCount, rv.end);
			rv.fullyVisibleEnd = Math.Min(visibleCount, rv.fullyVisibleEnd);

			return rv;
		}

		IEnumerable<IndexedMessage> GetVisibleMessagesIterator(Rectangle viewRect)
		{
			VisibleMessages vl = GetVisibleMessages(viewRect);
			return displayMessagesCollection.Forward(vl.begin, vl.end);
		}

		void ShowMessageDetails()
		{
			if (GetPropertiesForm() == null)
			{
				propertiesForm = new MessagePropertiesForm(this);
				components.Add(propertiesForm);
				if (this.host.MainForm != null)
					this.host.MainForm.AddOwnedForm(propertiesForm);
			}
			propertiesForm.UpdateView(focused.Message);
			propertiesForm.Show();
		}

		protected override void OnMouseWheel(MouseEventArgs e)
		{
			Rectangle clientRectangle = base.ClientRectangle;
			int p = this.ScrollPos.Y - e.Delta;
			SetScrollPos(new Point(scrollPos.X, p));
			if (e is HandledMouseEventArgs)
			{
				((HandledMouseEventArgs)e).Handled = true;
			}
			base.OnMouseWheel(e);
		}

		protected override void OnMouseDoubleClick(MouseEventArgs e)
		{
			drawContext.ScrollPos = this.ScrollPos;
			foreach (IndexedMessage i in GetVisibleMessagesIterator(ClientRectangle))
			{
				drawContext.MessageIdx = i.Index;
				MessageBase l = i.Message;
				MessageBase.Metrics m = l.GetMetrics(drawContext);
				if (!m.MessageRect.Contains(e.X, e.Y))
					continue;
				if (m.OulineBox.Contains(e.X, e.Y))
					continue;
				if (DoExpandCollapse(l, false, new bool?()))
					break;
				ShowMessageDetails();
				break;
			}
			base.OnMouseDoubleClick(e);
		}

		protected override void OnGotFocus(EventArgs e)
		{
			InvalidateMessagesArea();
			base.OnGotFocus(e);
		}

		protected override void OnLostFocus(EventArgs e)
		{
			InvalidateMessagesArea();
			base.OnLostFocus(e);
		}

		protected override void OnMouseDown(MouseEventArgs e)
		{
			this.Focus();

			drawContext.ScrollPos = this.ScrollPos;
			foreach (IndexedMessage i in GetVisibleMessagesIterator(ClientRectangle))
			{
				drawContext.MessageIdx = i.Index;
				MessageBase l = i.Message;
				MessageBase.Metrics mtx = l.GetMetrics(drawContext);

				// if used clicked line's outline box (collapse/expand cross)
				if (mtx.OulineBox.Contains(e.X, e.Y))
				{
					DoExpandCollapse(l, ModifierKeys == Keys.Control, new bool?());
					break;
				}

				// if user clicked line area
				if (mtx.MessageRect.Contains(e.X, e.Y))
				{
					if (e.Button == MouseButtons.Right)
					{
						if (!l.Selected)
						{
							DeselectAll();
							SelectMessage(l, i.Index);
						}
						DoContextMenu(e.X, e.Y);
					}
					else
					{
						if (Control.ModifierKeys != Keys.Control)
							DeselectAll();

						// In condition that frame is collaped and closed select content of frame
						FrameBegin fb = l as FrameBegin;
						if (fb != null && fb.Collapsed && fb.End != null)
							foreach (MessageBase j in EnumFrameContent(fb))
								SelectMessage(j, -1);

						// Select line itself (and focus it)
						SelectMessage(l, i.Index);
					}

					break;
				}
			}

			base.OnMouseDown(e);
		}

		IEnumerable<MessageBase> EnumFrameContent(FrameBegin fb)
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

		void MoveSelection(int newDisplayPosition, bool forwardMode, bool showExtraLinesAroundMessage)
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
				ScrollInView(l.Index, showExtraLinesAroundMessage);
				break;
			}
		}

		protected override bool IsInputKey(Keys keyData)
		{
			if (keyData == Keys.Up || keyData == Keys.Down 
					|| keyData == Keys.Left || keyData == Keys.Right)
				return true;
			return base.IsInputKey(keyData);
		}

		void DoContextMenu(int x, int y)
		{
			contextMenuStrip1.Show(this, x, y);
		}

		private void DoCopy()
		{
			if (selectedCount == 0)
				return;
			StringBuilder sb = new StringBuilder();
			/*for (Iterator it = MakeForward(); !it.AtEnd; it.Advance(1))
				if (it.Value.Selected)
					sb.AppendLine(it.Value.Text);
			Clipboard.SetText(sb.ToString());*/
		}

		bool DoExpandCollapse(MessageBase line, bool recursive, bool? collapse)
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

		void GoToParentFrame()
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

		void GoToEndOfFrame()
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

		void contextMenuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
		{
			if (e.ClickedItem == this.copyMenuItem)
				DoCopy();
			else if (e.ClickedItem == this.collapseMenuItem)
				DoExpandCollapse(focused.Message, false, new bool?());
			else if (e.ClickedItem == this.recursiveCollapseMenuItem)
				DoExpandCollapse(focused.Message, true, new bool?());
			else if (e.ClickedItem == this.gotoParentFrameMenuItem)
				GoToParentFrame();
			else if (e.ClickedItem == this.gotoEndOfFrameMenuItem)
				GoToEndOfFrame();
			else if (e.ClickedItem == this.showTimeMenuItem)
				ShowTime = (showTimeMenuItem.Checked = !showTimeMenuItem.Checked);
			else if (e.ClickedItem == this.propertiesMenuItem)
				ShowMessageDetails();
			else if (e.ClickedItem == this.toggleBmkStripMenuItem)
				ToggleBookmark(focused.Message);
		}

		protected override void OnKeyDown(KeyEventArgs kevent)
		{
			Keys k = kevent.KeyCode;
			bool ctrl = kevent.Modifiers == Keys.Control;

			if (k == Keys.F5)
			{
				OnRefresh();
				return;
			}

			if (focused.Message != null)
			{
				if (k == Keys.Up)
					if (ctrl)
						GoToParentFrame();
					else
						MoveSelection(focused.DisplayPosition - 1, true, false);
				else if (k == Keys.Down)
					if (ctrl)
						GoToEndOfFrame();
					else
						MoveSelection(focused.DisplayPosition + 1, false, false);
				else if (k == Keys.PageUp)
					MoveSelection(focused.DisplayPosition - Height / drawContext.MessageHeight, true, false);
				else if (k == Keys.PageDown)
					MoveSelection(focused.DisplayPosition + Height / drawContext.MessageHeight, false, false);
				else if (k == Keys.Left || k == Keys.Right)
					DoExpandCollapse(focused.Message, ctrl, k == Keys.Left);
				else if (k == Keys.Apps)
					DoContextMenu(0, (focused.DisplayPosition + 1) * drawContext.MessageHeight - 1 - drawContext.ScrollPos.Y);
				else if (k == Keys.Enter)
					ShowMessageDetails();
			}
			if (k == Keys.C && ctrl)
			{
				DoCopy();
			}
			if (k == Keys.Home)
			{
				if (!GetShiftPermissions().AllowUp)
					SelectFirstMessage();
			}
			else if (k == Keys.End)
			{
				if (!GetShiftPermissions().AllowDown)
					SelectLastMessage();
			}
			base.OnKeyDown(kevent);
		}

		protected override void OnPaintBackground(PaintEventArgs pevent)
		{
			// Do nothing. All painting in OnPaint.
		}

		protected override void OnPaint(PaintEventArgs pe)
		{
			DrawContext dc = drawContext;

			dc.Canvas = pe.Graphics;
			dc.ClientRect = ClientRectangle;
			dc.ScrollPos = this.ScrollPos;
			dc.ControlFocused = this.Focused;

			dc.Canvas.TextRenderingHint = System.Drawing.Text.TextRenderingHint.SingleBitPerPixelGridFit;

			// Area covered by visible lines
			Rectangle messagesArea = new Rectangle(FixedMetrics.CollapseBoxesAreaSize, 0, 0, 0);

			bool drawFocus = this.Focused && focused.Message != null;

			// Fill outline area with default brush
			Rectangle outlineArea = new Rectangle(0, 0, FixedMetrics.CollapseBoxesAreaSize, Height);
			dc.Canvas.FillRectangle(dc.DefaultBackgroundBrush, outlineArea);

			foreach (IndexedMessage il in GetVisibleMessagesIterator(pe.ClipRectangle))
			{
				MessageBase l = il.Message;
				dc.MessageIdx = il.Index;
				l.DrawOutline(dc, l.GetMetrics(dc));
			}

			int maxRight = 0;

			GraphicsState state = dc.Canvas.Save();
			try
			{
				dc.Canvas.ExcludeClip(outlineArea);

				// Get visible lines and draw them
				foreach (IndexedMessage il in GetVisibleMessagesIterator(pe.ClipRectangle))
				{
					MessageBase l = il.Message;

					// Prepare draw context
					dc.MessageIdx = il.Index;
					dc.MessageFocused = drawFocus && (il.Index == focused.DisplayPosition);

					MessageBase.Metrics m = l.GetMetrics(dc);

					// Draw the line
					l.Draw(dc, m);

					// Add bounds of current line to linesRect
					dc.Canvas.ExcludeClip(m.MessageRect);

					maxRight = Math.Max(maxRight, m.OffsetTextRect.Right);
				}

				// Fill remaining area with default background
				dc.Canvas.FillRectangle(dc.DefaultBackgroundBrush, dc.ClientRect);
			}
			finally
			{
				dc.Canvas.Restore(state);
			}

			if (dc.ShowTime)
			{
				float x = FixedMetrics.CollapseBoxesAreaSize + dc.TimeAreaSize - dc.ScrollPos.X - 2;
				if (x > FixedMetrics.CollapseBoxesAreaSize)
					dc.Canvas.DrawLine(dc.TimeSeparatorLine, x, 0, x, dc.ClientRect.Height);
			}

			if (focused.Message != null && !focused.Highligt.IsEmpty)
			{
				dc.MessageFocused = false;
				dc.MessageIdx = focused.DisplayPosition;
				focused.Message.DrawHighligt(dc, focused.Highligt, focused.Message.GetMetrics(dc));
			}

			if (maxRight > scrollSize.Width)
			{
				SetScrollSize(new Size(maxRight, scrollSize.Height), false, true);
			}

			base.OnPaint(pe);
		}

		protected override void OnResize(EventArgs e)
		{
			Invalidate();
			base.OnResize(e);
		}

		void InvalidateMessagesArea()
		{
			Rectangle r = ClientRectangle;
			r.X += FixedMetrics.CollapseBoxesAreaSize;
			r.Width -= FixedMetrics.CollapseBoxesAreaSize;
			Invalidate(r);
		}

		void EnsureNotCollapsed(int position)
		{
			IEnumerator<IndexedMessage> it = loadedMessagesCollection.Reverse(position, -1).GetEnumerator();
			if (!it.MoveNext())
				return;
			IThread thread = it.Current.Message.Thread;
			int frameCount = 0;
			for (; it.MoveNext();)
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

		void ScrollInView(int messageDisplayPosition, bool showExtraLinesAroundMessage)
		{
			int? newScrollPos = null;

			VisibleMessages vl = GetVisibleMessages(ClientRectangle);

			int extra = showExtraLinesAroundMessage ? 2 : 0;

			if (messageDisplayPosition < vl.begin + extra)
				newScrollPos = messageDisplayPosition - extra;
			else if (messageDisplayPosition >= vl.fullyVisibleEnd - extra)
				newScrollPos = messageDisplayPosition + 2 - (vl.fullyVisibleEnd - vl.begin) + extra;

			if (newScrollPos.HasValue)
				SetScrollPos(new Point(scrollPos.X, newScrollPos.Value * drawContext.MessageHeight));
		}

		void InvalidateMessage(MessageBase msg, int displayPosition)
		{
			drawContext.MessageIdx = displayPosition;
			Rectangle r = msg.GetMetrics(drawContext).MessageRect;
			if (this.focused.Message == msg && !focused.Highligt.IsEmpty)
				r.Inflate(0, 10);
			this.Invalidate(r);
		}

		void SelectMessage(MessageBase msg, int displayPosition)
		{
			using (tracer.NewFrame)
			{
				tracer.Info("Selecting line {0}. Display position = {1}", msg.GetHashCode(), displayPosition);
				if (!msg.Selected)
				{
					msg.SetSelected(true);
					++selectedCount;
					tracer.Info("The amount of selected lines has become = {0}", selectedCount);
					InvalidateMessage(msg, displayPosition);
				}
				else
				{
					tracer.Info("Line is already selected");
				}
				if (displayPosition >= 0 && focused.Message != msg)
				{
					if (focused.Message != null)
					{
						InvalidateMessage(focused.Message, focused.DisplayPosition);
					}
					focused.Message = msg;
					focused.DisplayPosition = displayPosition;
					focused.Highligt = new HighlightRange();
					InvalidateMessage(msg, displayPosition);
					OnCurrentMessageChanged();
					tracer.Info("Focused line changed to the new selection");
				}
			}
		}

		void DeselectMessage(MessageBase msg, int displayPosition)
		{
			if (!msg.Selected)
				return;
			--selectedCount;
			msg.SetSelected(false);
			if (displayPosition >= 0)
				InvalidateMessage(msg, displayPosition);
		}

		void DeselectAll()
		{
			if (selectedCount > 0)
			{
				foreach (IndexedMessage i in GetVisibleMessagesIterator(ClientRectangle))
				{
					if (i.Message.Selected)
						InvalidateMessage(i.Message, i.Index);
				}
				foreach (IndexedMessage i in loadedMessagesCollection.Forward(0, int.MaxValue))
				{
					DeselectMessage(i.Message, -1);
				}
			}
		}

		private Point ScrollPos
		{
			get
			{
				return new Point(GetScrollInfo(Native.SB.HORZ).nPos,
					GetScrollInfo(Native.SB.VERT).nPos);
			}
		}

		private Point scrollPos;
		private Size scrollSize;

		void SetScrollPos(Point pos)
		{
			if (pos.Y > scrollSize.Height)
				pos.Y = scrollSize.Height;
			else if (pos.Y < 0)
				pos.Y = 0;

			int xBefore = GetScrollInfo(Native.SB.HORZ).nPos;
			int yBefore = GetScrollInfo(Native.SB.VERT).nPos;

			scrollPos = pos;
			UpdateScrollBars(true, true);

			int xDelta = xBefore - GetScrollInfo(Native.SB.HORZ).nPos;
			int yDelta = yBefore - GetScrollInfo(Native.SB.VERT).nPos;

			if (xDelta == 0 && yDelta == 0)
			{
			}
			else if (xDelta != 0 && yDelta != 0)
			{
				Invalidate();
			}
			else
			{
				Rectangle r = ClientRectangle;
				if (xDelta != 0)
				{
					r.X += FixedMetrics.CollapseBoxesAreaSize;
					r.Width -= FixedMetrics.CollapseBoxesAreaSize;
				}
				Native.RECT scroll = new Native.RECT(r);
				Native.RECT clip = scroll;
				Native.RECT update = scroll;
				HandleRef hrgnUpdate = new HandleRef(null, IntPtr.Zero);
				Native.ScrollWindowEx(
					new HandleRef(this, base.Handle),
					xDelta, yDelta,
					ref scroll, ref clip, hrgnUpdate, ref update, 2);
			}
		}

		void SetScrollSize(Size sz, bool vRedraw, bool hRedraw)
		{
			scrollSize = sz;
			UpdateScrollBars(vRedraw, hRedraw);
		}

		void UpdateScrollBars(bool vDedraw, bool hRedraw)
		{
			if (this.IsHandleCreated && Visible)
			{
				HandleRef handle = new HandleRef(this, this.Handle);

				Native.SCROLLINFO v = new Native.SCROLLINFO();
				v.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
				v.fMask = Native.SIF.ALL;
				v.nMin = 0;
				v.nMax = scrollSize.Height;
				v.nPage = ClientSize.Height;
				v.nPos = scrollPos.Y;
				v.nTrackPos = 0;
				Native.SetScrollInfo(handle, Native.SB.VERT, ref v, vDedraw);

				Native.SCROLLINFO h = new Native.SCROLLINFO();
				h.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
				h.fMask = Native.SIF.ALL;
				h.nMin = 0;
				h.nMax = Math.Max(scrollSize.Width, ClientSize.Width + 100);
				h.nPage = ClientSize.Width;
				h.nPos = scrollPos.X;
				h.nTrackPos = 0;
				Native.SetScrollInfo(handle, Native.SB.HORZ, ref h, hRedraw);
			}
		}

		private void WmHScroll(ref System.Windows.Forms.Message m)
		{
			int ret = DoWmScroll(ref m, scrollPos.X, scrollSize.Width, Native.SB.HORZ);
			if (ret >= 0)
			{
				this.SetScrollPos(new Point(ret, scrollPos.Y));
			}
			if (focused.Message != null)
			{
				InvalidateMessage(focused.Message, focused.DisplayPosition);
			}
		}

		private Native.SCROLLINFO GetScrollInfo(Native.SB sb)
		{
			Native.SCROLLINFO si = new Native.SCROLLINFO();
			si.cbSize = Marshal.SizeOf(typeof(Native.SCROLLINFO));
			si.fMask = Native.SIF.ALL;
			Native.GetScrollInfo(new HandleRef(this, base.Handle), sb, ref si);
			return si;
		}

		int DoWmScroll(ref System.Windows.Forms.Message m,
			int num, int maximum, Native.SB bar)
		{
			if (m.LParam != IntPtr.Zero)
			{
				base.WndProc(ref m);
				return -1;
			}
			else
			{
				int smallChange = 50;
				int largeChange = 200;

				Native.SB sb = (Native.SB)Native.LOWORD(m.WParam);
				switch (sb)
				{
					case Native.SB.LINEUP:
						num -= smallChange;
						if (num <= 0)
							num = 0;
						break;

					case Native.SB.LINEDOWN:
						num += smallChange;
						if (num >= maximum)
							num = maximum;
						break;

					case Native.SB.PAGEUP:
						num -= largeChange;
						if (num <= 0)
							num = 0;
						break;

					case Native.SB.PAGEDOWN:
						num += largeChange;
						if (num >= maximum)
							num = maximum;
						break;

					case Native.SB.THUMBTRACK:
					case Native.SB.THUMBPOSITION:
						num = this.GetScrollInfo(bar).nTrackPos;
						break;

					case Native.SB.TOP:
						num = 0;
						break;

					case Native.SB.BOTTOM:
						num = maximum;
						break;
				}

				return num;
			}
		}

		private void WmVScroll(ref System.Windows.Forms.Message m)
		{
			int ret = DoWmScroll(ref m, scrollPos.Y, scrollSize.Height, Native.SB.VERT);
			if (ret >= 0)
			{
				this.SetScrollPos(new Point(scrollPos.X, ret));
			}
		}

		protected override void WndProc(ref System.Windows.Forms.Message m)
		{
			switch (m.Msg)
			{
				case 0x114:
					this.WmHScroll(ref m);
					return;

				case 0x115:
					this.WmVScroll(ref m);
					return;
			}
			base.WndProc(ref m);
		}

		private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
		{
			showTimeMenuItem.Checked = ShowTime;
		}

		protected override void OnLayout(LayoutEventArgs levent)
		{
			this.UpdateScrollBars(true, true);
			base.OnLayout(levent);
		}

		protected override CreateParams CreateParams
		{
			get
			{
				CreateParams createParams = base.CreateParams;
				createParams.Style |= 0x100000; // horz scroll
				createParams.Style |= 0x200000; // vert scroll
				return createParams;
			}
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

		protected virtual void OnRefresh()
		{
			if (ManualRefresh != null)
				ManualRefresh(this, EventArgs.Empty);
		}

		protected virtual void OnCurrentMessageChanged()
		{
			if (CurrentMessageChanged != null)
				CurrentMessageChanged(this, EventArgs.Empty);
			if (GetPropertiesForm() != null)
				GetPropertiesForm().UpdateView(focused.Message);
		}

		#region IMessagePropertiesFormHost Members

		public IUINavigationHandler UINavigationHandler
		{
			get { return host.UINavigationHandler; }
		}

		public bool BookmarksSupported
		{
			get { return host.Bookmarks != null; }
		}

		public void ToggleBookmark(MessageBase line)
		{
			if (host.Bookmarks == null)
				return;
			host.Bookmarks.ToggleBookmark(line);
			InternalUpdate();
		}

		public void FindBegin(FrameEnd end)
		{
			GoToParentFrame();
		}

		public void FindEnd(FrameBegin end)
		{
			GoToEndOfFrame();
		}

		public void ShowLine(IBookmark bmk)
		{
			SelectMessageAt(bmk);
		}

		public void Next()
		{
			MoveSelection(focused.DisplayPosition + 1, false, true);
		}

		public void Prev()
		{
			MoveSelection(focused.DisplayPosition - 1, true, true);
		}


		#endregion

		class Shifter : IDisposable
		{
			bool active;
			IMessagesCollection collection;
			LogViewerControl control;
			ShiftPermissions shiftPerm;

			public Shifter(
				LogViewerControl control, bool displayMode, 
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
				if (!control.host.IsShiftableDown)
					return 0;

				int lastHash = 0;
				int lastIndex = -1;

				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
				{
					lastHash = l.Message.GetHashCode();
					lastIndex = l.Index;
					break;
				}

				if (lastIndex < 0)
					return 0;

				EnsureActive();

				control.host.ShiftDown();
				control.InternalUpdate();

				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
				{
					if (lastHash == l.Message.GetHashCode())
						return 0;
					break;
				}

				foreach (IndexedMessage l in collection.Forward(0, int.MaxValue))
					if (l.Message.GetHashCode() == lastHash)
						return lastIndex - l.Index;

				return 0;
			}

			public int ShiftUp()
			{
				if (!control.host.IsShiftableUp)
					return 0;

				int firstHash = 0;
				int firstIndex = -1;

				foreach (IndexedMessage l in collection.Forward(0, 1))
				{
					firstHash = l.Message.GetHashCode();
					firstIndex = l.Index;
					break;
				}

				if (firstIndex < 0)
					return 0;

				EnsureActive();

				control.host.ShiftUp();
				control.InternalUpdate();

				foreach (IndexedMessage l in collection.Forward(0, 1))
				{
					if (firstHash == l.Message.GetHashCode())
						return 0;
					break;
				}

				foreach (IndexedMessage l in collection.Reverse(int.MaxValue, -1))
					if (l.Message.GetHashCode() == firstHash)
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

		struct ShiftPermissions
		{
			public readonly bool AllowUp;
			public readonly bool AllowDown;
			public ShiftPermissions(bool allowUp, bool allowDown)
			{
				this.AllowUp = allowUp;
				this.AllowDown = allowDown;
			}
		};

		ShiftPermissions GetShiftPermissions()
		{
			return new ShiftPermissions(focused.DisplayPosition == 0, focused.DisplayPosition == (visibleCount-1));
		}

		class MergedMessagesCollection : IMessagesCollection
		{
			LogViewerControl control;
			bool displayMessagesMode;

			public MergedMessagesCollection(LogViewerControl control, bool displayMessagesMode)
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
						yield return new IndexedMessage(begin, displayMessagesMode ? entry.DisplayMsg : entry.Msg);
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
						yield return new IndexedMessage(begin, displayMessagesMode ? entry.DisplayMsg : entry.Msg);
					}
				}
			}
		};

		MessagePropertiesForm GetPropertiesForm()
		{
			if (propertiesForm != null)
				if (propertiesForm.IsDisposed)
					propertiesForm = null;
			return propertiesForm;
		}

		ILogViewerControlHost host;
		Source tracer = Source.EmptyTracer;

		MergedMessagesCollection loadedMessagesCollection;
		MergedMessagesCollection displayMessagesCollection;

		struct MergedMessagesEntry
		{
			public MessageBase Msg;
			public MessageBase DisplayMsg;
		};
		List<MergedMessagesEntry> mergedMessages = new List<MergedMessagesEntry>();
		int mergedMessagesVersion = 0;

		DrawContext drawContext = new DrawContext();
		int visibleCount;
		int selectedCount;
		struct FocusedMessageInfo
		{
			public MessageBase Message;
			public int DisplayPosition;
			public HighlightRange Highligt;
		};
		FocusedMessageInfo focused;
		MessagePropertiesForm propertiesForm;
	}
}
