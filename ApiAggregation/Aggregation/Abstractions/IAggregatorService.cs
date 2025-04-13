using ApiAggregation.Aggregation.Models;
using ApiAggregation.ExternalApis.Abstractions;

namespace ApiAggregation.Aggregation.Abstractions;

public interface IAggregatorService
{
    Task<AggregatedData> GetAggregatedDataAsync(IExternalApiFilter filterOptions);
}