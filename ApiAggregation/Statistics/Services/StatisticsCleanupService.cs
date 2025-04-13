using ApiAggregation.Configuration;
using ApiAggregation.Infrastructure.Abstractions;
using ApiAggregation.Statistics.Abstractions;
using ApiAggregation.Statistics.Models;
using Microsoft.Extensions.Options;

namespace ApiAggregation.Statistics.Services;

public class StatisticsCleanupService(
    ILogger<StatisticsCleanupService> logger,
    IStatisticsService statisticsService,
    IDateTimeProvider dateTimeProvider,
    IOptions<StatisticsCleanupOptions> options)
    : BackgroundService
{
    private readonly StatisticsCleanupOptions _options = options.Value;

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Statistics Cleanup Service is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                statisticsService.CleanupOldEntries(_options.RetentionPeriod);
                logger.LogInformation("Cleanup completed at {Time}", dateTimeProvider.UtcNow);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error during statistics cleanup.");
            }

            await Task.Delay(_options.CleanupInterval, stoppingToken);
        }

        logger.LogInformation("Statistics Cleanup Service is stopping.");
    }
}