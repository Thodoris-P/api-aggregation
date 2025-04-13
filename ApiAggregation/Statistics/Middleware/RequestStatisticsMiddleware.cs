using System.Diagnostics;
using ApiAggregation.Configuration;
using ApiAggregation.Infrastructure.Attributes;
using ApiAggregation.Statistics.Abstractions;
using Microsoft.Extensions.Options;

namespace ApiAggregation.Statistics.Middleware;

public class RequestStatisticsMiddleware(
    RequestDelegate next,
    IStatisticsService statisticsService,
    IOptions<AggregatorSettings> aggregatorSettings)
{
    
    public async Task InvokeAsync(HttpContext context)
    {
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata.GetMetadata<TrackPerformanceAttribute>() is null)
        {
            await next(context);
            return;
        }
        
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await next(context);
            stopwatch.Stop();
            statisticsService.UpdateApiStatistics(aggregatorSettings.Value.AggregatorName, stopwatch.ElapsedMilliseconds);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            statisticsService.UpdateApiStatistics(aggregatorSettings.Value.AggregatorName, stopwatch.ElapsedMilliseconds);
            throw;
        }
    }
}