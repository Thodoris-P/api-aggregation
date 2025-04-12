using ApiAggregation.ExternalApis;

namespace ApiAggregation.Aggregation;

public interface IAggregatorService
{
    Task<AggregatedData> GetAggregatedDataAsync(IExternalApiFilter filterOptions);
}

public class AggregatorService(IEnumerable<IExternalApiClient> apiClients) : IAggregatorService
{
    public async Task<AggregatedData> GetAggregatedDataAsync(IExternalApiFilter filterOptions)
    {
        var tasks = apiClients.Select(api => api.GetDataAsync(filterOptions));
        var responses = await Task.WhenAll(tasks);
        var aggregatedData = new AggregatedData
        {
            ApiResponses = responses.ToDictionary(x => x.ApiName, x => x.Content),
        };
        return aggregatedData;
    }
}