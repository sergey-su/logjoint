using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;
using NSubstitute;
using System.Linq;
using System.Collections.Generic;
using LogJoint.UI.Presenters.TagsList;
using LogJoint.UI.Presenters.Postprocessing.Common;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.Tests.TagsListPresenterTests
{
	[TestFixture]
	public class TagsListPresenterTests
	{
		IPresenter presenter;
		IPostprocessorTags model;
		IViewModel viewModel;
		IDialogViewModel dialogViewModel;
		IView view;
		IDialogView dialogView;
		IAlertPopup alerts;
		IChangeNotification changeNotification;

		[SetUp] 
		public void BeforeEach()
		{
			model = Substitute.For<IPostprocessorTags>();
			view = Substitute.For<IView>();
			dialogView = Substitute.For<IDialogView>();
			alerts = Substitute.For<IAlertPopup>();
			view.When(v => v.SetViewModel(Arg.Any<IViewModel>())).Do(x => viewModel = x.Arg<IViewModel>());
			view.CreateDialog(Arg.Do<IDialogViewModel>(x => dialogViewModel = x), Arg.Any<IEnumerable<string>>(), Arg.Any<string>()).Returns(dialogView);
			changeNotification = Substitute.For<IChangeNotification>();
			model.AvailableTags.Returns(ImmutableHashSet.Create(new[] { "foo", "bar", "item-1", "item-2", "abd", "abc" }));
			model.TagsPredicate.Returns(TagsPredicate.Parse("bar OR item-1 AND NOT abc"));
			presenter = new TagsListPresenter(
				model,
				view,
				changeNotification,
				alerts
			);
		}

		[TestFixture]
		class Tests: TagsListPresenterTests
		{
			static IEnumerable<string> MakeTagsArgumentMatcher()
			{
				return Arg.Is<IEnumerable<string>>(tags => tags.SequenceEqual(new[] { "abc", "abd", "bar", "foo", "item-1", "item-2", }));
			}

			[Test]
			public void CanOpenDialogProgrammatically()
			{
				presenter.Edit();

				view.Received().CreateDialog(Arg.Any<IDialogViewModel>(), MakeTagsArgumentMatcher(), null);
				dialogView.Received().Open();
			}

			[Test]
			public void CanOpenDialogProgrammaticallyWithPreSelectedTagSpecified()
			{
				presenter.Edit("abd");

				view.Received().CreateDialog(Arg.Any<IDialogViewModel>(), MakeTagsArgumentMatcher(), "abd");
				dialogView.Received().Open();
			}

			[Test]
			public void CanOpenDialogByClick()
			{
				viewModel.OnEditLinkClicked();
				view.Received().CreateDialog(Arg.Any<IDialogViewModel>(), MakeTagsArgumentMatcher(), null);
				dialogView.Received().Open();
			}

			[Test]
			public void SingleLineFlagIsReflectedInViewModel()
			{
				Assert.IsTrue(viewModel.IsSingleLine);

				changeNotification.ClearReceivedCalls();
				presenter.SetIsSingleLine(false);
				Assert.IsFalse(viewModel.IsSingleLine);
				changeNotification.Received().Post();

				changeNotification.ClearReceivedCalls();
				presenter.SetIsSingleLine(true);
				Assert.IsTrue(viewModel.IsSingleLine);
				changeNotification.Received().Post();
			}
		};

		[TestFixture]
		class WhenDialogIsOpen : TagsListPresenterTests
		{
			[SetUp]
			public new void BeforeEach()
			{
				presenter.Edit();
			}

			[TestFixture]
			new class Tests : WhenDialogIsOpen
			{
				[Test]
				public void SelectedTagsAreRendered()
				{
					Assert.IsTrue(dialogViewModel.SelectedTags.SetEquals(new[] { "bar", "item-1", "abc" }));
					var (linkContent, severity) = dialogViewModel.TagsListStatus;
					Assert.AreEqual(0, LinkLabelUtils.ParseLinkLabelString(linkContent).Links.Count);
					Assert.AreEqual(MessageSeverity.None, severity);
				}

				[Test]
				public void FormulaRendered()
				{
					Assert.AreEqual("bar OR item-1 AND NOT abc", dialogViewModel.Formula);
					var (linkContent, severity) = dialogViewModel.FormulaStatus;
					Assert.AreEqual(0, LinkLabelUtils.ParseLinkLabelString(linkContent).Links.Count);
					Assert.AreEqual(MessageSeverity.None, severity);
					Assert.IsFalse(dialogViewModel.IsEditingFormula);
					var (suggesionts, selectedSuggestion) = dialogViewModel.FormulaSuggesions;
					Assert.AreEqual(0, suggesionts.Length);
					Assert.AreEqual(null, selectedSuggestion);
				}

				[Test]
				public void CanUseTag()
				{
					changeNotification.ClearReceivedCalls();
					dialogViewModel.OnUseTagClicked("foo");
					changeNotification.Received().Post();
					Assert.IsTrue(dialogViewModel.SelectedTags.SetEquals(new[] { "bar", "item-1", "abc", "foo" }));
					Assert.AreEqual("bar OR item-1 AND NOT abc OR foo", dialogViewModel.Formula);
				}

				[Test]
				public void CanUnuseTag()
				{
					changeNotification.ClearReceivedCalls();
					dialogViewModel.OnUnuseTagClicked("abc");
					changeNotification.Received().Post();
					Assert.IsTrue(dialogViewModel.SelectedTags.SetEquals(new[] { "bar", "item-1" }));
					Assert.AreEqual("bar OR item-1", dialogViewModel.Formula);
				}

				[Test]
				public void CanUseAllTags()
				{
					changeNotification.ClearReceivedCalls();
					dialogViewModel.OnUseAllClicked();
					changeNotification.Received().Post();
					Assert.IsTrue(dialogViewModel.SelectedTags.SetEquals(new[] { "foo", "bar", "item-1", "item-2", "abd", "abc" }));
					Assert.AreEqual("abc OR abd OR bar OR foo OR item-1 OR item-2", dialogViewModel.Formula);
				}

				[Test]
				public void CanUnuseAllTags()
				{
					changeNotification.ClearReceivedCalls();
					dialogViewModel.OnUnuseAllClicked();
					changeNotification.Received().Post();
					Assert.IsTrue(dialogViewModel.SelectedTags.SetEquals(new string[0]));
					Assert.AreEqual("", dialogViewModel.Formula);
				}

				[Test]
				public void CanConfirmDialogAndAcceptChanges()
				{
					dialogViewModel.OnUnuseTagClicked("abc");

					dialogViewModel.OnConfirmDialog();
					dialogView.Received().Close();
					model.Received().TagsPredicate = Arg.Is<TagsPredicate>(p => p.ToString() == "bar OR item-1");
				}

				[Test]
				public void CanDismissDialogAndDiscardChanges()
				{
					dialogViewModel.OnUnuseTagClicked("abc");

					dialogViewModel.OnCancelDialog();
					dialogView.Received().Close();
					model.DidNotReceive().TagsPredicate = Arg.Any<TagsPredicate>();
				}

				[TestFixture]
				class WhenStartedToEditFormula : WhenDialogIsOpen
				{
					[SetUp]
					public new void BeforeEach()
					{
						changeNotification.ClearReceivedCalls();
						dialogViewModel.OnEditFormulaClicked();
					}

					[TestFixture]
					new class Tests : WhenStartedToEditFormula
					{
						[Test]
						public void FormulaStatusIsRendered()
						{
							changeNotification.Received().Post();
							Assert.IsTrue(dialogViewModel.IsEditingFormula);
							Assert.AreEqual(dialogViewModel.FormulaStatus, ("Editing: no errors in formula", MessageSeverity.None));
						}

						[Test]
						public void TagsStatusHasClickableLinkThatLeadsToFormulaTab()
						{
							var (statusText, _) = dialogViewModel.TagsListStatus;
							var (_, _, linkData) = LinkLabelUtils.ParseLinkLabelString(statusText).Links.Single();
							dialogViewModel.OnTagsStatusLinkClicked(linkData);
							dialogView.Received().OpenFormulaTab();
						}

						[Test]
						public void CanChangeFormula()
						{
							changeNotification.ClearReceivedCalls();
							dialogViewModel.OnFormulaChange(" foo  OR bar");
							changeNotification.Received().Post();
							Assert.AreEqual(" foo  OR bar", dialogViewModel.Formula);
						}

						[Test]
						public void CanDismissDialogAndConfirmDismissal()
						{
							alerts.ShowPopup(null, null, AlertFlags.None).ReturnsForAnyArgs(AlertFlags.Yes);
							dialogViewModel.OnFormulaChange("foo");
							dialogViewModel.OnCancelDialog();
							alerts.ReceivedWithAnyArgs().ShowPopup(null, null, AlertFlags.None);
							dialogView.Received().Close();
							model.DidNotReceive().TagsPredicate = Arg.Any<TagsPredicate>();
						}

						[Test]
						public void CanDismissDialogAndCancelDismissal()
						{
							alerts.ShowPopup(null, null, AlertFlags.None).ReturnsForAnyArgs(AlertFlags.No);
							dialogViewModel.OnFormulaChange("foo");
							dialogViewModel.OnCancelDialog();
							alerts.ReceivedWithAnyArgs().ShowPopup(null, null, AlertFlags.None);
							dialogView.DidNotReceive().Close();
						}

						[Test]
						public void CanStopEditingFormula()
						{
							dialogViewModel.OnFormulaChange("foo");
							dialogViewModel.OnStopEditingFormulaClicked();
							Assert.AreEqual(false, dialogViewModel.IsEditingFormula);
							Assert.IsTrue(dialogViewModel.SelectedTags.SetEquals(new[] { "foo" }));
						}

						[TestFixture]
						class CanEnterInvalidTag : WhenStartedToEditFormula
						{
							[SetUp]
							public new void BeforeEach()
							{
								dialogView.FormulaCursorPosition.Returns(6);
								changeNotification.ClearReceivedCalls();
								dialogViewModel.OnFormulaChange("foo item");
							}

							[TestFixture]
							new class Tests : CanEnterInvalidTag
							{
								[Test]
								public void FormulaStatusIsWarningAndHasClickableLink()
								{
									var (statusText, severity) = dialogViewModel.FormulaStatus;
									Assert.AreEqual(MessageSeverity.Warning, severity);
									 var (_, _, linkData) = LinkLabelUtils.ParseLinkLabelString(statusText).Links.Single();
									dialogViewModel.OnFormulaLinkClicked(linkData);
									dialogView.Received().FormulaCursorPosition = 4;
								}

								[Test]
								public void SuggestionsListIsRendered()
								{
									var (suggestions, selectedSuggestion) = dialogViewModel.FormulaSuggesions;
									Assert.IsTrue(suggestions.SequenceEqual(new[] { "item-1", "item-2" }));
									Assert.AreEqual(null, selectedSuggestion);
								}

								[Test]
								public void CanPressEnterTwiceToAcceptTheSuggestion()
								{
									dialogViewModel.OnFormulaKeyPressed(KeyCode.Enter);
									var (_, selectedSuggestion) = dialogViewModel.FormulaSuggesions;
									Assert.AreEqual(0, selectedSuggestion);

									dialogViewModel.OnFormulaKeyPressed(KeyCode.Enter);
									var (suggestions, _) = dialogViewModel.FormulaSuggesions;
									Assert.AreEqual(0, suggestions.Length);

									Assert.AreEqual("foo item-1", dialogViewModel.Formula);
								}

								[Test]
								public void CanUseArrowsToSelectSuggestion()
								{
									dialogViewModel.OnFormulaKeyPressed(KeyCode.Down);
									Assert.AreEqual(0, dialogViewModel.FormulaSuggesions.Item2);

									dialogViewModel.OnFormulaKeyPressed(KeyCode.Up);
									Assert.AreEqual(0, dialogViewModel.FormulaSuggesions.Item2);

									dialogViewModel.OnFormulaKeyPressed(KeyCode.Down);
									Assert.AreEqual(1, dialogViewModel.FormulaSuggesions.Item2);

									dialogViewModel.OnFormulaKeyPressed(KeyCode.Down);
									Assert.AreEqual(1, dialogViewModel.FormulaSuggesions.Item2);

									dialogViewModel.OnFormulaKeyPressed(KeyCode.Up);
									Assert.AreEqual(0, dialogViewModel.FormulaSuggesions.Item2);

									dialogViewModel.OnFormulaKeyPressed(KeyCode.Down);
									Assert.AreEqual(1, dialogViewModel.FormulaSuggesions.Item2);

									dialogViewModel.OnFormulaKeyPressed(KeyCode.Enter);
									Assert.AreEqual("foo item-2", dialogViewModel.Formula);
								}


								[Test]
								public void CanClickToUseSuggestion()
								{
									dialogViewModel.OnSuggestionClicked(1);
									Assert.AreEqual(1, dialogViewModel.FormulaSuggesions.Item2);

									dialogViewModel.OnSuggestionClicked(1);
									Assert.AreEqual("foo item-2", dialogViewModel.Formula);
								}
							}
						}
					}
				}
			}
		}
	}
}
