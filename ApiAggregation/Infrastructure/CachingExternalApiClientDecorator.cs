using ApiAggregation.Services.Abstractions;
using Microsoft.Extensions.Caching.Hybrid;

namespace ApiAggregation.Infrastructure;

public class CachingExternalApiClientDecorator : IExternalApiClient
{
    private readonly IExternalApiClient _innerClient;
    private readonly HybridCache _hybridCache;
    private readonly HybridCacheEntryOptions _cacheOptions; 
    public string ApiName => _innerClient.ApiName;
    
    public CachingExternalApiClientDecorator(
        IExternalApiClient innerClient, 
        HybridCache hybridCache, 
        TimeSpan cacheDuration)
    {
        _innerClient = innerClient;
        _hybridCache = hybridCache;
        _cacheOptions = new HybridCacheEntryOptions { Expiration = cacheDuration, LocalCacheExpiration = cacheDuration };
    }


     public async Task<ApiResponse> GetDataAsync(IExternalApiFilter filterOptions, CancellationToken cancellationToken = default)
    {
        string cacheKey = $"{ApiName}_{filterOptions}";

        var cachedResponse = await _hybridCache.GetOrCreateAsync<ApiResponse>(
            cacheKey,
            async _ => await _innerClient.GetDataAsync(filterOptions, cancellationToken),
            _cacheOptions,
            cancellationToken: cancellationToken);

        // Do not cache failed / fallback responses.
        if (cachedResponse.IsFallback)
        {
            await _hybridCache.RemoveAsync(cacheKey, cancellationToken);
        }

        return cachedResponse;
    }
}

