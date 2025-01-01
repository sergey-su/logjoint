using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
    public interface ILogPartToken
    {
        int CompareTo(ILogPartToken otherToken);
        void Serialize(XElement to);
        ILogPartTokenFactory Factory { get; }
    };

    public interface ILogPartTokenFactory
    {
        /// <summary>
        /// Permanent unique ID of this factory.
        /// It's stored in persistent storage. It's used to find the
        /// factory that can deserialize the stored tokens.
        /// </summary>
        string Id { get; }
        ILogPartToken Deserialize(XElement element);
    };
}
