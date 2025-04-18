namespace ApiAggregation.Configuration;

public class PerformanceMonitoringOptions
{
    // The time window over which performance statistics are aggregated (e.g., last 5 minutes).
    public TimeSpan AnalysisPeriod { get; init; }

    // The frequency at which the performance analysis is executed.
    public TimeSpan CheckInterval { get; init; }
}