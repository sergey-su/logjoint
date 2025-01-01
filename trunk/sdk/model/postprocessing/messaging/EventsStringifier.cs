using System.Text;

namespace LogJoint.Postprocessing.Messaging
{
    internal class EventsStringifier : IEventsVisitor
    {
        public readonly StringBuilder Output = new StringBuilder();

        void IEventsVisitor.Visit(HttpMessage evt)
        {
            Output.AppendFormat("HttpMessage: id='{0}' {1} {2} {3}", evt.MessageId, evt.DisplayName, evt.MessageType, evt.DisplayName);
        }

        void IEventsVisitor.Visit(NetworkMessageEvent evt)
        {
            Output.AppendFormat("Net.{2}: id='{0}' {1}", evt.MessageId, evt.DisplayName, evt.EventType);
        }

        void IEventsVisitor.Visit(ResponselessNetworkMessageEvent evt)
        {
            Output.AppendFormat("ResponselessNet.{1}: {0}", evt.DisplayName ?? evt.MessageId, evt.EventType);
        }

        void IEventsVisitor.Visit(RequestCancellationEvent evt)
        {
            Output.AppendFormat("RequestCancellation: {0}", evt.DisplayName ?? evt.RequestMessageId);
        }

        void IEventsVisitor.Visit(FunctionInvocationEvent evt)
        {
            Output.AppendFormat("Function: {0}", evt.DisplayName ?? evt.InvocationId);
        }

        void IEventsVisitor.Visit(MetadataEvent evt)
        {
            Output.AppendFormat("Meta: {0}={1}", evt.Key, evt.Value);
        }
    }
}
