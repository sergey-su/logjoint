using System;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;
using NSubstitute;
using System.Linq;
using System.Collections.Generic;
using LogJoint.UI.Presenters.Postprocessing.TimelineVisualizer;
using LogJoint.Postprocessing.Timeline;
using LogJoint.Postprocessing;
using System.Collections.Immutable;
using LogJoint.Drawing;

namespace LogJoint.UI.Presenters.Tests.TimelineVisualizerPresenterTests
{
	[TestFixture]
	public class TimelineVisualizerPresenterTests
	{
		IPresenter presenter;
		IViewModel viewModel;
		IView view;
		Postprocessing.StateInspectorVisualizer.IPresenter stateInspectorVisualizer;
		Postprocessing.Common.IPresentationObjectsFactory presentationObjectsFactory;
		LoadedMessages.IPresenter loadedMessagesPresenter;
		IBookmarks bookmarks;
		Persistence.IStorageManager storageManager;
		IPresentersFacade presentersFacade;
		IUserNamesProvider userNamesProvider;
		QuickSearchTextBox.IPresenter quickSearchTextBoxPresenter;
		IChangeNotification changeNotification;
		IColorTheme theme;

		[SetUp] 
		public void Init()
		{
			view = Substitute.For<IView>();
			presentationObjectsFactory = Substitute.For<Postprocessing.Common.IPresentationObjectsFactory>();
			bookmarks = Substitute.For<IBookmarks>();
			storageManager = Substitute.For<Persistence.IStorageManager>();
			loadedMessagesPresenter = Substitute.For<LoadedMessages.IPresenter>();
			userNamesProvider = Substitute.For<IUserNamesProvider>();
			view.When(v => v.SetViewModel(Arg.Any<IViewModel>())).Do(x => viewModel = x.Arg<IViewModel>());
			quickSearchTextBoxPresenter = Substitute.For<QuickSearchTextBox.IPresenter>();
			changeNotification = Substitute.For<IChangeNotification>();
			theme = Substitute.For<IColorTheme>();
			theme.ThreadColors.Returns(ImmutableArray.Create(new Color(1), new Color(2)));
			presentationObjectsFactory.CreateQuickSearch(Arg.Any<QuickSearchTextBox.IView>()).Returns(quickSearchTextBoxPresenter);
		}

		protected void MakePresenter(ITimelineVisualizerModel model)
		{
			presenter = new TimelineVisualizerPresenter(
				model,
				view,
				stateInspectorVisualizer,
				presentationObjectsFactory,
				loadedMessagesPresenter,
				bookmarks,
				storageManager,
				presentersFacade,
				userNamesProvider,
				changeNotification,
				theme
			);
		}

		protected static IActivity MakeActivity(
			int b,
			int e,
			string displayName = null,
			ActivityType type = ActivityType.OutgoingNetworking,
			bool isEndedForcefully = false
		)
		{
			var a = Substitute.For<IActivity>();
			a.DisplayName.Returns(displayName);
			a.Begin.Returns(TimeSpan.FromMilliseconds(b));
			a.End.Returns(TimeSpan.FromMilliseconds(e));
			a.Type.Returns(type);
			a.Milestones.Returns(new ActivityMilestoneInfo[0]);
			a.Phases.Returns(new ActivityPhaseInfo[0]);
			a.Tags.Returns(new HashSet<string>());
			a.IsEndedForcefully.Returns(isEndedForcefully);
			return a;
		}

		protected static ITimelineVisualizerModel MakeModel(IActivity[] activities,
			DateTime? origin = null)
		{
			var model = Substitute.For<ITimelineVisualizerModel>();
			var output = Substitute.For<ITimelinePostprocessorOutput>();
			foreach (var a in activities)
			{
				a.BeginOwner.Returns(output);
				a.EndOwner.Returns(output);
			}
			output.TimelineOffset.Returns(TimeSpan.Zero);
			var avaRange = Tuple.Create(
				activities.Select(a => a.Begin).Min(),
				activities.Select(a => a.End).Max());
			model.AvailableRange.Returns(avaRange);
			model.Activities.Returns(activities);
			model.Origin.Returns(origin.GetValueOrDefault(new DateTime(2019, 1, 3)));
			model.Events.Returns(new List<IEvent>());
			model.Comparer.Returns(TimelineEntitiesComparer.Instance);
			return model;
		}

