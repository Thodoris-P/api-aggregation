using ApiAggregation.Statistics.Models;

namespace ApiAggregation.Statistics.Abstractions;

public interface IStatisticsService
{
    Task<Dictionary<PerformanceBucket, Dictionary<string, ApiStatistics>>> GetApiStatistics();
    void UpdateApiStatistics(string apiName, long elapsedMilliseconds);
    List<ApiPerformanceRecord> GetApiPerformanceRecords(string apiName, DateTime since);
    void CleanupOldEntries(TimeSpan retentionPeriod);
    IEnumerable<string> GetAllApiNames();
}