using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Messaging;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LogJoint.Chromium.HttpArchive
{
    public interface IMessagingEvents
    {
        IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<Message[]> input);
    };

    public class MessagingEvents : IMessagingEvents
    {
        IEnumerableAsync<Event[]> IMessagingEvents.GetEvents(IEnumerableAsync<Message[]> input)
        {
            return input.Select<Message, Event>(GetEvents, GetFinalEvents);
        }

        void GetEvents(Message msg, Queue<Event> buffer)
        {
            if (msg.ObjectType == Message.ENTRY)
            {
                switch (msg.MessageType.Value)
                {
                    case Message.START:
                        if (parser.TryParseStart(msg.Text, out var start))
                        {
                            HashSet<string> tags = null;
                            if (Uri.TryCreate(start.Url, UriKind.Absolute, out var uri))
                                tags = GetTags(uri.Host);
                            var displayName = string.Format("{0} {1}", start.Method, start.Url);
                            var startEvt = new HttpMessage(msg, displayName, MessageDirection.Outgoing, MessageType.Request, msg.ObjectId, start.Url, start.Method, "", null, null);
                            if (tags != null)
                                startEvt.SetTags(tags);
                            requests[msg.ObjectId] = new PendingRequest { Start = startEvt };
                            buffer.Enqueue(startEvt);
                        }
                        break;
                    case Message.END:
                        if (requests.TryGetValue(msg.ObjectId, out var endRequest))
                        {
                            var endEvt = new HttpMessage(msg, endRequest.Start.DisplayName, MessageDirection.Incoming, MessageType.Response, msg.ObjectId,
                                endRequest.Start.Url, endRequest.Start.Method, "", null, statusCode: endRequest.Status, statusComment: endRequest.StatusText);
                            endEvt.SetTags(endRequest.Start.Tags);
                            buffer.Enqueue(endEvt);
                            requests.Remove(msg.ObjectId);
                        }
                        break;
                    case Message.RECEIVE:
                        if (parser.TryParseReceive(msg.Text, out var receive) && requests.TryGetValue(msg.ObjectId, out var receiveRequest))
                        {
                            receiveRequest.Status = receive.Status;
                            receiveRequest.StatusText = receive.StatusText;
                        }
                        break;
                }
            }
        }

        void GetFinalEvents(Queue<Event> buffer)
        {
        }

        HashSet<string> GetTags(string host)
        {
            HashSet<string> tags;
            if (!tagsCache.TryGetValue(host, out tags))
            {
                tagsCache.Add(host, tags = new HashSet<string>());
                tags.Add(host);
            }
            return tags;
        }

        class PendingRequest
        {
            public HttpMessage Start;
            public int Status;
            public string StatusText;
        };

        readonly Dictionary<string, HashSet<string>> tagsCache = new Dictionary<string, HashSet<string>>();
        readonly Dictionary<string, PendingRequest> requests = new Dictionary<string, PendingRequest>();
        readonly Parser parser = new Parser();
    }
}
