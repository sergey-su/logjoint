using NUnit.Framework;
using NSubstitute;
using LogJoint.RegularExpressions;
using LogJoint.Settings;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;
using LogJoint.Progress;

namespace LogJoint.Tests
{
    [TestFixture]
    public class FilteringMessagesReaderTest
    {
        IFiltersFactory filtersFactory;
        IFiltersList filters;
        IMessagesReader reader;

        static string[] ToLog(IEnumerable<IMessage> messages)
        {
            return messages.Select(m => $"{m.Position}: {m.Text} {m.RawText}").ToArray();
        }

        async ValueTask<string[]> ReadToLog(ReadMessagesParams p)
        {
            return ToLog(await reader.Read(p).Select(m => m.Message).ToArrayAsync());
        }

        async ValueTask<string[]> SearchToLog(SearchMessagesParams p)
        {
            return ToLog(await reader.Search(p).Select(m => m.Message).ToArrayAsync());
        }

        [SetUp]
        public async Task Init()
        {
            filtersFactory = new FiltersFactory(Substitute.For<IChangeNotification>(), FCLRegexFactory.Instance);
            filters = filtersFactory.CreateFiltersList(FilterAction.Include, FiltersListPurpose.Display);
            ISynchronizationContext synchronizationContext = new SerialSynchronizationContext();
            IChangeNotification changeNotification = new ChangeNotification(synchronizationContext);
            reader = new FilteringMessagesReader(new FakeMessagesReader([0, 10, 20, 30]),
                new MediaBasedReaderParams(new LogSourceThreads(), new StringStreamMedia()), filters, new TempFilesManager(),
                LogMedia.FileSystemImpl.Instance, FCLRegexFactory.Instance,
                new TraceSourceFactory(), DefaultSettingsAccessor.Instance, synchronizationContext,
                new FilteringStats(new ProgressAggregator.Factory(synchronizationContext)));
            await reader.UpdateAvailableBounds(incrementalMode: false);
        }

        [Test]
        public async Task ProducesUnfilteredLogWhenFilteringIsDisabled()
        {
            filters.FilteringEnabled = false;
            await reader.UpdateAvailableBounds(incrementalMode: true);
            Assert.That(await ReadToLog(new ReadMessagesParams()),
                Is.EqualTo(["0: 0 0.r", "10: 10 10.r", "20: 20 20.r", "30: 30 30.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams() { StartPosition = 15 }),
                Is.EqualTo(["20: 20 20.r", "30: 30 30.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams()
                { StartPosition = 15, Direction = ReadMessagesDirection.Backward }),
                Is.EqualTo(["0: 0 0.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams()
                { StartPosition = 20, Direction = ReadMessagesDirection.Backward }),
                Is.EqualTo(["10: 10 10.r", "0: 0 0.r"]));
        }

        [Test]
        public async Task ProducesUnfilteredLogWhenFilteringIsEnabledWithAllMatchingFilter()
        {
            filters.FilteringEnabled = true;
            filters.Insert(0, filtersFactory.CreateFilter(FilterAction.Include, "", enabled: true, new Search.Options()
            {
                Template = ".",
                Regexp = true,
            }, new FilterTimeRange(null, null)));
            await reader.UpdateAvailableBounds(incrementalMode: true);
            Assert.That(await ReadToLog(new ReadMessagesParams()),
                Is.EqualTo(["0: 0 0.r", "10: 10 10.r", "20: 20 20.r", "30: 30 30.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams() {
                    StartPosition = 15, Direction = ReadMessagesDirection.Backward }),
                Is.EqualTo(["0: 0 0.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams()
                { StartPosition = 20, Direction = ReadMessagesDirection.Backward }),
                Is.EqualTo(["10: 10 10.r", "0: 0 0.r"]));
        }

        [Test]
        public async Task ReadsFilteredLog()
        {
            filters.Insert(0, filtersFactory.CreateFilter(FilterAction.Exclude, "", enabled: true, new Search.Options()
            {
                Template = "20"
            }, new FilterTimeRange(null, null)));
            await reader.UpdateAvailableBounds(incrementalMode: true);
            Assert.That(await ReadToLog(new ReadMessagesParams()),
                Is.EqualTo(["0: 0 0.r", "10: 10 10.r", "30: 30 30.r"]));

            Assert.That(await ReadToLog(new ReadMessagesParams() { StartPosition = 10 }),
                Is.EqualTo(["10: 10 10.r", "30: 30 30.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams() { StartPosition = 20 }),
                Is.EqualTo(["30: 30 30.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams()
                { StartPosition = 15, Direction = ReadMessagesDirection.Backward }),
                Is.EqualTo(["0: 0 0.r"]));
            Assert.That(await ReadToLog(new ReadMessagesParams()
                { StartPosition = 30, Direction = ReadMessagesDirection.Backward }),
                Is.EqualTo(["10: 10 10.r", "0: 0 0.r"]));
        }

        [Test]
        public async Task SearchesFilteredLog()
        {
            filters.Insert(0, filtersFactory.CreateFilter(FilterAction.Exclude, "", enabled: true, new Search.Options()
            {
                Template = "20"
            }, new FilterTimeRange(null, null)));
            await reader.UpdateAvailableBounds(incrementalMode: true);

            IFiltersList searchFilter = filtersFactory.CreateFiltersList(FilterAction.Include, FiltersListPurpose.Search);
            searchFilter.Insert(0, filtersFactory.CreateFilter(FilterAction.Exclude, "", enabled: true, new Search.Options()
            {
                Template = "10"
            }, new FilterTimeRange(null, null)));
            Assert.That(await SearchToLog(new SearchMessagesParams() 
                { Range = new FileRange.Range(0, 100), SearchParams = new SearchAllOccurencesParams(searchFilter, searchInRawText: false, null) }),
                Is.EqualTo(["0: 0 0.r", "30: 30 30.r"]));

            Assert.That(await SearchToLog(new SearchMessagesParams()
                { Range = new FileRange.Range(5, 100), SearchParams = new SearchAllOccurencesParams(searchFilter, searchInRawText: false, null) }),
                Is.EqualTo(["30: 30 30.r"]));
        }

    }
}
