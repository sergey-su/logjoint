using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;

namespace LogJoint
{
	internal class Filter : IDisposable, IFilter
	{
		public Filter(FilterAction type, string initialName, bool enabled, string template, bool wholeWord, bool regExp, bool matchCase, IFiltersFactory factory)
		{
			if (initialName == null)
				throw new ArgumentNullException("initialName");

			this.factory = factory;
			this.target = factory.CreateFilterTarget();
			this.initialName = initialName;
			this.enabled = enabled;
			this.action = type;
			this.template = template;
			this.wholeWord = wholeWord;
			this.regexp = regExp;
			this.matchCase = matchCase;

			InvalidateRegex();
			InvalidateName();
		}

		IFiltersFactory IFilter.Factory { get { return factory; } }
		FilterAction IFilter.Action
		{
			get
			{
				CheckDisposed();
				return action; 
			}
			set 
			{
				CheckDisposed();
				if (action == value)
					return;
				action = value; 
				OnChange(true, false); 
				InvalidateDefaultAction(); 
			}
		}
		string IFilter.Name
		{
			get 
			{
				CheckDisposed();
				InternalInsureName();
				return name; 
			}
		}
		string IFilter.InitialName { get { return initialName; } }
		void IFilter.SetUserDefinedName(string value)
		{
			CheckDisposed();
			InternalInsureName();
			if (name == value)
				return;
			if (string.IsNullOrEmpty(value))
				value = null;
			userDefinedName = value;
			InvalidateName();
			OnChange(false, false);
		}
		bool IFilter.Enabled
		{
			get 
			{
				CheckDisposed();
				return enabled; 
			}
			set 
			{
				CheckDisposed();
				if (enabled == value)
					return;
				enabled = value; 
				OnChange(true, false); 
				InvalidateDefaultAction();
			}
		}

		string IFilter.Template
		{
			get 
			{
				CheckDisposed();
				return template; 
			}
			set 
			{
				CheckDisposed();
				if (template == value)
					return;
				template = value; 
				InvalidateRegex();
				InvalidateName();
				OnChange(true, true); 
			}
		}
		bool IFilter.WholeWord
		{
			get 
			{
				CheckDisposed();
				return wholeWord; 
			}
			set 
			{
				CheckDisposed();
				if (wholeWord == value)
					return;
				wholeWord = value;
				InvalidateName();
				OnChange(true, true); 
			}
		}
		bool IFilter.Regexp
		{
			get 
			{
				CheckDisposed();
				return regexp; 
			}
			set 
			{
				CheckDisposed();
				if (regexp == value)
					return;
				regexp = value; 
				InvalidateRegex();
				InvalidateName();
				OnChange(true, true); 
			}
		}
		bool IFilter.MatchCase
		{
			get 
			{
				CheckDisposed();
				return matchCase; 
			}
			set 
			{
				CheckDisposed();
				if (matchCase == value)
					return;
				matchCase = value; 
				InvalidateRegex();
				InvalidateName();
				OnChange(true, true); 
			}
		}
		MessageBase.MessageFlag IFilter.Types
		{
			get 
			{
				CheckDisposed();
				return typesToApplyFilterTo; 
			}
			set
			{
				CheckDisposed();
				if (value == typesToApplyFilterTo)
					return;
				typesToApplyFilterTo = value;
				InvalidateName();
				OnChange(true, true); 
			}
		}

		bool IFilter.MatchFrameContent
		{
			get
			{
				CheckDisposed();
				return matchFrameContent;
			}
			set
			{
				CheckDisposed();
				if (value == matchFrameContent)
					return;
				matchFrameContent = value;
				InvalidateName();
				OnChange(true, false);
			}
		}

		IFilterTarget IFilter.Target
		{
			get 
			{
				CheckDisposed();
				return target; 
			}
			set 
			{
				CheckDisposed();
				if (value == null)
					throw new ArgumentNullException();
				target = value;
				InvalidateName();
				OnChange(true, true); 
			}
		}

