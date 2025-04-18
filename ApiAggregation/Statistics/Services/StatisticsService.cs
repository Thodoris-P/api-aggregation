using System.Collections.Concurrent;
using ApiAggregation.Configuration;
using ApiAggregation.Infrastructure.Abstractions;
using ApiAggregation.Statistics.Abstractions;
using ApiAggregation.Statistics.Models;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace ApiAggregation.Statistics.Services;

public class StatisticsService(HybridCache hybridCache, IDateTimeProvider dateTimeProvider, IOptions<StatisticsThresholds> thresholds) : IStatisticsService
{
    private readonly ConcurrentDictionary<string, ConcurrentQueue<ApiPerformanceRecord>> _requestRecords = new();
    private readonly StatisticsThresholds _thresholds = thresholds.Value;
    
    public async Task<Dictionary<PerformanceBucket, Dictionary<string, ApiStatistics>>> GetApiStatistics()
    {
        const string cacheKey = "stats_data";

        var cachedResponse = await hybridCache.GetOrCreateAsync(
            cacheKey, _ =>  ValueTask.FromResult(PerformStatisticsCalculations(_requestRecords)));
        return cachedResponse;
    }

    private Dictionary<PerformanceBucket, Dictionary<string, ApiStatistics>> PerformStatisticsCalculations(
        ConcurrentDictionary<string, ConcurrentQueue<ApiPerformanceRecord>> requestRecords)
    {
        var result = new Dictionary<PerformanceBucket, Dictionary<string, ApiStatistics>>
        {
            [PerformanceBucket.Fast] = [],
            [PerformanceBucket.Medium] = [],
            [PerformanceBucket.Slow] = []
        };
        
        foreach ((string? apiName, var times) in requestRecords)
        {
            // Utilize the snapshot pattern to avoid getting mixed results
            var snapshot = times.ToArray();
            if (snapshot.Length == 0) continue;
            
            double averageTime = snapshot.Average(r => r.ResponseTimeInMilliseconds);
            long minTime = snapshot.Min(r => r.ResponseTimeInMilliseconds);
            long maxTime = snapshot.Max(r => r.ResponseTimeInMilliseconds);
            
            var stats = new ApiStatistics
            {
                TotalRequests = snapshot.Length,
                AverageResponseTime = averageTime,
                MinResponseTime = minTime,
                MaxResponseTime = maxTime
            };

            var bucket = GetPerformanceBucket(averageTime);
            result[bucket].Add(apiName, stats);
        }
        
        return result;
    }

    private PerformanceBucket GetPerformanceBucket(double averageTime)
    {
        if (averageTime < _thresholds.FastUpperLimit)
        {
            return PerformanceBucket.Fast;
        }

        if (averageTime < _thresholds.MediumUpperLimit)
        {
            return PerformanceBucket.Medium;
        }

        return PerformanceBucket.Slow;
    }

    public void UpdateApiStatistics(string apiName, long elapsedMilliseconds)
    {
        // Store the request time in a thread-safe manner
        // only store minimum information (elapsed time) to avoid complex computations and locking for long times
        var queue = _requestRecords.GetOrAdd(apiName, _ => new ConcurrentQueue<ApiPerformanceRecord>());
        queue.Enqueue(new ApiPerformanceRecord
        {
            Timestamp = dateTimeProvider.UtcNow,
            ResponseTimeInMilliseconds = elapsedMilliseconds
        });
    }
    
    public void CleanupOldEntries(TimeSpan retentionPeriod)
    {
        var threshold = dateTimeProvider.UtcNow.Subtract(retentionPeriod);
    
        foreach (var queue in _requestRecords.Values)
        {
            // Keep removing items that are older than the threshold.
            while (queue.TryPeek(out var record) && record.Timestamp < threshold)
            {
                queue.TryDequeue(out _);
            }
        }
    }

    /// <summary>
    /// Returns all API names that have been tracked.
    /// </summary>
    /// <returns>A snapshot of all (if any) tracked APIs</returns>
    public IEnumerable<string> GetAllApiNames() => _requestRecords.Keys.ToArray();

    /// <summary>
    /// Get a snapshot of all performance records for a specific API since a given date.
    /// </summary>
    /// <param name="apiName"></param>
    /// <param name="since"></param>
    /// <returns>A snapshot of the data request or an empty collection if no data are present.</returns>
    public List<ApiPerformanceRecord> GetApiPerformanceRecords(string apiName, DateTime since)
    {
        return _requestRecords.TryGetValue(apiName, out var queue) ? queue.Where(record => record.Timestamp >= since).ToList() : [];
    }
}