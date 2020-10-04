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
			Assert.IsFalse(viewModel.SuggestionsListAvailabile);
			Assert.IsFalse(viewModel.SuggestionsListVisibile);
			Assert.IsEmpty(viewModel.SuggestionsListItems);
			Assert.IsNull(viewModel.SelectedSuggestionsListItem);
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

			Assert.IsTrue(viewModel.SuggestionsListVisibile);
			Assert.AreEqual(nrOfViewItems, viewModel.SuggestionsListItems.Count);
			Assert.AreEqual(1, viewModel.SelectedSuggestionsListItem);
		}

		[Test]
		public void SuggestionsListIsShownWhenDownKeyIsPressed()
		{
			SetSuggestionsEventHandler();

			viewModel.OnKeyDown(Key.Down);

			Assert.IsTrue(viewModel.SuggestionsListVisibile);
			Assert.AreEqual(nrOfViewItems, viewModel.SuggestionsListItems.Count);
			Assert.AreEqual(1, viewModel.SelectedSuggestionsListItem);
		}

		[Test]
		public void NearestSuggestionIsSelectedWhenListIsShown()
		{
			SetSuggestionsEventHandler();

			viewModel.OnChangeText("test 20");
			viewModel.OnKeyDown(Key.ShowListShortcut);

			Assert.AreEqual(21, viewModel.SelectedSuggestionsListItem);
		}

		[Test]
		public void WhenASuggestionIsPickedByClicking_ListIsHiddenAndSuggestionIsUsed()
		{
			SetSuggestionsEventHandler();

			viewModel.OnKeyDown(Key.ShowListShortcut);
			viewModel.OnSuggestionClicked(6);

			Assert.IsFalse(viewModel.SuggestionsListVisibile);
			Assert.AreEqual("search 5", viewModel.Text);
		}


		[Test]
		[Category("user use cases")]
		public void BrandNewSearchEnteredAndSubmitted()
		{
			SetSuggestionsEventHandler();

			viewModel.OnChangeText("foo");
			viewModel.OnChangeText("foo bar");
			viewModel.OnKeyDown(Key.Enter);

			Assert.AreEqual(1, onSearchNowRaised);
		}

		[Test]
		[Category("user use cases")]
		public void SearchStartedFromSelectedText()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("foo bar");
			viewModel.OnKeyDown(Key.Enter);

			Assert.AreEqual("foo bar", viewModel.Text);
			Assert.AreEqual(1, onSearchNowRaised);
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

			Assert.AreEqual("search 0", viewModel.Text);
			Assert.AreEqual(1, onCurrentSuggestionChanged);
			Assert.AreEqual(1, onSearchNowRaised);
			Assert.AreEqual("search 0", presenter.CurrentSuggestion.Value.SearchString);
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

			Assert.AreEqual("search 4", viewModel.Text);
			Assert.AreEqual(1, onCurrentSuggestionChanged);
			Assert.AreEqual(1, onSearchNowRaised);
			Assert.AreEqual("search 4", presenter.CurrentSuggestion.Value.SearchString);
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

			Assert.AreEqual("non-textual 2", viewModel.Text);
			Assert.AreEqual(1, onCurrentSuggestionChanged);
			Assert.AreEqual(1, onSearchNowRaised);
			Assert.AreEqual("cat2.2", (string)presenter.CurrentSuggestion.Value.Data);
		}

		[Test]
		[Category("user use cases")]
		public void NonTextualSuggestionSelectedThenCancelled()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("");
			viewModel.OnKeyDown(Key.ShowListShortcut);
			viewModel.OnSuggestionClicked(nrOfViewItems - 1);

			Assert.AreEqual(1, onCurrentSuggestionChanged);
			Assert.AreEqual("cat2.2", (string)presenter.CurrentSuggestion.Value.Data);

			viewModel.OnChangeText("");
			Assert.AreEqual(2, onCurrentSuggestionChanged);
			Assert.IsNull(presenter.CurrentSuggestion);
		}
	}
}
