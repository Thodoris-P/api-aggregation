using System.Text.Json;
using ApiAggregation.Aggregation.Abstractions;
using ApiAggregation.Aggregation.Models;
using ApiAggregation.ExternalApis.Abstractions;

namespace ApiAggregation.Aggregation.Services;

public class AggregatorService(IEnumerable<IExternalApiClient> apiClients) : IAggregatorService
{
    public async Task<AggregatedData> GetAggregatedDataAsync(IExternalApiFilter filterOptions)
    {
        var tasks = apiClients.Select(api => api.GetDataAsync(filterOptions));
        var responses = await Task.WhenAll(tasks);
        var aggregatedData = new AggregatedData
        {
            ApiResponses = responses.ToDictionary(
                x => x.ApiName, 
                x => JsonSerializer.Deserialize<JsonElement>(x.Content)
            ),
        };
        return aggregatedData;
    }
}