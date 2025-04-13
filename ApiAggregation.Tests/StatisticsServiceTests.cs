using ApiAggregation.Statistics;
using Microsoft.Extensions.Caching.Hybrid;
using Moq;
using System.Collections.Concurrent;
using System.Reflection;
using Shouldly;

namespace ApiAggregation.UnitTests;

internal sealed class FakeHybridCache : HybridCache
{
    public override ValueTask<T> GetOrCreateAsync<TState, T>(string key, TState state, Func<TState, CancellationToken, ValueTask<T>> factory,
        HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null, CancellationToken cancellationToken = default)
        => factory(state, cancellationToken);

    public override ValueTask RemoveAsync(string key, CancellationToken cancellationToken = default) => default;
    public override ValueTask RemoveByTagAsync(string tag, CancellationToken cancellationToken = default) => default;
    public override ValueTask SetAsync<T>(string key, T value, HybridCacheEntryOptions? options = null, IEnumerable<string>? tags = null,
        CancellationToken cancellationToken = default) => default;
}

public class StatisticsServiceTests
{
    private readonly StatisticsService _statisticsService;
    private readonly IDateTimeProvider _dateTimeProvider;
    private const string ApiName = "TestApi";
    private const string FastApi = "FastApi";
    private const string MediumApi = "MediumApi";
    private const string SlowApi = "SlowApi";
    private const long MinElapsedTime = 50;
    private const long MidElapsedTime = 70;
    private const long MaxElapsedTime = 90;
    private const double AverageElapsedTime = (MinElapsedTime + MaxElapsedTime + MidElapsedTime) / 3;
    private const double DefaultTolerance = 0.001;

    public StatisticsServiceTests()
    {
        var mockDateTimeProvider = new Mock<IDateTimeProvider>();
        var fixedTime = new DateTime(2025, 01, 01, 12, 00, 00, DateTimeKind.Utc);
        mockDateTimeProvider.Setup(provider => provider.UtcNow).Returns(fixedTime);
        _dateTimeProvider = mockDateTimeProvider.Object;
        _statisticsService = new StatisticsService(new FakeHybridCache(), _dateTimeProvider);
    }
        
    
    [Fact]
    public async Task GetApiStatistics_ReturnsCorrectComputedStatistics()
    {
        // Arrange
        _statisticsService.UpdateApiStatistics(ApiName, MinElapsedTime);
        _statisticsService.UpdateApiStatistics(ApiName, MidElapsedTime);
        _statisticsService.UpdateApiStatistics(ApiName, MaxElapsedTime);

        // Act
        var stats = await _statisticsService.GetApiStatistics();

        // Assert: since the average is 70, the bucket is "Fast" (< 100ms)
        stats.ShouldContainKey("Fast");
        stats["Fast"].ShouldContainKey(ApiName);

        stats["Fast"][ApiName].TotalRequests.ShouldBe(3);
        stats["Fast"][ApiName].AverageResponseTime.ShouldBe(AverageElapsedTime, DefaultTolerance);
        stats["Fast"][ApiName].MinResponseTime.ShouldBe(MinElapsedTime);
        stats["Fast"][ApiName].MaxResponseTime.ShouldBe(MaxElapsedTime);
    }

    [Fact]
    public void UpdateApiStatistics_AddsRecordAndIsRetrievable()
    {
        // Arrange

        // Act
        _statisticsService.UpdateApiStatistics(ApiName, 120);
        // Get performance records within the last minute.
        var records = _statisticsService.GetApiPerformanceRecords(ApiName, _dateTimeProvider.UtcNow.AddMinutes(-1));

        // Assert
        records.Count.ShouldBe(1);
        records[0].ResponseTimeInMilliseconds.ShouldBe(120);
    }

