using System;
using System.Collections.Generic;
using System.Linq;

namespace LogJoint.NLog
{
	public class ImportLog
	{
		public enum MessageType
		{
			Invalid = 0,
			NoDateTimeFound,
			DateTimeCannotBeParsed,
			NoTimeParsed,
			NothingToMatch,
			FirstRegexIsNotSpecific,
			ImportantFieldIsConditional,
			RendererUsageReport,
			RendererIgnored,
			UnknownRenderer,
			BadLayout,
		};

		public enum MessageSeverity { Info, Warn, Error };

		public class Message
		{
			public MessageType Type { get; internal set; }
			public MessageSeverity Severity { get; internal set; }
			public string LayoutId { get; internal set; }
			public class Fragment
			{
				public string Value { get; internal set; }
			};
			public class LayoutSliceLink : Fragment
			{
				public int LayoutSliceStart { get; internal set; }
				public int LayoutSliceEnd { get; internal set; }
			};
			public IEnumerable<Fragment> Fragments { get { return fragments; } }

			public override string ToString()
			{
				return Type.ToString() + ": " + fragments.Select(f => f.Value).Aggregate((ret, frag) => ret + " " + frag);
			}

			internal Message AddText(string txt)
			{
				fragments.Add(new Message.Fragment() { Value = txt });
				return this;
			}

			internal Message AddTextFmt(string fmt, params object[] par)
			{
				return AddText(string.Format(fmt, par));
			}

			internal Message AddLayoutSliceLink(string linkName, int? sliceStart, int? sliceEnd)
			{
				if (sliceStart == null || sliceEnd == null)
					return AddText(linkName);
				fragments.Add(new Message.LayoutSliceLink()
				{
					Value = linkName,
					LayoutSliceStart = sliceStart.Value,
					LayoutSliceEnd = sliceEnd.Value,
				});
				return this;
			}

			internal Message AddCustom(Action<Message> callback)
			{
				callback(this);
				return this;
			}

			internal Message SetLayoutId(string id)
			{
				LayoutId = id;
				return this;
			}

			internal List<Fragment> fragments = new List<Fragment>();
		}

		public IEnumerable<Message> Messages { get { return messages; } }

		public bool HasErrors { get { return messages.Any(m => m.Severity == MessageSeverity.Error); } }
		public bool HasWarnings { get { return messages.Any(m => m.Severity == MessageSeverity.Warn); } }

		public void Clear()
		{ 
			messages.Clear(); 
			currentLayoutId = null;
		}

		public override string ToString()
		{
			return messages.Select(m => m.ToString()).Aggregate((ret, s) => ret + " | " + s);
		}

		internal Message AddMessage(MessageType type, MessageSeverity sev)
		{
			var msg = new Message() { Type = type, Severity = sev, LayoutId = currentLayoutId };
			messages.Add(msg);
			return msg;
		}

		internal void FailIfThereIsError()
		{
			if (messages.Any(m => m.Severity == MessageSeverity.Error))
				throw new ImportErrorDetectedException("Cannot import because of error", this);
		}

		internal void StartHandlingLayout(string layoutId)
		{
			if (currentLayoutId != null)
				throw new InvalidOperationException("already handling layout " + currentLayoutId);
			currentLayoutId = layoutId;
		}

		internal void StopHandlingLayout()
		{
			currentLayoutId = null;
		}

		List<Message> messages = new List<Message>();
		string currentLayoutId;
	};
}
