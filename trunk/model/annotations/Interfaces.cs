using System.Collections.Generic;
using System.Threading.Tasks;

namespace LogJoint
{
    public interface IAnnotationsRegistry
    {
        IAnnotationsSnapshot Annotations { get; }
        void Add(string key, string value, ILogSource associatedLogSource);
        bool Change(string key, string value);
        bool Delete(string key);
        Task LoadAnnotations(ILogSource forLogSource);
    }

    public record struct StringAnnotationEntry(int BeginIndex, int EndIndex, string Key, string Annotation);

    public interface IAnnotationsSnapshot
    {
        bool IsEmpty { get; }
        IEnumerable<StringAnnotationEntry> FindAnnotations(string input);
        string Find(string key);
    }
}
