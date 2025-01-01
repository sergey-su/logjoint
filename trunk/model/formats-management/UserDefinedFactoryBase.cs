using LogJoint.RegularExpressions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint
{
    public struct LoadedRegex
    {
        public IRegex Regex { get; private set; }
        public bool SuffersFromPartialMatchProblem { get; private set; }

        public LoadedRegex(IRegex regex, bool suffersFromPartialMatchProblem)
        {
            this.Regex = regex;
            this.SuffersFromPartialMatchProblem = suffersFromPartialMatchProblem;
        }

        public LoadedRegex WithRegex(IRegex regex) => new LoadedRegex(regex, SuffersFromPartialMatchProblem);

        public MessagesSplitterFlags GetHeaderReSplitterFlags()
        {
            var headerRe = this;
            MessagesSplitterFlags ret = MessagesSplitterFlags.None;
            if (headerRe.SuffersFromPartialMatchProblem)
                ret |= MessagesSplitterFlags.PreventBufferUnderflow;
            return ret;
        }
    };

    public abstract class UserDefinedFactoryBase : IUserDefinedFactory, ILogProviderFactory, IDisposable
    {
        string IUserDefinedFactory.Location { get { return location; } }
        bool IUserDefinedFactory.IsDisposed { get { return disposed; } }

        string ILogProviderFactory.CompanyName { get { return companyName; } }
        string ILogProviderFactory.FormatName { get { return formatName; } }
        string ILogProviderFactory.FormatDescription { get { return description; } }
        IFormatViewOptions ILogProviderFactory.ViewOptions { get { return viewOptions; } }
        string ILogProviderFactory.GetConnectionId(IConnectionParams connectParams) { return ConnectionParamsUtils.GetConnectionIdentity(connectParams); }

        public abstract string UITypeKey { get; }
        public abstract string GetUserFriendlyConnectionName(IConnectionParams connectParams);
        public abstract IConnectionParams GetConnectionParamsToBeStoredInMRUList(IConnectionParams originalConnectionParams);
        public abstract Task<ILogProvider> CreateFromConnectionParams(ILogProviderHost host, IConnectionParams connectParams);
        public abstract LogProviderFactoryFlag Flags { get; }

        public UserDefinedFactoryBase(UserDefinedFactoryParams createParams, IRegexFactory regexFactory)
        {
            if (createParams.FormatSpecificNode == null)
                throw new ArgumentNullException("createParams.FormatSpecificNode");
            if (createParams.RootNode == null)
                throw new ArgumentNullException("createParams.RootNode");

            this.location = createParams.Location;
            this.factoryRegistry = createParams.FactoryRegistry;
            this.regexFactory = regexFactory;

            var idData = createParams.RootNode.Elements("id").Select(
                id => new { company = id.AttributeValue("company"), formatName = id.AttributeValue("name") }).FirstOrDefault();

            if (idData != null)
            {
                companyName = idData.company;
                formatName = idData.formatName;
            }

            description = ReadParameter(createParams.RootNode, "description").Trim();

            viewOptions = new FormatViewOptions(createParams.RootNode.Element("view-options"));

            if (factoryRegistry != null)
                factoryRegistry.Register(this);
        }

        public override string ToString()
        {
            return LogProviderFactoryRegistry.ToString(this);
        }

        void IDisposable.Dispose()
        {
            if (disposed)
                return;
            disposed = true;
            if (factoryRegistry != null)
                factoryRegistry.Unregister(this);
        }

        protected static string ReadParameter(XElement root, string name)
        {
            return root.Elements(name).Select(a => a.Value).FirstOrDefault() ?? "";
        }

        protected LoadedRegex ReadRe(XElement root, string name, ReOptions opts, MessagesReaderExtensions.XmlInitializationParams extensionsInitData)
        {
            LoadedRegex ret = new LoadedRegex();
            var n = root.Element(name);
            if (n == null)
                return ret;
            string pattern = n.Value;
            if (string.IsNullOrEmpty(pattern))
                return ret;
            Regex precompiledRegex = null;
            XAttribute precompiledAttr = n.Attribute("precompiled");
            if (precompiledAttr != null)
            {
                if (extensionsInitData == null)
                    throw new Exception($"'precompiled' attribute is present but extensions are not provided");
                var match = Regex.Match(precompiledAttr.Value, @"^(?<ext>[\w_]+)\.(?<prop>[\w_]+)$", RegexOptions.ExplicitCapture);
                if (!match.Success)
                    throw new Exception($"'precompiled' attribute value '{precompiledAttr.Value}' has wrong format");
                using var unattachedExtensions = new MessagesReaderExtensions(null, extensionsInitData);
                var ext = unattachedExtensions.Items.FirstOrDefault(e => e.Name == match.Groups["ext"].Value);
                if (ext.Name == null)
                    throw new Exception($"'precompiled' attribute '{precompiledAttr.Value}' refers to a non-existing extension");
                precompiledRegex = ext.Instance().GetType().InvokeMember(
                    match.Groups["prop"].Value, BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty, null, ext.Instance(), null) as Regex;
                if (precompiledRegex == null)
                    throw new Exception($"'precompiled' attribute '{precompiledAttr.Value}' refers to a non-Regex property");
            }
            XAttribute partialMatchAttr = n.Attribute("suffers-from-partial-match-problem");
            ret = new LoadedRegex(regexFactory.Create(pattern, opts, precompiledRegex),
                suffersFromPartialMatchProblem: partialMatchAttr != null && partialMatchAttr.Value == "yes");
            return ret;
        }

        protected static Type ReadType(XElement root, string name, Type defType)
        {
            string typeName = ReadParameter(root, name);
            if (string.IsNullOrEmpty(typeName))
                return defType;
            return Type.GetType(typeName);
        }

        protected static void ReadPatterns(XElement formatSpecificNode, List<string> patternsList)
        {
            patternsList.AddRange(
                from patterns in formatSpecificNode.Elements("patterns")
                from pattern in patterns.Elements("pattern")
                let patternVal = pattern.Value
                where patternVal != ""
                select patternVal);
        }

        readonly string location;
        readonly string companyName;
        readonly string formatName;
        readonly string description = "";
        readonly ILogProviderFactoryRegistry factoryRegistry;
        readonly IRegexFactory regexFactory;
        protected readonly FormatViewOptions viewOptions;
        bool disposed;
    };
}
