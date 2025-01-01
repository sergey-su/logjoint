﻿using System;

namespace LogJoint.Settings
{
    public class DefaultSettingsAccessor : IGlobalSettingsAccessor
    {
        public const int DefaultMaxSearchResultSize = 50000;
        public const bool DefaultMultithreadedParsingDisabled = false;
        public const bool DefaultEnableAutoPostprocessing = false;

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

        StorageSizes IGlobalSettingsAccessor.UserDataStorageSizes
        {
            get { return StorageSizes.Default; }
            set { throw new NotImplementedException(); }
        }

        StorageSizes IGlobalSettingsAccessor.ContentStorageSizes
        {
            get { return StorageSizes.Default; }
            set { throw new NotImplementedException(); }
        }

        bool IGlobalSettingsAccessor.EnableAutoPostprocessing
        {
            get { return DefaultEnableAutoPostprocessing; }
            set { throw new NotImplementedException(); }
        }
    }
}
