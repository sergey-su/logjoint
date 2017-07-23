using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;

namespace LogJoint
{
	internal class Filter : IDisposable, IFilter
	{
		public Filter(FilterAction action, string initialName, bool enabled,
			Search.Options options, IFiltersFactory factory)
		{
			this.factory = factory;

			this.initialName = initialName ?? throw new ArgumentNullException(nameof(initialName));
			this.enabled = enabled;
			this.action = action;

			this.options = options;

			// Filters ignores following flags passed.
			// Actually used values are provided later when filters are appied.
			this.options.ReverseSearch = false;
			this.options.SearchInRawText = false;

			InvalidateName();
		}

		public Filter(XElement e, IFiltersFactory factory)
		{
			this.factory = factory;

			LoadInternal(e);
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

		Search.Options IFilter.Options
		{
			get
			{
				CheckDisposed();
				return options;
			}
			set
			{
				CheckDisposed();

				this.options = value;

				InvalidateName();

				OnChange(true, true);
			}
		}

		IFiltersList IFilter.Owner { get { return owner; } }

		IFilter IFilter.Clone(string newFilterInitialName)
		{
			IFilter ret = factory.CreateFilter(action, newFilterInitialName, enabled, options);
			return ret;
		}

		bool IFilter.IsDisposed
		{
			get { return isDisposed; }
		}

		IFilterBulkProcessing IFilter.StartBulkProcessing(bool matchRawMessages, bool reverseMatchDirection)
		{
			CheckDisposed();
			var tmp = options;
			tmp.SearchInRawText = matchRawMessages;
			tmp.ReverseSearch = reverseMatchDirection;
			return new BulkProcessing()
			{
				searchState = tmp.BeginSearch()
			};
		}

		void IFilter.Save(XElement e)
		{
			SaveInternal(e);
		}

		void IFilter.SetOwner(IFiltersList newOwner)
		{
			CheckDisposed();
			if (newOwner != null && owner != null)
				throw new InvalidOperationException("Filter can not be attached to FiltersList: already attached to another list");
			owner = newOwner;
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

		void InternalUpdateName()
		{
			if (userDefinedName != null)
			{
				name = userDefinedName;
				return;
			}
			List<string> templateIndependentModifiers = new List<string>();
			GetTemplateIndependentModifiers(templateIndependentModifiers);
			if (!string.IsNullOrEmpty(options.Template))
			{
				StringBuilder builder = new StringBuilder();
				builder.Append(options.Template);
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
			if (options.MatchCase)
				modifiers.Add("match case");
			if (options.WholeWord)
				modifiers.Add("whole word");
			if (options.Regexp)
				modifiers.Add("regexp");
		}

		void GetTemplateIndependentModifiers(List<string> modifiers)
		{
			if (options.TypesToLookFor == 0)
			{
				return;
			}
			MessageFlag contentTypes = options.TypesToLookFor & MessageFlag.ContentTypeMask;
			if (contentTypes != MessageFlag.ContentTypeMask)
			{
				if ((contentTypes & MessageFlag.Info) != 0)
					modifiers.Add("infos");
				if ((contentTypes & MessageFlag.Warning) != 0)
					modifiers.Add("warns");
				if ((contentTypes & MessageFlag.Error) != 0)
					modifiers.Add("errs");
			}
			MessageFlag types = options.TypesToLookFor & MessageFlag.TypeMask;
			if (types != MessageFlag.TypeMask)
			{
				if ((types & MessageFlag.StartFrame) == 0 && (types & MessageFlag.EndFrame) == 0)
					modifiers.Add("no frames");
			}
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

		void SaveInternal(XElement e)
		{
			options.Save(e);
			if (!enabled)
				e.SetAttributeValue("enabled", "0");
			e.SetAttributeValue("action", (int)action);
			e.SetAttributeValue("initial-name", initialName);
			if (userDefinedName != null)
				e.SetAttributeValue("given-name", userDefinedName);
		}

		void LoadInternal(XElement e)
		{
			options.Load(e);	
			enabled = e.SafeIntValue("enabled", 1) != 0;
			action = (FilterAction)e.SafeIntValue("action", (int)FilterAction.Include);
			initialName = e.AttributeValue("initial-name", defaultValue: "");
			userDefinedName = e.AttributeValue("given-name", defaultValue: null);
		}

		#endregion

		#region Members

		private readonly IFiltersFactory factory;

		private bool isDisposed;
		private IFiltersList owner;
		private string initialName;
		private string userDefinedName;

		private FilterAction action;
		private bool enabled;
		private Search.Options options;

		private bool nameInvalidated;
		private string name;

		#endregion

		class BulkProcessing : IFilterBulkProcessing
		{
			internal Search.SearchState searchState;

			void IDisposable.Dispose()
			{
			}

			Search.MatchedTextRange? IFilterBulkProcessing.Match(IMessage message, int? startFromChar)
			{
				return Search.SearchInMessageText(message, searchState, startFromChar);
			}
		};
	};
}
