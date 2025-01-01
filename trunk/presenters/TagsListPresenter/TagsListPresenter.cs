using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using LogJoint.UI.Presenters.Postprocessing.Common;

namespace LogJoint.UI.Presenters.TagsList
{
    public class TagsListPresenter : IPresenter, IViewModel, IDialogViewModel
    {
        readonly IPostprocessorTags model;
        readonly IView view;
        readonly IChangeNotification changeNotification;
        readonly IAlertPopup alerts;
        readonly Func<(string, int, int)> getEditLinkValue;
        bool isSingleLine = true;

        IDialogView currentDialog;
        TagsPredicate predicate;
        string formula;
        readonly Func<(string, MessageSeverity)> getFormulaStatus;
        readonly Func<(ImmutableArray<string>, int?)> getFormulaSuggesions;
        string selectedSuggestion;
        readonly Func<ISet<string>> getSelectedTags;
        readonly Func<ImmutableArray<string>> getAvailableTagsSorted;
        readonly Func<(string, MessageSeverity)> getTagsListStatus;

        public TagsListPresenter(
            IPostprocessorTags model,
            IView view,
            IChangeNotification changeNotification,
            IAlertPopup alerts
        )
        {
            this.model = model;
            this.view = view;
            this.changeNotification = changeNotification;
            this.alerts = alerts;

            this.getAvailableTagsSorted = Selectors.Create(
                () => model.AvailableTags,
                availableTags => ImmutableArray.CreateRange(availableTags.OrderBy(tag => tag))
            );

            this.getEditLinkValue = Selectors.Create(
                () => model.AvailableTags,
                () => model.TagsPredicate.UsedTags,
                (availableTags, usedTags) =>
                {
                    var tagsString = string.Join(", ", usedTags.Select(tag => tag.Item1));
                    var clickablePrefix = "tags";
                    var selectedCount = usedTags.Count;
                    if (availableTags.Count > selectedCount)
                        clickablePrefix += string.Format(" ({0} out of {1})", selectedCount, availableTags.Count);
                    return (
                        string.Format("{0}: {1}", clickablePrefix, tagsString == "" ? "<none selected>" : tagsString),
                        0,
                        clickablePrefix.Length
                    );
                }
            );

            this.getFormulaStatus = Selectors.Create(
                () => formula,
                () => model.AvailableTags,
                (value, availableTags) =>
                {
                    if (value == null)
                        return ("Show activities matching this formula", MessageSeverity.None);
                    TagsPredicate tmp;
                    try
                    {
                        tmp = TagsPredicate.Parse(value);
                    }
                    catch (TagsPredicate.SyntaxError e)
                    {
                        return ($"{e.Message} *{e.Position} where?*", MessageSeverity.Error);
                    }
                    catch
                    {
                        return ("Failed to parse", MessageSeverity.Error);
                    }
                    var (unavailableTag, unavailableTagPos) = tmp.UsedTags.Where(usedTag => !availableTags.Contains(usedTag.Item1)).FirstOrDefault();
                    if (unavailableTag != null)
                    {
                        return ($"Unknown tag: *{unavailableTagPos} {unavailableTag}*", MessageSeverity.Warning);
                    }
                    return ("Editing: no errors in formula", MessageSeverity.None);
                }
            );

            this.getTagsListStatus = Selectors.Create(
                () => formula,
                (editedFormula) =>
                {
                    var defaultStatus = ("Show activities having any of selected tags", MessageSeverity.None);
                    if (editedFormula != null)
                        return ("Selection unavailable. *goToFormula Go to formula*", MessageSeverity.None);
                    return defaultStatus;
                }
            );

            this.getFormulaSuggesions = Selectors.Create(
                () => formula,
                getAvailableTagsSorted,
                () => currentDialog?.FormulaCursorPosition,
                () => selectedSuggestion,
                (value, availableTags, cursorPos, selectedSuggestion) =>
                {
                    if (value != null && cursorPos != null)
                    {
                        var (formulaWord, formulaWordIndex) = GetFormulaWord(value, cursorPos.Value);
                        if (formulaWord != null)
                        {
                            var builder = ImmutableArray.CreateBuilder<string>();
                            builder.AddRange(availableTags.Where(tag =>
                                   tag.IndexOf(formulaWord, StringComparison.InvariantCultureIgnoreCase) >= 0
                                && string.Compare(tag, formulaWord, StringComparison.InvariantCultureIgnoreCase) != 0
                            ).Take(8));
                            int selectedIndex = selectedSuggestion != null ? builder.IndexOf(selectedSuggestion) : -1;
                            return (builder.ToImmutable(), selectedIndex < 0 ? new int?() : selectedIndex);
                        }
                    }
                    return (ImmutableArray<string>.Empty, new int?());
                }
            );

            this.getSelectedTags = Selectors.Create(
                () => predicate.UsedTags,
                usedTags => ImmutableHashSet.ToImmutableHashSet(usedTags.Select(tag => tag.Item1))
            );

            view?.SetViewModel(this);
        }

