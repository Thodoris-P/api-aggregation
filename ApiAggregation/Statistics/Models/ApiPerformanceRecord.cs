namespace ApiAggregation.Statistics.Models;

public class ApiPerformanceRecord
{
    public DateTime Timestamp { get; set; }
    public long ResponseTimeInMilliseconds { get; set; }
}