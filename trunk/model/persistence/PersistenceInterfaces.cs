using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace LogJoint.Persistence
{
    namespace Implementation
    {
        public interface IStorageManagerImplementation : IDisposable
        {
            void SetTrace(LJTraceSource trace);
            void Init(ITimingAndThreading timingThreading, IFileSystemAccess fs, IStorageConfigAccess config);
            Task<IStorageEntry> GetEntry(string entryKey, ulong additionalNumericKey);
            ulong MakeNumericKey(string stringToBeHashed);
            Task<IStorageEntry> GetEntryById(string id);
        };

        public interface ITimingAndThreading
        {
            DateTime Now { get; }
            Task StartTask(Func<Task> routine);
        };

        public interface IStorageConfigAccess
        {
            long SizeLimit { get; } // megs
            int CleanupPeriod { get; } // hours
        };

        /// <summary>
        /// Implements actual platform-dependent data access for StorageManager.
        /// </summary>
        public interface IFileSystemAccess
        {
            void SetTrace(LJTraceSource trace);
            Task EnsureDirectoryCreated(string relativePath);
            /// <summary>
            /// Opens file stream specified by its relative path.
            /// OpenFile may fail because of for example lack of space or
            /// concurrent access to the file by another instance of LogJoint.
            /// In case of failure the method returns null if file is being open 
            /// for reading. The method throws an exception if file is being open for writing.
            /// OpenFile does the best to handle concurrent access and fails only
            /// if something really bad happens.
            /// </summary>
            Task<Stream> OpenFile(string relativePath, bool readOnly);
            /// <summary>
            /// Returns relative paths of subdirectories. It throws OperationCanceledException if cancellation was requested before enumeration is finished.
            /// </summary>
            Task<string[]> ListDirectories(string rootRelativePath, CancellationToken cancellation);
            Task<string[]> ListFiles(string rootRelativePath, CancellationToken cancellation);
            Task DeleteDirectory(string relativePath);
            string AbsoluteRootPath { get; }
            /// <summary>
            /// Returns total storage size in bytes. May take time. It throws OperationCanceledException if cancellation was requested.
            /// </summary>
            Task<long> CalcStorageSize(CancellationToken cancellation);
            /// <summary>
            /// Converts implementation specific exception to StorageException
            /// </summary>
            void ConvertException(Exception e);
        };

        public interface IStorageSectionInternal
        {
            ValueTask<bool> ExistsInFileSystem();
        };

        public interface IStorageEntryInternal : IStorageEntry
        {
            Task<IRawStreamStorageSection> OpenRawXMLSection(string sectionKey, StorageSectionOpenFlag openFlags, ulong additionalNumericKey);
        };
    }
}
