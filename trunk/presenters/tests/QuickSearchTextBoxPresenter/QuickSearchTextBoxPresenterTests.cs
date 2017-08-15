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
		IViewEvents eventsHandler;
		IPresenter presenter;
		IView view;
		const int nrOfSuggestions = 50;
		const int nrOfNonTextualSuggestions = 3;
		const int nrOfViewItems = nrOfSuggestions + 2; // +2 is category captions
		int onSearchNowRaised, onCurrentSuggestionChanged;

		[SetUp] 
		public void Init()
		{
			view = Substitute.For<IView>();
			view.When(v => v.SetPresenter(Arg.Any<IViewEvents>())).Do(
				x => eventsHandler = x.Arg<IViewEvents>());
			presenter = new Presenter(view);
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

			eventsHandler.OnKeyDown(Key.ShowListShortcut);
			eventsHandler.OnKeyDown(Key.Down);

			// none of these is called to avoid unnecessary creation of view's objects
			// for suggestions-less SearchTextBox-es
			view.DidNotReceive().SetListVisibility(true);
			view.DidNotReceiveWithAnyArgs().SetListItems(null);
			view.DidNotReceiveWithAnyArgs().SetListSelectedItem(0);
		}

		[Test]
		public void ClickingDropdownButtonMakesInputBoxFocused()
		{
			eventsHandler.OnDropDownButtonClicked();

			view.Received().ReceiveInputFocus();
		}

		[Test]
		public void SuggestionsListIsShownWhenShortcutIsPressed()
		{
			SetSuggestionsEventHandler();

			eventsHandler.OnKeyDown(Key.ShowListShortcut);

			view.Received().SetListVisibility(true);
			view.Received().SetListItems(Arg.Is<List<ViewListItem>>(l => l.Count == nrOfViewItems));
			view.Received().SetListSelectedItem(1);
		}

		[Test]
		public void SuggestionsListIsShownWhenDownKeyIsPressed()
		{
			SetSuggestionsEventHandler();

			eventsHandler.OnKeyDown(Key.Down);

			view.Received().SetListVisibility(true);
			view.Received().SetListItems(Arg.Is<List<ViewListItem>>(l => l.Count == nrOfViewItems));
			view.Received().SetListSelectedItem(1);
		}

		[Test]
		public void NearestSuggestionIsSelectedWhenListIsShown()
		{
			SetSuggestionsEventHandler();

			view.Text.Returns("test 20");
			eventsHandler.OnKeyDown(Key.ShowListShortcut);

			view.Received().SetListSelectedItem(21);
		}

		[Test]
		public void WhenASuggestionIsPickedByClicking_ListIsHiddenAndSuggestionIsUsed()
		{
			SetSuggestionsEventHandler();

			eventsHandler.OnKeyDown(Key.ShowListShortcut);
			eventsHandler.OnSuggestionClicked(6);

			view.Received().SetListVisibility(false);
			view.Received().Text = "search 5";
		}

		[Test]
		[Category("user use cases")]
		public void BrandNewSearchEnteredAndSubmitted()
		{
			SetSuggestionsEventHandler();

			view.Text.Returns("foo");
			eventsHandler.OnTextChanged();
			view.Text.Returns("foo bar");
			eventsHandler.OnTextChanged();
			eventsHandler.OnKeyDown(Key.Enter);

			Assert.AreEqual(1, onSearchNowRaised);
		}

		[Test]
		[Category("user use cases")]
		public void SearchStartedFromSelectedText()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("foo bar");
			eventsHandler.OnKeyDown(Key.Enter);

			view.Received().Text = "foo bar";
			Assert.AreEqual(1, onSearchNowRaised);
		}

		[Test]
		[Category("user use cases")]
		public void LastSearchUsed()
		{
			SetSuggestionsEventHandler();

			presenter.Focus("");
			eventsHandler.OnKeyDown(Key.Down);
			eventsHandler.OnKeyDown(Key.Enter);
			eventsHandler.OnKeyDown(Key.Enter);

			view.Received().Text = "search 0";
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
			eventsHandler.OnKeyDown(Key.ShowListShortcut);
			view.Text.Returns("test");
			eventsHandler.OnTextChanged();
			view.Text.Returns("test 4");
			eventsHandler.OnTextChanged();
			eventsHandler.OnKeyDown(Key.Enter);
			eventsHandler.OnKeyDown(Key.Enter);

			view.Received().Text = "search 4";
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
			eventsHandler.OnKeyDown(Key.ShowListShortcut);
			eventsHandler.OnSuggestionClicked(nrOfViewItems - 1);
			eventsHandler.OnKeyDown(Key.Enter);

			view.Received().Text = "non-textual 2";
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
			eventsHandler.OnKeyDown(Key.ShowListShortcut);
			eventsHandler.OnSuggestionClicked(nrOfViewItems - 1);

			Assert.AreEqual(1, onCurrentSuggestionChanged);
			Assert.AreEqual("cat2.2", (string)presenter.CurrentSuggestion.Value.Data);

			view.Text.Returns("");
			eventsHandler.OnTextChanged();
			Assert.AreEqual(2, onCurrentSuggestionChanged);
			Assert.IsNull(presenter.CurrentSuggestion);
		}
	}
}
