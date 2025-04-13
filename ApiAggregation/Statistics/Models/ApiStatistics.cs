namespace ApiAggregation.Statistics.Models;

public class ApiStatistics
{
    public double AverageResponseTime { get; set; }
    public long MinResponseTime { get; set; }
    public long MaxResponseTime { get; set; }
    public int TotalRequests { get; set; }
}