using LogJoint.Postprocessing;
using LogJoint.Postprocessing.Timeline;
using System.Collections.Generic;
using System.Linq;
using System;

namespace LogJoint.Chromium.ChromeDriver
{
    public interface ITimelineEvents
    {
        IEnumerableAsync<Event[]> GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input);
    };

    public class TimelineEvents : ITimelineEvents
    {
        public TimelineEvents(
            IPrefixMatcher matcher
        )
        {
            devToolsNetworkEventPrefix1 = matcher.RegisterPrefix(DevTools.Events.Network.Base.Prefix1);
            devToolsNetworkEventPrefix2 = matcher.RegisterPrefix(DevTools.Events.Network.Base.Prefix2);
        }


        IEnumerableAsync<Event[]> ITimelineEvents.GetEvents(IEnumerableAsync<MessagePrefixesPair<Message>[]> input)
        {
            return input.Select<MessagePrefixesPair<Message>, Event>(GetEvents, GetFinalEvents);
        }

        void GetEvents(MessagePrefixesPair<Message> msgPfx, Queue<Event> buffer)
        {
            var msg = msgPfx.Message;
            if (msgPfx.Prefixes.Contains(devToolsNetworkEventPrefix1) || msgPfx.Prefixes.Contains(devToolsNetworkEventPrefix2))
            {
                var m = DevTools.Events.LogMessage.Parse(msg.Text);
                if (m != null)
                {
                    ActivityEventType? type = null;
                    DevTools.Events.Network.Base basePayload = null;
                    ActivityStatus status = ActivityStatus.Unspecified;
                    string displayName = null;
                    string host = null;
                    List<ActivityPhase> phases = null;

                    if (m.EventType == DevTools.Events.Network.RequestWillBeSent.EventType)
                    {
                        type = ActivityEventType.Begin;
                        var payload = m.ParsePayload<DevTools.Events.Network.RequestWillBeSent>();
                        basePayload = payload;
                        if (payload?.request?.url != null)
                        {
                            string methodPart = payload.request.method;
                            methodPart = string.IsNullOrEmpty(methodPart) ? "" : (methodPart + " ");
                            string urlPart = payload.request.url;
                            Uri uri;
                            if (Uri.TryCreate(payload.request?.url, UriKind.Absolute, out uri)
                                    && !(uri.PathAndQuery == "" || uri.PathAndQuery == "/"))
                            {
                                urlPart = uri.PathAndQuery;
                                host = uri.Host;
                            }
                            displayName = string.Format("{0}{1}", methodPart, urlPart);
                        }
                    }
                    else if (m.EventType == DevTools.Events.Network.LoadingFinished.EventType)
                    {
                        type = ActivityEventType.End;
                    }
                    else if (m.EventType == DevTools.Events.Network.LoadingFailed.EventType)
                    {
                        type = ActivityEventType.End;
                        displayName = "Failed";
                        status = ActivityStatus.Error;
                    }
                    else if (m.EventType == DevTools.Events.Network.ResponseReceived.EventType)
                    {
                        type = ActivityEventType.Milestone;
                        var payload = m.ParsePayload<DevTools.Events.Network.ResponseReceived>();
                        basePayload = payload;
                        if (payload?.response.timing != null)
                            phases = CreatePhases(payload);
                        displayName = "Response received";
                        if (payload?.response?.status > 200)
                            status = ActivityStatus.Error;
                    }
                    else if (m.EventType == DevTools.Events.Network.DataReceived.EventType)
                    {
                        type = ActivityEventType.Milestone;
                        var payload = m.ParsePayload<DevTools.Events.Network.DataReceived>();
                        basePayload = payload;
                        displayName = string.Format("Data received {0} bytes", payload?.dataLength);
                    }
                    else if (m.EventType == DevTools.Events.Network.WebSocketCreated.EventType)
                    {
                        type = ActivityEventType.Begin;
                        var payload = m.ParsePayload<DevTools.Events.Network.WebSocketCreated>();
                        basePayload = payload;
                        if (payload != null)
                        {
                            displayName = string.Format("WebSocket: {0}", payload.url);
                            Uri uri;
                            if (Uri.TryCreate(payload.url ?? "", UriKind.Absolute, out uri))
                                host = uri.Host;
                        }
                    }
                    else if (m.EventType == DevTools.Events.Network.WebSocketClosed.EventType)
                    {
                        type = ActivityEventType.End;
                        displayName = "WebSocket closed";
                    }
                    else
                    {
                        type = ActivityEventType.Milestone;
                        displayName = m.EventType;
                    }

                    if (basePayload == null)
                        basePayload = m.ParsePayload<DevTools.Events.Network.Base>();

                    if (basePayload?.requestId != null)
                    {
                        displayName = displayName ?? basePayload.requestId;
                        string pidTag = DevTools.Events.Network.Base.ParseRequestPid(basePayload.requestId);

                        var netEvt = new NetworkMessageEvent(msg, displayName, basePayload.requestId, type.Value, NetworkMessageDirection.Outgoing, status: status);
                        netEvt.SetTags(GetRequestTags(basePayload.frameId, host, pidTag));
                        netEvt.Phases = phases;

                        if (netEvt.Type == ActivityEventType.End)
                            requestEnds[netEvt.ActivityId] = netEvt;
                        else
                            buffer.Enqueue(netEvt);

                        // sometimes responseReceived follows loadingFinished.
                        // in order not to lose phases and status, patch end event generated by loadingFinished.
                        if (phases != null || status != ActivityStatus.Unspecified)
                        {
                            if (requestEnds.TryGetValue(netEvt.ActivityId, out NetworkMessageEvent requestEnd))
                            {
                                if (phases != null)
                                    requestEnd.Phases = phases;
                                if (status != ActivityStatus.Unspecified)
                                    requestEnd.Status = status;
                            }
                        }
                        // GC requestEnds
                        if (requestEnds.Count > 1024)
                        {
                            var oldKeys = requestEnds
                                .OrderBy(x => ((ITriggerTime)x.Value.Trigger).Timestamp)
                                .Take(requestEnds.Count / 2)
                                .Select(x => x.Key)
                                .ToList();
                            oldKeys.ForEach(k =>
                            {
                                buffer.Enqueue(requestEnds[k]);
                                requestEnds.Remove(k);
                            });
                        }
                    }
                }
            }
        }

        private static List<ActivityPhase> CreatePhases(DevTools.Events.Network.ResponseReceived payload)
        {
            List<ActivityPhase> phases = new List<ActivityPhase>();
            Predicate<double?> isGoodTs =
                x => x != null
                && Math.Abs(x.Value) > 1e-5 // not 0
                && Math.Abs(x.Value + 1) > 1e-5; // not -1
            Action<double?, double?, string, int> addPhase = (b, e, name, t) =>
            {
                if (isGoodTs(b) && isGoodTs(e))
                {
                    phases.Add(new ActivityPhase(
                        TimeSpan.FromTicks((long)(b.Value * 1e4)),
                        TimeSpan.FromTicks((long)(e.Value * 1e4)),
                        t,
                        name
                    ));
                }
            };
            var timing = payload.response.timing;
            addPhase(timing.dnsStart, timing.dnsEnd, "DNS", 0);
            addPhase(timing.sslStart, timing.sslEnd, "SSL", 1);
            addPhase(timing.proxyStart, timing.proxyEnd, "PROXY", 2);
            addPhase(timing.sendStart, timing.sendEnd, "Sending", 3);
            addPhase(timing.pushStart, timing.pushEnd, "Push", 4);
            return phases;
        }

        void GetFinalEvents(Queue<Event> buffer)
        {
            foreach (var evt in requestEnds.Values)
            {
                buffer.Enqueue(evt);
            }
            requestEnds.Clear();
        }

        HashSet<string> GetRequestTags(string frameId, string host, string pid)
        {
            string key = string.Format("frame: {0}, tag: {1}, pid: {2}", frameId, host, pid);
            HashSet<string> ret;
            if (!tagsCache.TryGetValue(key, out ret))
            {
                ret = new HashSet<string>();
                if (frameId != null)
                {
                    string alias;
                    if (!frameAliases.TryGetValue(frameId, out alias))
                        frameAliases.Add(frameId, alias = string.Format(string.Format("frame-{0}", frameAliases.Count + 1)));
                    ret.Add(alias);
                }
                if (!string.IsNullOrEmpty(host))
                    ret.Add(host);
                if (!string.IsNullOrEmpty(pid))
                    ret.Add(string.Format("process-{0}", pid));
                tagsCache.Add(key, ret);
            }
            return ret;
        }

        readonly int devToolsNetworkEventPrefix1, devToolsNetworkEventPrefix2;
        readonly Dictionary<string, NetworkMessageEvent> requestEnds = new Dictionary<string, NetworkMessageEvent>();
        static readonly Dictionary<string, HashSet<string>> tagsCache = new Dictionary<string, HashSet<string>>();
        static readonly Dictionary<string, string> frameAliases = new Dictionary<string, string>();
    }
}
