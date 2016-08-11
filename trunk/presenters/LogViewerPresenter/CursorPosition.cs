using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;

namespace LogJoint.UI.Presenters.LogViewer
{
	public struct CursorPosition
	{
		public IMessage Message;
		public IMessagesSource Source;
		public int DisplayIndex;
		public int TextLineIndex;
		public int LineCharIndex;

		public static int Compare(CursorPosition p1, CursorPosition p2)
		{
			if (p1.Message == null && p2.Message == null)
				return 0;
			if (p1.Message == null)
				return -1;
			if (p2.Message == null)
				return 1;
			int i;
			i = MessagesComparer.Compare(p1.Message, p2.Message);
			if (i != 0)
				return i;
			i = p1.TextLineIndex - p2.TextLineIndex;
			if (i != 0)
				return i;
			i = p1.LineCharIndex - p2.LineCharIndex;
			return i;
		}

		public static CursorPosition FromViewLine(ViewLine l, int charIndex)
		{
			return new CursorPosition()
			{
				Message = l.Message,
				DisplayIndex = l.LineIndex,
				TextLineIndex = l.TextLineIndex,
				LineCharIndex = charIndex
			};
		}
		public ViewLine ToDisplayLine()
		{ 
			return new ViewLine()
			{ 
				Message = Message,
				LineIndex = DisplayIndex,
				TextLineIndex = TextLineIndex
			}; 
		}
	};
};