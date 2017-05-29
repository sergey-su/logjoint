using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Postprocessing.Timeline
{
	class EventImpl: IEvent
	{
		readonly ITimelinePostprocessorOutput owner;
		readonly TimeSpan time;
		readonly string name;
		readonly EventType type;
		readonly object trigger;

		public EventImpl(ITimelinePostprocessorOutput owner, TimeSpan time, string name, EventType type, object trigger)
		{
			this.owner = owner;
			this.time = time;
			this.name = name;
			this.type = type;
			this.trigger = trigger;
		}

		ITimelinePostprocessorOutput IEvent.Owner { get { return owner; } }

		TimeSpan IEvent.Time { get { return time; } }

		string IEvent.DisplayName { get { return name; } }

		EventType IEvent.Type { get { return type; } }
		object IEvent.Trigger { get { return trigger; } }
	}
}