        void IPresenter.Edit(string focusedTag)
        {
            this.Edit(focusedTag);
        }

        void IPresenter.SetIsSingleLine(bool value)
        {
            this.isSingleLine = value;
            changeNotification.Post();
        }

        ISet<string> IDialogViewModel.SelectedTags => getSelectedTags();

        bool IDialogViewModel.IsEditingFormula => IsEditingFormula == true;

        string IDialogViewModel.Formula => formula ?? predicate.ToString();

        (string, MessageSeverity) IDialogViewModel.FormulaStatus => getFormulaStatus();

        (string, MessageSeverity) IDialogViewModel.TagsListStatus => getTagsListStatus();

        (ImmutableArray<string>, int?) IDialogViewModel.FormulaSuggesions => getFormulaSuggesions();

        IChangeNotification IViewModel.ChangeNotification => changeNotification;

        void IViewModel.OnEditLinkClicked()
        {
            Edit(null);
        }

        (string, int, int) IViewModel.EditLinkValue => getEditLinkValue();

        bool IViewModel.IsSingleLine => isSingleLine;

        void IDialogViewModel.OnUseTagClicked(string tag)
        {
            if (IsEditingFormula == false)
            {
                predicate = predicate.Add(tag);
                changeNotification.Post();
            }
        }

        void IDialogViewModel.OnUnuseTagClicked(string tag)
        {
            if (IsEditingFormula == false)
            {
                predicate = predicate.Remove(tag);
                changeNotification.Post();
            }
        }

        void IDialogViewModel.OnUseAllClicked()
        {
            if (IsEditingFormula == false)
            {
                predicate = TagsPredicate.MakeMatchAnyPredicate(getAvailableTagsSorted());
                changeNotification.Post();
            }
        }

        void IDialogViewModel.OnUnuseAllClicked()
        {
            if (IsEditingFormula == false)
            {
                predicate = TagsPredicate.Empty;
                changeNotification.Post();
            }
        }

        void IDialogViewModel.OnEditFormulaClicked()
        {
            if (IsEditingFormula == false)
            {
                formula = predicate.ToString();
                changeNotification.Post();
            }
        }

        void IDialogViewModel.OnStopEditingFormulaClicked()
        {
            StopEditingFormula();
        }

        void IDialogViewModel.OnFormulaChange(string value)
        {
            if (IsEditingFormula == true)
            {
                formula = value;
                changeNotification.Post();
            }
        }

