using System;
using System.Collections.Generic;
using System.Text;
using System.IO;

namespace LogJoint.Log4net
{
    public class RollingStrategy : IRollingFilesMediaStrategy
    {
        public static IConnectionParams CreateConnectionParamsFromBaseFileName(string baseFileName)
        {
            return ConnectionParamsUtils.CreateFileBasedConnectionParamsFromFileName(baseFileName);
        }

        static readonly string fileNameParam = ConnectionParamsKeys.PathConnectionParam;

        readonly string baseFileName;
        readonly char baseFileNameFirstChar;
        readonly string baseDirectory;

        public RollingStrategy(LJTraceSource trace, IConnectionParams connectionParams)
        {
            baseFileName = connectionParams[fileNameParam];
            if (string.IsNullOrEmpty(baseFileName))
                throw new ArgumentException("Base file name is not specified in the connection params");
            baseFileNameFirstChar = GetFileNameFirstChar(baseFileName);
            baseDirectory = Path.GetDirectoryName(baseFileName);
            trace.Info("Base file name first character: {0}", baseFileNameFirstChar);
        }

        public string BaseDirectory
        {
            get { return baseDirectory; }
        }

        public IEnumerable<string> SearchPatterns
        {
            get { yield return baseFileNameFirstChar + "*"; }
        }

        public bool IsFileARolledLog(string fileNameToTest)
        {
            return GetFileNameFirstChar(fileNameToTest) == baseFileNameFirstChar;
        }

        static char GetFileNameFirstChar(string path)
        {
            string fileName = Path.GetFileName(path);
            if (fileName.Length == 0)
                throw new ArgumentException("path is invalid");
            return fileName[0];
        }
    };
}
