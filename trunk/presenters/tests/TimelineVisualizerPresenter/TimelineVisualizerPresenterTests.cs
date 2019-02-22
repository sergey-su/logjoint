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
			}
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
	}
}
