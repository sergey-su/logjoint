using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace LogJoint.Postprocessing
{
	public class LogSourcePostprocessorImpl : ILogSourcePostprocessor
	{
		readonly string typeId;
		readonly string caption;
		readonly Func<XDocument, ILogSource, object> deserializeOutputData;
		readonly Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> run;

		public LogSourcePostprocessorImpl(
			string typeId,
			string caption,
			Func<XDocument, ILogSource, object> deserializeOutputData,
			Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> run
		)
		{
			this.typeId = typeId;
			this.caption = caption;
			this.deserializeOutputData = deserializeOutputData;
			this.run = run;
		}

		public LogSourcePostprocessorImpl(
			string typeId,
			string caption,
			Func<XDocument, ILogSource, object> deserializeOutputData,
			Func<LogSourcePostprocessorInput, Task> run
		): this(typeId, caption, deserializeOutputData, MakeRunAdapter(run))
		{
		}

		public LogSourcePostprocessorImpl(
			string typeId,
			string caption,
			Func<XDocument, ILogSource, object> deserializeOutputData,
			Func<LogSourcePostprocessorInput, Task<IPostprocessorRunSummary>> run
		): this(typeId, caption, deserializeOutputData, MakeRunAdapter(run))
		{
		}

		string ILogSourcePostprocessor.TypeID
		{
			get { return typeId; }
		}

		string ILogSourcePostprocessor.Caption
		{
			get { return caption; }
		}

		object ILogSourcePostprocessor.DeserializeOutputData(XDocument fromXmlDocument, ILogSource forLogSource)
		{
			return deserializeOutputData(fromXmlDocument, forLogSource);
		}

		Task<IPostprocessorRunSummary> ILogSourcePostprocessor.Run(LogSourcePostprocessorInput[] forLogs)
		{
			return run(forLogs);
		}


		static Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> MakeRunAdapter(Func<LogSourcePostprocessorInput, Task> postprocessor)
		{
			Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> helper = async (inputs) =>
			{
				await Task.WhenAll(inputs.Select(i => postprocessor(i)));
				return (IPostprocessorRunSummary)null;
			};
			return helper;
		}
		
		static Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> MakeRunAdapter(Func<LogSourcePostprocessorInput, Task<IPostprocessorRunSummary>> postprocessor)
		{
			Func<LogSourcePostprocessorInput[], Task<IPostprocessorRunSummary>> helper = async (inputs) =>
			{
				var tasks = await Task.WhenAll(inputs.Select(i => postprocessor(i)));
				return new AggregatedRunSummary(
					inputs.Zip(tasks, (input, task) => new {input.LogSource, task}).ToDictionary(x => x.LogSource, x => x.task)
				);
			};
			return helper;
		}
	};
}
