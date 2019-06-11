using LogJoint.Preprocessing;
using System;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.Wireshark.Dpml
{
	public class PreprocessingManagerExtension : IPreprocessingManagerExtension
	{
		private readonly IPreprocessingStepsFactory preprocessingStepsFactory;
		private readonly ITShark tshark;
		private readonly PreprocessingsState preprocessingsState = new PreprocessingsState();

		public PreprocessingManagerExtension(IPreprocessingStepsFactory preprocessingStepsFactory, ITShark tshark)
		{
			this.preprocessingStepsFactory = preprocessingStepsFactory;
			this.tshark = tshark;
		}

		IPreprocessingStep IPreprocessingManagerExtension.DetectFormat(PreprocessingStepParams fileInfo, IStreamHeader header)
		{
			if (tshark.IsAvailable)
			{
				if (IsPcap(header))
					return new SaveParamsStep() { state = preprocessingsState, pcap = fileInfo };
				else if (IsKeys(header))
					return new SaveParamsStep() { state = preprocessingsState, key = fileInfo };
			}
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.CreateStepByName(string stepName, PreprocessingStepParams stepParams)
		{
			if (stepName == PcapUnpackPreprocessingStep.stepName)
				return preprocessingStepsFactory.CreatePcapUnpackStep(stepParams, null);
			return null;
		}

		IPreprocessingStep IPreprocessingManagerExtension.TryParseLaunchUri(Uri url)
		{
			return null;
		}

		Task IPreprocessingManagerExtension.FinalizePreprocessing(IPreprocessingStepCallback callback)
		{
			if (preprocessingsState.preprocessings.TryRemove(callback.Owner, out var prepState))
			{
				foreach (var pcap in prepState.pcaps)
				{
					callback.YieldNextStep(preprocessingStepsFactory.CreatePcapUnpackStep(pcap, prepState.keys.ToArray()));
				}
			}
			return Task.FromResult(0);
		}

		static bool IsPcap(IStreamHeader header)
		{
			if (header.Header.Length < 4)
				return false;
			var h = header.Header;
			// Magic number. See https://wiki.wireshark.org/Development/LibpcapFileFormat.
			return h[3] == 0xA1 && h[2] == 0xB2 && h[1] == 0xC3 && h[0] == 0xD4;
		}

		static bool IsKeys(IStreamHeader header)
		{
			var str = Encoding.ASCII.GetString(header.Header);
			return
				str.StartsWith("RSA Session-ID:") ||
				str.StartsWith("CLIENT_HANDSHAKE_TRAFFIC_SECRET ") ||
				str.StartsWith("SERVER_HANDSHAKE_TRAFFIC_SECRET ") ||
				str.StartsWith("CLIENT_TRAFFIC_SECRET_0 ") ||
				str.StartsWith("EXPORTER_SECRET ") ||
				str.StartsWith("CLIENT_RANDOM ");
		}

		class PreprocessingState
		{
			public readonly List<PreprocessingStepParams> pcaps = new List<PreprocessingStepParams>();
			public readonly List<PreprocessingStepParams> keys = new List<PreprocessingStepParams>();
		};

		class PreprocessingsState
		{
			public readonly ConcurrentDictionary<ILogSourcePreprocessing, PreprocessingState> preprocessings =
				new ConcurrentDictionary<ILogSourcePreprocessing, PreprocessingState>();
		};

		class SaveParamsStep : IPreprocessingStep
		{
			public PreprocessingsState state;
			public PreprocessingStepParams pcap;
			public PreprocessingStepParams key;

			Task IPreprocessingStep.Execute(IPreprocessingStepCallback callback)
			{
				var prepState = state.preprocessings.GetOrAdd(callback.Owner, _ => new PreprocessingState());
				if (pcap != null)
					prepState.pcaps.Add(pcap);
				else if (key != null)
					prepState.keys.Add(key);
				return Task.FromResult(0);
			}

			Task<PreprocessingStepParams> IPreprocessingStep.ExecuteLoadedStep(IPreprocessingStepCallback callback)
			{
				throw new NotImplementedException();
			}
		};
	};
}
