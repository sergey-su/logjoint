using System;
using LogJoint.UI.Presenters.QuickSearchTextBox;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;
using NSubstitute;
using System.Linq;
using System.Collections.Generic;

namespace LogJoint.UI.Presenters.Tests
{
	[TestFixture]
	public class QuickSearchTextBoxPresenterTests
	{
		IViewModel viewModel;
		IPresenter presenter;
		IView view;
		IChangeNotification changeNotification;
		const int nrOfSuggestions = 50;
		const int nrOfNonTextualSuggestions = 3;
		const int nrOfViewItems = nrOfSuggestions + 2; // +2 is category captions
		int onSearchNowRaised, onCurrentSuggestionChanged;

		[SetUp] 
		public void Init()
		{
			view = Substitute.For<IView>();
			view.When(v => v.SetViewModel(Arg.Any<IViewModel>())).Do(
				x => viewModel = x.Arg<IViewModel>());
			changeNotification = Substitute.For<IChangeNotification>();
			presenter = new Presenter(view, changeNotification);
			onSearchNowRaised = 0;
			presenter.OnSearchNow += (sender, e) => ++onSearchNowRaised;
			onCurrentSuggestionChanged = 0;
			presenter.OnCurrentSuggestionChanged += (sender, e) => ++onCurrentSuggestionChanged;
		}

		void SetSuggestionsEventHandler()
		{
			string etag = "test";
			presenter.SetSuggestionsHandler((sender, e) => 
			{
				if (e.Etag == etag)
					return;
				for (int i = 0; i < nrOfSuggestions - nrOfNonTextualSuggestions; ++i)
					e.AddItem(new SuggestionItem()
					{
						DisplayString = "test " + i.ToString(),
						SearchString = "search " + i.ToString(),
						LinkText = (i % 3 != 0) ? "link " + i.ToString() : null,
						Category = "cat1",
						Data = "cat1." + i.ToString()
					});
				for (int i = 0; i < nrOfNonTextualSuggestions; ++i)
					e.AddItem(new SuggestionItem()
					{
						DisplayString = "non-textual " + i.ToString(),
						LinkText = "edit",
						Category = "cat2",
						Data = "cat2." + i.ToString()
					});
				e.Etag = etag;
			});
		}

		[Test]
		public void EmptySuggestionsListIsNotShown()
		{
			presenter.SetSuggestionsHandler((sender, e) => { /* does not provide any suggestions */});

			viewModel.OnKeyDown(Key.ShowListShortcut);
			viewModel.OnKeyDown(Key.Down);

			// availablity must be false to avoid unnecessary creation of view's objects
			// for suggestions-less SearchTextBox-es
			Assert.That(viewModel.SuggestionsListAvailable, Is.False);
			Assert.That(viewModel.SuggestionsListVisibile, Is.False);
			Assert.That(viewModel.SuggestionsListItems, Is.Empty);
			Assert.That(viewModel.SelectedSuggestionsListItem, Is.Null);
		}

		[Test]
		public void ClickingDropdownButtonMakesInputBoxFocused()
		{
			viewModel.OnDropDownButtonClicked();

			view.Received().ReceiveInputFocus();
		}

		[Test]
		public void SuggestionsListIsShownWhenShortcutIsPressed()
		{
			SetSuggestionsEventHandler();

			viewModel.OnKeyDown(Key.ShowListShortcut);

			Assert.That(viewModel.SuggestionsListVisibile, Is.True);
			Assert.That(nrOfViewItems, Is.EqualTo(viewModel.SuggestionsListItems.Count));
			Assert.That(1, Is.EqualTo(viewModel.SelectedSuggestionsListItem));
		}

		[Test]
		public void SuggestionsListIsShownWhenDownKeyIsPressed()
		{
			SetSuggestionsEventHandler();

			viewModel.OnKeyDown(Key.Down);

			Assert.That(viewModel.SuggestionsListVisibile, Is.True);
			Assert.That(nrOfViewItems, Is.EqualTo(viewModel.SuggestionsListItems.Count));
			Assert.That(1, Is.EqualTo(viewModel.SelectedSuggestionsListItem));
		}

