namespace ApiAggregation.Statistics.Models;

public class ApiStatistics
{
    public double AverageResponseTime { get; init; }
    public long MinResponseTime { get; init; }
    public long MaxResponseTime { get; init; }
    public int TotalRequests { get; init; }
}