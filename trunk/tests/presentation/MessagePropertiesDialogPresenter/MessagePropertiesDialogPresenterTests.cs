using System;
using System.Linq;
using LogJoint.UI.Presenters.MessagePropertiesDialog;
using System.Threading;
using System.Threading.Tasks;
using System.Text;
using NUnit.Framework;
using NSubstitute;

namespace LogJoint.UI.Presenters.Tests.MessagePropertiesDialogPresenterTests
{
    [TestFixture]
    public class MessagePropertiesDialogPresenterTests
    {
        IAnnotationsRegistry annotations;

        [SetUp]
        public void Setup()
        {
            annotations = new AnnotationsRegistry(
                new ChangeNotification(new ManualSynchronizationContext()), new TraceSourceFactory());
        }

        [Test]
        public void CreatesTextFragmentsWhenNoAnnotationsNoSearchResults()
        {
            Assert.That(Presenter.CreateTextFragments("foo", null, annotations.Annotations),
                Is.EqualTo([new TextSegment(TextSegmentType.Plain, new StringSlice("foo"))]));
        }

        [Test]
        public void CreatesTextFragmentsWithSearchResults()
        {
            Assert.That(
                Presenter.CreateTextFragments("aaa bbb ccc bbb ddd",
                    new Presenter.InlineSearchData() { Query = "bbb", Index = 0 }, annotations.Annotations),
                Is.EqualTo([
                    new TextSegment(TextSegmentType.Plain, new StringSlice("aaa ")),
                    new TextSegment(TextSegmentType.PrimarySearchResult, new StringSlice("bbb")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice(" ccc ")),
                    new TextSegment(TextSegmentType.SecondarySearchResult, new StringSlice("bbb")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice(" ddd")),
                ]));

            Assert.That(
                Presenter.CreateTextFragments("aaa bbb ccc bbb ddd",
                    new Presenter.InlineSearchData() { Query = "bbb", Index = 1 }, annotations.Annotations),
                Is.EqualTo([
                    new TextSegment(TextSegmentType.Plain, new StringSlice("aaa ")),
                    new TextSegment(TextSegmentType.SecondarySearchResult, new StringSlice("bbb")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice(" ccc ")),
                    new TextSegment(TextSegmentType.PrimarySearchResult, new StringSlice("bbb")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice(" ddd")),
                ]));

            Assert.That(
                Presenter.CreateTextFragments("aaa bbb ccc bbb ddd",
                    new Presenter.InlineSearchData() { Query = "bbb", Index = 2 }, annotations.Annotations),
                Is.EqualTo([
                    new TextSegment(TextSegmentType.Plain, new StringSlice("aaa ")),
                    new TextSegment(TextSegmentType.PrimarySearchResult, new StringSlice("bbb")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice(" ccc ")),
                    new TextSegment(TextSegmentType.SecondarySearchResult, new StringSlice("bbb")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice(" ddd")),
                ]));
        }

        [Test]
        public void CreatesTextFragmentsWithAnnotations()
        {
            annotations.Add("bar", "ann1", associatedLogSource: null);

            Assert.That(Presenter.CreateTextFragments("foo bar baz", null, annotations.Annotations),
                Is.EqualTo([
                    new TextSegment(TextSegmentType.Plain, new StringSlice("foo ")),
                    new TextSegment(TextSegmentType.Annotation, new StringSlice("ann1")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice("bar baz")),
                ]));

            Assert.That(Presenter.CreateTextFragments("foo bar baz bar", null, annotations.Annotations),
                Is.EqualTo([
                    new TextSegment(TextSegmentType.Plain, new StringSlice("foo ")),
                    new TextSegment(TextSegmentType.Annotation, new StringSlice("ann1")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice("bar baz ")),
                    new TextSegment(TextSegmentType.Annotation, new StringSlice("ann1")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice("bar")),
                ]));
        }

        [Test]
        public void CreatesTextFragmentsWithAnnotationsAndSearchResults()
        {
            annotations.Add("bar", "ann1", associatedLogSource: null);

            Assert.That(Presenter.CreateTextFragments("foo bar baz",
                new Presenter.InlineSearchData() { Query = "baz", Index = 0 }, annotations.Annotations),
                Is.EqualTo([
                    new TextSegment(TextSegmentType.Plain, new StringSlice("foo ")),
                    new TextSegment(TextSegmentType.Annotation, new StringSlice("ann1")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice("bar ")),
                    new TextSegment(TextSegmentType.PrimarySearchResult, new StringSlice("baz")),
                ]));

            Assert.That(Presenter.CreateTextFragments("foo bar baz",
                new Presenter.InlineSearchData() { Query = "foo bar", Index = 0 }, annotations.Annotations),
                Is.EqualTo([
                    new TextSegment(TextSegmentType.PrimarySearchResult, new StringSlice("foo ")),
                    new TextSegment(TextSegmentType.Annotation, new StringSlice("ann1")),
                    new TextSegment(TextSegmentType.PrimarySearchResult, new StringSlice("bar")),
                    new TextSegment(TextSegmentType.Plain, new StringSlice(" baz")),
                ]));
        }
    }
}