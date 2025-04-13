using ApiAggregation.Configuration;
using ApiAggregation.Statistics;
using ApiAggregation.Statistics.Models;
using ApiAggregation.Statistics.Services;
using ApiAggregation.UnitTests.Fakes;
using Bogus;
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
    private readonly Faker _faker;
    private readonly StatisticsThresholds _thresholds;
    
    public StatisticsServiceTests()
    {
        var fixedTime = new DateTime(2025, 01, 01, 12, 00, 00, DateTimeKind.Utc);
        _dateTimeProvider = new FakeDateTimeProvider(fixedTime);
        _thresholds = new StatisticsThresholds
        {
            FastUpperLimit = FastUpperLimit,
            MediumUpperLimit = MediumUpperLimit,
        };
        var options = Options.Create(_thresholds);
        _statisticsService = new StatisticsService(new FakeHybridCache(), _dateTimeProvider, options);
        _faker = new Faker();
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
        int responseTime = _faker.Random.Int(1, 1000);

        // Act
        _statisticsService.UpdateApiStatistics(ApiName, responseTime);
        // Get performance records within the last minute.
        var records = _statisticsService.GetApiPerformanceRecords(ApiName, _dateTimeProvider.UtcNow.AddMinutes(-1));

        // Assert
        records.Count.ShouldBe(1);
        records[0].ResponseTimeInMilliseconds.ShouldBe(responseTime);
    }

    [Fact]
    public void GetAllApiNames_ReturnsAllTrackedApiNames()
    {
        // Arrange
        int responseTime = _faker.Random.Int(1, 1000);
        _statisticsService.UpdateApiStatistics(ApiName, responseTime);
        _statisticsService.UpdateApiStatistics(FastApi, responseTime);

        // Act
        var apiNames = _statisticsService.GetAllApiNames().ToList();

        // Assert
        apiNames.ShouldContain(ApiName);
        apiNames.ShouldContain(FastApi);
    }

    [Fact]
    public void GetApiPerformanceRecords_FiltersRecordsBasedOnTimestamp()
    {
        // Arrange
        _statisticsService.UpdateApiStatistics(ApiName, MinElapsedTime);
        _dateTimeProvider.Advance(TimeSpan.FromMinutes(10));
        _statisticsService.UpdateApiStatistics(ApiName, MaxElapsedTime);

        // Act: get records since 5 minutes ago
        var recentRecords = _statisticsService.GetApiPerformanceRecords(ApiName, _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromMinutes(5)));

        // Assert: Only the recently added record should be returned.
        recentRecords.Count.ShouldBe(1);
        recentRecords[0].ResponseTimeInMilliseconds.ShouldBe(MaxElapsedTime);
    }

    [Fact]
    public void CleanupOldEntries_RemovesRecordsOlderThanRetentionPeriod()
    {
        // Arrange
        _statisticsService.UpdateApiStatistics(ApiName, MinElapsedTime);
        _dateTimeProvider.Advance(TimeSpan.FromDays(2));

        // Act: Cleanup entries older than 1 day.
        _statisticsService.CleanupOldEntries(TimeSpan.FromDays(1));

        // Assert: There should be no records remaining.
        var recordsAfterCleanup = _statisticsService.GetApiPerformanceRecords(ApiName, _dateTimeProvider.UtcNow.Subtract(TimeSpan.FromDays(3)));
        recordsAfterCleanup.Count.ShouldBe(0);
    }

    [Fact]
    public async Task GetApiStatistics_AssignsCorrectPerformanceBucket()
    {
        //Arrange
        // For Fast (< 100ms)
        _statisticsService.UpdateApiStatistics(FastApi, _faker.Random.Long(1, (int)_thresholds.FastUpperLimit - 1));
        _statisticsService.UpdateApiStatistics(FastApi, _faker.Random.Long(1, (int)_thresholds.FastUpperLimit - 1));
        // For Medium (>=100ms and <200ms)
        _statisticsService.UpdateApiStatistics(MediumApi, _faker.Random.Long((int)_thresholds.FastUpperLimit, (int)_thresholds.MediumUpperLimit - 1));
        _statisticsService.UpdateApiStatistics(MediumApi, _faker.Random.Long((int)_thresholds.FastUpperLimit, (int)_thresholds.MediumUpperLimit - 1));
        // For Slow (>=200ms)
        _statisticsService.UpdateApiStatistics(SlowApi, _faker.Random.Long((int)_thresholds.MediumUpperLimit, int.MaxValue));
        _statisticsService.UpdateApiStatistics(SlowApi, _faker.Random.Long((int)_thresholds.MediumUpperLimit, int.MaxValue));
        
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