		IFiltersList IFilter.Owner { get { return owner; } }

		int IFilter.Counter
		{
			get { CheckDisposed(); return counter; }
		}

		IFilter IFilter.Clone(string newFilterInitialName)
		{
			IFilter ret = factory.CreateFilter(action, newFilterInitialName, enabled, template, wholeWord, regexp, matchCase);
			ret.Target = target; // FilterTarget is immutable. Safe to refer to the same object.
			ret.Types = typesToApplyFilterTo;
			ret.MatchFrameContent = matchFrameContent;
			return ret;
		}

		bool IFilter.IsDisposed
		{
			get { return isDisposed; }
		}

		bool IFilter.Match(MessageBase message, bool matchRawMessages)
		{
			CheckDisposed();
			InternalInsureRegex();

			if (!MatchText(message, matchRawMessages))
				return false;

			if (!target.Match(message))
				return false;

			if (!MatchTypes(message))
				return false;

			return true;
		}


		void IFilter.SetOwner(IFiltersList newOwner)
		{
			CheckDisposed();
			if (newOwner != null && owner != null)
				throw new InvalidOperationException("Filter can not be attached to FiltersList: already attached to another list");
			owner = newOwner;
		}

		void IFilter.IncrementCounter()
		{
			counter++;
		}

		void IFilter.ResetCounter()
		{
			counter = 0;
		}


		void IDisposable.Dispose()
		{
			if (isDisposed)
				return;
			owner = null;
			isDisposed = true;
		}


		#region Implementation

		void CheckDisposed()
		{
			if (isDisposed)
				throw new ObjectDisposedException(this.ToString());
		}

		bool MatchTypes(MessageBase msg)
		{
			MessageBase.MessageFlag typeAndContentType = msg.Flags & (MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask);
			return (typeAndContentType & typesToApplyFilterTo) == typeAndContentType;
		}

		bool MatchText(MessageBase msg, bool matchRawMessages)
		{
			if (string.IsNullOrEmpty(template))
				return true;

			// matched string position
			int matchBegin = 0; // index of the first matched char
			int matchEnd = 0; // index of the char following after the last matched one

			StringSlice text = matchRawMessages ? msg.RawText : msg.Text;

			int textPos = 0;
			if (this.re != null)
			{
				if (!this.re.Match(text, textPos, ref reMatch))
					return false;
				matchBegin = reMatch.Index;
				matchEnd = matchBegin + reMatch.Length;
			}
			else
			{
				int i = text.IndexOf(this.template, textPos, 
					this.matchCase ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase);
				if (i < 0)
					return false;
				matchBegin = i;
				matchEnd = matchBegin + this.template.Length;
			}

			if (this.wholeWord)
			{
				if (!IsWholeWord(text, matchBegin, matchEnd))
					return false;
			}

			return true;
		}

		static bool IsWholeWord(StringSlice text, int matchBegin, int matchEnd)
		{
			if (matchBegin > 0)
				if (StringUtils.IsWordChar(text[matchBegin - 1]))
					return false;
			if (matchEnd < text.Length - 1)
				if (StringUtils.IsWordChar(text[matchEnd]))
					return false;
			return true;
		}

		void InvalidateRegex()
		{
			this.regexInvalidated = true;
			this.re = null;
			this.reMatch = null;
		}

		void InvalidateName()
		{
			this.nameInvalidated = true;
			this.name = null;
		}

		void InvalidateDefaultAction()
		{
			if (owner != null)
				owner.InvalidateDefaultAction();
		}

		void InternalUpdateRegex()
		{
			if (regexp)
			{
				ReOptions reOpts = ReOptions.None;
				if (!matchCase)
					reOpts |= ReOptions.IgnoreCase;
				re = RegexFactory.Instance.Create(template, reOpts);
				reMatch = null;
			}
		}

