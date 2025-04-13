namespace ApiAggregation.Statistics.Models;

public class ApiPerformanceRecord
{
    public DateTime Timestamp { get; init; }
    public long ResponseTimeInMilliseconds { get; init; }
}