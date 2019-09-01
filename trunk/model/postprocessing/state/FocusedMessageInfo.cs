using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.Postprocessing.StateInspector
{
	public class FocusedMessageInfo // todo: get rid of
	{
		readonly IMessage focusedMessage;

		public FocusedMessageInfo(IMessage focusedMessage)
		{
			this.focusedMessage = focusedMessage;
		}

		public IMessage FocusedMessage
		{
			get { return focusedMessage; }
		}
	}
}
