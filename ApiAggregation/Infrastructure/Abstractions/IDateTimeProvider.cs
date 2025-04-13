namespace ApiAggregation.Infrastructure.Abstractions;

public interface IDateTimeProvider
{
    DateTime UtcNow { get; }
}