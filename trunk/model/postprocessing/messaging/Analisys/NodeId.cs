using System;
using System.Xml.Linq;

namespace LogJoint.Postprocessing.Messaging.Analisys
{
    public class NodeId : IEquatable<NodeId>
    {
        public readonly string Role;
        public readonly string RoleInstance;
        internal static string xmlName = "id";

        public NodeId(string role, string roleInstance)
        {
            this.Role = role;
            this.RoleInstance = roleInstance;
        }

        public bool Equals(NodeId other)
        {
            return Role == other.Role && RoleInstance == other.RoleInstance;
        }

        public override int GetHashCode()
        {
            return Role.GetHashCode() ^ RoleInstance.GetHashCode();
        }

        public override string ToString()
        {
            return Role + "[" + RoleInstance + "]";
        }

        internal XElement Serialize()
        {
            return new XElement(xmlName, new XAttribute("role", Role), new XAttribute("instance", RoleInstance));
        }

        internal NodeId(XElement node)
        {
            Role = node.Attribute("role").Value;
            RoleInstance = node.Attribute("instance").Value;
        }
    };
}