		protected class ADE // activity drawing expectation
		{
			public string Caption;
			public double? X1;
			public double? X2;
			public bool? Selected;
			public ActivityDrawType? Type;
			public int? PairedActivityIndex;
			public bool VerifyPairedActivityIndex;

			public ADE(string caption = null)
			{
				Caption = caption;
			}
 		};

		protected void VerifyView(ADE[] expectations)
		{
			var actual = viewModel.OnDrawActivities().ToList();
			Assert.AreEqual(expectations.Length, actual.Count);
			for (int i = 0; i < expectations.Length; ++i)
			{
				var a = actual[i];
				var e = expectations[i];
				Assert.AreEqual(i, a.Index);
				if (e.Caption != null)
					Assert.AreEqual(e.Caption, a.Caption);
				if (e.X1 != null)
					Assert.AreEqual(e.X1.Value, a.X1, 1e-3);
				if (e.X2 != null)
					Assert.AreEqual(e.X2.Value, a.X2, 1e-3);
				if (e.Selected != null)
					Assert.AreEqual(e.Selected.Value, a.IsSelected);
				if (e.Type != null)
					Assert.AreEqual(e.Type.Value, a.Type);
				if (e.VerifyPairedActivityIndex)
					Assert.AreEqual(e.PairedActivityIndex, a.PairedActivityIndex);
			}
		}

		object ExpectNewMouseHit(HitTestResult htResult)
		{
			object htToken = new object();
			view.HitTest(htToken).Returns(htResult);
			return htToken;
		}

		void SelectActivity(int? idx)
		{
			object htToken1 = new object();
			view.HitTest(htToken1).Returns(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.1, idx));
			viewModel.OnMouseDown(htToken1, KeyCode.None, false);
			viewModel.OnMouseUp(htToken1);
		}


		[TestFixture]
		public class InitialVisibleRangeSelection: TimelineVisualizerPresenterTests
		{
			[Test]
			public void IfFirstActivityIsNonEmpty_ItShouldBeStretchedTo40Percent()
			{
				MakePresenter(MakeModel(new[]
				{
					MakeActivity(0, 10, "a"),
					MakeActivity(5, 20, "b"),
					MakeActivity(100, 130, "c")
				}));
					VerifyView(new[]
				{
					new ADE("a") { X1 = 0, X2 = 0.4 },
					new ADE("b") { X1 = 0.2, X2 = 0.8 },
				});
			}

			[Test]
			public void IfFirstActivityIsEmpty_TheFirstTwoActivitiesShouldBStretchedTo40Percent()
			{
				MakePresenter(MakeModel(new[]
				{
					MakeActivity(0, 0, "a"),
					MakeActivity(5, 20, "b"),
					MakeActivity(100, 130, "c")
				}));
				VerifyView(new[]
			{
					new ADE("a") { X1 = 0, X2 = 0 },
					new ADE("b") { X1 = 0.1, X2 = 0.4 },
				});
			}

		};

		[TestFixture]
		public class OriginChangeTests: TimelineVisualizerPresenterTests
		{
			[Test]
			public void WhenOriginChangesTheVisibleRangeShouldBeAdjustedToMatchOldView()
			{
				var myOrigin = new DateTime(2019, 4, 5);
				var activity = MakeActivity(0, 10, "a");
				var model = MakeModel(new[]
				{
					activity,
				}, origin: myOrigin);
				MakePresenter(model);
				VerifyView(new[]
				{
					new ADE("a") { X1 = 0, X2 = 0.4 },
				});
				model.Origin.Returns(myOrigin.AddHours(-5));
				model.AvailableRange.Returns(Tuple.Create(TimeSpan.Zero, TimeSpan.FromHours(6)));
				activity.BeginOwner.TimelineOffset.Returns(TimeSpan.FromHours(5));
				model.EverythingChanged += Raise.EventWith(new object(), new EventArgs());
				VerifyView(new[]
				{
					new ADE("a") { X1 = 0, X2 = 0.4 },
				});
			}
		};

