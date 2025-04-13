using ApiAggregation.Aggregation.Models;
using ApiAggregation.Statistics.Abstractions;
using ApiAggregation.Statistics.Models;
using Microsoft.Extensions.Options;

namespace ApiAggregation.Statistics.Services;

public class PerformanceMonitoringService(
    ILogger<PerformanceMonitoringService> logger,
    IStatisticsService statisticsService,
    IOptions<PerformanceMonitoringOptions> options,
    IOptions<AggregatorSettings> aggregatorSettings)
    : BackgroundService
{
    private readonly PerformanceMonitoringOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Performance Monitoring Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            var windowStart = DateTime.UtcNow.Subtract(_options.AnalysisPeriod);
            try
            {
                // Retrieve aggregator statistics.
                double aggregatorAvg = GetAggregatorAveragePerformanceSince(windowStart);

                if (aggregatorAvg <= 0)
                {
                    logger.LogWarning("Aggregator performance data is insufficient to compare. Skipping analysis.");
                }
                else
                {
                    PerformPerformanceAnalysis(windowStart, aggregatorAvg);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during performance analysis.");
            }

            // Wait for the next check interval based on the configured options.
            await Task.Delay(_options.CheckInterval, stoppingToken);
        }

        logger.LogInformation("Performance Monitoring Service is stopping.");
    }

    private void PerformPerformanceAnalysis(DateTime windowStart, double aggregatorAvg)
    {
        var externalApiNames = GetExternalApiNames();

        foreach (string externalApi in externalApiNames)
        {
            var externalRecords = statisticsService.GetApiPerformanceRecords(externalApi, windowStart);
            if (externalRecords.Count == 0)
            {
                logger.LogInformation("No recent data for external API {ExternalApi}", externalApi);
                continue;
            }

            double externalAvg = externalRecords.Average(r => r.ResponseTimeInMilliseconds);

            logger.LogInformation("Analysis for {ExternalApi}: Aggregator Avg = {AggregatorAvg} ms, External Avg = {ExternalAvg} ms",
                externalApi, aggregatorAvg, externalAvg);

            // Log anomaly if the average performance of an external API over the last 5 minutes is over 50% bigger than the average performance of the API
            if (externalAvg > aggregatorAvg * 1.5)
            {
                logger.LogWarning(
                    "Performance anomaly for {ExternalApi}: External API average ({ExternalAvg} ms) exceeds Aggregator's average ({AggregatorAvg} ms) by over 50%.",
                    externalApi, externalAvg, aggregatorAvg);
            }
        }
    }

    private IEnumerable<string> GetExternalApiNames()
    {
        var allApiNames = statisticsService.GetAllApiNames();
        var externalNames = allApiNames
            .Where(name => !name.Equals(aggregatorSettings.Value.AggregatorName, StringComparison.OrdinalIgnoreCase));
        return externalNames;
    }

    private double GetAggregatorAveragePerformanceSince(DateTime windowStart)
    {
        var aggregatorRecords = statisticsService.GetApiPerformanceRecords(aggregatorSettings.Value.AggregatorName, windowStart);
        double aggregatorAvg = aggregatorRecords.Count != 0
            ? aggregatorRecords.Average(r => r.ResponseTimeInMilliseconds)
            : 0;
        return aggregatorAvg;
    }
}