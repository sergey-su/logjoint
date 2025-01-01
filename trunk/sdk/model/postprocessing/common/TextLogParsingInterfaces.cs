using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing
{
    public interface IHeaderMatch
    {
        int Index { get; }
        int Length { get; }
    };

    public interface IRegexHeaderMatch : IHeaderMatch
    {
        Match Match { get; }
    };

    public interface IHeaderMatcher
    {
        unsafe IHeaderMatch Match(char* pBuffer, int length, int startFrom, string buffer);
    };

    [Flags]
    public enum TextLogParserFlags
    {
        None = 0,
        UCS2 = 1,
        SkipDoubleBytePeamble = 2
    };

    public struct MessageInfo
    {
        public IHeaderMatch HeaderMatch;
        public int MessageIndex;
        public long StreamPosition;
        public string MessageBoby;
        public string Buffer;
    };

    public class TextLogParserOptions
    {
        public TextLogParserFlags Flags { get; set; }
        public Action<double> ProgressHandler { get; set; }
        public int RawBufferSize { get; set; } = 1024 * 512;

        public TextLogParserOptions(Action<double> progressHandler)
        {
            this.ProgressHandler = progressHandler;
        }
    };

    public interface ITextLogParser
    {
        IHeaderMatcher CreateRegexHeaderMatcher(Regex regex);
        Task ParseStream(
            Stream inputStream,
            IHeaderMatcher headerMatcher,
            Func<List<MessageInfo>, Task<bool>> messagesSink,
            TextLogParserOptions options
        );
    };
}
