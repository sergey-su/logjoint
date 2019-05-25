using LogJoint.Postprocessing.TimeSeries;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LogJoint.UI.Presenters.Postprocessing.TimeSeriesVisualizer
{
	public interface IPresenter
	{
		void OpenConfigDialog();
		bool SelectConfigNode(Predicate<TreeNodeData> predicate);
		bool ConfigNodeExists(Predicate<TreeNodeData> predicate);
	};

	public enum ConfigDialogNodeType
	{
		Log,
		ObjectTypeGroup,
		ObjectIdGroup,
		TimeSeries,
		Events
	};

	public class TreeNodeData
	{
		public ConfigDialogNodeType Type { get; internal set; }
		public string Caption { get; internal set; }
		public int? Counter { get; internal set; }
		public bool Checkable { get; internal set; }
		public IEnumerable<TreeNodeData> Children { get; internal set; }
		public ITimeSeriesPostprocessorOutput Owner { get { return output; } }

		internal ITimeSeriesPostprocessorOutput output;
		internal TimeSeriesData ts;
		internal EventBase evt;
	};
}