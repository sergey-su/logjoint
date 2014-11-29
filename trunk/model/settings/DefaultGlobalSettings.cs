using System;

namespace LogJoint.Settings
{
	public class DefaultSettingsAccessor : IGlobalSettingsAccessor
	{
		public const int DefaultMaxSearchResultSize = 50000;
		public const bool DefaultMultithreadedParsingDisabled = false;

		public static readonly IGlobalSettingsAccessor Instance = new DefaultSettingsAccessor();

		FileSizes IGlobalSettingsAccessor.FileSizes
		{
			get { return FileSizes.Default; }
			set { throw new NotImplementedException(); }
		}

		int IGlobalSettingsAccessor.MaxNumberOfHitsInSearchResultsView
		{
			get { return DefaultMaxSearchResultSize; }
			set { throw new NotImplementedException(); }
		}

		bool IGlobalSettingsAccessor.MultithreadedParsingDisabled
		{
			get { return DefaultMultithreadedParsingDisabled; }
			set { throw new NotImplementedException(); }
		}

		Appearance IGlobalSettingsAccessor.Appearance
		{
			get { return Appearance.Default; }
			set { throw new NotImplementedException(); }
		}


		event EventHandler<SettingsChangeEvent> IGlobalSettingsAccessor.Changed
		{
			add {}
			remove {}
		}
	}
}
