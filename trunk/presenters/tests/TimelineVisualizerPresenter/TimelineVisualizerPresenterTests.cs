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

namespace LogJoint.UI.Presenters.Tests.TimelineVisualizerPresenterTests
{
	[TestFixture]
	public class TimelineVisualizerPresenterTests
	{
		IPresenter presenter;
		IViewEvents eventsHandler;
		IView view;
		Postprocessing.StateInspectorVisualizer.IPresenter stateInspectorVisualizer;
		Postprocessing.Common.IPresentationObjectsFactory presentationObjectsFactory;
		LoadedMessages.IPresenter loadedMessagesPresenter;
		IBookmarks bookmarks;
		Persistence.IStorageManager storageManager;
		IPresentersFacade presentersFacade;
		IUserNamesProvider userNamesProvider;

		[SetUp] 
		public void Init()
		{
			view = Substitute.For<IView>();
			presentationObjectsFactory = Substitute.For<Postprocessing.Common.IPresentationObjectsFactory>();
			bookmarks = Substitute.For<IBookmarks>();
			storageManager = Substitute.For<Persistence.IStorageManager>();
			loadedMessagesPresenter = Substitute.For<LoadedMessages.IPresenter>();
			userNamesProvider = Substitute.For<IUserNamesProvider>();
			view.When(v => v.SetEventsHandler(Arg.Any<IViewEvents>())).Do(x => eventsHandler = x.Arg<IViewEvents>());
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
				userNamesProvider
			);
		}

		protected static IActivity MakeActivity(
			int b,
			int e,
			string displayName = null,
			ActivityType type = ActivityType.OutgoingNetworking
		)
		{
			var a = Substitute.For<IActivity>();
			a.DisplayName.Returns(displayName);
			a.Begin.Returns(TimeSpan.FromMilliseconds(b));
			a.End.Returns(TimeSpan.FromMilliseconds(e));
			a.Type.Returns(type);
			a.Milestones.Returns(new ActivityMilestone[0]);
			a.Phases.Returns(new ActivityPhase[0]);
			a.Tags.Returns(new HashSet<string>());
			return a;
		}

		protected static ITimelineVisualizerModel MakeModel(IActivity[] activities)
		{
			var model = Substitute.For<ITimelineVisualizerModel>();
			var output = Substitute.For<ITimelinePostprocessorOutput>();
			foreach (var a in activities)
			{
				a.BeginOwner.Returns(output);
				a.EndOwner.Returns(output);
			}
			var avaRange = Tuple.Create(
				activities.Select(a => a.Begin).Min(),
				activities.Select(a => a.End).Max());
			model.AvailableRange.Returns(avaRange);
			model.Activities.Returns(activities);
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

			public ADE(string caption = null)
			{
				Caption = caption;
			}
 		};

		protected void VerifyView(ADE[] expectations)
		{
			var actual = eventsHandler.OnDrawActivities().ToList();
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
			}
		}

		object ExpectNewMouseHit(HitTestResult htResult)
		{
			object htToken = new object();
			view.HitTest(htToken).Returns(htResult);
			return htToken;
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
				eventsHandler.OnMouseDown(htToken1, KeyCode.None, false);

				object htToken2 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.4));
				eventsHandler.OnMouseMove(htToken2, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("a") { X1 = 0.8/8, X2 = 10.8/8 },
					new ADE("b") { X1 = 5.8/8, X2 = 15.8/8 },
				});

				object htToken3 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, -1.2));
				eventsHandler.OnMouseMove(htToken3, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("b") { X1 = -7/8.0, X2 = 3/8.0 },
					new ADE("c") { X1 = -2/8.0, X2 = 8/8.0 },
				});
				eventsHandler.OnMouseUp(htToken3);

				object htToken4 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.5));
				eventsHandler.OnMouseMove(htToken4, KeyCode.None);
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
				eventsHandler.OnMouseDown(htToken1, KeyCode.None, false);

				object htToken2 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.NavigationPanelThumb, 0.1));
				eventsHandler.OnMouseMove(htToken2, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("a") { X1 = 2/8.0, X2 = 12/8.0 },
					new ADE("b") { X1 = 7/8.0, X2 = 17/8.0 },
				});

				object htToken3 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.NavigationPanelThumb, 0.5));
				eventsHandler.OnMouseMove(htToken3, KeyCode.None);
				VerifyView(new[]
				{
					new ADE("a") { X1 = -6/8.0, X2 = 4/8.0 },
					new ADE("b") { X1 = -1/8.0, X2 = 9/8.0 },
					new ADE("c") { X1 = 4/8.0, X2 = 14/8.0 },
				});
				eventsHandler.OnMouseUp(htToken3);

				object htToken4 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.NavigationPanelThumb, 0.1));
				eventsHandler.OnMouseMove(htToken4, KeyCode.None);
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

			void SelectActivity(int? idx)
			{
				object htToken1 = new object();
				view.HitTest(htToken1).Returns(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.1, idx));
				eventsHandler.OnMouseDown(htToken1, KeyCode.None, false);
				eventsHandler.OnMouseUp(htToken1);
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
				view.Received().UpdateCurrentActivityControls("OutgoingNetworking: a", Arg.Any<string>(), Arg.Any<IEnumerable<Tuple<object, int, int>>>(), Arg.Any<string>(), Arg.Any<Tuple<object, int, int>>());

				SelectActivity(1);
				VerifyView(new[]
				{
					new ADE("a") { Selected = false },
					new ADE("b") { Selected = true },
				});
				view.Received().UpdateCurrentActivityControls("OutgoingNetworking: b", Arg.Any<string>(), Arg.Any<IEnumerable<Tuple<object, int, int>>>(), Arg.Any<string>(), Arg.Any<Tuple<object, int, int>>());
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
				view.Received().UpdateCurrentActivityControls("", "", Arg.Any<IEnumerable<Tuple<object, int, int>>>(), null, Arg.Any<Tuple<object, int, int>>());
			}


			[Test]
			public void SelectedActivityCanBeResetByPanning()
			{
				SelectActivity(0);

				object htToken1 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, 0.1));
				eventsHandler.OnMouseDown(htToken1, KeyCode.None, false);

				view.ClearReceivedCalls();

				object htToken2 = ExpectNewMouseHit(new HitTestResult(HitTestResult.AreaCode.ActivitiesPanel, -1.3));
				eventsHandler.OnMouseMove(htToken2, KeyCode.None);

				VerifyView(new[]
				{
					new ADE("b") { Selected = false },
					new ADE("c") { Selected = false },
				});
				view.Received().UpdateCurrentActivityControls("", "", Arg.Any<IEnumerable<Tuple<object, int, int>>>(), null, Arg.Any<Tuple<object, int, int>>());
			}
		};

	}
}
