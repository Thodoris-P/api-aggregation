namespace ApiAggregation.Statistics.Models;

public class StatisticsCleanupOptions
{
    // Interval at which the cleanup job runs.
    public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);

    // How long records are retained before they are considered stale.
    public TimeSpan RetentionPeriod { get; set; } = TimeSpan.FromMinutes(10);
}