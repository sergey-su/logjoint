using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.TagsList
{
	public interface IPresenter
	{
		void SetIsSingleLine (bool value);
		void Edit(string focusedTag = null);
	};

	public interface IView
	{
		void SetViewModel(IViewModel viewModel);

		IDialogView CreateDialog(
			IDialogViewModel dialogViewModel,
			IEnumerable<string> tags,
			string initiallyFocusedTag
		);
	};

	public interface IViewModel
	{
		IChangeNotification ChangeNotification { get; }
		(string, int, int) EditLinkValue { get; }
		bool IsSingleLine { get; }

		void OnEditLinkClicked();
	};

	public interface IDialogViewModel
	{
		ISet<string> SelectedTags { get; }
		string Formula { get; }
		/// <summary>
		/// When true, formula input should be editable and tags list disabled.
		/// When false, formula input is read-only, tags list is enabled.
		/// </summary>
		bool IsEditingFormula { get; }
		/// <summary>
		/// Tuple with user-friendly error description and message severity.
		/// If severity is error, formula confirmation button should be disabled as well as dialog confirmation button.
		/// </summary>
		(string, MessageSeverity) FormulaStatus { get; }

		(ImmutableArray<string>, int?) FormulaSuggesions { get; }

		(string, MessageSeverity) TagsListStatus { get; }

		void OnUseTagClicked(string tag);
		void OnUnuseTagClicked(string tag);
		void OnUseAllClicked();
		void OnUnuseAllClicked();
		void OnEditFormulaClicked();
		void OnStopEditingFormulaClicked();
		void OnFormulaChange(string value);
		bool OnFormulaKeyPressed(KeyCode key);
		void OnSuggestionClicked(int idx);
		void OnFormulaLinkClicked(string linkData);
		void OnTagsStatusLinkClicked(string linkData);
		void OnConfirmDialog();
		void OnCancelDialog();
	};

	public enum KeyCode
	{
		None,
		Enter,
		Up,
		Down
	};

	public enum MessageSeverity
	{
		None,
		Error,
		Warning
	};

	public interface IDialogView
	{
		void Open();
		void Close();
		int FormulaCursorPosition { get; set; }
		void OpenFormulaTab();
	};
}
