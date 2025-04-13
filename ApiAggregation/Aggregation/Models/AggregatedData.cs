using System.Text.Json;

namespace ApiAggregation.Aggregation.Models;

public class AggregatedData
{
    public required Dictionary<string, JsonElement> ApiResponses { get; init; }
}