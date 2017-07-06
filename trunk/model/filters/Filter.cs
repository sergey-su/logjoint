using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;

namespace LogJoint
{
	internal class Filter : IDisposable, IFilter
	{
		public Filter(FilterAction type, string initialName, bool enabled,
			Search.Options options, IFiltersFactory factory)
		{
			if (initialName == null)
				throw new ArgumentNullException("initialName");

			this.factory = factory;

			this.initialName = initialName;
			this.enabled = enabled;
			this.action = type;

			this.options = options;

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

		IFilterBulkProcessing IFilter.StartBulkProcessing(bool matchRawMessages)
		{
			CheckDisposed();
			return new BulkProcessing()
			{
				matchRawMessages = matchRawMessages,
				searchState = options.BeginSearch()
			};
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

		#endregion

		#region Members

		private readonly IFiltersFactory factory;

		private bool isDisposed;
		private IFiltersList owner;
		private readonly string initialName;
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
			internal bool matchRawMessages;

			void IDisposable.Dispose()
			{
			}

			bool IFilterBulkProcessing.Match(IMessage message)
			{
				return Search.SearchInMessageText(message, searchState, matchRawMessages) != null;
			}
		};
	};
}
