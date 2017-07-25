using System.Collections.Generic;

namespace LogJoint.UI.Presenters.FilterDialog
{
	public interface IView
	{
		void SetEventsHandler(IViewEvents handler);
		void SetData(
			string title,
			KeyValuePair<string, ModelColor?>[] actionComboBoxOptions, 
			string[] typesOptions,
			DialogValues values
		);
		DialogValues GetData();
		void SetScopeItemChecked(int idx, bool checkedValue);
		void SetNameEditValue(string value);
		bool ShowDialog();
	};

	public struct DialogValues
	{
		public string NameEditValue;
		public bool EnabledCheckboxValue;
		public string TemplateEditValue;
		public bool MatchCaseCheckboxValue;
		public bool RegExpCheckBoxValue;
		public bool WholeWordCheckboxValue;
		public int ActionComboBoxValue;
		public List<KeyValuePair<ScopeItem, bool>> ScopeItems;
		public List<bool> TypesCheckboxesValues;
	};

	public interface IPresenter
	{
		bool ShowTheDialog(IFilter forFilter);
	};

	public abstract class ScopeItem
	{
		public int Indent { get; internal set; }
		public override abstract string ToString();
	};

	public interface IViewEvents
	{
		void OnScopeItemChecked(ScopeItem item, bool checkedValue);
		void OnCriteriaInputChanged();
	};
};