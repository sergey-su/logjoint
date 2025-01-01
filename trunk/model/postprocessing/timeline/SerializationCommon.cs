﻿
namespace LogJoint.Postprocessing.Timeline
{
    internal static class SerializationCommon
    {
        internal const string Elt_Procedure = "procedure";
        internal const string Elt_Lifetime = "lifetime";
        internal const string Elt_NetworkMessage = "networkMessage";
        internal const string Elt_UserAction = "userAction";
        internal const string Elt_APICall = "apiCall";
        internal const string Elt_EOF = "eof";
        internal const string Elt_Phase = "phase";

        internal const string Attr_DisplayName = "displayName";
        internal const string Attr_ActivityId = "activityId";
        internal const string Attr_Type = "type";
        internal const string Attr_Direction = "direction";
        internal const string Attr_Tags = "tags";
        internal const string Attr_TimelineNameType = "nameType";
        internal const string Attr_Begin = "b";
        internal const string Attr_End = "e";
        internal const string Attr_Status = "s";
    }
}
