using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Xml.Linq;

namespace LogJoint.UI.Presenters.Postprocessing.Common
{
	public interface IPostprocessorTags
	{
		ImmutableHashSet<string> AvailableTags { get; }
		TagsPredicate TagsPredicate { get; set; }
	};

	public class PresenterPersistentState: IPostprocessorTags
	{
		readonly IChangeNotification changeNotification;
		readonly Persistence.IStorageEntry stateEntry;
		readonly string logSourceStateSectionName;
		readonly Func<ImmutableHashSet<string>> availableTagsSelector;
		readonly Func<IEnumerable<ILogSource>> sourcesSelector;
		readonly Func<TagsPredicate> getTagsPredicate;
		readonly Func<TagsPredicate> getDefaultPredicate;
		int savedTagsRevision;

		public PresenterPersistentState(
			Persistence.IStorageManager storageManager,
			string globalStateStorageEntryName,
			string logSourceSpecificStateSectionName,
			IChangeNotification changeNotification,
			Func<ImmutableHashSet<string>> availableTagsSelector,
			Func<IEnumerable<ILogSource>> sourcesSelector
		)
		{
			this.changeNotification = changeNotification;
			this.stateEntry = storageManager.GetEntry(globalStateStorageEntryName);
			this.logSourceStateSectionName = logSourceSpecificStateSectionName;
			this.availableTagsSelector = CreateAvailableTagsSelectorAdapter(availableTagsSelector);
			this.sourcesSelector = sourcesSelector;

			this.getDefaultPredicate = Selectors.Create(
				this.availableTagsSelector,
				availableTags => TagsPredicate.MakeMatchAnyPredicate(availableTags)
			);

			this.getTagsPredicate = Selectors.Create(
				getDefaultPredicate,
				sourcesSelector,
				() => savedTagsRevision,
				(defaultPredicate, sources, _revision) => LoadPredicate(sources, defaultPredicate, logSourceStateSectionName)
			);
		}

		public ImmutableHashSet<string> AvailableTags => availableTagsSelector();

		public TagsPredicate TagsPredicate
		{
			get => getTagsPredicate();
			set => SetPredicate(value);
		}

		private static TagsPredicate TryLoadPredicate(Persistence.IStorageEntry entry, string sectionName)
		{
			using (var section = entry.OpenXMLSection(sectionName, Persistence.StorageSectionOpenFlag.ReadOnly))
			{
				var tagsElt = section.Data.SafeElement(Constants.StateRootEltName).SafeElement(Constants.TagsEltName);
				var strValue = tagsElt?.Nodes()?.OfType<XText>()?.FirstOrDefault()?.Value;
				if (strValue != null && TagsPredicate.TryParse(strValue, out var predicate))
					return predicate;
			}
			return null;
		}

		static private TagsPredicate LoadPredicate(IEnumerable<ILogSource> sources, TagsPredicate defaultPredicate, string logSourceStateSectionName)
		{
			return TagsPredicate.Combine(
				sources
				.Select(s => s.LogSourceSpecificStorageEntry)
				.Select(entry => TryLoadPredicate(entry, logSourceStateSectionName) ?? defaultPredicate)
			);
		}

		private void SetPredicate(TagsPredicate value)
		{
			void savePredicate(Persistence.IStorageEntry entry, string sectionName)
			{
				using (var section = entry.OpenXMLSection(sectionName,
					Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen | Persistence.StorageSectionOpenFlag.IgnoreStorageExceptions))
				{
					section.Data.Add(new XElement(
						Constants.StateRootEltName,
						new XElement(Constants.TagsEltName, value.ToString()))
					);
				}
			}

			foreach (var entry in sourcesSelector().Select(s => s.LogSourceSpecificStorageEntry))
				savePredicate(entry, logSourceStateSectionName);

			++savedTagsRevision;
			changeNotification.Post();
		}

		static Func<ImmutableHashSet<string>> CreateAvailableTagsSelectorAdapter(Func<ImmutableHashSet<string>> inner)
		{
			ImmutableHashSet<string> cachedResult = null;
			ImmutableHashSet<string> prevInner = null;
			return () =>
			{
				var tmp = inner();
				if (tmp != prevInner)
				{
					if (cachedResult?.SetEquals(tmp) != true)
						cachedResult = tmp;
					prevInner = tmp;
				}
				return cachedResult;
			};
		}

		static class Constants
		{
			public const string StateRootEltName = "root";
			public const string TagsEltName = "tags";
		};
	};
}
