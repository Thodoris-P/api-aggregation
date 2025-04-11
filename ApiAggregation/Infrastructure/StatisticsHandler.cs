using System.Diagnostics;
using ApiAggregation.Services;

namespace ApiAggregation.Infrastructure;

public class StatisticsHandler : DelegatingHandler
{
    private readonly IStatisticsSevice _statisticsService;

    public StatisticsHandler(IStatisticsSevice statisticsService)
    {
        _statisticsService = statisticsService;
    }
    
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        string requestUri = request.RequestUri?.Host ?? "unknown";
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();
            _statisticsService.UpdateApiStatistics(requestUri, (int)stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception e)
        {
            stopwatch.Stop();
            _statisticsService.UpdateApiStatistics(requestUri, (int)stopwatch.ElapsedMilliseconds);
            Console.WriteLine(e);
            throw;
        }
    }
}