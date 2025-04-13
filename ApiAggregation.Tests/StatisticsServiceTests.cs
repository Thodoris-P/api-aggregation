using ApiAggregation.Statistics;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;
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
    private readonly FakeDateTimeProvider _dateTimeProvider;
    private const string ApiName = "TestApi";
    private const string FastApi = "FastApi";
    private const string MediumApi = "MediumApi";
    private const string SlowApi = "SlowApi";
    private const long MinElapsedTime = 50;
    private const long MidElapsedTime = 70;
    private const long MaxElapsedTime = 90;
    private const double AverageElapsedTime = ((double)MinElapsedTime + MaxElapsedTime + MidElapsedTime) / 3;
    private const double DefaultTolerance = 0.001;
    private const double FastUpperLimit = 100;
    private const double MediumUpperLimit = 200;

    public StatisticsServiceTests()
    {
        var fixedTime = new DateTime(2025, 01, 01, 12, 00, 00, DateTimeKind.Utc);
        _dateTimeProvider = new FakeDateTimeProvider(fixedTime);
        var thresholds = new StatisticsThresholds
        {
            FastUpperLimit = FastUpperLimit,
            MediumUpperLimit = MediumUpperLimit,
        };
        var options = Options.Create(thresholds);
        _statisticsService = new StatisticsService(new FakeHybridCache(), _dateTimeProvider, options);
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
        stats.ShouldContainKey(PerformanceBucket.Fast);
        stats[PerformanceBucket.Fast].ShouldContainKey(ApiName);

        stats[PerformanceBucket.Fast][ApiName].TotalRequests.ShouldBe(3);
        stats[PerformanceBucket.Fast][ApiName].AverageResponseTime.ShouldBe(AverageElapsedTime, DefaultTolerance);
        stats[PerformanceBucket.Fast][ApiName].MinResponseTime.ShouldBe(MinElapsedTime);
        stats[PerformanceBucket.Fast][ApiName].MaxResponseTime.ShouldBe(MaxElapsedTime);
    }

    [Fact]
    public void UpdateApiStatistics_ShouldAddRecord()
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
        _statisticsService.UpdateApiStatistics(ApiName, 150);
        _dateTimeProvider.Advance(TimeSpan.FromMinutes(10));
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
        _statisticsService.UpdateApiStatistics(ApiName, 100);
        _dateTimeProvider.Advance(TimeSpan.FromDays(2));

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
        stats[PerformanceBucket.Fast].ShouldContainKey(FastApi);
        // MediumApi should be in Medium bucket.
        stats[PerformanceBucket.Medium].ShouldContainKey(MediumApi);
        // SlowApi should be in Slow bucket.
        stats[PerformanceBucket.Slow].ShouldContainKey(SlowApi);
    }
}
