using System.Collections.Concurrent;

namespace ApiAggregation.Statistics;

public interface IStatisticsSevice
{
    Dictionary<string, Dictionary<string, ApiStatistics>> GetApiStatistics();
    void UpdateApiStatistics(string apiName, long elapsedMilliseconds);
}


public class ApiStatistics
{
    public double AverageResponseTime { get; set; }
    public long MinResponseTime { get; set; }
    public long MaxResponseTime { get; set; }
    public int TotalRequests { get; set; }
}

// In a real world scenario, we would use a more sophisticated approach
// because now we are storing data indefinitely which will eventually be bad for memory usage
// We could use a sliding window or a fixed size buffer to limit the number of stored times,
// but .NET doesn't provide such a structure out of the box
// and implementing one would be out of scope for this assignment
public class StatisticsSevice : IStatisticsSevice
{
    private readonly ConcurrentDictionary<string, ConcurrentBag<long>> _requestTimes = new();
    
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
            if (times.IsEmpty) continue;
            
            // Utilize the snapshot pattern to avoid getting mixed results
            long[] snapshot = times.ToArray();
            
            double averageTime = snapshot.Average();
            long minTime = snapshot.Min();
            long maxTime = snapshot.Max();
            
            var stats = new ApiStatistics
            {
                TotalRequests = snapshot.Length,
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

    public void UpdateApiStatistics(string apiName, long elapsedMilliseconds)
    {
        // Store the request time in a thread-safe manner
        // only store minimum information (elapsed time) to avoid complex computations and locking for long times
        var bag = _requestTimes.GetOrAdd(apiName, _ => []);
        bag.Add(elapsedMilliseconds);
    }
}