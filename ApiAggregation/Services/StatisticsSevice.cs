using System.Collections.Concurrent;

namespace ApiAggregation.Services;

public interface IStatisticsSevice
{
    Dictionary<string, ApiStatistics> GetApiStatistics();
    void UpdateApiStatistics(string apiName, int elapsedMilliseconds);
}

public class ApiStatistics
{
    public double AverageResponseTime { get; set; }
    public long MinResponseTime { get; set; }
    public long MaxResponseTime { get; set; }
}

public class StatisticsSevice : IStatisticsSevice
{
    private readonly ConcurrentDictionary<string, List<long>> _requestTimes = new();
    
    public Dictionary<string, ApiStatistics> GetApiStatistics()
    {
        var result = new Dictionary<string, ApiStatistics>();
        
        foreach ((string? apiName, var times) in _requestTimes)
        {
            if (times.Count <= 0) continue;
            
            double averageTime = times.Average();
            long minTime = times.Min();
            long maxTime = times.Max();

            result[apiName] = new ApiStatistics
            {
                AverageResponseTime = averageTime,
                MinResponseTime = minTime,
                MaxResponseTime = maxTime
            };
        }
        
        return result;
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