		[TestFixture]
		public class PanningTest : TimelineVisualizerPresenterTests
		{
			[SetUp]
			public void Setup()
			{
				MakePresenter(MakeModel(new[]
				{
					MakeActivity(0, 10, "a"),
					MakeActivity(5, 15, "b"),
					MakeActivity(10, 20, "c")
				}));
				presenter.Navigate(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(8));
				VerifyView(new[]
				{
					new ADE("a") { X1 = 0, X2 = 1.25 },
					new ADE("b") { X1 = 0.625, X2 = 1.875 },
				});
			}

			[Test]
			public void CanPanByDraggingActivitiesPanel()
			{
				object htToken1 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.3));
				viewModel.OnMouseDown(htToken1, KeyCode.None, false);

				object htToken2 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.4));
				viewModel.OnMouseMove(htToken2, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("a") { X1 = 0.8/8, X2 = 10.8/8 },
					new ADE("b") { X1 = 5.8/8, X2 = 15.8/8 },
				});

				object htToken3 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, -1.2));
				viewModel.OnMouseMove(htToken3, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("b") { X1 = -7/8.0, X2 = 3/8.0 },
					new ADE("c") { X1 = -2/8.0, X2 = 8/8.0 },
				});
				viewModel.OnMouseUp(htToken3);

				object htToken4 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.5));
				viewModel.OnMouseMove(htToken4, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("b") { X1 = -7/8.0, X2 = 3/8.0 },
					new ADE("c") { X1 = -2/8.0, X2 = 8/8.0 },
				});
			}

			[Test]
			public void CanPanByDraggingNavigationPanelThumb()
			{
				object htToken1 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.NavigationPanelThumb, 0.2));
				viewModel.OnMouseDown(htToken1, KeyCode.None, false);

				object htToken2 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.NavigationPanelThumb, 0.1));
				viewModel.OnMouseMove(htToken2, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("a") { X1 = 2/8.0, X2 = 12/8.0 },
					new ADE("b") { X1 = 7/8.0, X2 = 17/8.0 },
				});

				object htToken3 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.NavigationPanelThumb, 0.5));
				viewModel.OnMouseMove(htToken3, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("a") { X1 = -6/8.0, X2 = 4/8.0 },
					new ADE("b") { X1 = -1/8.0, X2 = 9/8.0 },
					new ADE("c") { X1 = 4/8.0, X2 = 14/8.0 },
				});
				viewModel.OnMouseUp(htToken3);

				object htToken4 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.NavigationPanelThumb, 0.1));
				viewModel.OnMouseMove(htToken4, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("a") { X1 = -6/8.0, X2 = 4/8.0 },
					new ADE("b") { X1 = -1/8.0, X2 = 9/8.0 },
					new ADE("c") { X1 = 4/8.0, X2 = 14/8.0 },
				});
			}
		};

		[TestFixture]
		public class SelectionTest : TimelineVisualizerPresenterTests
		{
			[SetUp]
			public void Setup()
			{
				MakePresenter(MakeModel(new[]
				{
					MakeActivity(0, 10, "a"),
					MakeActivity(5, 15, "b"),
					MakeActivity(10, 20, "c")
				}));
				presenter.Navigate(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(8));
			}

			[Test]
			public void CanSelectActivities()
			{
				SelectActivity(0);
				VerifyView(new[]
				{
					new ADE("a") { Selected = true },
					new ADE("b") { Selected = false },
				});
				Assert.AreEqual("OutgoingNetworking: a", viewModel.CurrentActivity.Caption);

				SelectActivity(1);
				VerifyView(new[]
				{
					new ADE("a") { Selected = false },
					new ADE("b") { Selected = true },
				});
				Assert.AreEqual("OutgoingNetworking: b", viewModel.CurrentActivity.Caption);
			}

			[Test]
			public void CanDeselectByClickingOutOfAnyActivity()
			{
				SelectActivity(0);
				view.ClearReceivedCalls();
				SelectActivity(null);
				VerifyView(new[]
				{
					new ADE("a") { Selected = false },
					new ADE("b") { Selected = false },
				});
				Assert.AreEqual("", viewModel.CurrentActivity.Caption);
			}


			[Test]
			public void SelectedActivityCanBeResetByPanning()
			{
				SelectActivity(0);

				object htToken1 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.1));
				viewModel.OnMouseDown(htToken1, KeyCode.None, false);

				view.ClearReceivedCalls();

				object htToken2 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, -1.3));
				viewModel.OnMouseMove(htToken2, KeyCode.None);

				VerifyView(new[]
				{
					new ADE("b") { Selected = false },
					new ADE("c") { Selected = false },
				});
				Assert.AreEqual("", viewModel.CurrentActivity.Caption);
			}
		};

		[TestFixture]
		public class UnfinishedActivitiesTest : TimelineVisualizerPresenterTests
		{
			[SetUp]
			public void Setup()
			{
				MakePresenter(MakeModel(new[]
				{
					MakeActivity(0, 10, "a"),
					MakeActivity(5, 100, "b", isEndedForcefully: true),
					MakeActivity(20, 100, "c", isEndedForcefully: true),
					MakeActivity(30, 100, "d", isEndedForcefully: true),
				}));
				presenter.Navigate(TimeSpan.FromMilliseconds(0), TimeSpan.FromMilliseconds(10));
			}

			[Test]
			public void WhenStartOfOneUnfinishedActivityIsVisble_UnfinishedActivitiesGroupIsNotShown()
			{
				VerifyView(new[]
				{
					new ADE("a") { X1 = 0, X2 = 1 },
					new ADE("b") { X1 = 0.5, X2 = 10 },
				});
			}

			[Test]
			public void WhenOneUnfinishedActivityIsVisble_UnfinishedActivitiesGroupIsNotShown()
			{
				presenter.Navigate(TimeSpan.FromMilliseconds(6), TimeSpan.FromMilliseconds(16));
				VerifyView(new[]
				{
					new ADE("a") { X1 = -0.6, X2 = 0.4 },
					new ADE("b") { X1 = -0.1, X2 = 9.4 },
				});
			}

			[Test]
			public void WhenTwoUnfinishedActivitiesAreVisble_UnfinishedActivitiesGroupIsShown()
			{
				presenter.Navigate(TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(35));
				VerifyView(new[]
				{
					new ADE("started and never finished (2)") { Type = ActivityDrawType.Group },
					new ADE("d") { X1 = 0.5, X2 = 7.5 },
				});
			}

			void TestFoldingToggling(Action toggleFolding)
			{
				presenter.Navigate(TimeSpan.FromMilliseconds(25), TimeSpan.FromMilliseconds(35));

				toggleFolding();
				VerifyView(new[]
				{
					new ADE("started and never finished (2)") { Type = ActivityDrawType.Group },
					new ADE("b") { X1 = -2.0, X2 = 7.5 },
					new ADE("c") { X1 = -0.5, X2 = 7.5 },
					new ADE("d") { X1 = 0.5, X2 = 7.5 },
				});

				toggleFolding();
				VerifyView(new[]
				{
					new ADE("started and never finished (2)") { Type = ActivityDrawType.Group },
					new ADE("d") { X1 = 0.5, X2 = 7.5 },
				});
			}

			[Test]
			public void CanToggleFoldingByFoldingSignClick()
			{
				TestFoldingToggling(() =>
				{
					viewModel.OnMouseDown(ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.FoldingSign, 0, 0)), KeyCode.None, false);
				});
			}

			[Test]
			public void CanToggleFoldingByDoubleClick()
			{
				TestFoldingToggling(() =>
				{
					viewModel.OnMouseDown(ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.CaptionsPanel, 0, 0)), KeyCode.None, doubleClick: true);
				});
			}

			[Test]
			public void CanToggleFoldingByEnterKey()
			{
				TestFoldingToggling(() =>
				{
					SelectActivity(0);
					viewModel.OnKeyDown(KeyCode.Enter);
				});
			}

		};

		[TestFixture]
		public class PairedActivitiesTest : TimelineVisualizerPresenterTests
		{
			[SetUp]
			public void Setup()
			{
				IActivity c1, c2;
				var model = MakeModel(new[]
				{
					MakeActivity(0, 100, "a", isEndedForcefully: true),
					MakeActivity(5, 100, "b", isEndedForcefully: true),
					(c1 = MakeActivity(10, 20, "c1", type: ActivityType.OutgoingNetworking)),
					(c2 = MakeActivity(15, 16, "c2", type: ActivityType.IncomingNetworking)),
					MakeActivity(20, 30, "d"),
				});
				model.GetPairedActivities(c1).Returns(Tuple.Create(c1, c2));
				model.GetPairedActivities(c2).Returns(Tuple.Create(c1, c2));
				MakePresenter(model);
				presenter.Navigate(TimeSpan.FromMilliseconds(5), TimeSpan.FromMilliseconds(25));
			}

			[Test]
			public void DetectsPairedActivities()
			{
				VerifyView(new[]
				{
					new ADE("started and never finished (2)") { Type = ActivityDrawType.Group },
					new ADE("c1") { X1 = 5/20d, X2 = 15/20d, VerifyPairedActivityIndex = true, PairedActivityIndex = null },
					new ADE("c2") { X1 = 10/20d, X2 = 11/20d, VerifyPairedActivityIndex = true, PairedActivityIndex = 1 },
					new ADE("d") { X1 = 15/20d, X2 = 25/20d, VerifyPairedActivityIndex = true, PairedActivityIndex = null },
				});
			}
		}

		[TestFixture]
		public class NoContentLinkTest : TimelineVisualizerPresenterTests
		{
			[SetUp]
			public void Setup()
			{
				MakePresenter(MakeModel(new[]
				{
					MakeActivity(0, 20, "a"),
					MakeActivity(5, 15, "b"),
					MakeActivity(50, 70, "c"),
					MakeActivity(55, 65, "d"),

				}));
				view.ClearReceivedCalls();
				presenter.Navigate(TimeSpan.FromMilliseconds(30), TimeSpan.FromMilliseconds(40));
			}

			[Test]
			public void NoContentLinkIsVisible()
			{
				Assert.AreEqual(true, viewModel.NoContentMessageVisibile);
			}

			[Test]
			public void CanFindPreviousVisibleActivity()
			{
				viewModel.OnNoContentLinkClicked(searchLeft: true);
				VerifyView(new[]
				{
					new ADE("a") { X1 = -15/10d, X2 = 5/10d },
					new ADE("b") { X1 = -10/10d, X2 = 0/10d },
				});
			}

			[Test]
			public void CanFindNextVisibleActivity()
			{
				viewModel.OnNoContentLinkClicked(searchLeft: false);
				VerifyView(new[]
				{
					new ADE("c") { X1 = 5/10d, X2 = 25/10d },
				});
			}

			[Test]
			public void CanFindPreviousVisibleActivity_WidthFiltering()
			{
				quickSearchTextBoxPresenter.Text.Returns("b");
				viewModel.OnNoContentLinkClicked(searchLeft: true);
				VerifyView(new[]
				{
					new ADE("b") { X1 = -5/10d, X2 = 5/10d },
				});
			}

			[Test]
			public void CanFinNextVisibleActivity_WidthFiltering()
			{
				quickSearchTextBoxPresenter.Text.Returns("d");
				viewModel.OnNoContentLinkClicked(searchLeft: false);
				VerifyView(new[]
				{
					new ADE("d") { X1 = 5/10d, X2 = 15/10d },
				});
			}
		}

	}
}
