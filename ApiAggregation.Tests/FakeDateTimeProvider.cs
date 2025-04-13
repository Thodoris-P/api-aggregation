using ApiAggregation.Statistics;

namespace ApiAggregation.UnitTests;

public class FakeDateTimeProvider : IDateTimeProvider
{
    public DateTime UtcNow { get; private set; }
    private readonly DateTime _initialTime;

    public FakeDateTimeProvider(DateTime initialTime)
    {
        UtcNow = initialTime;
        _initialTime = initialTime;
    }
    
    public void Advance(TimeSpan timeSpan)
    {
        UtcNow = UtcNow.Add(timeSpan);
    }
    
    public void ResetTime(DateTime newTime)
    {
        UtcNow = _initialTime;
    }
}