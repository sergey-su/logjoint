using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
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
		readonly string logSourceStateSectionName;
		readonly Func<ImmutableHashSet<string>> availableTagsSelector;
		readonly Func<IEnumerable<ILogSource>> sourcesSelector;
		readonly Func<TagsPredicate> getTagsPredicate;
		readonly Func<TagsPredicate> getDefaultPredicate;
		ImmutableDictionary<ILogSource, Task<XDocument>> cache = ImmutableDictionary<ILogSource, Task<XDocument>>.Empty;
		int cacheTaskCompletionRevision = 0;

		public PresenterPersistentState(
			string logSourceSpecificStateSectionName,
			IChangeNotification changeNotification,
			Func<ImmutableHashSet<string>> availableTagsSelector,
			Func<IEnumerable<ILogSource>> sourcesSelector
		)
		{
			this.changeNotification = changeNotification;
			this.logSourceStateSectionName = logSourceSpecificStateSectionName;
			this.availableTagsSelector = CreateAvailableTagsSelectorAdapter(availableTagsSelector);
			this.sourcesSelector = sourcesSelector;

			this.getDefaultPredicate = Selectors.Create(
				this.availableTagsSelector,
				availableTags => TagsPredicate.MakeMatchAnyPredicate(availableTags)
			);

			var getCache = Selectors.Create(sourcesSelector, () => cacheTaskCompletionRevision, MaybeUpdateAndGetCache);

			var getCachedDocuments = Selectors.Create(getCache, () => cacheTaskCompletionRevision,
				(cache, _) => cache
				.Where(i => i.Value.IsCompleted && !i.Value.IsFaulted)
				.Select(i => new KeyValuePair<ILogSource, XDocument>(i.Key, i.Value.Result))
				.ToImmutableDictionary());

			this.getTagsPredicate = Selectors.Create(
				getDefaultPredicate,
				sourcesSelector,
				getCachedDocuments,
				(defaultPredicate, sources, cache) => LoadPredicate(sources, cache, defaultPredicate)
			);
		}

		public ImmutableHashSet<string> AvailableTags => availableTagsSelector();

		public TagsPredicate TagsPredicate
		{
			get => getTagsPredicate();
			set => SetPredicate(value);
		}

		private static TagsPredicate TryLoadPredicate(ILogSource source,
			IReadOnlyDictionary<ILogSource, XDocument> cachedDocuments)
		{
			if (cachedDocuments.TryGetValue(source, out var data))
			{
				var tagsElt = data.SafeElement(Constants.StateRootEltName).SafeElement(Constants.TagsEltName);
				var strValue = tagsElt?.Nodes()?.OfType<XText>()?.FirstOrDefault()?.Value;
				if (strValue != null && TagsPredicate.TryParse(strValue, out var predicate))
					return predicate;
			}
			return null;
		}

		static private TagsPredicate LoadPredicate(IEnumerable<ILogSource> sources,
			IReadOnlyDictionary<ILogSource, XDocument> cachedDocuments,
			TagsPredicate defaultPredicate)
		{
			return TagsPredicate.Combine(
				sources
				.Select(s => TryLoadPredicate(s, cachedDocuments) ?? defaultPredicate)
			);
		}

		IReadOnlyDictionary<ILogSource, Task<XDocument>> MaybeUpdateAndGetCache(IEnumerable<ILogSource> sources,
			int ignoredCacheTaskCompletionRevision)
        {
			var staleSources = cache.Keys.ToHashSet();
			foreach (var source in sources)
            {
				if (!staleSources.Remove(source))
                {
					async Task<XDocument> Query()
                    {
                        await using var section = await source.LogSourceSpecificStorageEntry.OpenXMLSection(
                                logSourceStateSectionName, Persistence.StorageSectionOpenFlag.ReadOnly);
                        ++cacheTaskCompletionRevision;
                        changeNotification.Post();
                        return section.Data;
                    }
					cache = cache.Add(source, Query());
				}
            }
			foreach (ILogSource staleSource in staleSources)
			{
				cache = cache.Remove(staleSource);
			}
			return cache;
        }

		private void SetPredicate(TagsPredicate value)
		{
			async void SavePredicate(ILogSource source, XDocument doc)
			{
                await using var section = await source.LogSourceSpecificStorageEntry.OpenXMLSection(logSourceStateSectionName,
                    Persistence.StorageSectionOpenFlag.ReadWrite | Persistence.StorageSectionOpenFlag.ClearOnOpen | Persistence.StorageSectionOpenFlag.IgnoreStorageExceptions);
                section.Data.ReplaceNodes(doc.Nodes());
            }

			foreach (var source in sourcesSelector())
            {
				if (cache.TryGetValue(source, out var task))
                {
					var doc = new XDocument();
					doc.Add(new XElement(
						Constants.StateRootEltName,
						new XElement(Constants.TagsEltName, value.ToString()))
					);
					cache = cache.SetItem(source, Task.FromResult(doc));
					SavePredicate(source, new XDocument(doc));
					cacheTaskCompletionRevision++;
				}
            }

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
