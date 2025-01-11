using System;

namespace LogJoint
{
    // Thread safe.
    public interface ITempFilesManager
    {
        string GenerateNewName();
        bool IsTemporaryFile(string filePath);
    };

    // Thread safe.
    public interface ITempFilesCleanupList : IDisposable
    {
        void Add(string fileName);
    };
}