    [Fact]
    public void GetAllApiNames_ReturnsAllTrackedApiNames()
    {
        // Arrange
        _statisticsService.UpdateApiStatistics("Api1", 10);
        _statisticsService.UpdateApiStatistics("Api2", 20);

        // Act
        var apiNames = _statisticsService.GetAllApiNames().ToList();

        // Assert
        apiNames.ShouldContain("Api1");
        apiNames.ShouldContain("Api2");
    }

    [Fact]
    public void GetApiPerformanceRecords_FiltersRecordsBasedOnTimestamp()
    {
        // Arrange

        // Add a record that is current (should be included)
        _statisticsService.UpdateApiStatistics(ApiName, 150);

        // Use reflection to adjust timestamp for existing records.
        // (Normally the service stamps DateTime.UtcNow so we simulate an older entry.)
        var field = typeof(StatisticsService)
            .GetField("_requestRecords", BindingFlags.NonPublic | BindingFlags.Instance);
        var requestRecords = (ConcurrentDictionary<string, ConcurrentQueue<ApiPerformanceRecord>>) field.GetValue(_statisticsService);
        if (requestRecords.TryGetValue(ApiName, out var queue))
        {
            // Modify each record's timestamp to simulate them being older than 10 minutes.
            foreach (var record in queue.ToArray())
            {
                record.Timestamp = _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromMinutes(10));
            }
        }

        // Add another record that is recent.
        _statisticsService.UpdateApiStatistics(ApiName, 200);

        // Act: get records since 5 minutes ago
        var recentRecords = _statisticsService.GetApiPerformanceRecords(ApiName, _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        // Assert: Only the recently added record should be returned.
        recentRecords.Count.ShouldBe(1);
        recentRecords[0].ResponseTimeInMilliseconds.ShouldBe(200);
    }

    [Fact]
    public void CleanupOldEntries_RemovesRecordsOlderThanRetentionPeriod()
    {
        // Arrange
        // Add a record that will later be marked as old.
        _statisticsService.UpdateApiStatistics(ApiName, 100);

        // Access the private field _requestRecords via reflection.
        var field = typeof(StatisticsService)
            .GetField("_requestRecords", BindingFlags.NonPublic | BindingFlags.Instance);
        var requestRecords = (ConcurrentDictionary<string, ConcurrentQueue<ApiPerformanceRecord>>) field.GetValue(_statisticsService);

        // Modify the timestamp of all records for this API to be 2 days old.
        if (requestRecords.TryGetValue(ApiName, out var queue))
        {
            // To modify, dequeue and re-enqueue with updated timestamp.
            var tempList = queue.ToList();
            queue.Clear();
            foreach (var rec in tempList)
            {
                rec.Timestamp = _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromDays(2));
                queue.Enqueue(rec);
            }
        }

        // Act: Cleanup entries older than 1 day.
        _statisticsService.CleanupOldEntries(TimeSpan.FromDays(1));

        var recordsAfterCleanup = _statisticsService.GetApiPerformanceRecords(ApiName, _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromDays(3)));

        // Assert: There should be no records remaining.
        recordsAfterCleanup.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetApiStatistics_AssignsCorrectPerformanceBucket()
    {
        //Arrange
        // For Fast (< 100ms)
        _statisticsService.UpdateApiStatistics(FastApi, 80);
        _statisticsService.UpdateApiStatistics(FastApi, 90);

        // For Medium (>=100ms and <200ms)
        _statisticsService.UpdateApiStatistics(MediumApi, 120);
        _statisticsService.UpdateApiStatistics(MediumApi, 180);

        // For Slow (>=200ms)
        _statisticsService.UpdateApiStatistics(SlowApi, 250);
        _statisticsService.UpdateApiStatistics(SlowApi, 300);

        // Act
        var stats = await _statisticsService.GetApiStatistics();

        // Assert
        // FastApi should be in Fast bucket.
        stats["Fast"].ShouldContainKey(FastApi);
        // MediumApi should be in Medium bucket.
        stats["Medium"].ShouldContainKey(MediumApi);
        // SlowApi should be in Slow bucket.
        stats["Slow"].ShouldContainKey(SlowApi);
    }
}
