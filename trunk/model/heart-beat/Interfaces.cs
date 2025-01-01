using System;
using System.Collections.Generic;
using System.Text;
using LogJoint.RegularExpressions;
using System.Linq;

namespace LogJoint
{
    [Flags]
    public enum HeartBeatEventType
    {
        RareUpdate = 1,
        NormalUpdate = 2,
        FrequentUpdate = 4
    };

    public class HeartBeatEventArgs : EventArgs
    {
        public readonly HeartBeatEventType Type;

        public bool IsRareUpdate { get { return (Type & HeartBeatEventType.RareUpdate) != 0; } }
        public bool IsNormalUpdate { get { return (Type & HeartBeatEventType.NormalUpdate) != 0; } }

        public HeartBeatEventArgs(HeartBeatEventType type) { Type = type; }
    };

    public interface IHeartBeatTimer
    {
        void Suspend();
        void Resume();
        event EventHandler<HeartBeatEventArgs> OnTimer;
    };
}
