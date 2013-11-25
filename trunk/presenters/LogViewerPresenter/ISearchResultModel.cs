using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;

namespace LogJoint.UI.Presenters.LogViewer
{
	public interface ISearchResultModel : IModel
	{
		SearchAllOccurencesParams SearchParams { get; }
	};
};