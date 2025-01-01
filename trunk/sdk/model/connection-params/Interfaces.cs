using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace LogJoint
{
    public interface IConnectionParams
    {
        string this[string key] { get; set; }
        void AssignFrom(IConnectionParams other);
        bool AreEqual(IConnectionParams other);
        IConnectionParams Clone(bool makeWritebleCopyIfReadonly = false);
        string ToNormalizedString();
        bool IsReadOnly { get; }
    };

    public class InvalidConnectionParamsException : Exception
    {
        public InvalidConnectionParamsException(string msg) : base(msg) { }
    };

    public static class ConnectionParamsKeys
    {
        /// <summary>
        /// A mandatory IConnectionParams key.
        /// Specifies the identity of the log source. IConnectionParams having equal identities 
        /// are considered to be referencing to the same log source.
        /// </summary>
        public static readonly string IdentityConnectionParam = "id";
        /// <summary>
        /// An IConnectionParams key.
        /// Specifies a path to the file that will be open to read log content. It could be a path 
        /// to actual log file in the local file system or on a network drive. Or it could be 
        /// a temporary file written by logjoint.
        /// If connection parameters doesn't contain PathConnectionParam usually
        /// preprocessing steps are specified. <see cref="PreprocessingStepParamPrefix"/>.
        /// </summary>
        public static readonly string PathConnectionParam = "path";
        /// <summary>
        /// A prefix of a group of IConnectionParams keys.
        /// Each key in the group specifies a preprocessing step that must be taken to obtain actual log. 
        /// Complete keys are composed this way: [prefix][zero-based-index]. 
        /// Example: prep-step0 = 'get http://example.com/log.zip'; prep-step1 = download; prep-step2 = 'unzip f/mylog.txt'
        /// </summary>
        public static readonly string PreprocessingStepParamPrefix = "prep-step";
        /// <summary>
        /// An optional IConnectionParams key.
        /// Specifies user-friendly string representing the log source.
        /// </summary>
        public static readonly string DisplayNameConnectionParam = "display-as";
        /// <summary>
        /// An IConnectionParams key.
        /// Specifies the path to a folder that will be monitored for the parts of the rotated log.
        /// </summary>
        public static readonly string RotatedLogFolderPathConnectionParam = "rotated-log-folder-path";
        /// <summary>
        /// A prefix for connection params that specify the search patterns, like my-log-*.txt,
        /// that will be matched to find the rotated log parts in the folder
        /// specified in <see cref="RotatedLogFolderPathConnectionParam"/>.
        /// </summary>
        public static readonly string RotatedLogPatternParamPrefix = "rotated-log-pattern-";
        /// <summary>
        /// An IConnectionParams key.
        /// When specified defines an initial time offset of a provider.
        /// Value is a TimeSpan formatted by lossless format specifier "c".
        /// </summary>
        public static readonly string TimeOffsetConnectionParam = "time-offset";
    };
}
