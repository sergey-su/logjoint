using NSubstitute;
using NUnit.Framework;
using NUnit.Framework.Legacy;

namespace LogJoint.Tests
{
    [TestFixture]
    public class AnnotationsTests
    {
        readonly IChangeNotification changeNotification = Substitute.For<IChangeNotification>();
        readonly ITraceSourceFactory traceSourceFactory = new TraceSourceFactory();

        [Test]
        public void InitiallyEmpty()
        {
            IAnnotationsRegistry reg = new AnnotationsRegistry(changeNotification, traceSourceFactory);
            Assert.That(reg.Annotations.IsEmpty, Is.True);
        }

        [Test]
        public void FindsAnnotationsWithoutCommonKeyPrefix()
        {
            IAnnotationsRegistry reg = new AnnotationsRegistry(changeNotification, traceSourceFactory);
            reg.Add("abc", "a1", null);
            reg.Add("def", "a2", null);

            CollectionAssert.AreEqual(
                new StringAnnotationEntry[] { new(2, 5, "a2"), new(8, 11, "a1") },
                reg.Annotations.FindAnnotations("x def y abc"));
        }

        [Test]
        public void FindsAnnotationsSharingCommonKeyPrefix()
        {
            IAnnotationsRegistry reg = new AnnotationsRegistry(changeNotification, traceSourceFactory);
            reg.Add("abc", "a1", null);
            reg.Add("abd", "a2", null);

            CollectionAssert.AreEqual(
                new StringAnnotationEntry[] { new(4, 7, "a1"), new(12, 15, "a2") },
                reg.Annotations.FindAnnotations("foo abc abx abd"));
        }

        [Test]
        public void DoesNotFindAnnotationWithKeyEmbeddedIntoAnotherKey()
        {
            IAnnotationsRegistry reg = new AnnotationsRegistry(changeNotification, traceSourceFactory);
            reg.Add("abcd", "a1", null);
            reg.Add("bc", "a2", null);
            reg.Add("bcd", "a3", null);
            reg.Add("cd", "a4", null);

            CollectionAssert.AreEqual(
                new StringAnnotationEntry[] { new(2, 6, "a1") },
                reg.Annotations.FindAnnotations("x abcd y"));
        }

        [Test]
        public void FindsAnnotationWithShortestPrefix()
        {
            IAnnotationsRegistry reg = new AnnotationsRegistry(changeNotification, traceSourceFactory);
            reg.Add("abc", "a1", null);
            reg.Add("ab", "a2", null);

            CollectionAssert.AreEqual(
                new StringAnnotationEntry[] { new(2, 4, "a2") },
                reg.Annotations.FindAnnotations("x abc y"));
        }

        [Test]
        public void AnnotationCanBeOverwritten()
        {
            IAnnotationsRegistry reg = new AnnotationsRegistry(changeNotification, traceSourceFactory);
            reg.Add("abc", "a1", null);

            CollectionAssert.AreEqual(
                new StringAnnotationEntry[] { new(2, 5, "a1") },
                reg.Annotations.FindAnnotations("x abc y"));

            reg.Add("abc", "a2", null);
            CollectionAssert.AreEqual(
                new StringAnnotationEntry[] { new(2, 5, "a2") },
                reg.Annotations.FindAnnotations("x abc y"));
        }
    }
}
