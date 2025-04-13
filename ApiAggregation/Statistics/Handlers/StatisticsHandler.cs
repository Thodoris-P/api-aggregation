using System.Diagnostics;
using ApiAggregation.Statistics.Abstractions;

namespace ApiAggregation.Statistics.Handlers;

public class StatisticsHandler(IStatisticsService statisticsService) : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var stopwatch = Stopwatch.StartNew();
        string requestUri = request.RequestUri?.Host ?? "unknown";
        try
        {
            var response = await base.SendAsync(request, cancellationToken);
            stopwatch.Stop();
            statisticsService.UpdateApiStatistics(requestUri, stopwatch.ElapsedMilliseconds);
            return response;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            statisticsService.UpdateApiStatistics(requestUri, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}