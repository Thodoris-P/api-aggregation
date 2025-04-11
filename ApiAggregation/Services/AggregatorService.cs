using ApiAggregation.Services.Abstractions;

namespace ApiAggregation.Services;

public interface IAggregatorService
{
    Task<AggregatedData> GetAggregatedDataAsync(IExternalApiFilter filterOptions);
}

public class AggregatorService : IAggregatorService
{
    private readonly IEnumerable<IExternalApiClient> _apiClients;
    public AggregatorService(IEnumerable<IExternalApiClient> apiClients)
    {
        _apiClients = apiClients;
    }
    
    public async Task<AggregatedData> GetAggregatedDataAsync(IExternalApiFilter filterOptions)
    {
        var tasks = _apiClients.Select(api => api.GetDataAsync(filterOptions));
        var responses = await Task.WhenAll(tasks);
        var aggregatedData = new AggregatedData
        {
            RawResponses = responses.Select(x => x.Content).ToList(),
        };
        return aggregatedData;
    }
}