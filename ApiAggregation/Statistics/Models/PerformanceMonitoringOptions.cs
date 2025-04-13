namespace ApiAggregation.Statistics.Models;

public class PerformanceMonitoringOptions
{
    // The time window over which performance statistics are aggregated (e.g., last 5 minutes).
    public TimeSpan AnalysisPeriod { get; set; }

    // The frequency at which the performance analysis is executed.
    public TimeSpan CheckInterval { get; set; }
}