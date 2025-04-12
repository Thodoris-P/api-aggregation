namespace ApiAggregation.Aggregation;

public class AggregatedData
{
    public List<string> RawResponses { get; set; }
}

public class AggregatorSettings
{
    public string AggregatorName { get; set; } = string.Empty;
}