using ApiAggregation.ExternalApis.Models;

namespace ApiAggregation.ExternalApis.Abstractions;

public interface IExternalApiClient
{
    string ApiName { get; }
    public ApiSettings Settings { get; }
    Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default);
}