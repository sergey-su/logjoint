
namespace LogJoint.Analytics.Messaging
{
	internal static class SerializationCommon
	{
		internal const string Elt_NetworkMessage = "net";
		internal const string Elt_ResponselessNetworkMessage = "rsplssnet";
		internal const string Elt_HttpMessage = "httpMessage";
		internal const string Elt_Cancellation = "cancellation";
		internal const string Elt_Function = "function";
		internal const string Elt_Meta = "meta";

		internal const string Attr_DisplayName = "displayName";
		internal const string Attr_Tags = "tags";
		internal const string Attr_MessageDirection = "direction";
		internal const string Attr_MessageType = "type";
		internal const string Attr_EventType = "etype";
		internal const string Attr_MessageId = "id";
		internal const string Attr_Method = "method";
		internal const string Attr_Body = "body";
		internal const string Attr_Headers = "headers";
		internal const string Attr_Type = "type";
		internal const string Attr_Remote = "remote";
		internal const string Attr_StatusCode = "scode";
		internal const string Attr_StatusComment = "smsg";
		internal const string Attr_MetaKey = "key";
		internal const string Attr_MetaValue = "value";
		internal const string Attr_TargetId = "targetId";
	}
}
