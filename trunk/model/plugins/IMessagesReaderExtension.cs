using System;
using System.Collections.Generic;
using System.Text;

namespace LogJoint
{
	public interface IMessagesReaderExtension : IDisposable
	{
		void Attach(IPositionedMessagesReader reader);
		void OnAvailableBoundsUpdated(bool incrementalMode, UpdateBoundsStatus updateBoundsStatus);
	};
}
