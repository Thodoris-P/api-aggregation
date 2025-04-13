using ApiAggregation.ExternalApis.Abstractions;
using ApiAggregation.ExternalApis.Models;
using Microsoft.Extensions.Caching.Hybrid;

namespace ApiAggregation.ExternalApis.Decorators;

public class CachingExternalApiClientDecorator(
    IExternalApiClient innerClient,
    HybridCache hybridCache)
    : IExternalApiClient
{
    private readonly HybridCacheEntryOptions _cacheOptions = new()
    {
        Expiration = innerClient.Settings.CacheDuration, LocalCacheExpiration = innerClient.Settings.CacheDuration
    }; 
    public string ApiName => innerClient.ApiName;
    public ApiSettings Settings => innerClient.Settings;


    public async Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"{ApiName}_{filterOptions}";

        var cachedResponse = await hybridCache.GetOrCreateAsync<ApiResponse>(
            cacheKey,
            async _ => await innerClient.GetDataAsync(filterOptions, cancellationToken),
            _cacheOptions,
            cancellationToken: cancellationToken);

        // Do not cache failed / fallback responses.
        if (cachedResponse.IsFallback)
        {
            await hybridCache.RemoveAsync(cacheKey, cancellationToken);
        }

        return cachedResponse;
    }
}

