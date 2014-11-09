using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace LogJoint.Settings
{
	public interface IGlobalSettingsAccessor
	{
		FileSizes FileSizes { get; set; }
		int MaxNumberOfHitsInSearchResultsView { get; set; }
		bool MultithreadedParsingDisabled { get; set; }
	}

	public struct FileSizes
	{
		public int Threshold;
		public const int MaxThreshold = 80;
		
		public int WindowSize;
		public const int MinWindowSize = 1;
		public const int MaxWindowSize = 8;

		static public readonly FileSizes Default = new FileSizes() { Threshold = 30, WindowSize = 4 };
	};
}
