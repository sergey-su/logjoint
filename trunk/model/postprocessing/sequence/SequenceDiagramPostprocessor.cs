using Microsoft.Skype.Calling.CallFlowAnalyzer;
using System.Threading.Tasks;
using SkyLib = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.SkylibLog;
using NGService = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.CallControllerLog;
using Threshold = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.ThresholdLog;
using TLBlock = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Timeline;
using SIBlock = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.StateInspector;
using MBlock = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.Messaging;
using Telem = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.TelemetryDump;
using UL = Microsoft.Skype.Calling.CallFlowAnalyzer.Blocks.UnifiedLog;
using System.Collections.Generic;
using Microsoft.Skype.Calling.CallFlowAnalyzer.LogRotation;
using System.Xml.Linq;
using System;
using System.Linq;

namespace LogJoint.Skype.SequenceDiagram
{
	public class PostprocessorsFactory: IPostprocessorsFactory
	{
		readonly ILogPartTokenFactory skylibLogPartTokenFactory;
		readonly IShortNames shortNames;
		readonly static string typeId = "SequenceDiagram";
		readonly static string caption = "Sequence Diagram";
		readonly static string dataFileName = "skype.sequence.xml";

		public PostprocessorsFactory(ILogPartTokenFactory skylibLogPartTokenFactory, IShortNames shortNames)
		{
			this.skylibLogPartTokenFactory = skylibLogPartTokenFactory;
			this.shortNames = shortNames;
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateSkylibLogPostprocessor()
		{
			return CreatePostprocessor(RunForSkylibLog, true);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateThresholdLogPostprocessor()
		{
			return CreatePostprocessor(RunForThresholdLog, true);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateNGServiceLogPostprocessor()
		{
			return CreatePostprocessor(RunForNGServiceLog, false);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateTelemetryDumpPostprocessor()
		{
			return CreatePostprocessor(RunForTelemetry, false);
		}

		ILogSourcePostprocessor IPostprocessorsFactory.CreateUnifiedLogPostprocessor()
		{
			return CreatePostprocessor(RunForUL, true);
		}


		ILogSourcePostprocessor CreatePostprocessor(Func<LogSourcePostprocessorInput, Task> run, bool isSkylibBasedLogFormat)
		{
			return new LogSourcePostprocessorImpl(
				typeId, caption, dataFileName,
				(doc, logSource) => DeserializeOutput(doc, logSource, isSkylibBasedLogFormat: isSkylibBasedLogFormat),
				run
			);
		}

		ISequenceDiagramPostprocessorOutput DeserializeOutput(
			XDocument data, ILogSource forSource, bool isSkylibBasedLogFormat)
		{
			return new SequenceDiagramPostprocessorOutput(data, forSource, isSkylibBasedLogFormat  ? skylibLogPartTokenFactory : null);
		}

		async Task RunForSkylibLog(LogSourcePostprocessorInput input)
		{
			await RunForSkylibMessages(new SkyLib.Reader(input.CancellationToken).Read(input.LogFileName, input.GetLogFileNameHint(), input.ProgressHandler), input.OutputFileName, input.InputContentsEtagAttr);
		}

		async Task RunForThresholdLog(LogSourcePostprocessorInput input)
		{
			await RunForSkylibMessages(Threshold.Convert.ToSkylibMessages(new Threshold.Reader(input.CancellationToken).Read(input.LogFileName, input.GetLogFileNameHint(), input.ProgressHandler)), input.OutputFileName, input.InputContentsEtagAttr);
		}

		async Task RunForUL(LogSourcePostprocessorInput input)
		{
			await RunForSkylibMessages(UL.Convert.ToSkylibMessages(UL.Extensions.Read(new UL.Reader(input.CancellationToken), 
				input.LogFileName, input.GetLogFileNameHint(), input.ProgressHandler)), input.OutputFileName, input.InputContentsEtagAttr);
		}

		async Task RunForTelemetry(LogSourcePostprocessorInput input)
		{
			Telem.IReader reader = new Telem.Reader(input.CancellationToken);
			Telem.INGMessagingEventsSource ngMessagingEventsSource = new Telem.NGMessagingEventsSource(reportMetadata: true, reportMessagingEvents: false);
			Telem.IP2PTransportMessagingEventsSource p2pMessagingEventsSource = new Telem.P2PTransportMessagingEventsSource();
			Telem.ICallStateInspector callStateInspectorEventsSource = new Telem.CallStateInspector();
			Telem.ICallingTimelineEvents callingTimelineEventsSource = new Telem.CallingTimelineEvents()
			{
				ReportTeamsUserActions = false
			};
			Telem.IRoleNameMetadataEventsSource roleNameMetadataEventsSource = new Telem.RoleNameMetadataEventsSource();

			var log = Telem.Extensions.Read(reader, input.LogFileName, input.ProgressHandler).Multiplex();

			var messagingEventsTask = EnumerableAsync.Merge(
				ngMessagingEventsSource.GetCSAEvents(log),
				p2pMessagingEventsSource.GetEvents(log),
				roleNameMetadataEventsSource.GetEvents(log)
			).ToList();

			var stateInspectorCommentsFilter = new StateInspectorCommentsFilter(shortNames, pc =>
			{
				if (pc.ObjectType == Telem.CallStateInspector.NGCallingCallModalityTypeInfo)
					return Telem.CallStateInspector.IsBasicNGCallModalityState(pc.Value);
				if (pc.ObjectType == Telem.CallStateInspector.CallingFacadeCallMemberTypeInfo)
					return true;
				return false;
			});
			var callStateInspectorEvents = callStateInspectorEventsSource.GetEvents(log);
			var stateInspectorEventsTask =
				EnumerableAsync.Merge(callStateInspectorEvents)
				.Select(stateInspectorCommentsFilter.Get)
				.Where(e => e != null)
				.ToList();

			var timelineCommentsFilter = new TimelineCommentsFilter(
				proceduresFilter: procedureId => procedureId.StartsWith("team")
			);
			var callingTimelineEvents = callingTimelineEventsSource.GetEvents(log);
			var timelineEventsTask =
				EnumerableAsync.Merge(callingTimelineEvents)
				.Select(timelineCommentsFilter.Get)
				.Where(e => e != null)
				.ToList();

			await Task.WhenAll(
				messagingEventsTask,
				stateInspectorEventsTask,
				timelineEventsTask,
				log.Open()
			);

			SequenceDiagramPostprocessorOutput.SerializePostprocessorOutput(
				await messagingEventsTask,
				await timelineEventsTask,
				await stateInspectorEventsTask,
				null,
				evtTrigger => new TextLogEventTrigger((Telem.Message)evtTrigger),
				input.InputContentsEtagAttr
			).Save(input.OutputFileName);
		}

		async Task RunForSkylibMessages(IEnumerableAsync<SkyLib.Message[]> skylibLogMessages, 
			string outputFileName, XAttribute contentsEtagAttr)
		{
			IPrefixMatcher matcher = new PrefixMatcher();

			var logMessages = SkyLib.Helpers.MatchPrefixes(skylibLogMessages, matcher).Multiplex();

			SkyLib.ISkylibStateInspector skylibStateInspector = new SkyLib.SkylibStateInspector(matcher);
			SkyLib.ILogPartTokenSource rotatedLogPartTokenSource = new SkyLib.RotatedLogPartTokenSource(new SkyLib.ThreadsStateInspector(matcher, skylibStateInspector), skylibStateInspector);
			SkyLib.INGMessagingEventsSource ngMessagingEventsSource = new SkyLib.NGMessagingEventsSource(matcher, skylibStateInspector);

			var p2pMessagingEvts = (new SkyLib.P2PMessagingEventsSource(matcher)).GetEvents(logMessages);
			var commLayerMessagingEvts = (new SkyLib.CommLayerMessagingEventsSource(matcher)).GetEvents(logMessages);
			var httpMessagingEvts = ngMessagingEventsSource.GetEvents(logMessages);
			var roleMetadata = ((SkyLib.IRoleNameMetadataEventsSource)new SkyLib.RoleNameMetadataEventsSource(skylibStateInspector)).GetEvents(logMessages);
			var griffinMessagingEvts =
				(new SkyLib.ServerMMessagingEventsSource(matcher, skylibStateInspector))
				.GetEvents(logMessages)
				.Select(batch => batch.Where(e => e.Tags.Contains(Tags.Calling)).ToArray());
			var messagingEvtsLister = EnumerableAsync.Merge(
				p2pMessagingEvts,
				commLayerMessagingEvts,
				httpMessagingEvts,
				griffinMessagingEvts,
				roleMetadata
			)
			.Select(batch =>
			{
				SkyLib.Message m;
				foreach (var e in batch)
					if ((m = e.Trigger as SkyLib.Message) != null)
						e.Trigger = new TextLogEventTrigger(m);
				return batch;
			})
			.ToFlatList();

			var noCodepathTracking = new NullCodepathTracker();
			var generalCallingTimelineEvts = (new SkyLib.GeneralCallingTimelineEventsSource(matcher, skylibStateInspector, noCodepathTracking)).GetEvents(logMessages);
			var ngCallingTimelineEvts = (new SkyLib.NGTimelineEventsSource(matcher, noCodepathTracking, new SkyLib.TrouterStateInspector(matcher, skylibStateInspector))).GetEvents(logMessages);
			var skylibTimelineEvts = (new SkyLib.SkylibTimelineEventsSource(matcher)).GetEvents(logMessages);
			var timelineCommentsFilter = new TimelineCommentsFilter();
			var timelineEvtsLister = 
				EnumerableAsync.Merge(generalCallingTimelineEvts, ngCallingTimelineEvts, skylibTimelineEvts)
				.Select(timelineCommentsFilter.Get)
				.Where(e => e != null)
				.Select(e => { e.Trigger = new TextLogEventTrigger((SkyLib.Message)e.Trigger); return e; })
				.ToList();

			SkyLib.IP2PCallingStateInspector p2pCallingStateInspector = new SkyLib.P2PCallingStateInspector(matcher, null, skylibStateInspector, null, null);
			SkyLib.INGCallingStateInspector ngCallingStateInspector = new SkyLib.NGCallingStateInspector(matcher, skylibStateInspector, null, null, null);

			var ngCallingStateInspectorEvents = ngCallingStateInspector.GetEvents(logMessages);
			var leagcyCallingStateInspectorEvents = p2pCallingStateInspector.GetEvents(logMessages);
			var stateInspectorCommentsFilter = new StateInspectorCommentsFilter(shortNames);
			var stateInspectorEvtsLister =
				EnumerableAsync.Merge(ngCallingStateInspectorEvents, leagcyCallingStateInspectorEvents)
				.Select(stateInspectorCommentsFilter.Get)
				.Where(e => e != null)
				.Select(e => { e.Trigger = new TextLogEventTrigger((SkyLib.Message)e.Trigger); return e; })
				.ToList();

			var rotatedLogPartToken = rotatedLogPartTokenSource.GetToken(logMessages);


			await Task.WhenAll(
				messagingEvtsLister,
				timelineEvtsLister,
				stateInspectorEvtsLister,
				rotatedLogPartToken,
				logMessages.Open()
			);

			SequenceDiagramPostprocessorOutput.SerializePostprocessorOutput(
				await messagingEvtsLister,
				await timelineEvtsLister,
				await stateInspectorEvtsLister,
				await rotatedLogPartToken,
				evtTrigger => (TextLogEventTrigger)evtTrigger,
				contentsEtagAttr
			).Save(outputFileName);
		}

		async Task RunForNGServiceLog(LogSourcePostprocessorInput input)
		{
			string outputFileName = input.OutputFileName;
			var reader = NGService.Extensions.ReadPlaintextLog(new NGService.Reader(), input.LogFileName).Multiplex();
			var ccMessaging = (new NGService.HttpMessagingEventsSource()).GetEvents(reader);
			var metadataEvents = (new NGService.RoleNameMetadataEventsSource(GetLogFileNameHint(input.LogSource.Provider))).GetEvents(reader);
			//var ccMessaging2 = (new NGService.ClientTelemetryCompatibeMessagingEventsSource()).GetEvents(reader);
			var listerTask = EnumerableAsync.Merge(ccMessaging, metadataEvents).ToFlatList();
			await Task.WhenAll(
				listerTask,
				reader.Open()
			);
			SequenceDiagramPostprocessorOutput.SerializePostprocessorOutput(
				await listerTask,
				new List<TLBlock.Event>(),
				new List<SIBlock.Event>(),
				null,
				evtTrigger => new TextLogEventTrigger((NGService.Message)evtTrigger),
				input.InputContentsEtagAttr
			).Save(outputFileName);
		}

		public static string GetLogFileNameHint(ILogProvider provider)
		{
			var saveAs = provider as ISaveAs;
			if (saveAs == null || !saveAs.IsSavableAs)
				return null;
			return saveAs.SuggestedFileName;
		}
	};

	class TimelineCommentsFilter : TLBlock.IEventsVisitor
	{
		Predicate<string> proceduresFilter;
		TLBlock.Event currentEvent;
		
		public TimelineCommentsFilter(Predicate<string> proceduresFilter = null)
		{
			this.proceduresFilter = proceduresFilter;
		}

		public TLBlock.Event Get(TLBlock.Event evt)
		{
			currentEvent = null;
			evt.Visit(this);
			return currentEvent;
		}

		void TLBlock.IEventsVisitor.Visit(TLBlock.ProcedureEvent evt)
		{
			if (proceduresFilter?.Invoke(evt.ActivityId) == true
			&& (evt.Type == TLBlock.ActivityEventType.Begin || evt.Type == TLBlock.ActivityEventType.End))
			{
				currentEvent = evt;
			}
		}

		void TLBlock.IEventsVisitor.Visit(TLBlock.ObjectLifetimeEvent evt)
		{
		}

		void TLBlock.IEventsVisitor.Visit(TLBlock.UserActionEvent evt)
		{
			currentEvent = evt;
		}

		void TLBlock.IEventsVisitor.Visit(TLBlock.NetworkMessageEvent evt)
		{
		}

		void TLBlock.IEventsVisitor.Visit(TLBlock.APICallEvent evt)
		{
			currentEvent = evt;
		}

		void TLBlock.IEventsVisitor.Visit(TLBlock.EndOfTimelineEvent evt)
		{
		}
	};

	class StateInspectorCommentsFilter : SIBlock.IEventsVisitor
	{
		class ObjectInfo
		{
			public string PrimaryPropertyName;
			public string DisplayIdPropertyName;
			public string DisplayIdPropertyValue;
		};

		Dictionary<string, ObjectInfo> objects = new Dictionary<string, ObjectInfo>();
		SIBlock.Event currentEvent;
		Predicate<SIBlock.PropertyChange> propsFilter;
		IShortNames shortNames;

		public StateInspectorCommentsFilter(IShortNames shortNames, Predicate<SIBlock.PropertyChange> propsFilter = null)
		{
			this.shortNames = shortNames;
			this.propsFilter = propsFilter ?? (_ => true);
		}

		public SIBlock.Event Get(SIBlock.Event evt)
		{
			currentEvent = null;
			evt.Visit(this);
			return currentEvent;
		}

		public void Visit(SIBlock.ObjectCreation objectCreation)
		{
			objects[objectCreation.ObjectId] = new ObjectInfo()
			{
				PrimaryPropertyName = objectCreation.ObjectType.PrimaryPropertyName,
				DisplayIdPropertyName = objectCreation.ObjectType.DisplayIdPropertyName
			};
		}

		public void Visit(SIBlock.ObjectDeletion objectDeletion)
		{
			objects.Remove(objectDeletion.ObjectId);
		}

		public void Visit(SIBlock.PropertyChange propertyChange)
		{
			ObjectInfo obj;
			if (!objects.TryGetValue(propertyChange.ObjectId, out obj))
				return;
			if (obj.DisplayIdPropertyName != null && propertyChange.PropertyName == obj.DisplayIdPropertyName)
			{
				if (propertyChange.ValueType == SIBlock.ValueType.UserHash)
					obj.DisplayIdPropertyValue = shortNames.GetShortNameForUserHash(propertyChange.Value);
				else
					obj.DisplayIdPropertyValue = propertyChange.Value;
			}
			else if (obj.PrimaryPropertyName != null && obj.PrimaryPropertyName == propertyChange.PropertyName)
			{
				if (!propsFilter(propertyChange))
					return;
				currentEvent = new SIBlock.PropertyChange(
					propertyChange.Trigger,
					propertyChange.ObjectId + (!string.IsNullOrEmpty(obj.DisplayIdPropertyValue) ? "(" + obj.DisplayIdPropertyValue + ")" : ""),
					propertyChange.ObjectType,
					propertyChange.PropertyName,
					propertyChange.Value,
					propertyChange.ValueType,
					propertyChange.OldValue
				);
				currentEvent.SetTags(propertyChange.Tags);
			}
		}

		public void Visit(SIBlock.ParentChildRelationChange parentChildRelationChange)
		{
		}
	};
}