		[Test]
		public void NearestSuggestionIsSelectedWhenListIsShown()
		{
			SetSuggestionsEventHandler();

			viewModel.OnChangeText("test 20");
			viewModel.OnKeyDown(Key.ShowListShortcut);

			Assert.That(21, Is.EqualTo(viewModel.SelectedSuggestionsListItem));
		}

		[Test]
		public void WhenASuggestionIsPickedByClicking_ListIsHiddenAndSuggestionIsUsed()
		{
			SetSuggestionsEventHandler();

			viewModel.OnKeyDown(Key.ShowListShortcut);
			viewModel.OnSuggestionClicked(6);

			Assert.That(viewModel.SuggestionsListVisibile, Is.False);
			Assert.That("search 5", Is.EqualTo(viewModel.Text));
		}


		[Test]
		[Category("user use cases")]
		public void BrandNewSearchEnteredAndSubmitted()
		{
			SetSuggestionsEventHandler();

			viewModel.OnChangeText("foo");
			viewModel.OnChangeText("foo bar");
			viewModel.OnKeyDown(Key.Enter);

			Assert.That(1, Is.EqualTo(onSearchNowRaised));
		}

		[Test]
		[Category("user use cases")]
		public void SearchStartedFromSelectedText()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("foo bar");
			viewModel.OnKeyDown(Key.Enter);

			Assert.That("foo bar", Is.EqualTo(viewModel.Text));
			Assert.That(1, Is.EqualTo(onSearchNowRaised));
		}

		[Test]
		[Category("user use cases")]
		public void LastSearchUsed()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("");
			viewModel.OnKeyDown(Key.Down);
			viewModel.OnKeyDown(Key.Enter);
			viewModel.OnKeyDown(Key.Enter);

			Assert.That("search 0", Is.EqualTo(viewModel.Text));
			Assert.That(1, Is.EqualTo(onCurrentSuggestionChanged));
			Assert.That(1, Is.EqualTo(onSearchNowRaised));
			Assert.That("search 0", Is.EqualTo(presenter.CurrentSuggestion.Value.SearchString));
		}

		[Test]
		[Category("user use cases")]
		public void RecentSearchFoundByTypingAndUsed()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("");
			viewModel.OnKeyDown(Key.ShowListShortcut);
			viewModel.OnChangeText("test");
			viewModel.OnChangeText("test 4");
			viewModel.OnKeyDown(Key.Enter);
			viewModel.OnKeyDown(Key.Enter);

			Assert.That("search 4", Is.EqualTo(viewModel.Text));
			Assert.That(1, Is.EqualTo(onCurrentSuggestionChanged));
			Assert.That(1, Is.EqualTo(onSearchNowRaised));
			Assert.That("search 4", Is.EqualTo(presenter.CurrentSuggestion.Value.SearchString));
		}

		[Test]
		[Category("user use cases")]
		public void NonTextualSuggestionSelectedAndUsed()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("");
			viewModel.OnKeyDown(Key.ShowListShortcut);
			viewModel.OnSuggestionClicked(nrOfViewItems - 1);
			viewModel.OnKeyDown(Key.Enter);

			Assert.That("non-textual 2", Is.EqualTo(viewModel.Text));
			Assert.That(1, Is.EqualTo(onCurrentSuggestionChanged));
			Assert.That(1, Is.EqualTo(onSearchNowRaised));
			Assert.That("cat2.2", Is.EqualTo((string)presenter.CurrentSuggestion.Value.Data));
		}

		[Test]
		[Category("user use cases")]
		public void NonTextualSuggestionSelectedThenCancelled()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("");
			viewModel.OnKeyDown(Key.ShowListShortcut);
			viewModel.OnSuggestionClicked(nrOfViewItems - 1);

			Assert.That(1, Is.EqualTo(onCurrentSuggestionChanged));
			Assert.That("cat2.2", Is.EqualTo((string)presenter.CurrentSuggestion.Value.Data));

			viewModel.OnChangeText("");
			Assert.That(2, Is.EqualTo(onCurrentSuggestionChanged));
			Assert.That(presenter.CurrentSuggestion, Is.Null);
		}
	}
}
