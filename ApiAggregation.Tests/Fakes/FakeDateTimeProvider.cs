using ApiAggregation.Infrastructure.Abstractions;
using ApiAggregation.Infrastructure.Providers;
using ApiAggregation.Statistics;

namespace ApiAggregation.UnitTests.Fakes;

public class FakeDateTimeProvider(DateTime initialTime) : IDateTimeProvider
{
    public DateTime UtcNow { get; private set; } = initialTime;
    private readonly DateTime _initialTime = initialTime;

    public void Advance(TimeSpan timeSpan)
    {
        UtcNow = UtcNow.Add(timeSpan);
    }
    
    public void ResetTime(DateTime newTime)
    {
        UtcNow = _initialTime;
    }
}