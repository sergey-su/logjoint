using System.Collections.Generic;

namespace LogJoint
{
    public interface IAnnotationsRegistry
    {
        IAnnotationsSnapshot Annotations { get; }
        void Add(string key, string value, ILogSource associatedLogSource);
    }

    public record struct StringAnnotationEntry(int BeginIndex, int EndIndex, string Annotation);

    public interface IAnnotationsSnapshot
    {
        bool IsEmpty { get; }
        IEnumerable<StringAnnotationEntry> FindAnnotations(string input);
    }
}
