using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;
using M = LogJoint.Postprocessing.Messaging;

namespace LogJoint.Postprocessing.Correlation
{
	public class SameNodeDetectionResult
	{
		public TimeSpan TimeDiff { get; private set; }
		public SameNodeDetectionResult(TimeSpan timeDiff)
		{
			TimeDiff = timeDiff;
		}
	};

	public interface ISameNodeDetectionToken
	{
		SameNodeDetectionResult DetectSameNode(ISameNodeDetectionToken otherNodeToken);
		ISameNodeDetectionTokenFactory Factory { get; }
		void Serialize(XElement node);
	};

	public interface ISameNodeDetectionTokenFactory
	{
		/// <summary>
		/// Permanent unique ID of this factory.
		/// It's stored in persistent storage. It's used to find the
		/// factory that can deserialize the stored tokens.
		/// </summary>
		string Id { get; }
		ISameNodeDetectionToken Deserialize(XElement element);
	};

	public interface IModel
	{
		Task SavePostprocessorOutput( // todo: collect args into struct?
			Task<ILogPartToken> logPartTask,
			IEnumerableAsync<M.Event[]> events,
			Task<ISameNodeDetectionToken> sameNodeDetectionTokenTask,
			Func<object, TextLogEventTrigger> triggersConverter,
			LogSourcePostprocessorInput postprocessorInput
		);
	};
}
