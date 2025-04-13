namespace ApiAggregation.Configuration;

public class StatisticsCleanupOptions
{
    // Interval at which the cleanup job runs.
    public TimeSpan CleanupInterval { get; init; }

    // How long records are retained before they are considered stale.
    public TimeSpan RetentionPeriod { get; init; }
}