        bool IDialogViewModel.OnFormulaKeyPressed(KeyCode key)
        {
            if (IsEditingFormula != true)
                return false;
            if (key == KeyCode.Enter)
            {
                var (suggestions, selectedSuggestionIndex) = getFormulaSuggesions();
                if (selectedSuggestionIndex != null)
                {
                    UseSuggestion();
                }
                else if (!suggestions.IsEmpty)
                {
                    selectedSuggestion = suggestions.First();
                    changeNotification.Post();
                }
                else if (getFormulaStatus().Item2 != MessageSeverity.Error)
                {
                    StopEditingFormula();
                }
                return true;
            }
            else if (key == KeyCode.Down || key == KeyCode.Up)
            {
                var (suggestions, selectedSuggestionIndex) = getFormulaSuggesions();
                if (selectedSuggestionIndex == null)
                    selectedSuggestion = suggestions.FirstOrDefault();
                else if (!(key == KeyCode.Down && selectedSuggestionIndex == suggestions.Length - 1) && !(key == KeyCode.Up && selectedSuggestionIndex == 0))
                    selectedSuggestion = suggestions.ElementAtOrDefault((selectedSuggestionIndex.Value + (key == KeyCode.Down ? 1 : -1)));
                changeNotification.Post();
                return !suggestions.IsEmpty;
            }
            return false;
        }

        void IDialogViewModel.OnSuggestionClicked(int idx)
        {
            var (suggestions, selectedSuggestionIndex) = getFormulaSuggesions();
            if (selectedSuggestionIndex == idx)
            {
                UseSuggestion();
            }
            else
            {
                selectedSuggestion = suggestions[idx];
                changeNotification.Post();
            }
        }

        void IDialogViewModel.OnFormulaLinkClicked(string linkData)
        {
            if (int.TryParse(linkData ?? "", out int pos))
                currentDialog.FormulaCursorPosition = pos;
        }

        void IDialogViewModel.OnTagsStatusLinkClicked(string linkData)
        {
            if (linkData == "goToFormula")
            {
                currentDialog.OpenFormulaTab();
            }
        }

        void IDialogViewModel.OnConfirmDialog()
        {
            if (IsDialogOpen)
            {
                model.TagsPredicate = predicate;
                currentDialog.Close();
                currentDialog = null;
                formula = null;
                predicate = null;
            }
        }

        void IDialogViewModel.OnCancelDialog()
        {
            if (IsDialogOpen)
            {
                if (IsEditingFormula == true && alerts.ShowPopup("Tags", "Do you want to discard edited formula", AlertFlags.YesNoCancel | AlertFlags.QuestionIcon) != AlertFlags.Yes)
                    return;
                currentDialog.Close();
                currentDialog = null;
                formula = null;
                predicate = null;
                changeNotification.Post();
            }
        }

        private void Edit(string focusedTag)
        {
            if (!IsDialogOpen)
            {
                predicate = model.TagsPredicate;
                formula = null;
                currentDialog = view.CreateDialog(this, getAvailableTagsSorted(), focusedTag);
                currentDialog.Open();
            }
        }

        private bool IsDialogOpen => currentDialog != null;

        private bool? IsEditingFormula => IsDialogOpen ? formula != null : new bool?();

        private static (string, int?) GetFormulaWord(string formula, int cursorPos)
        {
            int b = cursorPos;
            while (b > 0 && !char.IsWhiteSpace(formula[b - 1]))
                --b;
            int e = cursorPos;
            while (e < formula.Length && !char.IsWhiteSpace(formula[e]))
                ++e;
            if (e == b)
                return (null, null);
            var word = formula.Substring(b, e - b);
            if (TagsPredicate.IsKeyword(word))
                return (null, null);
            return (word, b);
        }

        private void UseSuggestion()
        {
            var (word, wordIndex) = GetFormulaWord(formula, currentDialog.FormulaCursorPosition);
            if (word == null || selectedSuggestion == null)
                return;
            formula = $"{formula.Substring(0, wordIndex.Value)}{selectedSuggestion}{formula.Substring(wordIndex.Value + word.Length)}";
            changeNotification.Post();
            currentDialog.FormulaCursorPosition = wordIndex.Value + selectedSuggestion.Length;
        }

        private void StopEditingFormula()
        {
            if (IsEditingFormula == true && getFormulaStatus().Item2 != MessageSeverity.Error)
            {
                predicate = TagsPredicate.Parse(formula);
                formula = null;
                changeNotification.Post();
            }
        }
    }
}
