namespace ApiAggregation.Aggregation;

public class AggregatedData
{
    public Dictionary<string, string> ApiResponses { get; set; }
}

public class AggregatorSettings
{
    public string AggregatorName { get; set; }
}