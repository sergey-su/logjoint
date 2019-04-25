using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Linq;
using LogJoint.RegularExpressions;
using System.Threading;
using LogFontSize = LogJoint.Settings.Appearance.LogFontSize;
using ColoringMode = LogJoint.Settings.Appearance.ColoringMode;
using System.Threading.Tasks;
using System.Collections.Immutable;

namespace LogJoint.UI.Presenters.LogViewer
{
	public static class Extensions
	{
		static (double, List<(int, int)>) FindRepaintRegions(IReadOnlyList<ViewLine> list1, IReadOnlyList<ViewLine> list2)
		{
			var LookingForFirstMatch = 0;
			var LookingForDiff = 1;
			var LookingForSecondMatch = 2;

			var state = LookingForFirstMatch;
			int idx1 = 0;
			int idx2 = 0;
			while (idx1 < list1.Count && idx2 < list2.Count)
			{
				var cmp = ViewLine.Compare(list1[idx1], list2[idx2]);
				if (state == LookingForFirstMatch)
				{
					if (cmp == (0, false))
					{
						state = LookingForDiff;
						// todo: compute scroll dist AND optional first repaint region
					}
				}
				else if (state == LookingForDiff)
				{
					if (cmp.relativeOrder != 0)
					{
						// todo: repaint rest of messages
						break;
					}
					if (cmp.changed)
					{
						state = LookingForSecondMatch;
					}
				}
				else if (state == LookingForSecondMatch)
				{
					if (cmp.relativeOrder != 0)
					{
						// todo: repaint rest of messages
						break;
					}

				}

				if (cmp.relativeOrder >= 0)
					++idx1;
				if (cmp.relativeOrder <= 0)
					++idx2;
			}
			// 

			return (0, null);
		}
	};
};