		void InternalUpdateName()
		{
			if (userDefinedName != null)
			{
				name = userDefinedName;
				return;
			}
			List<string> templateIndependentModifiers = new List<string>();
			GetTemplateIndependentModifiers(templateIndependentModifiers);
			if (!string.IsNullOrEmpty(template))
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(template);
				List<string> modifiers = new List<string>();
				GetTemplateDependentModifiers(modifiers);
				modifiers.AddRange(templateIndependentModifiers);
				ConcatModifiers(builder, modifiers);
				name = builder.ToString();
			}
			else if (templateIndependentModifiers.Count > 0)
			{
				StringBuilder builder = new StringBuilder();
				builder.Append("<any text>");
				ConcatModifiers(builder, templateIndependentModifiers);
				name = builder.ToString();
			}
			else
			{
				name = initialName;
			}
		}

		static void ConcatModifiers(StringBuilder ret, List<string> modifiers)
		{
			if (modifiers.Count > 0)
			{
				ret.Append(" (");
				for (int i = 0; i < modifiers.Count; ++i)
				{
					if (i > 0)
						ret.Append(", ");
					ret.Append(modifiers[i]);
				}
				ret.Append(")");
			}
		}

		void GetTemplateDependentModifiers(List<string> modifiers)
		{
			if (matchCase)
				modifiers.Add("match case");
			if (wholeWord)
				modifiers.Add("whole word");
			if (regexp)
				modifiers.Add("regexp");
		}

		void GetTemplateIndependentModifiers(List<string> modifiers)
		{
			if (this.typesToApplyFilterTo == 0)
			{
				modifiers.Add("no types to match!");
				return;
			}
			MessageBase.MessageFlag contentTypes = this.typesToApplyFilterTo & MessageBase.MessageFlag.ContentTypeMask;
			if (contentTypes != MessageBase.MessageFlag.ContentTypeMask)
			{
				if ((contentTypes & MessageBase.MessageFlag.Info) != 0)
					modifiers.Add("infos");
				if ((contentTypes & MessageBase.MessageFlag.Warning) != 0)
					modifiers.Add("warns");
				if ((contentTypes & MessageBase.MessageFlag.Error) != 0)
					modifiers.Add("errs");
			}
			MessageBase.MessageFlag types = this.typesToApplyFilterTo & MessageBase.MessageFlag.TypeMask;
			if (types != MessageBase.MessageFlag.TypeMask)
			{
				if ((types & MessageBase.MessageFlag.StartFrame) == 0 && (types & MessageBase.MessageFlag.EndFrame) == 0)
					modifiers.Add("no frames");
			}
		}

		void InternalInsureRegex()
		{
			CheckDisposed();
			if (!regexInvalidated)
				return;
			InternalUpdateRegex();
			regexInvalidated = false;
		}

		void InternalInsureName()
		{
			CheckDisposed();
			if (!nameInvalidated)
				return;
			InternalUpdateName();
			nameInvalidated = false;
		}

		protected void OnChange(bool changeAffectsFilterResult, bool changeAffectsPreprocessingResult)
		{
			if (owner != null)
				owner.FireOnPropertiesChanged(this, changeAffectsFilterResult, changeAffectsPreprocessingResult);
		}

		#endregion

		#region Members

		private readonly IFiltersFactory factory;

		private bool isDisposed;
		private IFiltersList owner;
		private readonly string initialName;
		private string userDefinedName;

		private FilterAction action;
		private bool enabled;

		private string template;
		private bool wholeWord;
		private bool regexp;
		private bool matchCase;

		private bool regexInvalidated;
		private IRegex re;
		private IMatch reMatch;
		private bool nameInvalidated;
		private string name;

		private IFilterTarget target;
		private int counter;
		private MessageBase.MessageFlag typesToApplyFilterTo = MessageBase.MessageFlag.TypeMask | MessageBase.MessageFlag.ContentTypeMask;
		private bool matchFrameContent = true;

		#endregion
	};
}
