using ApiAggregation.Infrastructure.Abstractions;

namespace ApiAggregation.Infrastructure.Providers;

public class SystemDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow => DateTime.UtcNow;
}