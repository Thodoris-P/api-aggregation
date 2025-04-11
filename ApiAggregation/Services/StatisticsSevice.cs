using System.Collections.Concurrent;

namespace ApiAggregation.Services;

public interface IStatisticsSevice
{
    Dictionary<string, Dictionary<string, ApiStatistics>> GetApiStatistics();
    void UpdateApiStatistics(string apiName, int elapsedMilliseconds);
}


public class ApiStatistics
{
    public double AverageResponseTime { get; set; }
    public long MinResponseTime { get; set; }
    public long MaxResponseTime { get; set; }
    public int TotalRequests { get; set; }
}

public class StatisticsSevice : IStatisticsSevice
{
    private readonly ConcurrentDictionary<string, List<long>> _requestTimes = new();
    
    public Dictionary<string, Dictionary<string, ApiStatistics>> GetApiStatistics()
    {
        var result = new Dictionary<string, Dictionary<string, ApiStatistics>>
        {
            ["Fast"] = [],
            ["Medium"] = [],
            ["Slow"] = []
        };
        
        foreach ((string? apiName, var times) in _requestTimes)
        {
            if (times.Count <= 0) continue;

            double averageTime = times.Average();
            long minTime = times.Min();
            long maxTime = times.Max();
            
            var stats = new ApiStatistics
            {
                TotalRequests = times.Count,
                AverageResponseTime = averageTime,
                MinResponseTime = minTime,
                MaxResponseTime = maxTime
            };

            string bucket = GetPerformanceBucket(averageTime);
            result[bucket].Add(apiName, stats);
        }
        
        return result;
    }
    
    private static string GetPerformanceBucket(double averageTime)
    {
        return averageTime switch
        {
            < 100 => "Fast",
            < 200 => "Medium",
            _ => "Slow"
        };
    }

    public void UpdateApiStatistics(string apiName, int elapsedMilliseconds)
    {
        var times = _requestTimes.GetOrAdd(apiName, _ => []);
        lock (times)
        {
            times.Add(elapsedMilliseconds);
        }
    }
}